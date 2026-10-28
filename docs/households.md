# Households service

Households owns the groups people live in: a household, who belongs to it with which role, its invites and its guest
link. A person can belong to many households; the household role (Guest < Member < Admin < Owner) has nothing to do
with the server role from Auth. Households keeps a copy of each person's display name, avatar and whether the account
is active, following Accounts through events. The specification is `plans/features/household.md`, the
implementation plan `plans/features/household-implementation-plan.md`.

## Run it locally

Start the infrastructure and Auth as described in [auth.md](auth.md), then run `Households.Api` from Rider with the
`http` profile (port 5300). Traefik forwards `https://app.localhost/households`, `/invites` and `/admin/households` to
it (`dynamic/dev-routes.yaml`). In Development Households applies its migrations on start (database
`homeapps_households`). Keys come from Auth exactly as for [Accounts](accounts.md): `http://localhost:5100` from Rider,
`http://auth:8080` in the `services` profile.

A person is known to Households a moment after Accounts creates the account (`UserCreated`). Creating a household
or accepting an invite works before that; the name shows as "Użytkownik" until the copy arrives.

## Endpoints

Every endpoint needs an access token from Auth. Errors are `application/problem+json` with a `code`; a body that
cannot be read (bad JSON, an unknown enum value) is `400 request.invalid`. Enums are strings in JSON. Someone who
is not a member gets `404`, never `403`, so the ids of other households give nothing away.

| Endpoint | Who | What it does |
| --- | --- | --- |
| `POST /households` `{ name, description? }` | active account | Create a household; I become its Owner. `201` with `{ id }`. |
| `GET /households` | anyone | My households with my role; `ownershipReceived` marks a household I got automatically. |
| `GET /households/{id}` | member, server admin | One household. Guests see no member count; only Owner and Admins see the guest link. |
| `PUT /households/{id}` `{ name, description? }` | Owner, Admin | Rename or describe it. |
| `POST /households/{id}/archive` | Owner, server admin | Hide it from everyone and revoke its invites. Kept for audit. |
| `POST /households/{id}/restore` | server admin | Bring an archived household back. |
| `POST /households/{id}/ownership/acknowledge` | Owner | Hide the "you are the Owner now" banner. |
| `POST /households/{id}/guest-link` | Owner, Admin | A new guest link `{ code }`; the old one stops working. |
| `DELETE /households/{id}/guest-link` | Owner, Admin | Turn the guest link off. |
| `GET /households/{id}/members` | Member and up, server admin | Members with names, roles and `isActive` (false: show greyed out). Guests get `403`. |
| `GET /households/{id}/members/me` | member | My membership. |
| `PUT /households/{id}/members/me/nickname` `{ nickname? }` | Member and up | My nickname in this household. |
| `DELETE /households/{id}/members/me` | member but the Owner | Leave. The Owner hands over first. |
| `PUT /households/{id}/members/{userId}/nickname` | Owner, Admin | Name someone, for example a guest. |
| `PUT /households/{id}/members/{userId}/role` `{ role }` | Owner | Change a role to Guest, Member or Admin. Ownership moves only by handover. |
| `PUT /households/{id}/members/{userId}/access` `{ until? }` | Owner, Admin | When a guest's access ends; `null` for no end. |
| `DELETE /households/{id}/members/{userId}` | Owner, Admin | Remove someone below my role; their invites are revoked. |
| `POST /households/{id}/ownership` `{ userId }` | Owner | Hand the household over; I become an Admin. |
| `POST /households/{id}/invites` `{ role, validity?, guestAccessUntil? }` | Member and up | A personal invite, `Member` or `Guest`, valid `ThirtyMinutes` … `SevenDays` (default). `201`. |
| `GET /households/{id}/invites` | Member and up | Working invites; a Member sees only their own. |
| `DELETE /households/{id}/invites/{inviteId}` | the author, Owner, Admin | Revoke an invite. |
| `GET /invites/{code}` | anyone | What a code leads to: household name, role, who invited. |
| `POST /invites/{code}/accept` | active account | Join. `{ id }` of the household. |
| `GET /admin/households?q=&before=` | server Admin, Owner | Every household, newest first, 50 at a time; `before` is the last `createdAt` shown. |

A code is 10 characters shown as `ABCDE-FGHJK`; case and the dash do not matter. Personal invites and the guest link
use the same endpoints. In the app they open one route; use `/join/{code}`, because `/invite/{token}` is already
Auth's link for joining the server.

### When the Owner's account goes away

The Owner leaves only after handing the household over. If an admin disables the Owner's account, or the account is
erased, ownership goes to the oldest active Admin, otherwise to the oldest active Member, and the banned Owner stays an
ordinary Member. The new Owner sees a banner (`ownershipReceivedAt` / `ownershipReceived`) until they acknowledge it,
and Auth sends them an email. With nobody to take over, an erased Owner's household is archived. An Owner who disabled
their own account keeps the household.

## Limits

Per signed-in person, in memory: opening or accepting invite codes 10 a minute, every other change 30 in 10 minutes,
then `429 rate_limited` with `Retry-After`. A household holds at most 100 people, guests and inactive accounts
included (`409 household.full`).

## Configuration

| Key | What it is |
| --- | --- |
| `ConnectionStrings:Database` | Households' own Postgres database. |
| `RabbitMq:*`, `Messaging:Transport` | The broker (`RabbitMq`, or `InMemory` for tests only). |
| `Jwt:Authority`, `Jwt:Issuer`, `Jwt:RequireHttpsMetadata` | As in [accounts.md](accounts.md). |
| `Households:MaxMembers` | People per household (default 100; `HOUSEHOLDS_MAX_MEMBERS` in compose). |
| `Households:RateLimits:Invites`, `InvitesWindow` | Invite codes opened or accepted (default 10 in `00:01:00`). |
| `Households:RateLimits:Changes`, `ChangesWindow` | Other changes (default 30 in `00:10:00`). |

## Events

Households consumes `UserCreated`, `UserProfileUpdated`, `UserDisabled`, `UserEnabled` and `UserErased` from
Accounts. An event that arrives before `UserCreated` is tried again after 1, 2, 5, 10 and 30 seconds, then it waits
in the `_error` queue. Once an hour a job removes guests whose access ended.

It publishes `HouseholdCreated`, `HouseholdUpdated`, `HouseholdArchived`, `HouseholdRestored`, `MemberJoined`,
`MemberLeft`, `MemberRoleChanged` and `OwnershipTransferred` (`src/Contracts/HomeApps.Contracts/Households`). Auth
consumes `OwnershipTransferred` to email the new Owner. Nothing about households goes into the JWT; another service
that needs a person's role asks `GET /households/{id}/members/me` with that person's token.

## Tests

`Households.Domain.Tests` and `Households.Application.Tests` need nothing. `Households.Infrastructure.Tests` and
`Households.Api.Tests` run on a real Postgres (`HOUSEHOLDS_TEST_DATABASE`, otherwise Testcontainers).
`tests/Integration/HomeApps.IntegrationTests` runs Auth, Accounts and Households together:

```bash
HOUSEHOLDS_TEST_DATABASE="Host=localhost;Username=postgres;Password=postgres" \
AUTH_TEST_DATABASE="Host=localhost;Username=postgres;Password=postgres" \
AUTH_TEST_RABBITMQ=localhost \
dotnet test tests/Integration/HomeApps.IntegrationTests
```
