using System.Text.RegularExpressions;
using FluentResults;

namespace Devices.Domain.Devices;

/// <summary>
/// A device in the registry (jarvis-satellites.md 2a): a person's computer, or a household satellite (S-1, 3a) that a
/// household admin adds and the device then pairs with a one-time code. The device announces its actions on every
/// connection and they replace the old ones; "Nie pytaj" stays for an action that keeps its name. Removing only
/// disables it (data kept); it can connect again only after it is added back.
/// </summary>
public sealed partial class Device
{
    public const int MaxNameLength = 20;
    public const int MaxActions = 50;

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

    /// <summary>Unique per owner (per household for a household device), e.g. <c>mac-biuro</c>; part of the tool names JARVIS sees.</summary>
    public string Name { get; private set; } = string.Empty;

    public DeviceKind Kind { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset LastSeenAt { get; private set; }

    public DateTimeOffset? DisabledAt { get; private set; }

    public bool IsDisabled => DisabledAt is not null;

    public bool IsHouseholdDevice => HouseholdId is not null;

    public IReadOnlyList<DeviceAction> Actions => _actions;

    public static Result<Device> Create(Guid ownerId, string name, DeviceKind kind, DateTimeOffset now)
    {
        if (ownerId == Guid.Empty)
        {
            throw new ArgumentException("A device needs an owner.", nameof(ownerId));
        }

        name = name?.Trim() ?? string.Empty;
        if (!NamePattern().IsMatch(name))
        {
            return Result.Fail(DeviceErrors.InvalidName);
        }

        return new Device { Id = Guid.CreateVersion7(), OwnerId = ownerId, Name = name, Kind = kind, CreatedAt = now, LastSeenAt = now };
    }

    /// <summary>A household admin adds a satellite; it can connect once it is paired with <paramref name="pairingCodeHash"/>'s code.</summary>
    public static Result<Device> AddToHousehold(Guid householdId, Guid addedBy, string name, string pairingCodeHash, DateTimeOffset now)
    {
        if (householdId == Guid.Empty)
        {
            throw new ArgumentException("A household device needs a household.", nameof(householdId));
        }

        var device = Create(addedBy, name, DeviceKind.Satellite, now);
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

    public void Disable(DateTimeOffset now) => DisabledAt ??= now;

    /// <summary>Added back after removing: it may connect again.</summary>
    public void Enable() => DisabledAt = null;

    public DeviceAction? Find(string action) => _actions.Find(a => string.Equals(a.Name, action, StringComparison.Ordinal));

    /// <summary>The tool name JARVIS sees without the gateway's prefix: <c>mac-biuro_say</c>.</summary>
    public string ToolName(DeviceAction action)
    {
        ArgumentNullException.ThrowIfNull(action);
        return $"{Name}_{action.Name}";
    }

    [GeneratedRegex("^[a-z0-9][a-z0-9-]{0,19}$")]
    private static partial Regex NamePattern();
}
