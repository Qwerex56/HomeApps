using FluentResults;
using HomeMix.Domain.Packages;
using HomeMix.Domain.Manifests;

namespace HomeMix.Domain.Installations;

/// <summary>
/// An add-on in one household or on one account (docs/homemix.md, "Installing"). It grants exactly what the package
/// asks for and the installer accepted; an update that asks for more needs a new consent. Removing keeps the row (and
/// later the data) for 30 days, so it is not deleted here.
/// </summary>
public sealed class Installation
{
    private List<string> _capabilities = [];
    private List<string> _network = [];
    private List<WidgetChoice> _widgets = [];
    private List<DevicePick> _devicePicks = [];

    /// <summary>The quick links bar holds at most this many add-ons (UI 2.4.1, P5).</summary>
    public const int MaxPins = 8;

    private Installation() { }

    public Guid Id { get; private set; }

    public string AddonId { get; private set; } = string.Empty;

    public string PackageSha256 { get; private set; } = string.Empty;

    public string Version { get; private set; } = string.Empty;

    public TargetKind TargetKind { get; private set; }

    /// <summary>The household id, or the user id for an account.</summary>
    public Guid TargetId { get; private set; }

    public IReadOnlyList<string> Capabilities => _capabilities;

    public IReadOnlyList<string> Network => _network;

    /// <summary>The widget chosen for each place that has a choice (<see cref="WidgetChoice"/>).</summary>
    public IReadOnlyList<WidgetChoice> Widgets => _widgets;

    /// <summary>The device chosen for each widget that matches devices (<see cref="DevicePick"/>); a widget without one shows the best match.</summary>
    public IReadOnlyList<DevicePick> DevicePicks => _devicePicks;

    /// <summary>The add-on offers JARVIS something and the installer agreed.</summary>
    public bool JarvisAllowed { get; private set; }

    public Guid InstalledBy { get; private set; }

    public DateTimeOffset InstalledAt { get; private set; }

    public Guid? UpdatedBy { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public Guid? RemovedBy { get; private set; }

    public DateTimeOffset? RemovedAt { get; private set; }

    /// <summary>Where an account's add-on stands on its owner's quick links bar (0 first), or null when not pinned (UI 2.4.1).</summary>
    public int? Pin { get; private set; }

    public bool IsActive => RemovedAt is null;

    public InstallTarget Target => new(TargetKind, TargetId);

    public static Result<Installation> Install(AddonPackage package, InstallTarget target, Consent consent, Guid by, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(package);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(consent);

        var manifest = package.Manifest;
        if (!manifest.Targets.Contains(target.Kind))
        {
            return Result.Fail(InstallationErrors.TargetNotSupported(target.Kind));
        }

        var missing = Missing(manifest.Capabilities, manifest.Network, consent);
        if (missing.Count > 0)
        {
            return Result.Fail(InstallationErrors.ConsentRequired(missing));
        }

        return new Installation
        {
            Id = Guid.CreateVersion7(),
            AddonId = package.AddonId,
            PackageSha256 = package.Sha256,
            Version = package.Version,
            TargetKind = target.Kind,
            TargetId = target.Id,
            _capabilities = [.. manifest.Capabilities],
            _network = [.. manifest.Network],
            JarvisAllowed = manifest.Jarvis && consent.Jarvis,
            InstalledBy = by,
            InstalledAt = now,
        };
    }

    /// <summary>
    /// Moves to another version of the same add-on. Asking for no more than was granted applies at once; asking for
    /// more needs <paramref name="consent"/> covering it. Without a consent, JARVIS keeps its earlier answer.
    /// </summary>
    public Result Update(AddonPackage package, Consent? consent, Guid by, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(package);
        if (!IsActive)
        {
            return Result.Fail(InstallationErrors.NotFound);
        }

        if (!string.Equals(package.AddonId, AddonId, StringComparison.Ordinal))
        {
            return Result.Fail(InstallationErrors.OtherAddon);
        }

        var manifest = package.Manifest;
        if (!manifest.Targets.Contains(TargetKind))
        {
            return Result.Fail(InstallationErrors.TargetNotSupported(TargetKind));
        }

        var asksMore = manifest.Capabilities.Except(_capabilities, StringComparer.Ordinal)
            .Concat(manifest.Network.Except(_network, StringComparer.OrdinalIgnoreCase))
            .ToList();
        if (asksMore.Count > 0)
        {
            var missing = consent is null ? asksMore : Missing(manifest.Capabilities, manifest.Network, consent);
            if (missing.Count > 0)
            {
                return Result.Fail(InstallationErrors.ConsentRequired(missing));
            }
        }

        PackageSha256 = package.Sha256;
        Version = package.Version;
        _widgets = [.. _widgets.Select(c => Fit(c, manifest)).OfType<WidgetChoice>()];
        _devicePicks = [.. _devicePicks.Where(p => manifest.Widgets.Any(w => w.Id == p.WidgetId && w.Devices.Count > 0))];
        _capabilities = [.. manifest.Capabilities];
        _network = [.. manifest.Network];
        JarvisAllowed = manifest.Jarvis && (consent?.Jarvis ?? JarvisAllowed);
        UpdatedBy = by;
        UpdatedAt = now;
        return Result.Ok();
    }

    /// <summary>
    /// Chooses which of the add-on's widgets stands in <paramref name="place"/>, in which size and order; a
    /// <paramref name="widgetId"/> of <c>null</c> shows none there. The size defaults to the widget's own.
    /// </summary>
    public Result ChooseWidget(AddonManifest manifest, string place, string? widgetId, string? size, int order)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        if (!IsActive)
        {
            return Result.Fail(InstallationErrors.NotFound);
        }

        if (!WidgetPlaces.For(TargetKind).Contains(place))
        {
            return Result.Fail(InstallationErrors.WidgetPlace(place));
        }

        string? chosenSize = null;
        if (widgetId is not null)
        {
            var widget = manifest.Widgets.FirstOrDefault(w => w.Id == widgetId && w.Places.Contains(place));
            if (widget is null)
            {
                return Result.Fail(InstallationErrors.WidgetUnknown(widgetId, place));
            }

            chosenSize = size ?? widget.Size;
            if (!widget.Sizes.Contains(chosenSize))
            {
                return Result.Fail(InstallationErrors.WidgetSize(chosenSize, widget.Sizes));
            }
        }

        _widgets = [.. _widgets.Where(c => c.Place != place), new WidgetChoice(place, widgetId, chosenSize, order)];
        return Result.Ok();
    }

