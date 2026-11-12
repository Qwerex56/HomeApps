# HomeMix: add-ons from the web app

A **HomeMix** is an add-on that a household or a person adds **from the web app**, without touching the server: a zip
with a `homemix.json` and static files (HTML, JS, CSS). It runs in a sandboxed frame, talks to HomeApps through a
narrow bridge, keeps its data in the core service HomeMix and can give JARVIS tools. Why it is built this way:
[ADR 0009](adr/0009-homemix-addons-in-a-sandbox.md).

> Status: the contract for HomeApps 2.3.0, built in stages A1-A7 (plan `plans/features/household-apps-upload.md`,
> order agreed with JARVIS in `plans/features/jarvis-v1.md` 10a). Sections say which stage brings them. Built so far:
> A1, the service with packages, installations and the sandbox; A2, the data; A3, the web app, the bridge, the
> tooling and Pomodoro; A4, the Owner's read-only "Pluginy serwera" view; A5, the store, Pogoda, `notify.push` and the
> Owner's blocks.

## Plugin or HomeMix

| | Plugin ([plugins.md](plugins.md)) | HomeMix |
|---|---|---|
| Added | with the server start: compose, `HOMEAPPS_PLUGINS`, by whoever runs the server | from the web app: a GitHub link or a zip |
| Applies to | the instance; switched on per household | one household (installed by Admin and up) or one account (by its owner) |
| Code | .NET service + Vue module in the app's origin | static front end in a sandboxed iframe; optional `tools/*.js` run in Jint |
| Data | its own database | its own space in the HomeMix service |
| Rights | the person's token | only what the bridge offers and the installer accepted |
| JARVIS | its MCP server | the HomeMix service's MCP server, from `homemix.json` or scripts |

Anything with its own backend, integration messages or instance-wide reach is a plugin.

## The package

A zip of at most 5 MB (`HomeMix:MaxPackageBytes`) with `homemix.json` at its root. Allowed files: `.html`, `.js`,
`.mjs`, `.css`, `.json`, `.svg`, `.png`, `.webp`, `.woff2`, `.md`. No paths with `..`, no links, no hidden files.

```json
{
  "homemixVersion": 1,
  "id": "pomodoro",
  "version": "1.0.0",
  "requires": { "homeApps": "2.3.0" },
  "name": { "pl": "Pomodoro", "en": "Pomodoro" },
  "description": { "pl": "Minutnik do skupionej pracy.", "en": "A timer for focused work." },
  "icon": "pi-stopwatch",
  "targets": ["Household", "User"],
  "entry": "index.html",
  "pages": [ { "path": "", "title": { "pl": "Pomodoro", "en": "Pomodoro" }, "menu": true } ],
  "widgets": [
    { "id": "timer", "title": { "pl": "Minutnik", "en": "Timer" }, "size": "Small",
      "places": ["Household", "Wall", "Account"], "entry": "widget.html" }
  ],
  "capabilities": ["storage.household", "storage.user", "household.members", "notify"],
  "network": [],
  "permissions": [ { "id": "sessions.write", "minRole": "Member" } ],
  "tools": [],
  "jarvis": true
}
```

| Field | Meaning |
|---|---|
| `homemixVersion` | Shape of this file. Grows only by optional fields; anything else is a new version. |
| `id` | `[a-z][a-z0-9-]{1,30}`. Also the prefix of its JARVIS tools. Must not be a plugin id from `HOMEAPPS_PLUGINS` or a core source name: `households`, `accounts`, `jarvis`, `homemix`, `notifications`, `auth`. |
| `requires.homeApps` | Oldest HomeApps version it works with. |
| `targets` | Optional, both by default: `Household`, `User`. Narrow it only when the add-on makes no sense elsewhere. |
| `entry`, `pages`, `widgets` | The page shown at `/homes/{householdId}/mix/{id}/{path}` (household) or `/me/mix/{id}/{path}` (account), and tiles with where they may stand (`Household`, `Wall`, `Account`). Every page opens `entry` with its path as `#/{path}` (and in `context().page`); pages with `menu` join the household's tabs. A widget opens its own `entry`, or the main one, with `context().widget`. |
| `capabilities` | What the bridge will offer (see below). Shown to the installer. |
| `network` | `https://` origins (no wildcards, no paths) the add-on may `fetch`. Shown to the installer as "may send data to". |
| `permissions` | Named permissions with the lowest household role that has them, as in plugins. |
| `tools` | Declared tools for JARVIS (A6). |
| `jarvis` | Whether it offers JARVIS anything; the installer agrees separately. |

`homemix check` (A3) and the server run the same validation and report every problem at once.

