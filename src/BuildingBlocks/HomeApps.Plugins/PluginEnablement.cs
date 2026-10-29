using HomeApps.BuildingBlocks.Domain;

namespace HomeApps.Plugins;

/// <summary>
/// The plugin is on or off in one household. Kept by every plugin in its own database (jarvis-hub.md 9b.1): Households
/// knows nothing about plugins. Switching raises <see cref="PluginSwitchedOn"/> or <see cref="PluginSwitchedOff"/>,
/// so the plugin can react in the same transaction, e.g. create a household's first list.
/// </summary>
public sealed class PluginEnablement : IHasDomainEvents
{
    private readonly List<IDomainEvent> _domainEvents = [];

    private PluginEnablement(Guid householdId)
    {
        HouseholdId = householdId;
    }

    public Guid HouseholdId { get; private set; }

    public bool Enabled { get; private set; }

    /// <summary>Who last switched it, and when; empty while it was never switched.</summary>
    public Guid? ChangedBy { get; private set; }

    public DateTimeOffset? ChangedAt { get; private set; }

    /// <summary>When it was first switched on here; a plugin seeds a household's starting data then.</summary>
    public DateTimeOffset? FirstEnabledAt { get; private set; }

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents;

    public void ClearDomainEvents() => _domainEvents.Clear();

    /// <summary>A household that never had the plugin: off.</summary>
    public static PluginEnablement For(Guid householdId) => new(householdId);

    /// <summary>Does nothing when it is already in that state.</summary>
    public void Switch(bool on, Guid by, DateTimeOffset at)
    {
        if (on == Enabled)
        {
            return;
        }

        var firstTime = on && FirstEnabledAt is null;
        Enabled = on;
        ChangedBy = by;
        ChangedAt = at;
        FirstEnabledAt ??= on ? at : null;
        _domainEvents.Add(on ? new PluginSwitchedOn(HouseholdId, by, firstTime) : new PluginSwitchedOff(HouseholdId, by));
    }
}

/// <param name="FirstTime">The household never had the plugin before: seed its starting data.</param>
public sealed record PluginSwitchedOn(Guid HouseholdId, Guid By, bool FirstTime) : IDomainEvent;

public sealed record PluginSwitchedOff(Guid HouseholdId, Guid By) : IDomainEvent;
