# 0007: A lean kit without templates, entity base classes or typed errors

Status: accepted (2026-10-16). Supersedes [0006](0006-shared-kit-and-templates.md) and the typed errors of [0002](0002-typed-results-instead-of-exceptions.md).

## Context

The old services (AccountManagement, Household.Api, AuthorizationService, TodoListService, Shared) are rewritten from
scratch on the kit, so nothing has to stay compatible with them. The `dotnet new` templates were 55 files, a script and
a CI job to maintain for three services, and generated code never received later fixes. The kit also carried
abstractions no service used yet (entity and value object base classes, five error types, a clock interface), and
MediatR 13+ needs a licence key.

## Decision

- No `templates/`. The first service written on the kit (Auth) is the reference for the others; Claude writes the
  repetitive parts. The templates can be restored from commit `2dc3669` if many services appear.
- No entity base classes: `Entity<TId>`, `AggregateRoot<TId>` and `ValueObject` are removed. An aggregate declares its
  own `Id` and implements `IHasDomainEvents` when it raises events; value objects are `record`s.
- No typed errors for now: `DomainError` and its five subclasses are removed. Validation failures are FluentResults
  errors with the field name in metadata; `ResultHttpMapper` maps every failure to 400. An error type is added when
  a service needs another status.
- Time comes from the .NET `TimeProvider` instead of `IDateTimeProvider`.
- MediatR 12.5.0 (Apache 2.0, the last version without a licence key) and MassTransit 8.x.

## Consequences

- Less code in the kit, and no abstraction is added before a service needs it.
- Until typed errors return, clients see 400 for every rule violation (not 404/409).
- Each aggregate repeats three lines for its domain event list.