## Where packages come from (A1, A5)

- **A GitHub repository link** (`https://github.com/owner/repo`): HomeMix reads the latest release, takes its asset
  `addon.zip` and the SHA-256 digest GitHub gives for it, downloads and checks it. A tag or release can be named.
- **An uploaded zip.**
- **The store** (A5) is only an aggregator of GitHub, see below.
- **An official add-on** (A5) that came with HomeMix, by its id.

The server keeps only installed packages, one copy per SHA-256, in the `homemix-packages` volume.

Adding a package installs nothing; it answers what the add-on asks for, for the consent screen:

| Endpoint | Body | Answer |
|---|---|---|
| `POST /homemix/api/packages` | `multipart/form-data`, field `file` | the package: `sha256`, `id`, `version`, `name`, `targets`, `capabilities`, `network`, `jarvis`, ... |
| `POST /homemix/api/packages/github` | `{ "repository": "https://github.com/owner/repo", "tag": null }` | the same, with `repository` and `tag` |
| `POST /homemix/api/packages/official/{addonId}` | none | the same, with `official: true` (404 for an unknown id) |

Any signed-in person may add one; the same zip is one package. Errors: `homemix.manifest_invalid` (every problem at
once), `homemix.package_invalid`, `homemix.package_too_large`, `homemix.id_reserved`, `homemix.needs_newer_homeapps`,
`homemix.download_failed`, `homemix.blocked`, `homemix.uploads_disabled`.

### The store (A5)

`GET /homemix/api/store` lists, for anyone signed in:

1. **The official add-ons** (`official: true`, `addonId`): `homemix/official/*` built into zips inside the HomeMix
   image (`homemix/official/build.sh`, `HomeMix:OfficialPath`). They need no GitHub and update with HomeApps: the
   web app's "check for an update" adds the image's zip again. Today: Pomodoro and Pogoda.
2. **The index's add-ons** (`repository`): `addons.json` (`HomeMix:StoreIndexUrl`, by default
   [`homemix/store/addons.json`](../homemix/store/addons.json) in the HomeApps repository) lists GitHub repositories;
   publishing an add-on is a pull request that adds its link. For each, HomeMix reads from GitHub the repository's
   description, its latest release (tag, date, the SHA-256 of `addon.zip`) and the README, at most 15 repositories,
   cached for `HomeMix:StoreCacheMinutes`. A repository GitHub cannot describe stays listed with `problem`
   (`no_release`, `no_addon_zip`, `no_digest`, `unavailable`).

Nothing is uploaded to the store and the server keeps nothing but that cache. Choosing an entry adds it like a link
(`/packages/github`) or by id (`/packages/official/{id}`), then the consent screen asks where to install it. Blocked
add-ons (by id or hash) leave the list. The web app shows a README as plain text, never as HTML.

## Installing (A1)

| Action | Household | Account |
|---|---|---|
| Install, update, uninstall | household role Admin and up | the account's owner |
| Use, read household data | every member; a guest reads only, unless `permissions` says more | the owner |
| Write data | by `permissions` (Member and up by default) | the owner |

The consent screen lists the capabilities, the network origins ("may send data to: api.open-meteo.com") and whether
JARVIS may use it. An update that adds a capability or an origin asks again; otherwise it applies at once. Uninstalling
keeps the data for 30 days. An installation is an aggregate `Installation` with `Target` (`Household(id)` or
`User(id)`), the package, the granted capabilities and origins, and who did what.

| Endpoint | |
|---|---|
| `GET`, `POST /homemix/api/households/{householdId}/installations` | list (members) / install (Admin and up) |
| `GET`, `POST /homemix/api/me/installations` | the same on the caller's account |
| `GET`, `PUT`, `DELETE /homemix/api/installations/{id}` | one installation; update to another package of the same add-on; uninstall |

Install and update take `{ "sha256": "...", "consent": { "capabilities": [...], "network": [...], "jarvis": false } }`;
an update needs `consent` only when it answered 409 `homemix.consent_required`. Each answer has `runUrl`, `entry`,
`pages`, `widgets` and `permissions`, which is what the web app needs to show it. HomeMix asks Households for the
caller's role with the caller's own token. An assistant acting for a person (JARVIS) reads these but never adds,
installs, updates or removes (403 `assistant.not_allowed`).

The Owner of the instance can switch uploads off (`HomeMix:UploadsEnabled`, official add-ons included) and block an
add-on by id (every version) or by package SHA-256, in configuration (`HomeMix:Blocked`) or in the web app.

### The Owner's view (A5)

