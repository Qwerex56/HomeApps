using HomeApps.BuildingBlocks.Domain;
using MediatR;

namespace HomeApps.BuildingBlocks.Application.Messaging;

/// <summary>
/// Wraps a domain event so it can travel through MediatR without the Domain layer depending on MediatR.
/// </summary>
public sealed record DomainEventNotification<TDomainEvent>(TDomainEvent DomainEvent) : INotification
    where TDomainEvent : IDomainEvent;

/// <summary>
/// Handles a domain event in-process, before the unit of work commits. Typical use: publish an integration
/// message through <see cref="Abstractions.IEventBus"/> so it lands in the outbox in the same transaction.
/// </summary>
public interface IDomainEventHandler<TDomainEvent> : INotificationHandler<DomainEventNotification<TDomainEvent>>
    where TDomainEvent : IDomainEvent;
