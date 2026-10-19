# 0008: One error type with a kind

Status: accepted (2026-10-19). Brings back typed errors removed by [0007](0007-lean-kit-without-templates.md), in a smaller form.

## Context

Auth is the first service that needs statuses other than 400: wrong credentials (401), a role that may not act (403),
a taken email (409), a lockout (423). Clients also need a stable code to react to (`auth.locked_out`), not a message.

## Decision

- `HomeApps.BuildingBlocks.Domain.Errors.AppError`: one FluentResults error with a `Code` (e.g. `email.taken`) and an
  `ErrorKind` (`Validation`, `Unauthorized`, `Forbidden`, `NotFound`, `Conflict`, `Locked`). No class hierarchy.
- `ResultHttpMapper` takes the status from the first `AppError`'s kind and adds `"code"` to the ProblemDetails.
  Field validation errors still give a 400 ValidationProblem; a plain error without a kind still gives 400.
- A service keeps its errors as static properties next to its aggregates (e.g. `IdentityErrors.EmailTaken`).

## Consequences

- Clients branch on `code`; messages can change freely.
- A new kind is added only when a service needs a status the list does not cover.
