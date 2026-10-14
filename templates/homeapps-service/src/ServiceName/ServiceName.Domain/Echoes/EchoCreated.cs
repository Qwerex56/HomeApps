using HomeApps.BuildingBlocks.Domain;

namespace ServiceName.Domain.Echoes;

public sealed record EchoCreated(Guid EchoId, Guid OwnerId, string Message) : IDomainEvent;
