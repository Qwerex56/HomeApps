# Changelog

Versions follow [Semantic Versioning](https://semver.org). Tags are `v<version>`.

## Unreleased: JARVIS voice

### Changed

- **Local development in one path:** `docs/development.md` covers the whole local setup: `scripts/dev-cert.sh`
  (mkcert certificate for `app.localhost` and `*.app.localhost`, so no warnings and no `--insecure` for the bridge;
  `certs/*.pem` are no longer in git), `.env.example` with `COMPOSE_FILE`/`COMPOSE_PROFILES` (plain
  `docker compose up -d`), every service container also on its Rider port, Auth getting the service clients' secrets
  from the same variables as JARVIS and Devices, TLS on every `websecure` router (plugins' own compose files work in
  dev), routes for Todo/Chat from Rider and `/homemix/hub`, JARVIS and HomeMix containers no longer pointing at
  Rider's ports, and vite reading `HOMEAPPS_PLUGINS` from `.env`.
- **The voice bridge moved out (S-2a):** `src/Jarvis/Jarvis.VoiceBridge` and its tests now live in their own
  repository, HomeApps.Desktop, with their history; HomeApps keeps only the server side (`/jarvis/voice`, Devices).
- **Server hardening for household devices (H-4c):**
  - `POST /devices/pair` is limited per client IP (`Devices:RateLimits:PairPerMinute`, 10; `429` with `Retry-After`).
    Devices now reads `X-Forwarded-For` like Auth (`Devices:Proxy:ForwardLimit`, `KnownNetworks`; the compose files set
    them, `compose.tunnel.yaml` counts two proxies).
  - `/auth/devices/token` has its own budget (`Auth:RateLimits:DeviceTokenPerMinute`, 600 per IP) instead of sharing the
    browsers' refresh limit, and answers `401 devices.device_unpaired` to the right secret of a removed or re-paired
    device (a wrong or old secret stays `devices.invalid_credentials`).
  - A device event's `at` older than 366 days counts as received now.
  - Guests get no values of a `geo` device (`values: {}`, `valuesAt: null`) over REST, the stream and JARVIS;
    health stays.
  - Satellite runs (also a turn lent by PIN) never list or call a person's computer tools. Computer action descriptions
    are quoted as data like household devices'. A device's "Pytaj" switch now works at once (no 5-minute cache).
  - Updating: JARVIS migrates its database on start (`AssistantRun.Satellite`).

### Added

- **Household JARVIS tab and live device stream (H-9):** the household "Urządzenia" tab is now **JARVIS**
  (`/homes/:id/devices`), combining household assistant settings, device pairing, live state/controls, event history,
  and HomeMix widget device selection.
  - Devices API & SSE stream: `DeviceDescriptorView` now includes `controls[]`, `sensors[]`, `events[]`, `sleepy`, and
    (for Admin+) `jarvis` text, always as non-null arrays; `status` stream frames carry `lowBattery`; new `changed`
    stream event (`paired`, `renamed`, `notes`, `jarvis`, `descriptor`, `removed`, `restored`) notifies open tabs of
    device lifecycle and configuration changes.
  - Web UI (`features/devices`): `AssistantCard` for household JARVIS settings (`enabled`, `name`, `accessLevel`,
    `toolSources` via `assistantFlags.ts`, `eventDays`, `locationEventDays`), devices grouped by purpose (`assistant`,
    `smart_home`) with a link to "Moje komputery", schema-driven `ControlWidget` (switch, slider committing on release,
    select, button, number, text; 20 s confirmation timeout without optimistic state), `SensorList`, lazy `DeviceEvents`
    with pagination, `DeviceAdminPanel`, and `useDeviceStream` (`fetch` + `ReadableStream` SSE with Bearer token, 15 min
    reconnect, 30 s polling fallback, and auto-closing the pairing dialog on `changed: paired`). Enforces the
    Guest / Member / Admin role matrix.
  - HomeMix widgets: device picker in `WidgetSettingsDialog.vue` (`PUT/DELETE .../widget-devices/{widgetId}`) for
    widgets matching household devices, and live `device.state` forwarding from the Devices SSE stream in `useMixHost.ts`
    when the installation has the `devices` capability. See `docs/devices.md`, `docs/web.md`, and
    `docs/homemix-authoring.md`.

- **Continuous listening for personal computers (H-8):** client-side continuous listening pipeline for personal
  computers running HomeApps.Desktop or the test Mac device emulator (`tools/mac-device-emulator/`).
  - Evaluates utterances locally using an "is this for me?" gate (`LocalUtteranceGate`) before any audio or text leaves
    the machine: wake words ("jarvis", "hej jarvis", etc.), microphone mute state, macOS screen lock state (`ioreg`),
    and self-voice echo suppression (`SpeechEchoSuppressor`).
  - Transcribed speech is sent over `/jarvis/voice?speech=local` as `say` frames only when accepted by the local gate.
  - Changes and dangerous actions requiring confirmation are confirmed **on screen only** (via web UI or CLI `/approve` / `/deny` calling
    `POST /jarvis/api/confirmations/{id}`); voice confirmations ("tak"/"nie") are strictly blocked on continuous listening to prevent unauthorized actions by bystanders.
  - Visible listening indicator (`[MIC: LISTENING]`, `[MIC: MUTED]`, `[MIC: PAUSED - SESSION LOCKED]`, `[MIC: PAUSED - JARVIS SPEAKING]`)
    and mute toggle (`/mute`, `/unmute`, `/toggle-mute`, or `--muted`). Fully emulated in `tools/mac-device-emulator/`.
    See `docs/jarvis.md` and `tools/mac-device-emulator/README.md`.

- **Computer tools and external MCP confirmations (H-7):** full confirmation flow for actions on a person's computer
  invoked by external MCP clients.
  - Calling a non-read-only computer tool that asks for confirmation creates an action in `NeedsConfirmation` within a
    closed run (`channel: "mcp"`, client ID reference), idempotent for duplicate pending calls, sends a `NotificationRequested`
    push notification, and answers `Not done yet: waits for the person's confirmation in HomeApps (action {id})`.
  - First-party API endpoints: `GET /jarvis/api/confirmations?pending=true` (lists pending confirmations for the user)
    and `POST /jarvis/api/confirmations/{id}` (approve or reject), restricted to first-party tokens (connected external apps get 403 `jarvis.forbidden`).
  - Read-only gateway MCP tool `jarvis_action_result(id)` for checking action status (`pending`, `approved` + result text,
    `denied`, or `expired`), accessible only to the action's owner.
  - Desktop contract documented: built-in computer actions declare `readOnly`/`confirm`, forwarded local MCP tools enforce
    `readOnly=false` and `confirm=true`, action aliases ≤ 20 characters. See `docs/devices.md`, `docs/device-capabilities.md`,
    and `docs/jarvis.md`.

- **Mac device emulator for tests (H-5):** `tools/mac-device-emulator/`, a standalone .NET 10 solution that emulates
  macOS-based devices over the `homeapps.device/1` protocol without physical hardware: an assistant Satellite (pairing,
  WebSocket `/devices/link`, `say`, volume/mute), a Powered smart home device (switch `lamp`, CPU load sensor, status
  heartbeats), a Battery sleepy sensor (periodic `/devices/report`, real Mac battery level via `pmset`), and a personal
  Computer (`purpose: computer`, bearer token, `say`, volume/mute). Secrets stored locally in
  `~/.homeapps/mac-devices.json` (`0600`). See `tools/mac-device-emulator/README.md` and `docs/devices-protocol.md`.
- **ESP-IDF firmware component (H-6):** `firmware/homeapps-device/` connects an ESP32-S3, C3 or C6 to HomeApps (setup
  access point, pairing, link, `/devices/report`, token handling, the protocol limits) and ships three examples in
  `firmware/examples/` (switch, battery weather station, presence tag), NVS encryption in their `sdkconfig.defaults`.
  `.github/workflows/firmware.yml` builds every example for the three targets. See
  docs/devices-protocol.md "Firmware: the ESP-IDF component (H-6)".
- **JARVIS hears and speaks (V0a):** `/jarvis/voice`, a WebSocket for listeners such as the Mac bridge: speech to text,
  the usual agent (channel `voice`, answers in the person's conversation), text to speech sentence by sentence, and
  "tak" / "nie" to confirm waiting actions. Engines through the OpenAI audio API (`JARVIS_VOICE_URL`), for tests
  Speaches on the Mac (`compose.voice.yaml`). Off without a URL. See docs/jarvis.md "Voice".
- **Voice bridge on the Mac (V0b):** `src/Jarvis/Jarvis.VoiceBridge`, a console app that listens for "Hey Jarvis"
  locally (openWakeWord through ONNX Runtime), streams the utterance until a short quiet, plays the answer, takes
  "tak" / "nie" for confirmations, and signs in through "Połączone aplikacje". See docs/jarvis.md "The Mac bridge".
- **Devices service (S-0):** a registry of each person's devices and their actions. Devices connect over
  `/devices/link` and announce what they can do; JARVIS gets the actions as tools `device_<device>_<action>`, with a
  confirmation for changes unless "Nie pytaj" is on. See docs/devices.md. New container `devices`, database
  `homeapps_devices`.
- **Bridge with its own speech and computer actions (S-0):** `--speech local` transcribes and speaks on the Mac and
  sends only text (`say` on `/jarvis/voice?speech=local`); `--actions <file>` lets JARVIS run commands from that file
  on the Mac.

## 2.4.1 (unreleased)

UI fixes: a simpler main bar, households in one place and a widget grid you arrange yourself.

Updating: HomeMix migrates its database on start (widget boards, quick links). The web app has a new dependency
(`grid-layout-plus`), so run `npm install` in `src/Web` for local development.

### Changed

- **Main bar (U1):** the store on the right; no household switcher and no "Moje konto" (it stays in the account menu);
  "Administracja" moved into the account menu.
- **Domy (U2):** the dashboard and the households list are one page: the household opened last across the whole width,
  the other households under it. `/homes` leads there.
- **Household (U3):** tabs on top ("Główna", "Widżety", "Ustawienia"), invites inside the settings, the household's
  apps in a bar on the left.
- **Widget grid (U4-U6):** 8 / 5 / 3 columns, one layout per household for its page and its wall tablet
  (`GET/PUT /homemix/api/households/{id}/boards/{place}`), arranged by drag and drop in "Widżety". "Główna" shows
  3×2 / 5×3 / 8×5, the wall 8×5 over the whole screen with "Wyśrodkuj" or "Wypełnij" per widget; the household card
  is a widget.
- **Apps (U7):** "Aplikacje" on the main bar with every add-on of my account (what "Moje aplikacje" in the account
  was; `/account/mix` leads there) and a quick links bar under the header with up to 8 of them, chosen with ✎
  (`PUT /homemix/api/me/pins`, `pin` in the installation).

## 2.4.0 (2026-11-16)

HomeMix 2.0: widgets you can choose, a store page with stats, RealTime and a framework for add-on authors.

Updating: no new settings in `.env`; HomeMix migrates its database on start (widget choices, the store's entries,
numbers and "domki"). The HomeMix tooling is 2.4.0 on npm (tag `homemix-v2.4.0`, with the new `@home-apps/css`), under the npm organization `home-apps`: `@home-apps/sdk`, `@home-apps/css`, `@home-apps/vue`, `@home-apps/homemix` (the CLI, still the `homemix` command) and `@home-apps/create-homemix` (`npm create @home-apps/homemix@latest`). The
official Pomodoro 1.2.1 and Pogoda 1.1.1 only take the new look; households see them as an update.

### Added

- **HomeMix widgets (B1):** `sizes` in homemix.json; per place (household page, wall tablet, "Moje aplikacje") admins
  pick the widget and its size or turn it off (`PUT/DELETE /homemix/api/installations/{id}/widgets/{place}`, ⚙ on the
  widget and "Widgety" in the add-on list). Each widget has a header with "Przejdź do aplikacji"; the bridge has
  `open(path?)` and `context().widget.size`. On the wall tablet an add-on opens in full at `/wall/mix/{id}`.

- **HomeMix RealTime (B2):** the SignalR hub `/homemix/hub`; the web app passes `data.changed` (also from other
  devices and JARVIS), `permissions.changed` (on a new household role), `installation.changed` and `notification`
  (also a toast) to the add-on frames; `@homemix/vue`'s `useCan()` follows a role change. Traefik routes `/homemix/hub`.

- **HomeMix store catalog (B3):** store entries in HomeMix's database. Anyone signed in submits a GitHub repository
  (`POST /homemix/api/store`, 5 a day); entries are keyed `{id}-{hash}`, so two people may list add-ons with the same
  id. `addons.json` becomes the quick listing of official entries; built-in add-ons win over the same id. Entries are
  read again from GitHub every hour (10 per 10-minute round); the author refreshes or removes an entry, the Owner
  hides one (`PUT /homemix/api/admin/store/{key}/hidden`). Installing a second add-on with the same id in one place
  answers `homemix.id_in_use`.

- **HomeMix store numbers (B4):** installs, updates and removals from the store are counted per add-on and source
  (never who); entries show active installations, installs in 30 days and "domki" (`PUT/DELETE
  /homemix/api/store/{key}/heart`). The store has tabs (`featured`, `popular`, `hearts`, `new`), search, filters and
  pages of 24; `GET /homemix/api/store/mine` gives the author's numbers, versions in use and a 30-day chart;
  `GET /homemix/api/admin/store` lists every entry, hidden ones too, for the Owner.

- **HomeMix store page (B5):** "Sklep" in the menu (`/store`) with tabs, search, filters and "Zgłoś dodatek"; an
  add-on's page (`/store/{listingKey}`) with its numbers, "domek", README rendered as safe Markdown and "Zainstaluj"
  (household or account); "Konto" with my entries' numbers and a 30-day chart; "Wpisy sklepu" for the Owner in
  "Administracja → Dodatki". The web app depends on `markdown-it`.

- **HomeMix framework (B6):** a new package `@homemix/css` with `hm-*` classes (layout, type, elements, form fields,
  states, widget sizes `hm-size-*`, the wall tablet `hm-wall`) on HomeApps' theme variables. `@homemix/vue` has
  `useWidget()` and building blocks (`HmWidget`, `HmStat`, `HmList`, `HmProgress`, `HmCountdown`, `HmEmpty`,
  `HmWallTile`, `HmPage`, `HmInput`, `HmDate`, `HmSelect`, `HmSwitch`); `@homemix/sdk` has `applyTheme()` and
  `applyLayout()`. The `vue` template is a widget and a page built from the blocks, `vanilla` uses `@homemix/css`;
  `homemix dev` picks a widget's place and size and shows the wall tablet.

### Changed

- **Web app:** "Dodaj dodatek" no longer lists the store; it links to the store page.

- **HomeMix:** `HomeMix:StoreCacheMinutes` is replaced by `HomeMix:StoreRefreshMinutes`; `StoreEntryView` has
  `listingKey`, `builtin` (added by id) and what the add-on asks for. `official` now also marks `addons.json` entries.
- **HomeMix:** the "działa w osobnej, zamkniętej ramce" caption is gone from widgets, it stays under add-on pages.

### Upgrading

- HomeMix gets the migrations `WidgetChoices` and `StoreListings` (applied at start). The store fills itself from
  `addons.json` within the first rounds after the update.

### Fixed

- **HomeMix tooling:** `homemix dev` did not start since B1 (a name was declared twice in the fake host).
- **Web app:** the consent screen showed a capability's key (`homemix.capability.storage.household`) instead of its
  description; keys with a dot are now read with brackets.

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

- **HomeMix:** an add-on's frame stayed empty in the web app: the host answered the bridge with the page's reactive
  state, which `postMessage` cannot clone, so `context()` failed ("Brak połączenia z serwerem"). Answers and events now
  go through JSON, like the add-on's calls.
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
