# 0004: One repository per aggregate, the DbContext as unit of work

Status: accepted (2026-10-14)

## Context

The old services had generic `IRepository<T>` interfaces, repositories that held business rules, and a separate
`UnitOfWork` class wrapping the DbContext.

## Decision

- One repository interface per aggregate (`IUserRepository`), declared in the service's Application layer and implemented in
  Infrastructure. It exposes only what use cases need. No generic `IRepository<T>` or `TEntity` base.
- `IUnitOfWork` has one method, `SaveChangesAsync`, and the service's DbContext implements it.
- `TransactionBehavior` (a MediatR pipeline behavior) calls `SaveChangesAsync` after a command whose result succeeded.
  Failed commands and all queries save nothing. Handlers don't save.

## Consequences

- Handlers are short: load, call one domain method, return.
- A command that must save in the middle of its work can still inject `IUnitOfWork`, but that should be rare and explained.
