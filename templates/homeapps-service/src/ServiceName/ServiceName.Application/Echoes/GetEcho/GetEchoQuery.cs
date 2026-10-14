using HomeApps.BuildingBlocks.Application.Messaging;

namespace ServiceName.Application.Echoes.GetEcho;

public sealed record GetEchoQuery(Guid EchoId) : IQuery<EchoResponse>;

public sealed record EchoResponse(Guid Id, string Message, DateTimeOffset CreatedAt, DateTimeOffset? DeliveredAt);
