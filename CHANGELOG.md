# Changelog

Versions follow [Semantic Versioning](https://semver.org). Tags are `v<version>`.

## 2.3.0 (2026-11-12)

JARVIS and HomeMix. JARVIS is the home's assistant: one MCP gateway (`/jarvis/mcp`) for the tools of the core,
the plugins and the add-ons, an agent on the owner's Claude subscription (the JARVIS page, `@Jarvis` in the chat
plugin), and the same gateway for your own Claude Code or claude.ai over OAuth. HomeMix brings add-ons
that a household or a person installs from the web app, sandboxed, with a store and the official Pomodoro and Pogoda.

### Upgrading

- **`.env`:** add `JARVIS_CLIENT_SECRET` (required, `openssl rand -base64 32`) and, for JARVIS's own answers,
  `JARVIS_CLAUDE_CODE_OAUTH_TOKEN` from `claude setup-token`. See [docs/jarvis.md](docs/jarvis.md#production).
- Two new databases (`homeapps_homemix`, `homeapps_jarvis`) and the volume `homemix-packages` (back it up with the
  databases, see docs/deployment.md). Plugins built against the kit need the 2.3.0 packages (`scripts/pack.sh`).

### Added

- **Release (J4):** `compose.prod.yaml` runs `jarvis` (Traefik `/jarvis/api`, `/jarvis/mcp` and its metadata) and gives
  Auth the `jarvis-service` client and the gateway as the connected apps' resource; every image is 2.3.0.
  `scripts/smoke.sh` also installs Pogoda from the store and checks the gateway and an answer from JARVIS.

- **MCP:** the kit package `HomeApps.BuildingBlocks.Mcp` serves a service's use cases as MCP tools for JARVIS and other
  MCP clients (`AddHomeAppsMcp`, `MapHomeAppsMcp`, `ToolResult`), see [docs/mcp.md](docs/mcp.md). Households serves
  `my_households` and `household_members`, Accounts `my_profile`, at `/mcp` inside the Docker network.
- **Plugins:** a manifest with `Mcp` gets its MCP server at `/plugins/{id}/mcp`; the tools must match the manifest.
- **Auth:** `Service` clients and `POST /internal/delegation`, a 5-minute token to act for one person
  (`act`, `amr: delegated`), only on the internal network.
- **Kit:** the assistant guard: a token with `act` only reads in Auth, Accounts and Households (403
  `assistant.not_allowed` on every change); plugins let it change their data, but not switch the plugin.
- **Plugins:** `IHouseholdDirectory` with `FindHouseholdAsync` / `FindPersonAsync`, so a plugin's tools take a
  household or person by name or id; `NameLookup` and `ToolResult.Error(errors)` in the MCP package.
- **Testing:** `TestJwt.CreateDelegatedToken`, `CreateAssistantClient`, `McpTestClient`.
- **Auth: connected apps (OAuth 2.1).** Discovery (RFC 8414), dynamic registration (RFC 7591), authorization code with
  PKCE and `resource`, refresh with rotation, revocation (RFC 7009); tokens only for the configured resource
  (`Auth:ConnectedApps:Resources`, off when empty). Database migration `ConnectedApps`. See docs/auth.md.
- **JARVIS (J2a):** new service `src/Jarvis` with the MCP gateway `/jarvis/mcp`: the tools of Households, Accounts,
  HomeMix and the plugins in one server, per person, always with a delegated token; OAuth protected resource metadata
  for MCP clients. See [docs/jarvis.md](docs/jarvis.md). `TestJwt.CreateAppToken` for tests.
- **JARVIS (J2b):** the agent. `POST /jarvis/api/ask` and the chat's `AssistantInvoked` run Claude Code (owner's
  subscription, `JARVIS_CLAUDE_CODE_OAUTH_TOKEN`) with the gateway as its only tools; every call is logged to the run, and
  a tool not marked read-only or non-destructive waits 10 minutes for the person's confirmation
  (`POST /jarvis/api/confirmations/{id}`, `AssistantConfirmed`). Daily budget, retention 7/30 days, erase on
  `UserErased`. Database `homeapps_jarvis` (migration `Initial`). Contracts `Assistant*`. `RecordingEventBus` moved to
  the testing kit.
- **JARVIS (J2c):** the person's conversations (`/jarvis/api/conversations`, answers over server-sent events, migration
  `Conversations`) and the **JARVIS** page in the web app with the action card and confirmations.
