using FluentResults;

namespace Notifications.Domain.Recipients;

/// <summary>
/// One person as Notifications knows them: the devices they turned notifications on for and their settings. The id is
/// the user id. Created on the first device or setting; a person who never did either gets nothing (no devices).
/// </summary>
public sealed class Recipient
{
    /// <summary>Old browsers pile up; the least recently used device goes when a new one comes over this limit.</summary>
    public const int MaxDevices = 10;

    private readonly List<PushDevice> _devices = [];
    private readonly List<Mute> _mutes = [];

    private Recipient() { }

    private Recipient(Guid userId) => UserId = userId;

    public Guid UserId { get; private set; }

    /// <summary>N4: show what the notification says on a locked screen; off means "a new message" without content.</summary>
    public bool ShowPreview { get; private set; } = true;

    /// <summary>N3: nothing is delivered in these hours; <c>null</c> for none.</summary>
    public QuietHours? QuietHours { get; private set; }

    public IReadOnlyList<PushDevice> Devices => _devices;

    public IReadOnlyList<Mute> Mutes => _mutes;

    public static Recipient Create(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("A recipient needs a user id.", nameof(userId));
        }

        return new Recipient(userId);
    }

    /// <summary>
    /// The browser subscribed (again). The same endpoint keeps its device and gets the new keys; a new one is added,
    /// pushing out the least recently used device above <see cref="MaxDevices"/>.
    /// </summary>
    public PushDevice RegisterDevice(PushTarget target, string? userAgent, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(target);
        var known = FindDevice(target.Endpoint);
        if (known is not null)
        {
            known.Renew(target, userAgent, now);
            return known;
        }

        var device = PushDevice.Create(target, userAgent, now);
        _devices.Add(device);
        while (_devices.Count > MaxDevices)
        {
            _devices.Remove(_devices.OrderBy(d => d.LastUsedAt ?? d.RegisteredAt).First());
        }

        return device;
    }

    /// <summary>Removed from the list of devices in the settings.</summary>
    public Result RemoveDevice(Guid deviceId)
    {
        var device = _devices.Find(d => d.Id == deviceId);
        if (device is null)
        {
            return Result.Fail(RecipientErrors.DeviceNotFound);
        }

        _devices.Remove(device);
        return Result.Ok();
    }

    /// <summary>The browser unsubscribed, or the endpoint moved to another person signed in on the same browser.</summary>
    public bool ForgetEndpoint(string endpoint)
    {
        var device = FindDevice(endpoint);
        return device is not null && _devices.Remove(device);
    }

    /// <summary>The push service answered that the subscription is gone (404/410): the browser will never get it.</summary>
    public void DeviceGone(Guid deviceId) => _devices.RemoveAll(d => d.Id == deviceId);

    public void DeviceReached(Guid deviceId, DateTimeOffset now) => _devices.Find(d => d.Id == deviceId)?.MarkUsed(now);

    /// <summary>The account was disabled: nothing should reach it until the person registers a device again.</summary>
    public void ForgetDevices() => _devices.Clear();

    public void ChangeSettings(bool showPreview, QuietHours? quietHours)
    {
        ShowPreview = showPreview;
        QuietHours = quietHours;
    }

    /// <summary>Mutes a household, or with <paramref name="source"/> one plugin or service in it. Muting twice changes nothing.</summary>
    public Result Mute(Guid householdId, string? source)
    {
        var mute = Recipients.Mute.Create(householdId, source);
        if (mute.IsFailed)
        {
            return mute.ToResult();
        }

        if (!_mutes.Contains(mute.Value))
        {
            _mutes.Add(mute.Value);
        }

        return Result.Ok();
    }

    public void Unmute(Guid householdId, string? source) =>
        _mutes.RemoveAll(m => m.HouseholdId == householdId && string.Equals(m.Source, Normalize(source), StringComparison.Ordinal));

    /// <summary>Whether a notification from <paramref name="source"/> about <paramref name="householdId"/> should go out now.</summary>
    public DeliveryDecision Decide(Guid? householdId, string source, DateTimeOffset now)
    {
        if (_devices.Count == 0)
        {
            return DeliveryDecision.NoDevices;
        }

        if (householdId is { } household && _mutes.Any(m => m.HouseholdId == household && (m.Source is null || m.Source == source)))
        {
            return DeliveryDecision.Muted;
        }

        if (QuietHours is not null && QuietHours.Covers(now))
        {
            return DeliveryDecision.QuietHours;
        }

        return DeliveryDecision.Deliver;
    }

    private PushDevice? FindDevice(string endpoint) =>
        _devices.Find(d => string.Equals(d.Endpoint, endpoint, StringComparison.Ordinal));

    private static string? Normalize(string? source) => string.IsNullOrWhiteSpace(source) ? null : source.Trim();
}
