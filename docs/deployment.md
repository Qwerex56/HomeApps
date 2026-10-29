# Deploying HomeApps

HomeApps runs on one machine with Docker Compose: Traefik in front (HTTPS from Let's Encrypt), the three services
(Auth, Accounts, Households), the Vue app in nginx, Postgres and RabbitMQ. Everything is served at one address,
`https://home-apps.dev` in our case; Traefik sends `/auth`, `/.well-known`, `/users`, `/admin/users`, `/households`,
`/invites` and `/admin/households` to the services and every other path to the web app.

The whole setup is `compose.prod.yaml`. Development is a different file (`compose.dev.yaml`, see
[working-with-the-repo.md](working-with-the-repo.md)).

## What you need

- A machine with Docker and the Compose plugin (a home server, a NAS, a small VPS).
- A domain whose DNS `A` (and `AAAA`, if you have IPv6) record points at that machine's public address.
- Ports 80 and 443 forwarded to it. Let's Encrypt checks the domain over port 80 (HTTP-01), and every page is HTTPS
  only (`.dev` domains are on the browsers' HSTS list anyway). No port? See [Without open ports](#without-open-ports).
- An SMTP account for invitations, password resets and notices (any provider; port 587 with STARTTLS by default).
- Optional: a Google OAuth client for "Sign in with Google".

## First start

```bash
git clone <the repository> homeapps && cd homeapps
cp .env.prod.example .env            # fill in every value marked "required"
scripts/generate-signing-key.sh      # once: secrets/auth-signing.pem
docker compose -f compose.prod.yaml up -d --build
```

On an empty database Auth sends an invitation to `AUTH_BOOTSTRAP_EMAIL`. That person opens the link, sets a password
(or uses Google) and becomes the Owner, the only account above Admin. Sign-up is closed (`Auth:RegistrationOpen`
is off): everyone else joins through an invitation from an existing account.

Every service creates its database (`homeapps_auth`, `homeapps_accounts`, `homeapps_households`) and applies its
migrations on start (`Database:MigrateOnStartup`, set by the compose file). Each runs as a single instance, so two
copies never migrate at the same time.

### Settings (`.env`)

| Variable | |
|---|---|
| `HOMEAPPS_DOMAIN` | The public address, without `https://`. Becomes the token issuer, the address in every email and Google's redirect. |
| `ACME_EMAIL` | Let's Encrypt writes here before a certificate expires. `ACME_CA_SERVER` can point at the staging CA for a first try. |
| `POSTGRES_USER`, `POSTGRES_PASSWORD` | The database account all three services use. |
| `RABBITMQ_USER`, `RABBITMQ_PASSWORD` | The message broker account. |
| `AUTH_BOOTSTRAP_EMAIL` | The first Owner. |
| `AUTH_GOOGLE_CLIENT_ID`, `AUTH_GOOGLE_CLIENT_SECRET` | Google sign-in; leave empty to turn it off. |
| `SMTP_HOST`, `SMTP_PORT`, `SMTP_USERNAME`, `SMTP_PASSWORD`, `SMTP_FROM` | The mail server. Auth does not start without `SMTP_HOST`. |
| `HOUSEHOLDS_MAX_MEMBERS` | Members per household, guests included (100). |

Anything else a service reads can be set the same way, as an environment variable in `compose.prod.yaml`
(`Auth__RefreshGraceWindow`, `Auth__RateLimits__PerMinute`, ...); [auth.md](auth.md), [accounts.md](accounts.md)
and [households.md](households.md) list them.

### The signing key

Auth signs access tokens with an RSA key; the other services check them with its public half from
`/.well-known/jwks.json`. `scripts/generate-signing-key.sh` writes the key to `secrets/auth-signing.pem` (git ignores
`secrets/`). Compose mounts it as the Docker secret `/run/secrets/Auth__Signing__PrivateKeyPem`, and every service
reads files in `/run/secrets` as settings, one file per key.

Back the key up with the database. Losing it is not a disaster: a new key only signs everyone out.

To change the key without signing anyone out, publish the old public key next to the new one for 15 minutes:

```bash
openssl pkey -in secrets/auth-signing.pem -pubout -out secrets/auth-signing-previous.pub
scripts/generate-signing-key.sh --force
```

then add a second secret to the `auth` service (`source: auth-signing-previous`, `target: Auth__Signing__PreviousPublicKeyPem`,
file `./secrets/auth-signing-previous.pub`), run `docker compose -f compose.prod.yaml up -d`, and remove it again
after 15 minutes.

### Google sign-in

In Google Cloud Console create an OAuth client of type "Web application" with the authorized redirect URI
`https://<HOMEAPPS_DOMAIN>/auth/external/google/callback`, and put its id and secret in `.env`.

## Checking a release: the smoke tests

`compose.smoke.yaml` turns the production stack into a test stack: Mailpit catches the emails (inbox at
http://localhost:8025) and Traefik uses its own certificate instead of Let's Encrypt. Run it on a machine with
nothing else on ports 80, 443 and 8025, and with a fresh `.env` (see `.env.prod.example`; `SMTP_HOST` can be anything):

```bash
HOMEAPPS_DOMAIN=home-apps.localhost docker compose -f compose.prod.yaml -f compose.smoke.yaml up -d --build
scripts/smoke.sh
```

`scripts/smoke.sh` (bash, curl, jq) needs an empty database. It goes through the whole release over HTTP: the web
app and a deep link, the Owner from the bootstrap invitation, sign-in, refresh (including a refresh whose answer
was lost), the profile reaching Accounts, a household and its invite, a second person invited to the app who signs up
and joins the household, and sign-out. It ends with `SMOKE TEST PASSED`.

The browser test of the web app runs against the same stack, with an account that exists (the Owner from above):

```bash
npm --prefix src/Web ci
(cd src/Web && npx playwright install chromium)
E2E_BASE_URL=https://home-apps.localhost E2E_EMAIL=<email> E2E_PASSWORD=<password> npm --prefix src/Web run test:e2e
```

See the "Smoke test" section of [web.md](web.md) for what it covers. Clean up with
`docker compose -f compose.prod.yaml -f compose.smoke.yaml down -v`.

## Updating

```bash
git pull
docker compose -f compose.prod.yaml up -d --build
```

Migrations run as the new containers start. Read [CHANGELOG.md](../CHANGELOG.md) first: a release that changes
`.env` says so there.

## Backups

All data lives in the `postgres-data` volume; `auth-keys` holds the keys for Google sign-in cookies and
`letsencrypt` the certificates (both are recreated if lost). Back up the databases and `secrets/`:

```bash
docker compose -f compose.prod.yaml exec -T postgres pg_dumpall -U homeapps > homeapps-$(date +%F).sql
```

Restore into an empty stack with `docker compose -f compose.prod.yaml exec -T postgres psql -U homeapps < file.sql`
before the services start (`up -d postgres` first).

## Without open ports

If ports 80 and 443 cannot be forwarded (CGNAT, a provider that blocks them), two options:

- **Let's Encrypt over DNS.** With the domain on Cloudflare, replace the `httpchallenge` line of the `traefik`
  service with `--certificatesresolvers.letsencrypt.acme.dnschallenge.provider=cloudflare` and give Traefik the
  environment variable `CF_DNS_API_TOKEN` (a token with `Zone:DNS:Edit` for the domain). The app is then reachable
  inside your network only, unless port 443 is open.
- **Cloudflare Tunnel.** Run `cloudflared` next to the stack and point the tunnel at `https://traefik:443` with
  `noTLSVerify: true` (Traefik has no public certificate then); nothing needs to be opened, and Cloudflare serves the
  public certificate.
