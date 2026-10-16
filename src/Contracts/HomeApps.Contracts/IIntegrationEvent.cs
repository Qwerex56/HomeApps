namespace HomeApps.Contracts;

/// <summary>
/// Marks a message that crosses service boundaries over RabbitMQ.
/// Rules: immutable <c>record</c>, primitive or Contracts-only types, named for what happened (<c>UserCreated</c>),
/// and never change a published shape in a breaking way: add optional fields, or add a new message.
/// Put each service's messages in a folder and namespace named after the publishing service, e.g. <c>HomeApps.Contracts.Accounts</c>.
/// </summary>
#pragma warning disable CA1040 // Marker interface on purpose.
public interface IIntegrationEvent;
#pragma warning restore CA1040
