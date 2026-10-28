# Web app (Vue)

The Vue app in `src/Web` is the main interface of HomeApps; JARVIS uses the same services as another client. The
plan, the screens and the decisions (Q1-Q11) are in `plans/features/frontend.md`. This page covers running it and
building on it.

## Run it locally

You need Node 22 or newer (`.nvmrc` says 24).

1. Start the infrastructure and the services as described in [auth.md](auth.md) and [accounts.md](accounts.md).
2. Start vite:

   ```bash
   cd src/Web
   npm install
   npm run dev
   ```

3. Open <https://app.localhost>. Traefik sends `/auth`, `/.well-known`, `/users` and `/admin/users` to the services
   and everything else to vite on port 5173 (`dynamic/dev-routes.yaml`), so the app and the services share one origin:
   no CORS, and the refresh cookie (`Path=/auth`) just works. Hot reload goes through Traefik as well.

`http://localhost:5173` opens the app too, but without the services behind it.

## Commands

| Command | What it does |
| --- | --- |
| `npm run dev` | Vite with hot reload on port 5173. |
| `npm run build` | Type check (`vue-tsc`) and the production build in `dist/`, with the service worker. |
| `npm run test` / `test:run` | Vitest (watch / once). |
| `npm run lint`, `npm run format` | ESLint with fixes, Prettier. |
| `npm run api:types` | Types from the services' OpenAPI documents (below). |

## Stack

Vue 3 with TypeScript and `<script setup>`, Vite, Vue Router (history mode), Pinia for the session only, Axios,
TanStack Query for everything that comes from a service, vue-i18n (Polish only for now), PrimeVue for complex
components (tables, dialogs, forms) and Tailwind for layout and our own components. `tailwindcss-primeui` turns the
PrimeVue theme into Tailwind classes (`bg-primary`, `text-muted-color`, `bg-emphasis`…), so both share one theme,
light and dark.

PrimeVue stays on **4.5** (with `@primeuix/themes` 2.0 and `primeicons` 7): these are MIT. From PrimeVue 5 (and
`@primeuix/themes` 3, `primeicons` 8) the libraries are under the PrimeUI License and show an "Invalid PrimeUI
License" notice without a license key. The versions are pinned with `~` in `package.json`; do not upgrade them
without deciding on the license first.

## Structure

```
src/Web/src/
  main.ts          wires the plugins
  app/             App.vue, router.ts, primevue.ts (theme), layouts/ (AppLayout, GuestLayout)
  api/             http.ts (Axios), problem.ts (service errors), errorMessage.ts, queryClient.ts, types/ (generated)
  features/<name>/ one folder per feature: its pages, api.ts and store, like the slices in the services
  shared/          components, composables and i18n used by more than one feature
  styles/main.css  Tailwind, tailwindcss-primeui, primeicons and the CSS layer order
```

A page lives in its feature. Something moves to `shared/` only when a second feature needs it.

## Talking to the services

- `api/http.ts` is the only Axios instance. It adds `Authorization: Bearer` from the session (F2); the access token
  lives only in memory. `CLIENT_ID` is `homeapps-web`, the browser client in `Auth:Clients`.
- Every error goes through `toProblem` (`api/problem.ts`). It reads both shapes the services send: an `AppError`
  with `code` (and `errors` when there are several) and a validation failure with messages per field
  (`fieldErrors`, field names in camelCase). `Retry-After` becomes `retryAfter`.
- `errorMessage` shows the Polish text for a code from `shared/i18n/pl.json` (`errors.<code>`), otherwise the
  service's own `detail`. A new error code in a service gets its text there.
- TanStack Query does not repeat a 4xx; network errors and 5xx get two more tries.

### Types from OpenAPI

With Auth on port 5100 and Accounts on 5200 (Rider, or the `services` profile), `npm run api:types` writes
`src/api/types/auth.ts` and `accounts.ts`. The files are committed, so a build never needs the services. Other
addresses: `AUTH_OPENAPI=… ACCOUNTS_OPENAPI=… npm run api:types`.

The documents describe the request bodies but not the responses yet, because the endpoints return `IResult` without
`Produces<T>()`. Until a service declares its responses, the response types are written by hand next to the
feature's `api.ts`.

## PWA and the wall tablet

The app installs on phones and tablets (`vite-plugin-pwa`, manifest in `vite.config.ts`, icons in `public/`). The
service worker caches only the app itself and updates it on its own. It never answers the services' paths with
the app (`servicePaths` in `vite.config.ts`), above all `/auth/external/google`, which must reach Auth. Add a new
service's path prefix there. Data is never served offline.

## Dark mode

`useColorScheme` (`shared/composables`) follows the system and remembers a choice made with the button in the
header (system, light, dark) in this browser. It puts `app-dark` on `<html>`; PrimeVue (`darkModeSelector`) and
Tailwind (`dark:` variant in `main.css`) both use that class.
