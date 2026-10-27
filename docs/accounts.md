# Accounts service

Accounts owns what a person tells about themselves and the state of their account: the profile, active or disabled,
how the account was created and its history. Who you are (email, password, Google, role) lives in Auth; Accounts
keeps read-only copies of the email and the role and follows Auth through events. The specification is
`plans/features/accounts.md`, the implementation plan `plans/features/accounts-implementation-plan.md`.

## Run it locally

Start the infrastructure and Auth as described in [auth.md](auth.md), then run `Accounts.Api` from Rider with the
`http` profile (port 5200). Traefik forwards `https://app.localhost/users` and `/admin/users` to it
(`dynamic/dev-routes.yaml`). In Development Accounts applies its migrations on start (database `homeapps_accounts`).

Accounts reads Auth's signing keys from `http://localhost:5100`, where Auth runs from Rider; the `auth` container
publishes the same port, so this works with either. With the `services` profile both run in containers and Accounts
reads the keys from `http://auth:8080` inside the Docker network:

```bash
docker compose -f compose.dev.yaml --profile services up -d --build
```

An account appears in Accounts a moment after sign-up in Auth (`IdentityRegistered` through RabbitMQ). Until then
`GET /users/me` answers `404` with the code `user.pending`, and the app asks again.

## Endpoints

Every endpoint needs an access token from Auth unless noted. Errors are `application/problem+json` with a `code`;
when several things are wrong (a profile form) they are all listed in `errors`. Enums are strings in JSON.

| Endpoint | What it does |
| --- | --- |
| `GET /users/me` | My account: profile, role, status, how it was created. |
| `PUT /users/me/profile` | Replace my whole profile. |
| `POST /users/me/disable` `{ reason? }` | Disable my own account. |
| `GET /users/me/history` | When my account changed and what kind of change it was (latest 50). |
| `GET /users/me/reactivation` | The profile to check before coming back. Reactivation ticket only. |
| `POST /users/me/reactivate` `{ profile }` | Bring my self-disabled account back. Reactivation ticket only. |
| `GET /admin/users?status=&role=&q=&page=&pageSize=&sort=&desc=` | The account list. Admin or Owner. |
| `GET /admin/users/{id}` | One account with everything, also who disabled it and when it was last seen. |
| `PUT /admin/users/{id}/profile` | Replace someone's profile. |
| `POST /admin/users/{id}/disable` `{ reason? }`, `POST /admin/users/{id}/enable` | Disable or enable an account. |
| `GET /admin/users/{id}/history?before=` | The full audit history, 100 entries at a time; `before` is the oldest `at` shown. |
| `DELETE /admin/users/{id}` `{ confirmEmail }` | Erase a disabled account for good ("forget me"). Owner only. |

Who may change whom is decided in the domain: an Admin manages Users, the Owner also manages Admins, nobody touches
the Owner, and only the Owner erases.

### Coming back after disabling your own account

1. Sign-in at Auth answers `403 account.disabled_by_user` with a short ticket instead of tokens.
2. The app shows the profile from `GET /users/me/reactivation` (sent with the ticket as the bearer token) and asks
   "Are you sure you want to restore your account?".
3. `POST /users/me/reactivate` with the confirmed profile enables the account; Accounts tells Auth (`UserEnabled`).
4. The app finishes at Auth with `POST /auth/reactivation/complete` and the same ticket. Until `UserEnabled` has
   reached Auth this answers `409 reactivation.pending` with `Retry-After`.

An account disabled by an admin cannot come back this way; an admin enables it.

## Limits

Every change (profile, disable, enable, reactivate, erase) counts against the caller: 10 changes in 10 minutes,
then `429 rate_limited` with `Retry-After`. Reads are free. A save that changes nothing leaves no history entry.
The limits are in memory, so a restart starts from zero.

## Configuration

| Key | What it is |
| --- | --- |
| `ConnectionStrings:Database` | Accounts' own Postgres database. |
| `RabbitMq:*`, `Messaging:Transport` | The broker (`RabbitMq`, or `InMemory` for tests only). |
| `Jwt:Authority` | Where Auth's keys are read: `http://auth:8080` in Docker, `http://localhost:5100` from Rider. |
| `Jwt:Issuer` | Auth's public address, the `iss` of the tokens, e.g. `https://app.localhost`. |
| `Jwt:RequireHttpsMetadata` | Off when `Jwt:Authority` is plain http inside the Docker network or on localhost. |
| `Accounts:RateLimits:Changes`, `Window` | The limit on changes per person (default 10 in `00:10:00`). |

## Events

Accounts consumes `IdentityRegistered`, `IdentityEmailChanged`, `IdentityRoleChanged` and `IdentitySignedIn`
from Auth. An event that arrives before the account exists is tried again after 1, 2, 5, 10 and 30 seconds, then
it waits in the `_error` queue in RabbitMQ, from where it can be moved back in the management UI.

It publishes `UserCreated`, `UserProfileUpdated`, `UserDisabled`, `UserEnabled` and `UserErased`
(`src/Contracts/HomeApps.Contracts/Accounts`).

## Tests

`Accounts.Domain.Tests` and `Accounts.Application.Tests` need nothing. `Accounts.Infrastructure.Tests` and
`Accounts.Api.Tests` run on a real Postgres (`ACCOUNTS_TEST_DATABASE`, otherwise Testcontainers).
`tests/Integration/HomeApps.IntegrationTests` runs the real Auth and Accounts together on Postgres and RabbitMQ
(`AUTH_TEST_DATABASE`, `AUTH_TEST_RABBITMQ`, otherwise Testcontainers):

```bash
ACCOUNTS_TEST_DATABASE="Host=localhost;Username=postgres;Password=postgres" \
AUTH_TEST_DATABASE="Host=localhost;Username=postgres;Password=postgres" \
AUTH_TEST_RABBITMQ=localhost AUTH_TEST_MAILPIT=localhost \
dotnet test HomeApps.slnx
```
