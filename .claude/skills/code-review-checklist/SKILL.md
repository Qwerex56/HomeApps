---
name: code-review-checklist
description: HomeApps review checklist for security, performance and quality in .NET services and the Vue app, plus spec/ADR conformance and the verdict format. Use when reviewing a diff, a spec or an ADR in this repository.
---

# HomeApps code review checklist

Review what changed (`git diff <base>...HEAD`), against the spec (`specs/<name>.md`) and its ADR
(`specs/<name>-adr.md`). Report only real problems with a concrete fix; at most **10 findings** per round,
most important first. Don't restyle working code and don't ask for abstractions the spec doesn't need.

## 0. Conformance (check first)

- Every acceptance criterion (spec section 2) is implemented **and** has a test. List missing ones as `[BLOCKER]`.
- Endpoints, payloads, status codes and error `code`s match the ADR contract exactly.
- Nothing out of scope (spec section 5) was built. No changes under `specs/` by the developer.
- No unrelated files (`.idea/`, `.env`, `bin/`, `obj/`, `node_modules/`, secrets, local paths).

## 1. .NET security

- [ ] Every endpoint requires a token; each `.AllowAnonymous()` is intended by the spec and commented.
- [ ] "Me" comes only from `ICurrentUser.RequiredUserId`, never from route/body (IDOR).
- [ ] Household access checked in the handler: outsider → 404 (`not_found`), role too low → 403 (`forbidden`).
      Server role (Auth) vs household role (Households) not mixed up.
- [ ] Every resource loaded by id is checked to belong to the caller's household/account before use or return.
- [ ] Secrets, tokens, codes: stored hashed (see `PairingCode.Hash`), never logged, never returned twice.
      `LoggingBehavior` never logs bodies; new logs must not either.
- [ ] Rate limits on new changing/anonymous endpoints where the service has them.
- [ ] Input lengths bounded in the validator **and** in the EF mapping (`HasMaxLength`).
- [ ] No raw SQL with string concatenation; `ILIKE` patterns escape `%`/`_`.
- [ ] Cross-service data only through `HomeApps.Contracts` messages or a port; no reading another service's database.
- [ ] JARVIS/MCP tools: only what the caller could do through the API; no account/household admin through JARVIS.

## 2. .NET correctness and data

- [ ] Handlers don't call `SaveChangesAsync`; failed results change nothing (the transaction behavior relies on it).
- [ ] Rules live in the aggregate and return `Result.Fail(AppError)`; no exceptions for business rules.
- [ ] New `AppError` codes are `<service>.<snake_case>` with the right `ErrorKind`.
- [ ] Integration events published from a domain-event handler through `IEventBus` (outbox), not directly after save.
      Contracts changed only additively. Consumers idempotent (the inbox drops duplicates, but logic must tolerate replays).
- [ ] Concurrency: two requests at once (double click, two tabs) can't break an invariant: unique index, `xmin`
      row version or a typed `ConcurrencyConflictException` handling.
- [ ] Migrations: new migration only, never an edited applied one; nullable/default for existing rows; indexes for new
      filters; unique indexes with filters where soft-deleted rows exist.
- [ ] Soft delete respected: disabled/removed rows filtered where the spec says, restorable.
- [ ] `TimeProvider` for time; `CancellationToken` passed to every async EF/HTTP call.

## 3. .NET performance

- [ ] No N+1: no repository/HTTP call inside a loop over rows; use `Contains` over a list of ids or one projection.
- [ ] Lists are bounded (paging or a hard limit) and filtered/sorted in SQL, not after `ToListAsync`.
- [ ] Read-only queries project with `AsNoTracking()`; aggregates loaded for a command stay tracked.
- [ ] No `.Result`/`.Wait()`; no sync-over-async; `HttpClient` from the factory, not `new`.
- [ ] Event handlers/consumers don't load whole tables.

## 4. .NET quality (owner's rules)

- [ ] One top-level type per file; one folder per command/query with `Command`/`Validator`/`Handler` files;
      namespace = folder. No duplicated type names.
- [ ] No generic repositories, base entities or new abstractions without a need in this spec.
- [ ] Endpoints end with `.ToHttpResult()`/`.ToCreatedHttpResult()` and declare `.Produces<T>()`.
- [ ] Builds with warnings as errors; no `#pragma warning disable` without a reason in a comment.
- [ ] XML summary on the command/query states the use case (spec reference).

## 5. Vue security and correctness

- [ ] No `v-html` with server/user data (use `SafeMarkdown.vue`). No tokens in localStorage.
- [ ] UI hides actions the role can't take **and** the server enforces them (UI hiding is not security).
- [ ] No route path starting with a service prefix (`/auth`, `/households`, `/devices`, `/homemix` …).
- [ ] Calls only through the feature's `api.ts`; request types from `@/api/types/*` where generated; no edits to
      generated files.

## 6. Vue reactivity and leaks

- [ ] `queryKey` reactive (`computed(() => keys.x(id.value))`) when it depends on props/route.
- [ ] Props not destructured into plain variables; `watch` sources are getters/refs.
- [ ] Timers, `addEventListener`, SignalR `on(...)`, `BroadcastChannel` cleaned up in `onBeforeUnmount`.
- [ ] After a mutation the right keys are invalidated (list and detail), no stale screens.

## 7. Vue UX edge states

- [ ] Error, loading (`Skeleton`), empty and data states all present.
- [ ] Forms: `useSubmit` + `FormField`; field errors shown under fields; button disabled/`loading` while busy;
      double submit impossible.
- [ ] Destructive actions confirm (`useConfirm`); success feedback (`useToast`) where the spec expects it.
- [ ] Every new text and every new backend error code has a Polish key in `pl.json`.
- [ ] Works at phone width (wall tablet and PWA are real targets); dark mode via tokens, not fixed colours.

## 8. Tests

- [ ] Tests assert behaviour from the spec (codes, statuses, visible text), not private details.
- [ ] Permission paths tested (401/403/404), not only the happy path.
- [ ] No skipped/disabled/deleted tests, no weakened assertions, no `Thread.Sleep`/real time.

## Verdict format (reviewer output)

```
VERDICT: APPROVED | CHANGES_REQUESTED

1. [BLOCKER] src/Devices/Devices.Application/.../AddHouseholdDeviceHandler.cs:24
   Problem: a Member can add a device; spec 2.3 says admins only.
   Fix: return DeviceErrors.Forbidden when role < HouseholdRole.Admin, and add an API test for 403.
2. [MINOR] src/Web/src/features/devices/pages/HouseholdDevicesTab.vue:57
   Problem: no empty state.
   Fix: add <p v-else-if="!devices?.length"> with t('devices.noSatellites').

Spec coverage: 2.1 ✓, 2.2 ✓, 2.3 ✗ (see 1)
For the human reviewer: <2–4 places worth a close look on GitHub>
```

`APPROVED` only with zero `[BLOCKER]`s; `[MINOR]`s may remain and go into the PR description.
