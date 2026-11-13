# Writing and publishing a HomeMix add-on

A guide for authors, from an empty folder to an add-on in every HomeApps store. The contract itself (every field,
endpoint, limit and error) is [homemix.md](homemix.md); this page is the path through it. Why add-ons are built this
way: [ADR 0009](adr/0009-homemix-addons-in-a-sandbox.md).

## What an add-on is

A HomeMix add-on is a **static web app** (HTML, JavaScript, CSS) that a household Admin, or a person on their own
account, adds to HomeApps from the web app. It runs in a sandboxed frame with no access to HomeApps' token, cookies or
APIs. Everything it gets goes through the **bridge**, and only what the installer agreed to on the consent screen:

- its own data (JSON documents), shared by the household or private to each person;
- the names and avatars of the household's members;
- toasts and push notifications to the household's devices;
- `fetch` to the `https://` origins it declared, nothing else;
- optionally, tools for JARVIS.

It has no server code of its own. If you need a backend, a database you own or access to other services' data, write
a server plugin instead ([plugins.md](plugins.md)); those are added by the instance's Owner, not from the web app.

Good add-ons: a shared timer (the official Pomodoro), the weather for the household (the official Pogoda), a chore
rota, a shopping list for one household, a family quiz, a counter on the wall tablet.

## 1. Tools

You need Node 22.2 or newer and Git. The tooling is versioned with HomeApps:

| Package | |
|---|---|
| `create-homemix` | `npm create homemix@latest`: a new add-on from a template (`vue` or `vanilla`) |
| `homemix` | the CLI: `dev`, `check`, `pack`, `tool` |
| `@homemix/sdk` | the bridge client without a framework, the protocol and `homemix.json` types, `ToolContext` |
| `@homemix/vue` | `useHomeMix()`, `useCollection()`, `useCan()`, the HomeApps look (`theme.css`, a PrimeVue preset) |

```bash
npm create homemix@latest chores                 # Vue 3 + TypeScript + Vite
npm create homemix@latest chores -- --template=vanilla
cd chores && npm install
```

