# Auth service

Auth owns who you are: sign-up, sign-in (password and Google), tokens, sessions, invitations and the server role
(User < Admin < Owner). Everything else about a person lives in Accounts, which follows Auth through events.
The specification is `plans/features/auth.md`. This page covers running it and building on it.

## Run it locally

Everything is served at `https://app.localhost` through Traefik. `/auth` and `/.well-known` go to Auth, and
everything else goes to the Vue app (vite on port 5173). The certificate for `*.app.localhost` is in `certs/` (mkcert).

1. Start the infrastructure: Traefik, Postgres, RabbitMQ and Mailpit.

   ```bash
   docker compose -f compose.dev.yaml up -d
   ```

2. Run `Auth.Api` from Rider with the `http` profile (port 5100). Traefik forwards `https://app.localhost/auth`
   to it (`dynamic/dev-routes.yaml`). In Development Auth applies its migrations on start and keeps a generated
   signing key in `src/Auth/Auth.Api/.keys/` (ignored by git).
3. Open the inbox at <http://localhost:8025>. Every email Auth sends lands there.

To run Auth in a container as well, add the `services` profile: `docker compose -f compose.dev.yaml --profile services up -d --build`.
The container's route has a higher priority than the route to Rider, so stop one of the two.

`.env.example` lists what compose reads. Every value has a working default, so `.env` is optional; it is never committed.

### The first Owner

On an empty database Auth invites the address in `Auth:Bootstrap:Email` (from Rider: user secrets or
`appsettings.Development.json`; in compose: `AUTH_BOOTSTRAP_EMAIL`). Open the email in Mailpit, follow the link
and set a password. That account becomes the Owner. In Development `Auth:RegistrationOpen` is on, so anyone can
also sign up with `POST /auth/register`.

### Google sign-in

Without Google settings the Google endpoints answer `external_login.provider_disabled`, and nothing else is affected.

1. In Google Cloud Console, open APIs & Services, then Credentials, and create an OAuth client ID of type "Web application".
2. Add the authorized redirect URI `https://app.localhost/auth/external/google/callback`.
3. Keep the client id and secret out of the repository:

   ```bash
   dotnet user-secrets --project src/Auth/Auth.Api set "Auth:Google:ClientId" "<id>"
   dotnet user-secrets --project src/Auth/Auth.Api set "Auth:Google:ClientSecret" "<secret>"
   ```

   For the container, put them in `.env` as `AUTH_GOOGLE_CLIENT_ID` and `AUTH_GOOGLE_CLIENT_SECRET`.

## Configuration

| Key | What it is |
| --- | --- |
| `ConnectionStrings:Database` | Auth's own Postgres database. |
| `RabbitMq:*`, `Messaging:Transport` | The broker (`RabbitMq`, or `InMemory` for tests only). |
| `Auth:Signing:Issuer` | Auth's public address, e.g. `https://app.localhost`; the `iss` of every token. |
| `Jwt:Authority` | Where Auth validates its own tokens from; the same public address. |
| `Auth:Signing:PrivateKeyPem` | RSA key in PEM. Required outside Development. When changing keys, put the old public key in `PreviousPublicKeyPem` for 15 minutes. |
| `Auth:Clients` | The apps that may ask for tokens. `Browser` gets the refresh token in a cookie, `Native` gets it in the body. `RedirectUris` limits where Google sign-in may return. |
| `Auth:RegistrationOpen` | Sign-up without an invitation. Off by default. |
| `Auth:Bootstrap:Email` | The first Owner (see above). |
| `Auth:Google:ClientId`, `ClientSecret` | Google sign-in. Secrets, never committed. |
| `Auth:Email:FrontendBaseUrl`, `From`, `Smtp:*` | Emails. Without an SMTP host, Development writes the emails to the log; any other environment does not start. |
| `Auth:DataProtection:KeysPath` | Keys that encrypt the Google sign-in cookie. Keep them on a volume in a container. |
| `Auth:Proxy:KnownNetworks` | Proxies whose `X-Forwarded-For` is trusted (Traefik), so rate limits see the real caller. |
| `Auth:RateLimits:*` | Anonymous attempts per IP and minute; emails per IP and per address and hour. |

## How other services trust Auth

Services validate access tokens themselves. They read `{Jwt:Authority}/.well-known/openid-configuration`, take
the keys from `/.well-known/jwks.json`, and check the audience `homeapps`. The metadata keeps the public `issuer`
but gives `jwks_uri` at the address the caller used. So a service can set `Jwt:Authority` to Auth's address inside
the Docker network (`http://auth:8080`, with `Jwt:Issuer` set to the public address and `Jwt:RequireHttpsMetadata`
off) and never goes through the public domain or needs a certificate for it. `AddHomeAppsDefaults` in the kit does all
of this from the `Jwt` section. The token carries `sub` (the user id, the same in every service), `email`, `role`,
`client_id` and `auth_time`. Auth only sends events (`src/Contracts/HomeApps.Contracts/Auth`). Accounts sends
`UserDisabled`, `UserEnabled` and `UserErased` back.

## Auth as the pattern for the next service

Auth is the first service written on the kit, so Accounts and Household can copy its shape:

- **Domain** (`Auth.Domain`). Rich aggregates, value objects as sealed records, and rules that return `Result`
  with an `AppError` instead of throwing. Domain events stay inside the service.
- **Application** (`Auth.Application`). One folder per use case, with the command, its validator and its handler
  in one file. Ports (repositories, token issuer) are interfaces here. Domain events become integration events
  (`src/Contracts`) through the outbox.
- **Infrastructure** (`Auth.Infrastructure`). The EF Core configuration maps the domain without changing it. There is
  one repository per aggregate, MassTransit consumers are public classes, and options are validated on start.
- **Api** (`Auth.Api`). `Program.cs` is a few lines. There is one `IEndpoint` class per group of routes, and each
  endpoint only binds the request, sends the command and maps the result (`ToHttpResult`). Endpoints need a token
  unless they say `AllowAnonymous()`. A test pins the list of anonymous endpoints.
- **Tests**. Domain and Application tests have no infrastructure. `Auth.Infrastructure.Tests` and `Auth.Api.Tests`
  run on a real Postgres. `Auth.IntegrationTests` adds real RabbitMQ and SMTP (Mailpit) and plays the other
  service with a second bus.

### Running the database tests

They need Docker (Testcontainers starts Postgres, RabbitMQ and Mailpit), or running services named by environment variables:

```bash
AUTH_TEST_DATABASE="Host=localhost;Username=postgres;Password=postgres" \
AUTH_TEST_RABBITMQ=localhost AUTH_TEST_MAILPIT=localhost \
dotnet test HomeApps.slnx
```

With `docker compose -f compose.dev.yaml up -d` running, those three variables point the tests at the compose services.
