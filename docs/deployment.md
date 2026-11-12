# Deploying HomeApps

HomeApps runs on one machine with Docker Compose: Traefik in front (HTTPS from Let's Encrypt), the three services
(Auth, Accounts, Households), the Vue app in nginx, Postgres and RabbitMQ. Everything is served at one address,
`https://home-apps.dev` in our case; Traefik sends `/auth`, `/.well-known`, `/users`, `/admin/users`, `/households`,
`/invites` and `/admin/households` to the services and every other path to the web app.

The whole setup is `compose.prod.yaml`. It reaches the internet in one of two ways:

- **Open ports** (the default): Traefik takes ports 80 and 443 and gets certificates from Let's Encrypt.
- **Cloudflare Tunnel**, for a connection without a public address (CGNAT) or with blocked ports: the overlay
  `compose.tunnel.yaml` adds `cloudflared`, which connects out to Cloudflare; Cloudflare serves the certificate and
  nothing is opened on the machine. See [Cloudflare Tunnel](#cloudflare-tunnel-no-open-ports).

Development is a different file (`compose.dev.yaml`, see
[working-with-the-repo.md](working-with-the-repo.md)).

## What you need

- A machine with Docker and the Compose plugin (a home server, a NAS, a small VPS).
- A domain. With open ports: its DNS `A` (and `AAAA`, if you have IPv6) record points at that machine's public
  address, and ports 80 and 443 are forwarded to it (Let's Encrypt checks the domain over port 80, HTTP-01). Behind
  CGNAT: the domain's DNS is on Cloudflare (the free plan is enough), see
  [Cloudflare Tunnel](#cloudflare-tunnel-no-open-ports). Every page is HTTPS only either way (`.dev` domains are on
  the browsers' HSTS list anyway).
- An SMTP account for invitations, password resets and notices (any provider; port 587 with STARTTLS by default).
- Optional: a Google OAuth client for "Sign in with Google".

## First start

```bash
git clone <the repository> homeapps && cd homeapps
cp .env.prod.example .env            # fill in every value marked "required"
scripts/generate-signing-key.sh      # once: secrets/auth-signing.pem
scripts/generate-vapid-keys.sh       # once: paste the two lines it prints into .env
docker compose -f compose.prod.yaml up -d --build
```

(Behind a Cloudflare Tunnel set up the tunnel first and start with both files, see
[Cloudflare Tunnel](#cloudflare-tunnel-no-open-ports).)

On an empty database Auth sends an invitation to `AUTH_BOOTSTRAP_EMAIL`. That person opens the link, sets a password
(or uses Google) and becomes the Owner, the only account above Admin. Sign-up is closed (`Auth:RegistrationOpen`
is off): everyone else joins through an invitation from an existing account.

Every service creates its database (`homeapps_auth`, `homeapps_accounts`, `homeapps_households`, `homeapps_notifications`) and applies its
migrations on start (`Database:MigrateOnStartup`, set by the compose file). Each runs as a single instance, so two
copies never migrate at the same time.

### Settings (`.env`)

| Variable | |
|---|---|
| `HOMEAPPS_DOMAIN` | The public address, without `https://`. Becomes the token issuer, the address in every email and Google's redirect. |
| `ACME_EMAIL` | Optional: the Let's Encrypt account address. `ACME_CA_SERVER` can point at the staging CA for a first try. Not used behind a tunnel. |
| `CLOUDFLARE_TUNNEL_TOKEN` | Only with `compose.tunnel.yaml`: the token of the tunnel from the Cloudflare dashboard. A secret. |
| `COMPOSE_FILE` | Optional: `compose.prod.yaml:compose.tunnel.yaml` makes a plain `docker compose ...` use both files, so no command needs `-f`. |
| `POSTGRES_USER`, `POSTGRES_PASSWORD` | The database account all the services use. |
| `RABBITMQ_USER`, `RABBITMQ_PASSWORD` | The message broker account. |
| `AUTH_BOOTSTRAP_EMAIL` | The first Owner. |
| `AUTH_GOOGLE_CLIENT_ID`, `AUTH_GOOGLE_CLIENT_SECRET` | Google sign-in; leave empty to turn it off. |
| `SMTP_HOST`, `SMTP_PORT`, `SMTP_USERNAME`, `SMTP_PASSWORD`, `SMTP_FROM` | The mail server. Auth does not start without `SMTP_HOST`. |
| `HOUSEHOLDS_MAX_MEMBERS` | Members per household, guests included (100). |
| `NOTIFICATIONS_VAPID_PUBLIC_KEY`, `NOTIFICATIONS_VAPID_PRIVATE_KEY` | The Web Push keys from `scripts/generate-vapid-keys.sh`. Notifications does not start without them. Back them up and keep them: new keys mean every device has to turn notifications on again. The private key is a secret. |
| `NOTIFICATIONS_VAPID_SUBJECT` | Optional: how push services can reach you, `mailto:` or `https:` (the site's address by default). |

Anything else a service reads can be set the same way, as an environment variable in `compose.prod.yaml`
(`Auth__RefreshGraceWindow`, `Auth__RateLimits__PerMinute`, ...); [auth.md](auth.md), [accounts.md](accounts.md),
[households.md](households.md) and [notifications.md](notifications.md) list them.

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
nothing else on ports 80, 443 and 8025, and with a fresh `.env` (see `.env.prod.example`; `SMTP_HOST` can be anything, the VAPID keys come from
`scripts/generate-vapid-keys.sh`):

```bash
HOMEAPPS_DOMAIN=home-apps.localhost docker compose -f compose.prod.yaml -f compose.smoke.yaml up -d --build
scripts/smoke.sh
```

`scripts/smoke.sh` (bash, curl, jq) needs an empty database. It goes through the whole release over HTTP: the web
app and a deep link, the Owner from the bootstrap invitation, sign-in, refresh (including a refresh whose answer
was lost), the profile reaching Accounts, a household and its invite, a second person invited to the app who signs up
and joins the household, the Notifications service, and sign-out. It ends with `SMOKE TEST PASSED`.

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

(Behind a tunnel: `-f compose.prod.yaml -f compose.tunnel.yaml`, or nothing when `COMPOSE_FILE` is set in `.env`.
The same goes for every command below.)

Migrations run as the new containers start. Read [CHANGELOG.md](../CHANGELOG.md) first: a release that changes
`.env` says so there.

## Plugins

Apps beyond the core run from their own repositories and compose files and join this stack's network. Each one is
added with `HOMEAPPS_PLUGINS` in `.env` and switched on per household. Step by step:
[the Todo plugin](plugin-todo.md) and [the chat plugin](plugin-chat.md).

## Backups

All data lives in the `postgres-data` volume; `auth-keys` holds the keys for Google sign-in cookies and
`letsencrypt` the certificates (both are recreated if lost). Back up the databases, `secrets/` and `.env` (it holds the
VAPID keys). HomeMix add-ons' files are in the volume `homeapps_homemix-packages`; back it up with the databases
(`docker run --rm -v homeapps_homemix-packages:/p -v "$PWD":/b alpine tar czf /b/homemix-packages.tar.gz -C /p .`), or
people reinstall their add-ons after a restore:

```bash
docker compose -f compose.prod.yaml exec -T postgres pg_dumpall -U homeapps > homeapps-$(date +%F).sql
```

Restore into an empty stack with `docker compose -f compose.prod.yaml exec -T postgres psql -U homeapps < file.sql`
before the services start (`up -d postgres` first).

### Encrypted backups (with the chat plugin)

With the [chat plugin](plugin-chat.md) the backup holds family conversations, photos and films, so it is encrypted
before it leaves the machine (chat plan C14: no end-to-end encryption in 1.0, encrypted backups instead). Photos and
films are not in Postgres but in the Docker volume `homeapps_chat_media`; take both at the same moment, so messages
and their files match. The passphrase lives in a file only you can read, kept also somewhere outside the machine
(a password manager): without it the backup cannot be read, by anyone.

```bash
# Once: the passphrase file.
openssl rand -base64 32 > ~/.homeapps-backup-pass && chmod 600 ~/.homeapps-backup-pass

# Each backup (in HomeApps): every database, then the chat's files, each encrypted with AES-256.
day=$(date +%F)
docker compose -f compose.prod.yaml exec -T postgres pg_dumpall -U homeapps \
  | gzip | openssl enc -aes-256-cbc -pbkdf2 -iter 600000 -salt -pass file:$HOME/.homeapps-backup-pass \
  > homeapps-$day.sql.gz.enc
docker run --rm -v homeapps_chat_media:/media:ro alpine:3 tar -C /media -czf - . \
  | openssl enc -aes-256-cbc -pbkdf2 -iter 600000 -salt -pass file:$HOME/.homeapps-backup-pass \
  > homeapps-chat-media-$day.tar.gz.enc
```

Copy the two `.enc` files, `secrets/` and `.env` off the machine (`.env` and `secrets/` are not encrypted by this:
keep them where only you can reach). The chat's files need room: up to 10 GB per household by default.

Restore, into an empty stack (HomeApps' `postgres` up, the chat plugin stopped):

```bash
openssl enc -d -aes-256-cbc -pbkdf2 -iter 600000 -pass file:$HOME/.homeapps-backup-pass -in homeapps-<day>.sql.gz.enc \
  | gunzip | docker compose -f compose.prod.yaml exec -T postgres psql -U homeapps
docker volume create homeapps_chat_media
openssl enc -d -aes-256-cbc -pbkdf2 -iter 600000 -pass file:$HOME/.homeapps-backup-pass -in homeapps-chat-media-<day>.tar.gz.enc \
  | docker run --rm -i -v homeapps_chat_media:/media alpine:3 sh -c 'tar -C /media -xzf - && chown -R 1654 /media'
```

`1654` is the user the plugin's image runs as (`$APP_UID` of the .NET images). Then start the chat plugin as usual.

## Cloudflare Tunnel (no open ports)

When the provider puts the connection behind CGNAT (no public address of its own) or blocks ports 80 and 443,
Let's Encrypt over HTTP-01 cannot work and nobody can reach the machine from outside. A Cloudflare Tunnel turns that
around: `cloudflared` in the stack opens an outgoing connection to Cloudflare, Cloudflare answers at
`https://<HOMEAPPS_DOMAIN>` with its own certificate and sends each request down the tunnel.

```
browser ──HTTPS──▶ Cloudflare ──tunnel (outgoing from home)──▶ cloudflared ──HTTP──▶ traefik:8080 ──▶ services, web
```

`compose.tunnel.yaml` changes the production stack in three ways: Traefik publishes no ports, has no Let's Encrypt and
listens on plain HTTP at `traefik:8080` inside the Docker network (TLS ends at Cloudflare); the `cloudflared` service
runs the tunnel; Auth counts two proxies in `X-Forwarded-For` (`Auth__Proxy__ForwardLimit: 2`), so rate limits see
the caller's address that Cloudflare reports and not cloudflared's. Traefik keeps the `X-Forwarded-Proto: https` it gets
from the tunnel, so Auth still builds `https://` addresses (the token issuer, Google's redirect, the emails).

### Setting up the tunnel (once)

1. Add the domain to Cloudflare (Add a site, free plan) and switch its nameservers at the registrar to the two
   Cloudflare gives you. Wait until Cloudflare shows the domain as active. If an `A` record for the domain already
   exists (from an earlier setup with open ports), delete it: the tunnel creates its own record.
2. In the Cloudflare dashboard open **Zero Trust → Networks → Tunnels** and create a tunnel of type **Cloudflared**,
   named e.g. `homeapps`. On the "install connector" step choose Docker and copy the token: the long value after
   `--token` in the command shown. Do not run that command; the stack runs cloudflared itself.
3. Add a **public hostname** to the tunnel: subdomain empty, domain `home-apps.dev` (your `HOMEAPPS_DOMAIN`), path
   empty, service type **HTTP**, URL `traefik:8080`. Cloudflare adds the DNS record (a proxied `CNAME` to the tunnel).
4. In the domain's **SSL/TLS → Edge Certificates** turn on **Always Use HTTPS**.

Leave caching and the rest at their defaults: Cloudflare does not cache the API answers or the app's HTML, and the
app's files have content hashes in their names. If **Bot Fight Mode** is on, it can block calls that do not come
from a browser (scripts, later the JARVIS client); keep it off.

### Starting

```bash
cp .env.prod.example .env            # required values as above, plus CLOUDFLARE_TUNNEL_TOKEN
scripts/generate-signing-key.sh      # once
docker compose -f compose.prod.yaml -f compose.tunnel.yaml up -d --build
docker compose -f compose.prod.yaml -f compose.tunnel.yaml logs cloudflared   # "Registered tunnel connection" x4
```

With `COMPOSE_FILE=compose.prod.yaml:compose.tunnel.yaml` in `.env` the `-f` options can go. Within a minute the
tunnel shows as **Healthy** in the dashboard and `https://<HOMEAPPS_DOMAIN>` opens the app; the first Owner's
invitation is on its way as described in [First start](#first-start).

Google sign-in does not change: the redirect URI is still `https://<HOMEAPPS_DOMAIN>/auth/external/google/callback`.

### When something does not work

- **The dashboard shows the tunnel as down, the logs say the token is invalid:** `CLOUDFLARE_TUNNEL_TOKEN` is the value
  after `--token` only, without quotes or the rest of the command. A new token (Refresh token in the dashboard) needs
  `up -d` again.
- **Error 1033:** Cloudflare has the hostname but no running tunnel: `cloudflared` is not running or cannot reach the
  internet (it needs outgoing port 7844, UDP or TCP, or 443).
- **502 Bad Gateway:** the public hostname is not `HTTP` + `traefik:8080` (for example `HTTPS`), or Traefik is not up.
- **404 from Traefik:** the hostname in the request is not `HOMEAPPS_DOMAIN` (a typo in `.env`, or another hostname
  pointed at the same tunnel).

### Reaching it from the local network

All traffic goes through Cloudflare, also from inside the house. That is enough for the app; if Cloudflare is not
reachable, neither is HomeApps. Reaching it locally without Cloudflare (for example with Let's Encrypt over DNS-01 and
a local DNS entry) is a possible later addition and not part of this setup.