- **Web:** the consent screen `/connect` and "Connected apps" in the account settings.
- **HomeMix (A1):** the core service `src/HomeMix` for add-ons from the web app (docs/homemix.md, ADR 0009): packages
  from a zip or a GitHub release (`addon.zip` checked against GitHub's SHA-256), the `homemix.json` validation,
  installations in a household (Admin and up) or on an account with consent to capabilities and network origins, and
  the sandbox route `/homemix/run/{installationId}/` (CSP `sandbox`, `connect-src` from the consent). An empty MCP
  server at `/mcp` for the JARVIS gateway. Compose service `homemix`, database `homeapps_homemix`, volume
  `homemix-packages`.
- **HomeMix (A2):** each installation's data: JSON documents in collections, shared (`storage.household`) or private
  per person (`storage.user`), with versions against lost updates, limits per document, collection and installation,
  write roles from the add-on's `{collection}.write` permission, and `/me` for the bridge's `can()`. An assistant may
  change this data; a test pins which changes it may make. Uninstalled add-ons' data comes back on a reinstall within
  30 days and is deleted after. Database migration `Documents`.
- **HomeMix (A3), web:** add-ons in a household's settings and in "Moje aplikacje" (add from GitHub or a zip, the
  consent screen, update, uninstall), their pages in the household's tabs and at `/me/mix/...`, widgets on the
  household page and the wall tablet, all in sandboxed frames with the bridge (`useMixHost`).
- **HomeMix (A3), tooling** in `src/Web/homemix`: `@homemix/sdk`, `@homemix/vue`, `create-homemix` and the `homemix`
  CLI (`dev` fake host, `check`, `pack`); the official add-on **Pomodoro** in `homemix/official/pomodoro`.
- **Web:** a Playwright test that an add-on's frame reads no storage, cookies, parent page or services
  (`e2e/homemix-sandbox.spec.ts`, needs no running stack).
- **Web (A4):** "Pluginy serwera" at `/admin/plugins`, for the Owner only and read-only: every plugin in
  `HOMEAPPS_PLUGINS` with its version, `requires.homeApps`, health (`endpoints.health`) and whether its UI loaded,
  listed plugins that are not running included. Plugins are still added and removed only on the server.
- **HomeMix (A5), store:** `GET /homemix/api/store` with the official add-ons built into the HomeMix image
  (`homemix/official/build.sh`) and the GitHub repositories listed in `homemix/store/addons.json` (README, release and
  SHA-256 read from GitHub, cached); "Dodaj dodatek" opens on it. The official **Pogoda** (Open-Meteo) joins Pomodoro.
- **HomeMix (A5), notifications:** `notify.push` in the bridge and `@homemix/sdk` (`push()`), sent through
  Notifications to people of the add-on's household, limited per installation; Pomodoro pushes the end of a focus.
- **HomeMix (A5), Owner:** Administracja → "Dodatki" lists every installation and blocks an add-on by id or SHA-256
  from the web app (`/homemix/api/admin/*`, Owner only), next to `HomeMix:Blocked`.
- **HomeMix (A6), JARVIS:** `tools` and `summary` in `homemix.json` become MCP tools `{id}_{name}` and `{id}_summary`
  at `http://homemix:8080/mcp`, per person: only where the installer agreed to JARVIS, only for roles the tool's
  permission and the data's write rule allow, with a `household` argument when the add-on is in more than one place.
  They run the bridge's data use cases; `delete` is destructive, the rest read-only or not destructive. `homemix check`
  checks them; Pomodoro 1.2.0 offers its timer (summary) and `pomodoro_stats`.
- **HomeMix (A7), tool scripts:** `"action": "script"` runs the add-on's `tools/{name}.js` in Jint with the bridge's
  contract as `ctx` (`context`, `can`, `data`, `members`, `fetch` only to the `network` origins) and limits (5 s,
  10 MB, statements, recursion, ctx calls); `readOnly` scripts may not write. `ToolContext` in `@homemix/sdk`,
  `homemix tool` in the CLI. Pogoda 1.1.0 gives JARVIS `pogoda_forecast`.

### Fixed

- `Auth.IntegrationTests` started without `AUTH_TEST_MAILPIT` again (the Mailpit address was read before it was set).

## 2.2.0 (unreleased)

### Added

