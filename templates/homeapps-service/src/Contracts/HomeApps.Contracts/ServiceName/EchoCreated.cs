namespace HomeApps.Contracts.ServiceName;

/// <summary>Published by the ServiceName service when an echo is created.</summary>
public sealed record EchoCreated(Guid EchoId, Guid OwnerId, string Message) : IIntegrationEvent;