Administracja → **Dodatki** (`/admin/addons`, server role Owner only) lists every active installation on the instance
(where, version, from where, network) and the blocks. A block stops the add-on everywhere at once: its files, data and
notifications answer 404, it cannot be added or installed again, and it leaves the store; the data stays and comes
back when the block is lifted. Blocks from the configuration are shown but lifted only there.

| Endpoint (policy Owner) | |
|---|---|
| `GET /homemix/api/admin/installations` | every active installation, newest first, with `blocked` |
| `GET /homemix/api/admin/blocks` | configuration's (`fromConfiguration`) and the web app's, with reason and who and when |
| `PUT /homemix/api/admin/blocks/{idOrSha256}` `{ reason }` | blocks; again only changes the reason |
| `DELETE /homemix/api/admin/blocks/{idOrSha256}` | lifts; `409 homemix.block_in_configuration`, `404 homemix.block_not_found` |

## The sandbox (A1)

Files are served at `/homemix/run/{installationId}/{path}` with:

```
Content-Security-Policy: sandbox allow-scripts allow-forms allow-popups; default-src 'self' 'unsafe-inline' data: blob:;
  script-src 'self' 'unsafe-inline' blob:; connect-src <network, or 'none'>; form-action 'none'; base-uri 'none';
  frame-ancestors 'self'
Access-Control-Allow-Origin: *                 (the frame's origin is opaque, so its module scripts are cross-origin)
Cache-Control: no-cache                        (the path holds the installation; an update changes the files behind it)
X-Content-Type-Options: nosniff
Referrer-Policy: no-referrer
```

`/homemix/run/{installationId}/` serves the manifest's `entry`; `homemix.json` itself, other paths and removed or
blocked installations answer 404.

`sandbox` in the header gives an opaque origin even when the URL is opened in a tab, so no separate domain is needed.
The web app embeds it as `<iframe sandbox="allow-scripts allow-forms allow-popups">` with the caption "HomeMix:
Pomodoro działa w osobnej, zamkniętej ramce"; the frame cannot navigate the app (`allow-top-navigation` is off). An installation id is a UUID
and the files are only code, so the route is anonymous; all data needs a token.

## The bridge (A3)

Messages are plain objects over `postMessage`; the host checks `event.source` against the frame it created.

```js
// frame -> host
{ homemix: 1, id: 7, method: 'data.put', params: { scope: 'household', collection: 'sessions', key: 'a1', value: {...} } }
// host -> frame
{ homemix: 1, id: 7, result: { key: 'a1', version: 3 } }
{ homemix: 1, id: 7, error: { code: 'homemix.forbidden', message: '...' } }
// host -> frame, unasked
{ homemix: 1, event: 'theme', data: { dark: true, vars: { '--p-primary-color': '...' } } }
{ homemix: 1, event: 'data.changed', data: { scope: 'household', collection: 'sessions' } }
```

| Capability | Methods | |
|---|---|---|
| always | `context()`, `can(permission)`, `resize(height)`, `navigate(path)` | context: add-on, target, `household { id, name, myRole }`, `user { id, name }`, language, theme |
| `storage.household` | `data.list/get/put/delete` with `scope: 'household'` | the installation's shared data (for an account installation: the person's) |
| `storage.user` | the same with `scope: 'user'` | the person's private data inside a household installation |
| `household.members` | `members()` | names and avatars only |
| `notify` | `toast(...)`; `push({ title, body, userIds?, path?, tag? })` through Notifications (A5) | see below |

**Never**, whatever the consent: tokens, cookies or sessions; emails or account data beyond names and avatars; server
roles or the list of accounts; data of other households or other installations, the same add-on elsewhere included;
instance configuration (domain, Owner, versions, plugins); calls to core or plugin APIs on the person's behalf. A new
capability is a change to this page plus a test that nothing on this list leaks.

### Notifications (A5)

`push()` sends a Web Push notification through the Notifications service (`NotificationRequested`, docs/notifications.md)
on the person's behalf: `POST /homemix/api/installations/{id}/notify` `{ title, body, userIds, path, tag }` answers
`{ recipients }`.

- Without `userIds` it goes to everyone else in the household; on an account, to its owner. Named people must be in
  the household (`400 homemix.not_a_recipient`); at most 50.
- A click opens the add-on's page (`/homes/{id}/mix/{addon}/{path}` or `/me/mix/{addon}/{path}`); the same `tag`
  replaces the previous notification of this installation. People mute it like a plugin: by household or by the
  add-on's id (the source); the category is `addon`.
- `HomeMix:NotificationsPerHour` (30) per installation, then `429` with `Retry-After`. Not open to an assistant.

## Data (A2)

