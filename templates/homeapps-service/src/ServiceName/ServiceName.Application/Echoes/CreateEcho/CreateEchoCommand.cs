using HomeApps.BuildingBlocks.Application.Messaging;

namespace ServiceName.Application.Echoes.CreateEcho;

public sealed record CreateEchoCommand(string Message) : ICommand<Guid>;
