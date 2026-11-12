# Changelog

Versions follow [Semantic Versioning](https://semver.org). Tags are `v<version>`.

## 2.3.0 (unreleased)

### Added

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
- **Web:** the consent screen `/connect` and "Connected apps" in the account settings.
- **HomeMix (A1):** the core service `src/HomeMix` for add-ons from the web app (docs/homemix.md, ADR 0009): packages
  from a zip or a GitHub release (`addon.zip` checked against GitHub's SHA-256), the `homemix.json` validation,
  installations in a household (Admin and up) or on an account with consent to capabilities and network origins, and
  the sandbox route `/homemix/run/{installationId}/` (CSP `sandbox`, `connect-src` from the consent). An empty MCP
  server at `/mcp` for the JARVIS gateway. Compose service `homemix`, database `homeapps_homemix`, volume
  `homemix-packages`.

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
