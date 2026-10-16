# 0003: Domain events before commit, integration messages through the outbox

Status: accepted (2026-10-14)

## Context

The old code saved to the database and then published to RabbitMQ. If the publish failed, other services never heard
about the change; if the save failed after a publish, they heard about something that never happened.

## Decision

- Aggregates raise domain events (`IDomainEvent`, plain records, no MediatR dependency).
- `HomeAppsDbContext.SaveChangesAsync` collects the events from tracked aggregates and dispatches them in-process through
  MediatR (wrapped in `DomainEventNotification<T>`), repeating until handlers raise no new events, then commits once.
- Handlers that must tell other services publish a `HomeApps.Contracts` message through `IEventBus`. With the MassTransit
  EF Core bus outbox, that writes an outbox row in the same transaction; MassTransit delivers it after the commit.
- Every consumer endpoint uses the EF Core inbox, so duplicate deliveries are ignored, and anything a consumer publishes
  is committed with its changes.
- Domain events never leave the service. Integration messages are a separate, versioned contract.

## Consequences

- Save and publish succeed or fail together; delivery is at least once, and consumers are idempotent through the inbox.
- Messages arrive after a short delay (the outbox delivery service polls every second).
- Every service database has the MassTransit `InboxState`, `OutboxState` and `OutboxMessage` tables.