To work on the tooling itself, or before a version is on npm, build it from a HomeApps checkout as the official
add-ons do (`"@homemix/vue": "file:<HomeApps>/src/Web/homemix/vue"`): see [homemix.md, "Building one"](homemix.md#building-one-a3).

The template gives you:

```
chores/
  public/homemix.json            the manifest; Vite copies public/ into dist/
  src/App.vue, src/main.ts       the add-on
  index.html, vite.config.ts     base './': HomeApps serves the add-on at /homemix/run/<installation>/
  .github/workflows/release.yml  a GitHub release with addon.zip on every v* tag
```

| Script | |
|---|---|
| `npm run dev` | builds on every change and runs a fake HomeApps at http://localhost:5180 |
| `npm run check` | builds and runs the checks HomeApps makes when the add-on is added |
| `npm run pack` | builds, checks and writes `addon.zip` with its SHA-256 |

## 2. The manifest

`public/homemix.json` says what the add-on is and what it asks for. Start small and add only what you use: every
capability and every origin is shown to the installer, and adding one later makes every installation ask again.

```json
{
  "homemixVersion": 1,
  "id": "chores",
  "version": "1.0.0",
  "requires": { "homeApps": "2.3.0" },
  "name": { "pl": "Obowiązki", "en": "Chores" },
  "description": { "pl": "Kto dziś wynosi śmieci.", "en": "Who takes the bins out today." },
  "icon": "pi-check-square",
  "entry": "index.html",
  "pages": [{ "path": "", "title": { "pl": "Obowiązki", "en": "Chores" }, "menu": true }],
  "widgets": [
    { "id": "today", "title": { "pl": "Dziś", "en": "Today" }, "size": "Small", "places": ["Household", "Wall"] }
  ],
  "capabilities": ["storage.household", "household.members"],
  "network": [],
  "permissions": [{ "id": "rota.write", "minRole": "Admin" }]
}
```

- **`id`** is permanent: it names the add-on's data, its notifications and its JARVIS tools. Lowercase letters,
  digits and `-`, 2-31 characters. Pick one nobody else is likely to use; a few names are reserved (`households`,
  `accounts`, `jarvis`, `homemix`, `notifications`, `auth`) and so are the instance's plugin ids.
- **`version`** is semantic (`1.2.0`) and must match your Git tag (`v1.2.0`); the release workflow checks it.
- **`requires.homeApps`** is the oldest HomeApps your add-on works with. Raise it when you use something newer.
- **`targets`** (`Household`, `User`) is both by default. Leave it out unless the add-on makes no sense on a personal
  account, or in a household.
- **`icon`** is a [PrimeIcons](https://primevue.org/icons/) name.
- **`pages`** and **`widgets`**: see [step 3](#3-pages-widgets-and-the-look).
- **`capabilities`**: `storage.household`, `storage.user`, `household.members`, `notify`.
- **`network`**: the `https://` origins you `fetch` from, without paths or wildcards
  (`"https://api.open-meteo.com"`). The sandbox's CSP blocks every other address.
- **`permissions`**: named permissions with the lowest household role that has them (`Guest`, `Member`, `Admin`,
  `Owner`). On an account the person has all of them.

The full table and the checks are in [homemix.md, "The package"](homemix.md#the-package).

## 3. Pages, widgets and the look

HomeApps opens `entry` in a frame for each page and each widget:

- A **page** is shown at `/homes/{householdId}/mix/{id}/{path}`, or `/me/mix/{id}/{path}` on an account. Its path
  reaches the frame as `#/{path}` and as `context().page`. Pages with `"menu": true` become tabs of the household.
  `homemix.navigate(path)` moves to another of your pages.
- A **widget** stands on the household's first page (`Household`), on the wall tablet (`Wall`) or in "Moje
  aplikacje" (`Account`). It opens its own `entry`, or the main one with `context().widget` set
  (`{ id, place }`), so one bundle can render both. Keep widgets glanceable: the wall tablet is read from across a
  room.
- The frame grows with its content: `useHomeMix()` and `autoResize(homemix)` report the height. Without them, call
  `homemix.resize(height)`.

The host sends HomeApps' theme (light or dark, and its CSS variables such as `--p-primary-color`) at the start and
whenever it changes. `useHomeMix()` applies it to `:root`; `@homemix/vue/theme.css` styles plain elements and the
`hm-card`, `hm-button`, `hm-muted` classes to match. With PrimeVue 4, use `HomeAppsPreset` from
`@homemix/vue/primevue`.

Write the interface in Polish first: HomeApps is Polish, and `context().language` says which language the person uses.

## 4. The bridge

With Vue:

```ts
import { useCan, useCollection, useHomeMix } from '@homemix/vue'

const { homemix, context, ready } = useHomeMix()     // context: add-on, household { id, name, myRole }, user { id, name }
const chores = useCollection<{ who: string; what: string }>('chores')   // reactive, refreshed on data.changed
const canPlan = useCan('rota.write')

await chores.put('2026-11-13', { who: context.value!.user.id, what: 'śmieci' })
const people = await homemix.members()              // needs household.members
await homemix.toast('Zapisano', 'success')          // needs notify
```

Without a framework:

```js
import { autoResize, connect } from '@homemix/sdk'

const homemix = connect()
autoResize(homemix)
const ctx = await homemix.context()
const doc = await homemix.data.get('counter', 'clicks')        // null when there is none
await homemix.data.put('counter', 'clicks', (doc?.value ?? 0) + 1, { version: doc?.version })
homemix.on('data.changed', ({ scope, collection }) => { /* someone else wrote */ })
```

Every call answers a promise; a refused one rejects with an error that has `code` and `message`
(`homemix.forbidden`, `homemix.read_only`, `homemix.conflict`, ...). Show the message to the person rather than
failing silently. What the bridge will never give, whatever you ask for, is listed in
[homemix.md, "The bridge"](homemix.md#the-bridge-a3).

## 5. Data and who may change it

Data lives in collections of JSON documents that you name (`chores`, `settings`). Each document has a key, a value
of up to 1 MB, a `version`, `updatedBy` and `updatedAt`; one installation holds up to 10 MB and 1000 documents per
collection.

| Scope | Capability | Who reads | Who writes |
|---|---|---|---|
| `household` (default) | `storage.household` | every member, guests too | the role of your permission `{collection}.write` if you declared one (e.g. `rota.write` for `rota`), otherwise Member and up |
| `user` | `storage.user` | only that person | that person |

On an account installation both scopes belong to its owner. Every installation has its own data: the same add-on in
two households sees two separate sets.

- Pass the `version` you read when you write (`useCollection` does it for you). If someone changed the document in
  the meantime the write fails with `homemix.conflict`; read again and retry, or tell the person.
- Your data outlives versions of your add-on. A new version must read what an older one wrote: add fields with
  defaults, never rename a collection without moving its documents.
- Uninstalling keeps the data for 30 days; installing the same add-on again in the same place brings it back.

## 6. The network, notifications and JARVIS

**Fetching.** List each origin in `network`, then use `fetch` as usual. The installer sees "may send data to
api.example.com". No API keys: anything in your bundle is public. Prefer services that need none (Open-Meteo) or let
the household paste its own key into a settings document.

**Notifications.** With `notify`, `homemix.toast(message)` shows a toast in the web app and
`homemix.push({ title, body, path?, tag?, userIds? })` sends a notification to people's devices: by default to
everyone else in the household. A `tag` replaces your previous notification instead of stacking; `path` is the page a
click opens. At most 30 an hour per installation, and people can mute your add-on.

**JARVIS.** Set `"jarvis": true` and the installer can let JARVIS use your add-on. Then:

- **Declared tools** need no code: an `add`, `list`, `get`, `update` or `delete` on one collection, with the fields
  JARVIS fills in and the sentence it answers.

  ```json
  "tools": [
    { "name": "add_chore", "description": "Adds a chore to the household's rota.", "collection": "chores",
      "action": "add", "fields": { "what": { "type": "string", "required": true } }, "answer": "Dodano: {what}." },
    { "name": "chores", "description": "Lists the household's chores.", "collection": "chores", "action": "list" }
  ],
  "summary": { "collection": "chores", "description": "Today's chores." }
  ```

  JARVIS sees them as `chores_add_chore`, `chores_chores` and `chores_summary`. It asks the person before a `delete`.
- **Tool scripts** for anything with logic or a `fetch`: `"action": "script"` runs `public/tools/{name}.js` on the
  HomeMix server with a `ctx` that is the bridge again (`ctx.data`, `ctx.members()`, `ctx.fetch()` to your
  `network` only, `ctx.context()`, `ctx.can()`), within 5 s and 10 MB. Mark scripts that only read
  `"readOnly": true`; others need the person's confirmation each time.

  ```js
  // public/tools/today.js
  export default async function (args, ctx) {
    const today = (await ctx.data.list('chores')).filter((d) => d.key.startsWith(new Date().toISOString().slice(0, 10)))
    return today.length ? today.map((d) => d.value.what).join(', ') : 'Dziś nic.'
  }
  ```

The description is what JARVIS picks a tool by: say what it does and for whom, in one sentence. Details and limits:
[homemix.md, "JARVIS"](homemix.md#jarvis-a6-a7).

## 7. Trying it

`npm run dev` opens a fake HomeApps at http://localhost:5180: a household with members, a role switch (see what a
Guest sees), the page and its widgets, data in memory and the same sandbox headers as the server. If it works there it
works in HomeApps; if `fetch` is blocked, the origin is missing from `network`.

```bash
npm run check                                               # the server's checks, every problem at once
npm run build && npx homemix tool today '{}' --data=data.json --role=Member
```

`homemix tool` runs a tool script with the server's `ctx` and limits over data from `data.json`
(`{ "household": { "chores": { "2026-11-13-1": { "what": "śmieci" } } } }`).

In a real HomeApps, an Admin of a household opens its settings → **Dodaj dodatek** → **Plik zip** and uploads
`addon.zip`. A zip has no newer version to look for, so to try the next build, uninstall it and add the new zip: the
data comes back, it is kept for 30 days. Add-ons from a GitHub link update in place (step 8).

## 8. Releasing on GitHub

An add-on is published by a GitHub release with an `addon.zip` asset. HomeApps installs from the repository link
(`https://github.com/you/chores`): it takes the latest release's `addon.zip` and checks it against the SHA-256
GitHub shows for it.

1. Put the add-on in a **public** GitHub repository. Write a README: the store shows it (as plain text) next to
   your add-on, so say what it does, what it asks for and why.
2. Raise `version` in `public/homemix.json`, commit, then tag and push:

   ```bash
   git tag v1.0.0 && git push origin v1.0.0
   ```

3. The template's workflow (`.github/workflows/release.yml`) builds, checks that the tag matches `version`, packs and
   creates the release with `addon.zip`. Check the release page: the asset must be named exactly `addon.zip`.

Now anyone can add it from the link: household settings → **Dodaj dodatek** → **Link do GitHuba**.

**Updates.** Each new release is a new version: tag `v1.1.0` and the workflow does the rest. In HomeApps an Admin
uses "Sprawdź aktualizację" on the installation. An update that asks for nothing new applies at once; one that adds a
capability, a network origin or JARVIS shows the consent screen again, and until the Admin agrees the old version
keeps running. Never reuse a tag: a release's zip and hash must not change under people who installed it.

## 9. Getting into the store

Every HomeApps instance shows a store under **Dodaj dodatek**: the official add-ons that come with HomeApps, and the
add-ons listed in [`homemix/store/addons.json`](../homemix/store/addons.json) in the HomeApps repository. To be
listed, open a pull request there that adds your repository:

```json
{ "addons": [{ "repository": "https://github.com/you/chores" }] }
```

Nothing is uploaded: each instance reads your repository's description, latest release and README from GitHub and
refreshes them about once an hour, so new releases appear on their own. A repository without a release, without
`addon.zip` or without a SHA-256 is shown with a problem instead of an install button.

The pull request is reviewed for: a working release, an honest README, the smallest set of capabilities and origins
the add-on needs, and no tracking or third-party analytics. An instance's Owner can still block any add-on, by id or
by the hash of one version, and can point the store at another index (`HOMEMIX_STORE_INDEX`) or none.

## Checklist

- [ ] `id` chosen for good; `version` and the Git tag match.
- [ ] Only the capabilities, origins and permissions the add-on uses.
- [ ] Works for a Guest (read-only) and on a personal account, or `targets` says otherwise.
- [ ] Widgets readable on the wall tablet, light and dark.
- [ ] Reads the data older versions wrote.
- [ ] `npm run check` passes; JARVIS tools have clear descriptions and scripts that only read are `readOnly`.
- [ ] Public repository with a README, a release and its `addon.zip`.
- [ ] A pull request to `homemix/store/addons.json`, if you want it in the store.

## When something goes wrong

| You see | Why |
|---|---|
| `homemix.manifest_invalid` on adding | `homemix.json` breaks a rule; the message lists every problem. Run `npm run check`. |
| `homemix.needs_newer_homeapps` | `requires.homeApps` is newer than the instance. |
| `homemix.id_reserved` | the id is a core name or a plugin on that instance. |
| `homemix.download_failed`, `no_addon_zip`, `no_digest` | the latest release has no asset named `addon.zip`, or GitHub has no digest for it yet. |
| `homemix.blocked` | the instance's Owner blocked this add-on or version. |
| `homemix.uploads_disabled` | the instance's Owner turned add-ons off. |
| A blank frame | a path that is not relative: keep `base: './'` in Vite; check the browser console for CSP errors. |
| `fetch` fails with a CSP error | the origin is not in `network` (or has a path). |
| `homemix.capability_missing` | the call needs a capability you did not declare. |
| `homemix.read_only` | the person's role may not write that collection; check `{collection}.write`. |
| `homemix.conflict` | someone wrote first; read and write again. |
