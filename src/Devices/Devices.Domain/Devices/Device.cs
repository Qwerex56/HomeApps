using Devices.Domain.Capabilities;
using Devices.Domain.Descriptors;
using FluentResults;

namespace Devices.Domain.Devices;

/// <summary>
/// A device in the registry (jarvis-satellites.md 2a): a person's computer, or a household satellite (S-1, 3a) that a
/// household admin adds and the device then pairs with a one-time code. The device announces its actions on every
/// connection and they replace the old ones; "Nie pytaj" stays for an action that keeps its name. Removing only
/// disables it (data kept); it can connect again only after it is added back.
/// </summary>
public sealed class Device
{
    public const int MaxActions = 50;
    public const int MaxNotesLength = 500;

    /// <summary>How long a pairing code works.</summary>
    public static readonly TimeSpan PairingValidity = TimeSpan.FromMinutes(10);

    private readonly List<DeviceAction> _actions = [];

    private Device() { }

    public Guid Id { get; private set; }

    /// <summary>The person the device belongs to; for a household device the admin who added it.</summary>
    public Guid OwnerId { get; private set; }

    /// <summary>Set for a household satellite: it belongs to the household, not to <see cref="OwnerId"/>.</summary>
    public Guid? HouseholdId { get; private set; }

    /// <summary>SHA-256 of the current one-time pairing code, until it is used or replaced.</summary>
    public string? PairingCodeHash { get; private set; }

    public DateTimeOffset? PairingExpiresAt { get; private set; }

    public DateTimeOffset? PairedAt { get; private set; }

    /// <summary>The display name: any text of up to 60 characters, unique (ignoring case) per owner, per household for a household device. Can be changed.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>The stable key made from the name when the device was added, e.g. <c>mac-biuro</c>; part of the tool names JARVIS sees.</summary>
    public string Slug { get; private set; } = string.Empty;

    public DeviceKind Kind { get; private set; }

    /// <summary>What the device is mainly for; set by the descriptor it sends, otherwise from <see cref="Kind"/>.</summary>
    public DevicePurpose Purpose { get; private set; }

    /// <summary>What a household admin tells JARVIS about this device ("to fotel taty, pytaj go").</summary>
    public string? AdminNotes { get; private set; }

    /// <summary>D6: JARVIS may control this household device (<c>_set</c>, announced actions); off, JARVIS neither sees nor can call them. People are not affected.</summary>
    public bool JarvisMayControl { get; private set; } = true;

    /// <summary>D6: JARVIS asks for a "tak" before controlling this household device; off is "Nie pytaj" for the whole device.</summary>
    public bool JarvisAsks { get; private set; } = true;

    /// <summary>The <c>homeapps.device/1</c> document as the device sent it, or <c>null</c> for one that sent none (an old bridge).</summary>
    public string? Descriptor { get; private set; }

    public string? Model { get; private set; }

    public string? DescriptorVersion { get; private set; }

    public string[] Roles { get; private set; } = [];

    /// <summary>The capabilities that counted: <c>weather</c>, <c>switch:lampka</c>.</summary>
    public string[] Capabilities { get; private set; } = [];

    /// <summary>Why something in the descriptor was ignored; shown with the device.</summary>
    public string[] Warnings { get; private set; } = [];

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset LastSeenAt { get; private set; }

    public DateTimeOffset? DisabledAt { get; private set; }

    public bool IsDisabled => DisabledAt is not null;

    public bool IsHouseholdDevice => HouseholdId is not null;

    /// <summary>S13: a household satellite with its own identity in Auth and a Member row in Households.</summary>
    public bool IsPairedSatellite => IsHouseholdDevice && PairedAt is not null;

    public IReadOnlyList<DeviceAction> Actions => _actions;

    /// <param name="takenSlugs">The slugs already used in the same scope (the person's devices, or the household's), for the number after a collision.</param>
    public static Result<Device> Create(Guid ownerId, string name, DeviceKind kind, IReadOnlyCollection<string> takenSlugs, DateTimeOffset now)
    {
        if (ownerId == Guid.Empty)
        {
            throw new ArgumentException("A device needs an owner.", nameof(ownerId));
        }

        var clean = DeviceName.Normalize(name);
        if (clean.IsFailed)
        {
            return clean.ToResult();
        }

        return new Device
        {
            Id = Guid.CreateVersion7(),
            OwnerId = ownerId,
            Name = clean.Value,
            Slug = DeviceSlug.From(clean.Value, takenSlugs),
            Kind = kind,
            Purpose = DevicePurposes.FromKind(kind),
            CreatedAt = now,
            LastSeenAt = now,
        };
    }

    /// <summary>A household admin adds a satellite; it can connect once it is paired with <paramref name="pairingCodeHash"/>'s code.</summary>
    public static Result<Device> AddToHousehold(Guid householdId, Guid addedBy, string name, string pairingCodeHash, IReadOnlyCollection<string> takenSlugs, DateTimeOffset now)
    {
        if (householdId == Guid.Empty)
        {
            throw new ArgumentException("A household device needs a household.", nameof(householdId));
        }

        var device = Create(addedBy, name, DeviceKind.Satellite, takenSlugs, now);
        if (device.IsSuccess)
        {
            device.Value.HouseholdId = householdId;
            device.Value.NewPairingCode(pairingCodeHash, now);
        }

        return device;
    }