Documents of JSON per installation, in collections the add-on names (`[A-Za-z0-9][A-Za-z0-9._-]{0,63}`, the same for
keys). Two scopes: `household`, the installation's shared data (on an account: the person's), needs
`storage.household`; `user`, each person's private data in it, needs `storage.user`. Limits:
`HomeMix:MaxDocumentBytes` 1 MB, `HomeMix:MaxInstallationBytes` 10 MB (both scopes), `HomeMix:MaxDocumentsPerCollection`
1000. Each document has a `version`, `updatedBy` and `updatedAt`.

```http
GET    /homemix/api/installations/{id}/data/{collection}?scope=household|user       every document, by key
GET    /homemix/api/installations/{id}/data/{collection}/{key}?scope=...
PUT    /homemix/api/installations/{id}/data/{collection}/{key}?scope=...           { "value": <any JSON>, "version": 2 }
DELETE /homemix/api/installations/{id}/data/{collection}/{key}?scope=...&version=2
GET    /homemix/api/installations/{id}/me                                           { "role": "Member", "permissions": [...] }
```

`scope` defaults to `household`. `version` is the one read before: a stale one answers 409 `homemix.conflict`;
without it the write wins. Who may do what:

| | Household installation | Account installation |
|---|---|---|
| Read either scope | every member, guests too | the owner |
| Write shared data | the role of the add-on's permission `{collection}.write` (e.g. `{ "id": "plan.write", "minRole": "Admin" }`), Member and up when there is none; otherwise 403 `homemix.read_only` | the owner |
| Write own private data | every member, guests too | the owner |

`/me` answers the caller's household role and which of the add-on's `permissions` they have; the bridge's `can()`
answers from it. Other errors: `homemix.capability_missing`, `homemix.document_too_large`, `homemix.storage_full`,
`homemix.collection_full`, `homemix.document_not_found`.

The host calls these with the person's token; the frame never does. These endpoints and `/mcp` allow assistant tokens
(`.AllowAssistant()`): they are the add-on's data, not HomeApps'. Adding packages, installing, updating and
uninstalling refuse them; `AssistantTests` pins that list.

Uninstalling keeps the data: reinstalling the same add-on in the same place within 30 days brings it back; after that
an hourly job deletes it.

## JARVIS (A6, A7)

The HomeMix service serves MCP at `http://homemix:8080/mcp` inside the Docker network (empty from A1, so the JARVIS
gateway can list it as a source from J2 on). For JARVIS a HomeMix tool is an ordinary MCP tool:

- `tools/list` answers **per person**: only installations the delegated token can reach, only tools its role allows,
  and only where the installer agreed to JARVIS.
- Names carry the add-on id (`pomodoro_add_session`); the gateway adds no prefix of its own for this source. When the
  same add-on is in two households, the tool gets a `household` argument (a name or an id, `HouseholdLookup` from the
  kit).
- `delete` tools are `Destructive = true`, so JARVIS asks the person first. Answers are short plain text, as in
  [mcp.md](mcp.md).
- A summary for context is a tool `{id}_summary` with `ReadOnly = true`.

**Declared tools (A6)** work on collections without any add-on code on the server:

```json
"tools": [
  { "name": "add_session", "description": "Starts a pomodoro session.", "collection": "sessions", "action": "add",
    "fields": { "task": { "type": "string", "description": "What the person works on." },
                "minutes": { "type": "integer", "default": 25 } },
    "permission": "sessions.write", "answer": "Started \"{task}\" for {minutes} min." },
  { "name": "my_sessions", "description": "Lists today's sessions.", "collection": "sessions", "action": "list", "scope": "user" }
]
```

Actions: `add`, `list`, `get`, `update`, `delete`.

**Tool scripts (A7)** for anything that needs logic or a fetch: `tools/{name}.js` exports one function, run in Jint
with 2 s, 10 MB and a statement and recursion limit. `ctx` is the bridge's contract (`storage`, `members`, `context`,
`can`, and `fetch` only to the `network` origins), so the "never" list holds here too.