    /// <summary>
    /// Chooses the device a widget shows (<paramref name="deviceId"/>), or goes back to the best match with <c>null</c>.
    /// The caller checks that the device is one of the widget's matches.
    /// </summary>
    public Result PickDevice(AddonManifest manifest, string widgetId, Guid? deviceId)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        if (!IsActive)
        {
            return Result.Fail(InstallationErrors.NotFound);
        }

        if (!manifest.Widgets.Any(w => w.Id == widgetId && w.Devices.Count > 0))
        {
            return Result.Fail(InstallationErrors.WidgetWithoutDevices(widgetId));
        }

        _devicePicks = [.. _devicePicks.Where(p => p.WidgetId != widgetId)];
        if (deviceId is { } id)
        {
            _devicePicks.Add(new DevicePick(widgetId, id));
        }

        return Result.Ok();
    }

    /// <summary>Back to the default for <paramref name="place"/>: every widget that lists it, in its own size.</summary>
    public Result ResetWidget(string place)
    {
        if (!IsActive)
        {
            return Result.Fail(InstallationErrors.NotFound);
        }

        _widgets = [.. _widgets.Where(c => c.Place != place)];
        return Result.Ok();
    }

    /// <summary>Puts an account's add-on on its owner's quick links bar at <paramref name="position"/>, or takes it off with null.</summary>
    public Result PinAt(int? position)
    {
        if (!IsActive)
        {
            return Result.Fail(InstallationErrors.NotFound);
        }

        if (TargetKind != TargetKind.User)
        {
            return Result.Fail(InstallationErrors.PinOnlyMine);
        }

        if (position is < 0 or >= MaxPins)
        {
            return Result.Fail(InstallationErrors.TooManyPins);
        }

        Pin = position;
        return Result.Ok();
    }

    public Result Remove(Guid by, DateTimeOffset now)
    {
        if (!IsActive)
        {
            return Result.Fail(InstallationErrors.NotFound);
        }

        RemovedBy = by;
        RemovedAt = now;
        return Result.Ok();
    }

    /// <summary>A choice after an update: gone with its widget, or back to the widget's own size if the size went.</summary>
    private static WidgetChoice? Fit(WidgetChoice choice, AddonManifest manifest)
    {
        if (choice.WidgetId is null)
        {
            return choice;
        }

        var widget = manifest.Widgets.FirstOrDefault(w => w.Id == choice.WidgetId && w.Places.Contains(choice.Place));
        return widget is null ? null : choice with { Size = widget.Sizes.Contains(choice.Size ?? widget.Size) ? choice.Size : widget.Size };
    }

    private static List<string> Missing(IReadOnlyList<string> capabilities, IReadOnlyList<string> network, Consent consent) =>
        capabilities.Except(consent.Capabilities, StringComparer.Ordinal)
            .Concat(network.Except(consent.Network, StringComparer.OrdinalIgnoreCase))
            .ToList();
}