- **Notifications:** a new service that sends Web Push notifications to browsers and installed apps, also when
  HomeApps is closed ([docs/notifications.md](docs/notifications.md)). Devices per person (at most 10), a preview
  switch for the lock screen, quiet hours in the person's time zone, muting a household or one plugin in it, a test
  notification. Expired subscriptions are removed; a disabled account loses its devices, an erased one everything.
  Other services ask for a notification with the `NotificationRequested` message.
- **Plugins:** `INotifier` sends a notification to chosen people, through the outbox with the command; the manifest
  lists the plugin's notification categories (`Notifications`, JSON `notifications`).
- **Web:** a "Powiadomienia" tab on the account page; the service worker shows notifications and opens their page on
  a click. Signing out forgets this browser's device first.
- **Plugins, realtime:** the manifest's optional `Realtime` path (`endpoints.realtime`) for a plugin's own hub; the
  hub's requests may carry the token as `?access_token=` (on that path only), and `IHouseholdAccess` passes it on.
  `IHouseholdAccess.GetMembershipAsync` gives the role with `JoinedAt` and `AccessUntil`. `useHost().accessToken()`
  in `@homeapps/plugin-sdk` for SignalR's `accessTokenFactory`.
- **Households:** `GET /households/{id}/members/names`, names and avatars for every member, guests included.
- `scripts/generate-vapid-keys.sh` makes the Web Push keys. The smoke test checks the Notifications service.

### Changed

- **Deployment:** `.env` needs `NOTIFICATIONS_VAPID_PUBLIC_KEY` and `NOTIFICATIONS_VAPID_PRIVATE_KEY`
  (`scripts/generate-vapid-keys.sh`); the stack has a fourth database, `homeapps_notifications`, created on start.
- The web app has its own service worker (`src/sw.ts`, vite-plugin-pwa `injectManifest`) instead of the generated one;
  it caches the same app shell.
- Images in `compose.prod.yaml` are tagged 2.2.0.

## 2.1.0

### Added

- **Plugins:** the kit for apps that live outside this repository ([docs/plugins.md](docs/plugins.md)). The plugin
  manifest (`GET /plugins/{id}/manifest`), checked at start; household roles asked from Households with a short cache
  that Households' events clear; permissions per household role checked before every plugin command and query;
  switching a plugin on per household; everything under `/plugins/{id}`; the built UI served at `/plugins/{id}/ui`.
- **Web:** plugin UIs inside the app: their pages as tabs of a household, their widgets on the household page and the
  wall tablet, and an "Aplikacje" card in a household's settings to switch them on and off. Plugins use the app's
  Vue and PrimeVue through `@homeapps/plugin-sdk`. The web image lists the instance's plugins from `HOMEAPPS_PLUGINS`.
- `scripts/pack.sh` packs the kit into a local feed for plugins: NuGet packages and the `@homeapps/plugin-sdk` tarball.
- **Deployment through a Cloudflare Tunnel** for a connection behind CGNAT: the overlay `compose.tunnel.yaml`
  (`cloudflared`, Traefik without published ports and without Let's Encrypt), `Auth:Proxy:ForwardLimit` so rate
  limits see the real caller behind two proxies ([docs/deployment.md](docs/deployment.md)).

### Changed

- `ACME_EMAIL` is optional.

## 2.0.0 (2026-10-29)

A rewrite from scratch. The services of the first version (`AccountManagement`, `Household.Api`, `TodoListService`)
and the `release/1.x` branches are history only; nothing of their data or API carries over.

### Added

- **Auth:** sign-in with a password or Google, RS256 access tokens with JWKS, refresh tokens with rotation, theft
  detection and a 10-second grace window, sessions per device, invitations (the first Owner from a bootstrap
  invitation), password reset, email change, server roles User, Admin and Owner, rate limits.
- **Accounts:** profiles created from Auth's events, account history, disabling and reactivation, administration,
  erasing on request.
- **Households:** households with Owner, Admin, Member and Guest roles, invites by code, a guest link, ownership
  transfer (automatic when the Owner is banned or erased), archiving, a member limit.
- **Web:** the Vue 3 app for all of the above, installable as a PWA, with a wall tablet mode.
- **Running it:** `compose.prod.yaml` with Traefik and Let's Encrypt, the signing key as a Docker secret, migrations
  on start, `scripts/smoke.sh` and a Playwright browser test for checking a release ([docs/deployment.md](docs/deployment.md)).
