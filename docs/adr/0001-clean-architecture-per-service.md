# 0001: Clean architecture with a four-project split per service

Status: accepted (2026-10-14)

## Context

The first services mixed controllers, services, repositories and models in one project, and business rules ended up in
service classes and repositories (an anemic model). Every new service would copy that.

## Decision

Each service has four projects: Domain, Application, Infrastructure and Api. Dependencies point inward only
(Api → Infrastructure → Application → Domain). Domain references no framework (no EF Core, MediatR or ASP.NET).
Use cases are vertical slices inside Application (`Feature/UseCase/Command + Validator + Handler`).
An architecture test in each service enforces the references.

## Consequences

- Business rules live in aggregates and can be unit tested without a database.
- More projects per service; the template creates them, so the cost is paid once.
- EF Core mapping must be done with `IEntityTypeConfiguration<T>`, not attributes, and aggregates need a private
  parameterless constructor for EF.
