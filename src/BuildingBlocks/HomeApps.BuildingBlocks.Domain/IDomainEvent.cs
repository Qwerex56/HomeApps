namespace HomeApps.BuildingBlocks.Domain;

/// <summary>
/// Something that happened inside an aggregate. Raised by the aggregate, dispatched in-process when the unit of work saves.
/// Domain events never leave the service; map the ones other services care about to a <c>HomeApps.Contracts</c> message.
/// </summary>
#pragma warning disable CA1040 // Marker interface on purpose.
public interface IDomainEvent;
#pragma warning restore CA1040
