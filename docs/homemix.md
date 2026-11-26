# HomeMix: add-ons from the web app

A **HomeMix** is an add-on that a household or a person adds **from the web app**, without touching the server: a zip
with a `homemix.json` and static files (HTML, JS, CSS). It runs in a sandboxed frame, talks to HomeApps through a
narrow bridge, keeps its data in the core service HomeMix and can give JARVIS tools. Why it is built this way:
[ADR 0009](adr/0009-homemix-addons-in-a-sandbox.md).

> Status: the contract for HomeApps 2.4.0 (HomeMix 2.0, stages B1-B7 of `plans/features/homemix-2.md`: widgets you
> choose, RealTime, the store with numbers and its page, the framework), first built in stages A1-A7 (plan `plans/features/household-apps-upload.md`,
> order agreed with JARVIS in `plans/features/jarvis-v1.md` 10a). Sections say which stage brings them. Built so far:
> A1, the service with packages, installations and the sandbox; A2, the data; A3, the web app, the bridge, the
> tooling and Pomodoro; A4, the Owner's read-only "Pluginy serwera" view; A5, the store, Pogoda, `notify.push` and the
> Owner's blocks; A6, declared tools for JARVIS; A7, tool scripts.

## Plugin or HomeMix

| | Plugin ([plugins.md](plugins.md)) | HomeMix |
|---|---|---|
| Added | with the server start: compose, `HOMEAPPS_PLUGINS`, by whoever runs the server | from the web app: a GitHub link or a zip |
| Applies to | the instance; switched on per household | one household (installed by Admin and up) or one account (by its owner) |
| Code | .NET service + Vue module in the app's origin | static front end in a sandboxed iframe; optional `tools/*.js` run in Jint |
| Data | its own database | its own space in the HomeMix service |
| Rights | the person's token | only what the bridge offers and the installer accepted |
| JARVIS | its MCP server | the HomeMix service's MCP server, from `homemix.json` or scripts |

**HomeMix first** (owner, 2026-11-22). A new app for a household is a HomeMix add-on. It is a plugin only when its
spec names at least one of these, and the spec says which:

1. It works on a clock without a person: reminders at a given time, a morning digest, periodic fetching.
2. It serves a public address, e.g. an ICS feed with a secret token in the URL.
3. It fetches from the network on the server outside a JARVIS tool run (e.g. importing ICS every 30 minutes).
4. It keeps binary files or a lot of data (photos and videos, ffmpeg, thousands of messages).
5. It enforces domain rules on the server that "who may write this collection" cannot express.
6. It reacts to core contracts other than devices and roles (e.g. `UserErased` with a 30-day retention).

Todo with the planner (1, 2, 3, 6) and Chat (4, 5) are plugins for these reasons. Weather, Pomodoro and Notes are
add-ons; Shopping is planned as one.

## The package

A zip of at most 5 MB (`HomeMix:MaxPackageBytes`) with `homemix.json` at its root. Allowed files: `.html`, `.js`,
`.mjs`, `.css`, `.json`, `.svg`, `.png`, `.webp`, `.woff2`, `.md`. No paths with `..`, no links, no hidden files.

