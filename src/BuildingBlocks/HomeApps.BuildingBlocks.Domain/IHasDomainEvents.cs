namespace HomeApps.BuildingBlocks.Domain;

/// <summary>
/// Implemented by an aggregate that raises domain events. The aggregate keeps its own list; the DbContext finds
/// tracked aggregates by this interface, dispatches their events before saving and then clears them.
/// </summary>
/// <example>
/// <code>
/// private readonly List&lt;IDomainEvent&gt; _domainEvents = [];
/// public IReadOnlyList&lt;IDomainEvent&gt; DomainEvents => _domainEvents;
/// public void ClearDomainEvents() => _domainEvents.Clear();
/// </code>
/// </example>
public interface IHasDomainEvents
{
    IReadOnlyList<IDomainEvent> DomainEvents { get; }

    void ClearDomainEvents();
}