```js
// tools/forecast.js
export default async function ({ day }, ctx) {
  const { lat, lon } = await ctx.storage.household.get('settings', 'location')
  const r = await ctx.fetch(`https://api.open-meteo.com/v1/forecast?latitude=${lat}&longitude=${lon}&daily=temperature_2m_max`)
  return `Tomorrow up to ${(await r.json()).daily.temperature_2m_max[1]}°C.`
}
```

## In the web app (A3)

- A household's settings list its HomeMix add-ons under the server's plugins; Admins and up add one (GitHub link or
  zip, then the consent screen), check a GitHub one for a new version, and uninstall. An update that asks for nothing
  new applies at once; one that asks for more shows the consent screen again.
- The account menu has "Moje aplikacje" (`/account/mix`): add-ons on the account and their `Account` widgets.
- Widgets stand on the household's first page (`Household`) and on the wall tablet (`Wall`).
- The host side of the bridge is `src/Web/src/features/homemix/useMixHost.ts`; `members()` goes through Households,
  so a guest, who may not list members, gets an error there.
- "Dodaj dodatek" opens on the store (A5), with the GitHub link and the zip next to it.

## Building one (A3)

Optional, light tooling, versioned with HomeApps, in `src/Web/homemix` (npm workspaces; not on npm yet):

- `@homemix/sdk` (`sdk`): `connect()` (the bridge client without a framework), `autoResize()`, the protocol types, the
  `homemix.json` types and `checkManifest()`/`checkFiles()`, the same checks the server makes. `@homemix/sdk/host`
  (`serveFrame()`) is the host side, used by the web app and by `homemix dev`.
- `@homemix/vue` (`vue`): `useHomeMix()` (applies the host's theme and follows it), `useCollection('sessions')`
  (reactive, writes with the version it holds, refreshes on `data.changed`), `useCan()`; `theme.css` (the HomeApps
  look without PrimeVue) and `@homemix/vue/primevue` (`HomeAppsPreset` for PrimeVue 4). The add-on bundles its own Vue.
- `create-homemix` (`npm create homemix@latest my-addon`, templates `vue` and `vanilla`) and the `homemix` CLI:
  `dev` (a fake host at http://localhost:5180 with a household, roles, data in memory and the sandbox headers),
  `check`, `pack` (`addon.zip` + SHA-256). The template has a GitHub Actions workflow that attaches `addon.zip` to a
  release on a `v*` tag, which is what HomeMix installs from a repository link and what the store reads.
  `homemix tool` comes with the tool scripts (A6).

To work on them: `cd src/Web && npm ci`, then `cd homemix && npm install && npm run build`.

The official **Pomodoro** (`homemix/official/pomodoro`) is written with `@homemix/vue`: one timer shared by the
household, each person's sessions counted privately (`storage.user`), the lengths set by Admins (`settings.write`) and
the end of a focus pushed to everyone else (`notify`). The official **Pogoda** (`homemix/official/pogoda`, A5) shows
the weather now and for a week from Open-Meteo for a place an Admin picks (`place.write`); its only `network` are
Open-Meteo's two hosts. `homemix/official/build.sh <folder>` builds both into `<id>.zip`; the HomeMix image runs it,
and `HomeMix:OfficialPath` points HomeMix at the folder (from Rider: `homemix/official/dist`).

The bridge client sends parameters through JSON, so a Vue `ref` of an object can be passed to `data.put` as it is.

## Running it (A1)

The `homemix` service joins the compose files like the other core services: database `homeapps_homemix`, volume
`homemix-packages` (back it up with the database, see [deployment.md](deployment.md)), Traefik paths `/homemix/api`
and `/homemix/run` (not `/mcp`). From Rider it runs on `http://localhost:5600`.

| Setting | Default | |
|---|---|---|
| `HomeMix:UploadsEnabled` | `true` | `HOMEMIX_UPLOADS_ENABLED` in `.env` |
| `HomeMix:Blocked` | empty | add-on ids or package SHA-256s; blocked ones are not added and stop being served |
| `HomeMix:Plugins` | `HOMEAPPS_PLUGINS` | ids an add-on may not take |
| `HomeMix:MaxPackageBytes` | 5 MB | |
| `HomeMix:MaxDocumentBytes`, `MaxInstallationBytes`, `MaxDocumentsPerCollection` | 1 MB, 10 MB, 1000 | data limits |
| `HomeMix:PackagesPath` | `/data/packages` | the volume |
| `HomeMix:HouseholdsUrl` | `http://households:8080` | |
| `HomeMix:GitHubToken` | empty | `HOMEMIX_GITHUB_TOKEN`, only to raise GitHub's limit of 60 anonymous requests an hour |
| `HomeMix:StoreIndexUrl` | `homemix/store/addons.json` on GitHub | `HOMEMIX_STORE_INDEX`; empty: only official add-ons |
| `HomeMix:StoreCacheMinutes` | 60 | how long the store keeps what it read from GitHub |
| `HomeMix:OfficialPath` | `/app/official` | the official add-ons' zips in the image |
| `HomeMix:NotificationsPerHour` | 30 | `notify.push` per installation |

Tests: `tests/HomeMix`; the Api tests use `HOMEMIX_TEST_DATABASE` (a Postgres connection string) or Testcontainers.
