using HomeApps.BuildingBlocks.Application.Messaging;

namespace ServiceName.Application.Echoes.MarkEchoDelivered;

public sealed record MarkEchoDeliveredCommand(Guid EchoId) : ICommand;
