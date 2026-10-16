# 0006: A shared kit plus `dotnet new` templates instead of copied code

Status: superseded by [0007](0007-lean-kit-without-templates.md) (2026-10-16)

## Context

Each service repeated the same MediatR, FluentValidation, FluentResults, EF Core, MassTransit and JWT wiring, and they had
started to drift. The old `Shared` project mixed building blocks with service-specific exceptions and DTOs.

## Decision

- `src/BuildingBlocks` holds four layer-matched libraries (Domain, Application, Infrastructure, Api) with the base types,
  pipeline behaviors and one-call registrations (`AddHomeAppsApplication`, `AddHomeAppsInfrastructure<TDbContext>`,
  `AddHomeAppsDefaults`, `UseHomeAppsDefaults`). `src/Contracts` holds integration messages only.
  No service-specific code goes into either.
- `tests/BuildingBlocks/HomeApps.BuildingBlocks.Testing` holds the shared integration-test fixture.
- `templates/` holds `homeapps-service`, `homeapps-command` and `homeapps-query`. `templates/verify.sh` generates services
  from them, builds and tests them, and runs in CI, so the templates cannot silently rot.
- Services reference the kit with project references (one repository, no package feed).
- The kit pins MediatR 14 (free community licence) and MassTransit 8.x (free).

## Consequences

- A change to the kit affects every service at once; keep it small and covered by tests.
- Generated code is owned by the service after generation; template improvements don't flow into existing services automatically.