```json
{
  "homemixVersion": 1,
  "id": "pomodoro",
  "version": "1.0.0",
  "requires": { "homeApps": "2.4.0" },
  "name": { "pl": "Pomodoro", "en": "Pomodoro" },
  "description": { "pl": "Minutnik do skupionej pracy.", "en": "A timer for focused work." },
  "icon": "pi-stopwatch",
  "targets": ["Household", "User"],
  "entry": "index.html",
  "pages": [ { "path": "", "title": { "pl": "Pomodoro", "en": "Pomodoro" }, "menu": true } ],
  "widgets": [
    { "id": "timer", "title": { "pl": "Minutnik", "en": "Timer" }, "size": "Small", "sizes": ["Small", "Medium"],
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
| `tools`, `summary` | Declared tools and a summary for JARVIS (A6, see "JARVIS"). |
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

### The store (A5, HomeMix 2.0 B3)

The store is this instance's own (homemix-2.md S1): its entries live in HomeMix's database. `GET /homemix/api/store`
lists, for anyone signed in, without blocked, hidden or removed entries:

1. **Built-in add-ons** (`builtin: true`, `official: true`, key `{id}-official`): `homemix/official/*` built into zips
   inside the HomeMix image (`homemix/official/build.sh`, `HomeMix:OfficialPath`). They need no GitHub and update
   with HomeApps. Today: Pomodoro and Pogoda.
2. **Official entries from `addons.json`** (`official: true`, key `{id}-official`): the quick listing of repositories
   the HomeApps team keeps (`HomeMix:StoreIndexUrl`, by default [`homemix/store/addons.json`](../homemix/store/addons.json)),
   one line each, no submitting. A built-in add-on with the same id wins.
3. **Submitted entries**, newest first: any signed-in person pastes a GitHub repository link
   (`POST /homemix/api/store { repository }`, `HomeMix:StoreSubmissionsPerDay` = 5 in 24 hours). HomeMix reads the
   latest release and its README, checks the package like any other (manifest, files, reserved ids, blocks) and
   lists it at once, with no review of rights for now.

**Keys.** An add-on's `id` need not be unique in the store: an entry is `{id}-{8 hex of SHA-256(submitter + repository)}`,
e.g. `chores-3f9a1c2e`. The same person with the same repository always gets the same entry, so submitting again reads
it again (and lists it again after they removed it); a new release is a new version of the entry, not a new one. Only
one add-on per id may stand in one household or account: installing another one with the same id (another repository)
answers `homemix.id_in_use`.

**Refreshing.** Every 10 minutes HomeMix lists new repositories of `addons.json` and reads again the entries not read
for `HomeMix:StoreRefreshMinutes` (60), at most 10 per round to stay within GitHub's anonymous limit. A release whose
SHA-256 it already has is not downloaded again. A release that cannot be used keeps the entry's last good version with
`problem`: `no_release`, `no_addon_zip`, `no_digest`, `unavailable`, `id_changed` or the package's error code. The
author can read it now (`POST /homemix/api/store/{key}/refresh`).

| Endpoint | Who |
|---|---|
| `GET /homemix/api/store?tab=&q=&target=&jarvis=&offline=&page=`, `GET /homemix/api/store/{key}` | anyone signed in (a hidden entry: its author and the Owner) |
| `POST /homemix/api/store` `{ repository }` | anyone signed in, not an assistant |
| `POST /homemix/api/store/{key}/refresh`, `DELETE /homemix/api/store/{key}` | the author or the Owner; official entries leave by a change to `addons.json` |
| `PUT`, `DELETE /homemix/api/store/{key}/heart` | anyone signed in, not an assistant |
| `GET /homemix/api/store/mine` | anyone signed in: their own entries with numbers |
| `GET /homemix/api/admin/store`, `PUT /homemix/api/admin/store/{key}/hidden` `{ hidden }` | the Owner |

Errors: `homemix.store_unusable` (with `problem`), `homemix.store_limit`, `homemix.not_author`,
`homemix.listing_official`, `homemix.listing_not_found`. Choosing an entry adds it like a link (`/packages/github`)
or, built in, by id (`/packages/official/{id}`), then the consent screen asks where to install it. The web app shows
a README as plain text, never as HTML.

### Numbers and "domki" (HomeMix 2.0, B4)

**Installs.** Every install, update and removal of a package that came from a repository (or a built-in add-on)
writes a `StoreEvents` row: add-on id, source (the repository, or `official` for a built-in one), version, household
or account, time. Never who or where. A zip upload has no source and is not counted. Numbers belong to the
add-on id and source, not to one entry: two entries of the same repository (official and submitted) show the same
numbers. Each entry shows `installs` (active installations now), `installs30` (installs in the last 30 days),
`hearts` and `hearted` (whether the caller gave one).

**Domki.** A heart (`StoreHearts`) is one person's like of one entry key, given once (`PUT .../heart` again changes
nothing) and taken back with `DELETE`. Built-in entries (`{id}-official`) take hearts too.

**Tabs.** `GET /homemix/api/store` without `tab` lists official entries first, then the newest. `tab=featured` only
official ones, `popular` by active installations (then installs in 30 days), `hearts` by hearts, `new` by submission
date (built-in ones have none). `q` searches id, name and description (both languages), `target=household|user`,
`jarvis=true` and `offline=true` (no `network`) narrow it. A page has 24 entries (`page` from 1); a shorter page is
the last one.

**"Konto".** `GET /homemix/api/store/mine` gives each of my listed or hidden entries with `counts` (active,
households, accounts, installs, installs30, updates, removals), `versions` (versions in use and how many
installations each) and `days` (installs a day for the last 30 days, oldest first). `GET /homemix/api/admin/store`
lists every listed and hidden entry for the Owner.

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
The web app embeds it as `<iframe sandbox="allow-scripts allow-forms allow-popups">`, under a page with the caption
"HomeMix: Pomodoro działa w osobnej, zamkniętej ramce" (a widget has its own header instead); the frame cannot
navigate the app (`allow-top-navigation` is off), only ask the bridge to (`navigate`, `open`). An installation id is a UUID
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
{ homemix: 1, event: 'data.changed', data: { scope: 'household', collection: 'sessions', key: 'a1', by: '<user id>' } }
```

Events (`homemix.on(name, listener)`): `theme`, `data.changed`, and from HomeMix 2.0 `permissions.changed`,
`installation.changed` and `notification`; see "RealTime" below.

| Capability | Methods | |
|---|---|---|
| always | `context()`, `can(permission)`, `resize(height)`, `navigate(path)`, `open(path?)` | context: add-on, target, `household { id, name, myRole }`, `user { id, name }`, language, theme, in a widget `widget { id, place, size }` |
| `storage.household` | `data.list/get/put/delete` with `scope: 'household'` | the installation's shared data (for an account installation: the person's) |
| `storage.user` | the same with `scope: 'user'` | the person's private data inside a household installation |
| `household.members` | `members()` | names and avatars only |
| `notify` | `toast(...)`; `push({ title, body, userIds?, path?, tag? })` through Notifications (A5) | see below |
| `devices` | `devices.list(filter?)`, `devices.state(id)`, `devices.onState(cb)` | the installation's household devices, read with the person's own rights (guests read); household installations only (H-4a) |
| `devices.control` | `devices.set(id, control, value)` | members and above (Devices refuses guests); needs `devices` too; only devices of the installation's household |

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

`PUT` may add `"expiresAt": "2026-12-31T00:00:00Z"` (or `null`): a document past that moment is hidden from every read
at once and deleted by the hourly purge job. In the bridge it is `data.put(key, value, { version, expiresAt })`. Use it
for things that stop mattering (a note with a due date), not for data that must be kept.

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

The HomeMix service serves MCP at `http://homemix:8080/mcp` inside the Docker network; the JARVIS gateway lists it as
a source with no prefix of its own. For JARVIS a HomeMix tool is an ordinary MCP tool:

- `tools/list` answers **per person**: the person's households (`GET /households/` with the delegated token) and their
  own account, the installations there that are active, not blocked, agreed to JARVIS by the installer
  (`jarvis` in the consent) and declare `tools` or a `summary`. A tool is listed only when the person's role passes its
  `permission` (`minRole`) and, for a change to shared data, the role that may write it (see "Data"). The account
  counts as Owner.
- Names carry the add-on id (`pomodoro_stats`). When the same add-on is in more than one place, every tool gets a
  `household` argument: a household's name or id, or "my account" (`NameLookup` from the kit). Without it the
  answer asks "Which household?" and names the places.
- Hints for the gateway's confirmation: `list`, `get` and the summary are `ReadOnly`; `add` and `update` are not
  destructive, so JARVIS does them without asking; `delete` is `Destructive`, so JARVIS asks the person first.
- A tool runs the same use cases as the bridge (`storage.*` with the person's token), so roles, limits and
  `data.changed` for open frames hold as for a person clicking.
- Answers are short plain text, as in [mcp.md](mcp.md): a change says the `answer` (or "Added {key}.", "Updated {key}.", "Deleted {key}."),
  a `list` gives the newest 30 entries as `- key: field: value, ...`, everything cut at 4000 characters. Wrong
  arguments come back as errors before anything is written.
- A summary for context is a tool `{id}_summary` (`ReadOnly`) listing one collection, from `summary`.

**Declared tools (A6)** work on collections without any add-on code on the server:

```json
"tools": [
  { "name": "add_session", "description": "Starts a pomodoro session.", "collection": "sessions", "action": "add",
    "fields": { "task": { "type": "string", "description": "What the person works on." },
                "minutes": { "type": "integer", "default": 25 } },
    "permission": "sessions.write", "answer": "Started \"{task}\" for {minutes} min." },
  { "name": "my_sessions", "description": "Lists today's sessions.", "collection": "sessions", "action": "list", "scope": "user" }
],
"summary": { "collection": "timer", "scope": "household", "description": "The shared timer." }
```

| Field | Rule |
|---|---|
| `name` | `[a-z][a-z0-9_]{0,39}`, not `summary`; `{id}_{name}` at most 64 characters. At most 20 tools. |
| `description` | Required, at most 500 characters: JARVIS picks the tool by it. |
| `collection`, `scope` | A collection name; `household` (default) or `user`, which needs the matching `storage.*` capability. |
| `action` | `add` (a new entry, key `yyyyMMddHHmmss-xxxxxx`), `list`, `get`, `update` (merges the fields into the entry), `delete`. `get`, `update` and `delete` take a `key` argument. |
| `fields` | Only `add` and `update`, at most 20: `type` `string`, `integer`, `number` or `boolean`, optional `description`, `required`, `default` (of that type). Not `key` or `household`. |
| `permission` | One of the add-on's `permissions`; the tool is listed only for roles that have it. |
| `answer` | At most 300 characters; `{key}` and `{field}` are filled in. |

`tools` and `summary` need `"jarvis": true`. `homemix check` makes the same checks.

**Tool scripts (A7)** for anything that needs logic or a fetch. A tool with `"action": "script"` runs
`tools/{name}.js` from the package (it must be there, `homemix check` and the upload say so) on the HomeMix server in
[Jint](https://github.com/sebastienros/jint): no files, network, timers or .NET, only `ctx`. The module's default
export takes the tool's fields and `ctx`; a string it returns is the answer, anything else goes to JARVIS as JSON.

```json
{ "name": "forecast", "description": "The weather where the household is.", "action": "script", "readOnly": true,
  "fields": { "days": { "type": "integer", "default": 2 } } }
```

```js
// public/tools/forecast.js (Vite copies public/ into the package)
export default async function ({ days }, ctx) {
  const place = (await ctx.data.get('place', 'current'))?.value
  if (!place) return 'No place yet.'
  const r = await ctx.fetch(`https://api.open-meteo.com/v1/forecast?latitude=${place.latitude}&longitude=${place.longitude}&daily=temperature_2m_max&forecast_days=${days}`)
  return `Up to ${(await r.json()).daily.temperature_2m_max.at(-1)}°C.`
}
```

`ctx` (type `ToolContext` in `@home-apps/sdk`) is the bridge's contract without the screen, answered by the same use
cases with the person's token, so roles, capabilities and the "never" list hold here too:

| `ctx.` | |
|---|---|
| `context()`, `can(permission)` | As in the frame; no theme, page or widget, `language` is `pl`. |
| `data.list/get/put/delete` | As `homemix.data`. In a `readOnly` tool `put` and `delete` fail with `homemix.read_only_tool`. |
| `members()` | With `household.members`; on an account just the person. |
| `fetch(url, { method, body, headers })` | Only the add-on's `network` origins (`homemix.network_denied` otherwise), GET or POST, no redirects or cookies, 3 s and 1 MB per answer, 5 per run. Answers `{ ok, status, headers, text(), json() }`. |

Errors come back as JavaScript errors with `code` and `message`, as in the frame. Limits per run: 5 s in all (fetches
included), 10 MB of memory, a million statements, 64 nested calls, 100 `ctx` calls, a 256 KB script. A script that
fails or hits a limit answers JARVIS `homemix.tool_script` with the reason; the server logs it. A `readOnly` script is
`ReadOnly` for the gateway; any other is `Destructive`, so JARVIS asks first.

`homemix tool <name> '{"days":3}' --data=data.json --role=Admin` runs a script on the built add-on with the same
`ctx` over data in memory (`{ "household": { "<collection>": { "<key>": value } }, "user": { … } }`), real `fetch` to
the `network` origins and the same limits. It mirrors the server's contract, not its isolation (`node:vm`).

## Triggers (H-4b)

An add-on's `triggers` (docs/homemix-authoring.md, "Triggers") react to household devices on the server. HomeMix listens to
`DeviceStateChanged` and `DeviceEventOccurred` (RabbitMQ, from Devices). The contracts carry the device's name, model,
roles and capabilities (`DeviceInfo`), so HomeMix matches without asking Devices again. For each active, unblocked
**household** installation that was granted `devices`, every trigger that fits the device and the field or event name runs its script in
Jint with the tool limits and `ctx.trigger`, `ctx.devices` (and no `data`, `members` or `fetch`).

**In whose name.** As the installation, within its consents, with the rights of the person who installed it (Admin or Owner
in the household). Nobody is logged in, so HomeMix calls Devices as itself (`X-Client-Id`/`X-Client-Secret`, `HomeMix:ClientId`/`ClientSecret`)
and names the installation, the add-on and the installer; Devices checks the installer is *still* Admin or Owner of that household
(`devices.forbidden` otherwise) and that the device belongs to it. JARVIS's per-device switch does not apply: add-ons are governed by
their consent (`devices.control` is checked by HomeMix before the call). See devices.md, "Add-on triggers".

**Safeguards** (household-jarvis-devices.md 2.6):

| | |
|---|---|
| Time and memory | the tool script limits: 5 s, 10 MB, 1 M statements, 100 `ctx` calls |
| Run limit | 30 runs a minute per installation (in memory); more are skipped and logged, they do not count as failures |
| Order | Runs of one household for the same kind of message (state change or event) are sequential (each consumer is partitioned by household); a state run and an event run of the same household, and runs of different households, may overlap |
| Loop guard | Devices marks a state change caused by an add-on's set with `ChangedBy = homemix:{installationId}` for 5 s; that installation's triggers skip it. Other add-ons still react |
| Failures | `TriggerStates` (installation, trigger id): `enabled`, consecutive failures, `lastRunAt`, `lastError`. 5 failures in a row turn the trigger off and send a `NotificationRequested` to the household's admins (source: the add-on id, category `trigger`); a success resets the count |

**Admins** (Admin and up; others get 403, a household they are not in 404):

| | |
|---|---|
| `GET /homemix/api/households/{householdId}/triggers` | every trigger of the household's add-ons with `enabled`, `consecutiveFailures`, `lastRunAt`, `lastError`, `disabledByFailures` |
| `PUT /homemix/api/installations/{id}/triggers/{triggerId}` `{ "enabled": true }` | turns it off or on (on clears the failures); 404 `homemix.trigger_not_found` |

The JARVIS tab in the web app shows these in H-9. A trigger without a row is on. Removing an installation deletes its rows.

## In the web app (A3)

- A household's settings list its HomeMix add-ons under the server's plugins; Admins and up add one (GitHub link or
  zip, then the consent screen), check a GitHub one for a new version, and uninstall. An update that asks for nothing
  new applies at once; one that asks for more shows the consent screen again.
- The account menu has "Moje aplikacje" (`/account/mix`): add-ons on the account and their `Account` widgets.
- Widgets stand on the household's first page (`Household`) and on the wall tablet (`Wall`). See "Widgets" below.
- The host side of the bridge is `src/Web/src/features/homemix/useMixHost.ts`; `members()` goes through Households,
  so a guest, who may not list members, gets an error there.
- The store is its own page, "Sklep" in the menu (`/store`, HomeMix 2.0 B5): tabs Wszystkie, Oficjalne, Najczęściej
  pobierane, Najwięcej domków, Nowe and Konto (`?tab=`), search, filters (dom/konto, JARVIS, bez sieci), "Pokaż
  więcej" for the next 24, and "Zgłoś dodatek" (a GitHub link). An add-on's page (`/store/{listingKey}`) shows its
  numbers, the "domek" button, what it asks for, its README as Markdown (`src/Web/src/shared/markdown.ts`: HTML is
  escaped, an image shows only its alt text, links only http(s) and mailto) and "Zainstaluj" with a choice of a
  household I administer or my account; its author refreshes or removes it there. "Konto" shows my entries' numbers
  with a 30-day chart. The Owner hides entries in "Administracja → Dodatki".
- "Dodaj dodatek" takes a GitHub link or a zip and links to the store.

## Widgets (HomeMix 2.0, B1)

A widget is a tile of an add-on in a place: `Household` (the household's first page), `Wall` (the wall tablet) or
`Account` ("Moje aplikacje"). A household installation may stand in `Household` and `Wall`, an account installation
in `Account`.

- **Sizes.** `size` is the default (`Small`, `Medium`, `Large`; `Medium` when left out) and `sizes` every size the
  widget can take (defaults to `[size]`, must include it). The size given is in `context().widget.size`; on the page
  `Small` is one column, `Medium` two, `Large` three.
- **Choosing.** Until someone chooses, a place shows every widget that lists it, in its default size. Admins and up of
  the household, or the account's owner, open ⚙ (top-left of the widget, or "Widgety" in the add-on list) and pick per
  place one widget and its size, or turn the add-on's widgets off there. "Wszystkie, jak w dodatku" goes back to the
  default.

  ```
  PUT    /homemix/api/installations/{id}/widgets/{place}   { "widget": "timer" | null, "size": "Medium", "order": 0 }
  DELETE /homemix/api/installations/{id}/widgets/{place}   back to the default
  ```

  Errors: `homemix.widget_place` (the installation cannot stand there), `homemix.widget_unknown` (no such widget for
  that place), `homemix.widget_size` (not one of its `sizes`), `homemix.forbidden` (not a manager). Both answer the
  installation with `widgetChoices`. An update keeps the choices: a widget that is gone is dropped, a size that is gone
  falls back to the widget's default. When a widget matches household devices (`devices` capability), the same ⚙ dialog
  lets admins pick which matching device the widget uses (`GET /homemix/api/installations/{id}/devices?widget=&state=true`,
  `PUT/DELETE /homemix/api/installations/{id}/widget-devices/{widgetId}` `{ "device": "<deviceId>" }`, or automatic selection).
- **Grid (UI 2.4.1).** A household's widgets stand on a grid of 8 columns, one layout per place (`Household`,
  `Wall`); narrower screens flow it into 5 or 3 columns. A widget is as wide as its size (1, 2 or 3 columns) and as
  high as the editor says. "Główna" and the wall tablet show only the first rows (3×2, 5×3, 8×5); the household's
  "Widżety" tab shows them all, and there admins and up arrange them. The household card (`core:household`) is a
  widget too. On the wall a widget is either centred in its own proportions (`Center`, the default) or fills its
  stretched cell (`Fill`). The `order` of a widget choice is not used on the grid.

  ```
  GET /homemix/api/households/{id}/boards/{Household|Wall}   any member; no items until someone arranged it
  PUT /homemix/api/households/{id}/boards/{Household|Wall}   { "items": [{ "key": "mix:{installationId}/{widgetId}", "x": 0, "y": 0, "w": 2, "h": 1, "fit": "Center" }] }
  ```

  Keys: `core:household`, `mix:{installationId}/{widgetId}`, `plugin:{pluginId}/{widgetId}`. Widgets missing from the
  layout go to the first free places; gone ones are skipped. Errors: `homemix.board_overlap`, `homemix.board_out_of_grid`
  (x + w over 8, w not 1-8, h not 1-12), `homemix.board_duplicate`, `homemix.board_fit`, `homemix.board_forbidden`
  (not an admin). Saving sends `board.changed` (`householdId`, `place`) to the household.
- **Quick links (UI 2.4.1).** Up to 8 add-ons of one's own account stand on the bar under the header, in order:
  `PUT /homemix/api/me/pins { "installations": [id, ...] }` pins those and unpins the rest; `pin` (0 first, or null)
  is in every installation. A household's add-ons and other people's cannot be pinned (`homemix.installation_not_found`).
- **Going to the app.** The frame is sandboxed, so a click inside it stays there. Each widget has a header with the
  add-on's name and "Przejdź do aplikacji"; the add-on itself can call `homemix.open(path?)` (e.g. on a click on its
  tile). On the wall tablet this opens the add-on in full at `/wall/mix/{id}/{path}` with a way back, elsewhere at its
  usual page.

## RealTime (HomeMix 2.0, B2)

The web app keeps one SignalR connection to `/homemix/hub` (the person's token as `?access_token=`, read only on that
path) while it is open. Every connection is in its person's group; it joins a household's group (`WatchHousehold`)
only as a member, guests included, checked with Households. Events name what changed, never data, and are sent only
after the change is saved. The host passes each to the frames of that installation:

| Event | Data in the frame | When | Who |
|---|---|---|---|
| `data.changed` | `{ scope, collection, key, by }` | a document written or deleted, on any device or by JARVIS | the household (account add-on: its owner); `user` data only its owner |
| `permissions.changed` | `{ role, permissions }` | `MemberRoleChanged` from Households (role `null` after `MemberLeft`) | the person |
| `installation.changed` | `{ change, version }` | `updated`, `removed`, `blocked`, `unblocked`, `widgets` (installs only refresh the list) | the household or the owner |
| `notification` | `{ title, body, path, tag }` | a `push()` (Web Push still goes out as before) | each recipient, also as a toast in the app |

- The host reloads a frame on a new version (its key includes it) and drops it when the list no longer has the
  installation. `@home-apps/vue`'s `useCan()` and context follow `permissions.changed` without a reload.
- A write in this tab reaches the other frames here at once; its echo over realtime within 2 s is not told twice.
- No new capabilities: an add-on only hears about what it could already read. Someone who leaves a household stays
  in its group until they reconnect; they only see keys, never values.
- `homemix dev` sends `permissions.changed` when you switch the role.

## Building one (A3)

A step-by-step guide for authors, from `npm create homemix` to the store: [homemix-authoring.md](homemix-authoring.md).

Optional, light tooling, versioned with HomeApps, in `src/Web/homemix` (npm workspaces), published to npm by
`.github/workflows/homemix-npm.yml` on a tag `homemix-v<version>`
([working-with-the-repo.md](working-with-the-repo.md#publishing-to-npm-the-first-time)):

- `@home-apps/sdk` (`sdk`): `connect()` (the bridge client without a framework), `autoResize()`, `applyTheme()` and
  `applyLayout()` (the `@home-apps/css` classes for where the add-on stands), the protocol types, the
  `homemix.json` types and `checkManifest()`/`checkFiles()`, the same checks the server makes. `@home-apps/sdk/host`
  (`serveFrame()`) is the host side, used by the web app and by `homemix dev`.
- `@home-apps/css` (`css`, HomeMix 2.0 B6): the `hm-*` classes in the HomeApps look on the host's theme variables,
  with or without a framework: layout, type, elements, form fields, states, widget sizes (`hm-size-*`) and the wall
  tablet (`hm-wall`).
- `@home-apps/vue` (`vue`): `useHomeMix()` (applies the host's theme and the layout classes and follows them),
  `useCollection('sessions')` (reactive, writes with the version it holds, refreshes on `data.changed`), `useCan()`,
  `useWidget()` (size, place, wall, `open()`) and the building blocks `HmWidget`, `HmStat`, `HmList`, `HmProgress`,
  `HmCountdown`, `HmEmpty`, `HmWallTile`, `HmPage`, `HmInput`, `HmDate`, `HmSelect`, `HmSwitch` (render functions,
  no compiler needed); `theme.css` (now an import of `@home-apps/css`) and `@home-apps/vue/primevue` (`HomeAppsPreset`
  for PrimeVue 4). The add-on bundles its own Vue.
- `@home-apps/create-homemix` (`npm create @home-apps/homemix@latest my-addon`, templates `vue` and `vanilla`) and the `@home-apps/homemix` CLI (command `homemix`):
  `dev` (a fake host at http://localhost:5180 with a household, roles, data in memory, the sandbox headers and, for
  a widget, its place and size, the wall tablet in its own dark frame),
  `check`, `pack` (`addon.zip` + SHA-256). The template has a GitHub Actions workflow that attaches `addon.zip` to a
  release on a `v*` tag, which is what HomeMix installs from a repository link and what the store reads.
  `homemix tool` runs a tool script (see "JARVIS"), and `dev` lists the add-on's JARVIS tools.

To work on them: `cd src/Web && npm ci`, then `cd homemix && npm install && npm run build`.

The official **Pomodoro** (`homemix/official/pomodoro`) is written with `@home-apps/vue`: one timer shared by the
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
| `HomeMix:DevicesUrl` | `http://devices:8080` | the devices bridge; from an IDE: `http://localhost:5800` |
| `HomeMix:ClientId`, `HomeMix:ClientSecret` | `homemix-service`, `HOMEMIX_CLIENT_SECRET` | HomeMix's service client for triggers: Devices (`Devices__Internal__HomeMix__ClientSecret`) and Households (`Households__Internal__HomeMix__ClientSecret`) list it; required in the prod compose, a development default in the dev one |
| `HomeMix:GitHubToken` | empty | `HOMEMIX_GITHUB_TOKEN`, only to raise GitHub's limit of 60 anonymous requests an hour |
| `HomeMix:StoreIndexUrl` | `homemix/store/addons.json` on GitHub | `HOMEMIX_STORE_INDEX`; empty: no official entries but the built-in ones |
| `HomeMix:StoreRefreshMinutes` | 60 | how often each store entry is read again from GitHub |
| `HomeMix:StoreSubmissionsPerDay` | 5 | store submissions per person in 24 hours |
| `HomeMix:OfficialPath` | `/app/official` | the official add-ons' zips in the image |
| `HomeMix:NotificationsPerHour` | 30 | `notify.push` per installation |

Tests: `tests/HomeMix`; the Api tests use `HOMEMIX_TEST_DATABASE` (a Postgres connection string) or Testcontainers.
