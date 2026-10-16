# 0002: Typed results instead of exceptions for business rules

Status: accepted (2026-10-14)

## Context

The old services threw custom exceptions for rule violations and mapped them in middleware, which also returned
`exception.Message` for unexpected errors (one of them contained a plain-text password).

## Decision

Domain methods and handlers return FluentResults `Result` / `Result<T>`. Failures carry one of five typed errors from
BuildingBlocks.Domain: `ValidationError` (400), `UnauthorizedError` (401), `ForbiddenError` (403), `NotFoundError` (404),
`ConflictError` (409). Each has a stable `Code`. `ResultHttpMapper` in BuildingBlocks.Api is the only place that turns
them into responses (ProblemDetails, with `code` as an extension). Any other error maps to 422.
FluentValidation failures become a `ValidationError` in a pipeline behavior instead of an exception.
Exceptions are for bugs and outages only; a last-resort handler logs them and returns a generic 500.

## Consequences

- Error messages are part of the API and must be safe to show.
- Handlers read a little more explicitly (`if (result.IsFailed) return ...`), in exchange for no hidden control flow.