    /// <summary>A new code, e.g. for new hardware: the old pairing ends and the device has to pair again.</summary>
    public void NewPairingCode(string pairingCodeHash, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrEmpty(pairingCodeHash);
        PairingCodeHash = pairingCodeHash;
        PairingExpiresAt = now + PairingValidity;
        PairedAt = null;
    }

    /// <summary>A household admin renames the device; the slug stays. Uniqueness is the caller's to check.</summary>
    public Result Rename(string name)
    {
        var clean = DeviceName.Normalize(name);
        if (clean.IsFailed)
        {
            return clean.ToResult();
        }

        Name = clean.Value;
        return Result.Ok();
    }

    /// <summary>A household admin sets whether JARVIS may control the device and whether it asks first (D6).</summary>
    public void SetJarvisAccess(bool mayControl, bool asks)
    {
        JarvisMayControl = mayControl;
        JarvisAsks = asks;
    }

    /// <summary>A household admin's notes for JARVIS; empty clears them.</summary>
    public Result SetAdminNotes(string? notes)
    {
        notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        if (notes is { Length: > MaxNotesLength })
        {
            return Result.Fail(DeviceErrors.InvalidNotes);
        }

        AdminNotes = notes;
        return Result.Ok();
    }

    /// <summary>
    /// The device sent its descriptor (at pairing or on a connection): it replaces the stored one, and its purpose
    /// becomes the device's. A household device is an assistant or a smart_home device, a person's own one a computer.
    /// </summary>
    public Result ApplyDescriptor(DeviceDescriptor descriptor, CapabilityResolution resolution)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(resolution);
        if (IsHouseholdDevice == (descriptor.Purpose == DevicePurpose.Computer))
        {
            return Result.Fail(DeviceErrors.WrongPurpose);
        }

        Purpose = descriptor.Purpose;
        Descriptor = descriptor.Json;
        Model = descriptor.Model;
        DescriptorVersion = descriptor.Version;
        Roles = [.. resolution.Roles];
        Capabilities = [.. resolution.Accepted];
        Warnings = [.. resolution.Warnings];
        return Result.Ok();
    }

    /// <summary>The device used its code: the code is spent and the device may connect.</summary>
    public Result Pair(DateTimeOffset now)
    {
        if (IsDisabled)
        {
            return Result.Fail(DeviceErrors.Disabled);
        }

        if (PairingCodeHash is null || PairingExpiresAt <= now)
        {
            return Result.Fail(DeviceErrors.PairingExpired);
        }

        PairingCodeHash = null;
        PairingExpiresAt = null;
        PairedAt = now;
        return Result.Ok();
    }

    /// <summary>The device connected and announced what it can do now.</summary>
    public Result Connect(IReadOnlyList<DeviceAction> announced, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(announced);
        if (IsDisabled)
        {
            return Result.Fail(DeviceErrors.Disabled);
        }

        if (IsHouseholdDevice && PairedAt is null)
        {
            return Result.Fail(DeviceErrors.NotPaired);
        }

        if (announced.Count > MaxActions || announced.Select(a => a.Name).Distinct(StringComparer.Ordinal).Count() != announced.Count)
        {
            return Result.Fail(DeviceErrors.TooManyActions);
        }

        foreach (var action in announced)
        {
            action.AllowInAdvance(Find(action.Name)?.AutoApprove ?? false);
        }

        _actions.Clear();
        _actions.AddRange(announced);
        LastSeenAt = now;
        return Result.Ok();
    }

    public void Seen(DateTimeOffset now) => LastSeenAt = now;

    /// <summary>"Nie pytaj" on or off for one action (S6).</summary>
    public Result AllowInAdvance(string action, bool allowed)
    {
        var found = Find(action);
        if (found is null)
        {
            return Result.Fail(DeviceErrors.ActionNotFound);
        }

        found.AllowInAdvance(allowed);
        return Result.Ok();
    }

    /// <summary>
    /// Removed. A household satellite also loses its pairing (S13: it leaves the household), so restoring it needs
    /// a new code.
    /// </summary>
    public void Disable(DateTimeOffset now)
    {
        DisabledAt ??= now;
        if (IsHouseholdDevice)
        {
            PairingCodeHash = null;
            PairingExpiresAt = null;
            PairedAt = null;
        }
    }

    /// <summary>Added back after removing: it may connect again.</summary>
    public void Enable() => DisabledAt = null;

    public DeviceAction? Find(string action) => _actions.Find(a => string.Equals(a.Name, action, StringComparison.Ordinal));

    /// <summary>The tool name JARVIS sees without the gateway's prefix: <c>mac-biuro_say</c>.</summary>
    public string ToolName(DeviceAction action)
    {
        ArgumentNullException.ThrowIfNull(action);
        return $"{Slug}_{action.Name}";
    }
}
