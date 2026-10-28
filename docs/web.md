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

3. Open <https://app.localhost>. Traefik sends `/auth`, `/.well-known`, `/users`, `/admin/users`, `/households`, `/invites` and
   `/admin/households` to the services
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

Forms are plain `reactive` objects: the services validate and send every problem at once, and `useSubmit`
(`shared/composables`) puts them under the fields (`FormField`). A client-side validation library comes only if a form
needs it.

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

- `api/http.ts` is the only Axios instance. It adds `Authorization: Bearer` from the session and refreshes on a 401
  (see Session). `CLIENT_ID` is `homeapps-web`, the browser client in `Auth:Clients`.
- Every error goes through `toProblem` (`api/problem.ts`). It reads both shapes the services send: an `AppError`
  with `code` (and `errors` when there are several) and a validation failure with messages per field
  (`fieldErrors`, field names in camelCase). `Retry-After` becomes `retryAfter`.
- `errorMessage` shows the Polish text for a code from `shared/i18n/pl.json` (`errors.<code>`), otherwise the
  service's own `detail`. A new error code in a service gets its text there.
- TanStack Query does not repeat a 4xx; network errors and 5xx get two more tries.

### Types from OpenAPI

With Auth on port 5100, Accounts on 5200 and Households on 5300 (Rider, or the `services` profile),
`npm run api:types` writes `src/api/types/auth.ts`, `accounts.ts` and `households.ts`. The files are committed, so a
build never needs the services. Other addresses: `AUTH_OPENAPI=… ACCOUNTS_OPENAPI=… HOUSEHOLDS_OPENAPI=… npm run api:types`.

Households declares its responses (`Produces<T>()`), so its types are all generated. Auth and Accounts describe only
the request bodies; their response types are written by hand next to the feature's `api.ts` until they declare them.

## Session

`features/auth/session.ts` is the session (frontend.md, section 4):

- On page load the app asks `POST /auth/refresh`; the HttpOnly cookie decides whether we are signed in. The router
  guards wait for it, and the app shows a spinner until then.
- The access token lives only in memory and is refreshed a minute before it expires, and once on a 401 (the request is
  then repeated). The sign-in endpoints themselves are never refreshed for (a 401 there is a wrong password).
- A refresh token works once and Auth has no grace window, so two tabs refreshing at once would end the session
  everywhere. Every refresh runs under one Web Lock (`homeapps.refresh`) shared by all tabs, and the new token goes to
  the other tabs over a `BroadcastChannel`, which also carries sign-out.
- Auth counts `/auth/refresh` against its anonymous limit (`Auth:RateLimits:PerMinute`, 10 per IP). A `429` is waited
  out with `Retry-After`, never treated as signed out.
- Pages inside `AppLayout` need a session (`meta.requiresAuth`), `meta.guestOnly` pages send a signed-in user to the
  dashboard and `meta.admin` needs the Admin or Owner role. The services check all of it again.
- A request with `{ ticket }` in its Axios config sends that ticket instead of the session token (reactivation).

### Sign-in pages

| Route | What it does |
| --- | --- |
| `/login`, `/register` | Password sign-in and open sign-up (`Auth:RegistrationOpen`); Google button. |
| `/signin/callback` | Where Google sign-in comes back (A-05). Reads the fragment and clears it at once. |
| `/signin/link` | "An account with this email exists": its password links Google (A-15). |
| `/signin/reactivate` | Bringing back an account you disabled (A-09, UC-05); waits out `409 reactivation.pending`. |

| `/invite/:token` | The invitation email (A-07): preview, then a password or Google with the invitation. |
| `/forgot-password`, `/reset-password?token=` | Forgotten password (A-08); the reset signs out every session. |
| `/confirm-password?token=`, `/confirm-email?token=` | Confirming a password or email change from the email. A button, never an automatic request, because mail scanners open links. |

The email paths are fixed by Auth's templates (`EmailTemplates.Link`); changing one means changing both. In
Development without an SMTP host Auth writes the emails, links included, to its log.

Tickets travel between pages in `history.state`, never in the URL. No page may live under `/auth`, `/users`,
`/admin/users`, `/households`, `/invites` or `/admin/households`: Traefik sends those paths to the services.

A signed-out visitor sent to sign in from a page (for example `/join/{code}`) comes back to it afterwards: the path is
in `?redirect=` and also in this tab's sessionStorage, because the query does not survive signing up, Google or an
invitation (`useAfterSignIn.ts`).

Right after sign-up Accounts needs a moment to create the account; `useMyAccount` (`features/account/api.ts`) retries
`404 user.pending` and the layout shows "preparing your account" meanwhile.

### My account

`/account` (`features/account`) has a tab per subject:

| Route | What it does |
| --- | --- |
| `/account` | Profile (PUT the whole profile, Q5) and disabling the account (D4); disabling signs out every tab. |
| `/account/security` | Login methods (link/unlink Google), password and email change. Both are confirmed from an email. Without a password Auth wants a Google sign-in from the last 5 minutes (`auth.reauthentication_required`). |
| `/account/sessions` | Signed-in devices (UserAgent only, D6), revoking one or all. |
| `/account/invitations` | Inviting people to the app (D7), resending and revoking. |
| `/account/history` | Own account history (date and kind only). |

Accounts validates the profile as a nested object, so its field errors arrive as `Profile.DisplayName`;
`useSubmit('profile.')` strips the prefix. Validation messages come from FluentValidation in English for now.
Auth answers a wrong current password with `auth.invalid_credentials`; the security page shows it under the field.

### Administration

`/admin/accounts` (`features/admin`) is shown to Admin and Owner (the `role` claim; the services check it again). The
front routes are `/admin/accounts` and `/admin/accounts/:id`, never `/admin/users` or `/admin/households`, which
Traefik sends to the services.

| Route | What it does |
| --- | --- |
| `/admin/accounts` | UC-10: search (name or email), status and role filters, sorting and paging. The filter lives in the URL. |
| `/admin/accounts/:id` | UC-11 to UC-17: details, profile edit, history (100 at a time, "older" loads more), disable/enable, password reset (Auth A-22), role (Auth A-23) and erase. |

`permissions()` in `features/admin/api.ts` mirrors R1, R3, R5 and R18 only to show the right buttons. A role change is
made in Auth; Accounts' copy follows from Auth's event a moment later, and the person gets the new role in their token
on the next refresh. Erasing asks for the account's email, which goes in the DELETE body.

### Households

`features/households`, after [households.md](households.md). The front routes are `/homes`, `/homes/:id/*`,
`/join/:code` and `/admin/homes`.

| Route | What it does |
| --- | --- |
| `/` | The dashboard: the current household with its people, the "you are the Owner now" banner, or how to start. |
| `/homes` | My households, creating one, joining by a typed code or a pasted link. |
| `/homes/:id/members` | Members (Member and up): roles, nicknames, a guest's access end, removing, handing over. Inactive accounts greyed out (D6). |
| `/homes/:id/invites` | Personal invites (Member or Guest, 30 minutes to 7 days) with copy and share, and the guest link (Owner, Admin). |
| `/homes/:id/settings` | My membership (nickname, leaving), name and description (Owner, Admin), archiving (Owner). A guest sees only this tab. |
| `/join/:code` | Personal invites and the guest link: what the code leads to, then joining. |
| `/admin/homes` | Every household for the server Admin and Owner, archiving and restoring. |

The current household (Q6) is `useCurrentHousehold()`: the one picked last (localStorage, per person), otherwise the
first. The header switcher shows once there are two. `householdPermissions()` in `api.ts` mirrors the roles in
households.md only to show the right buttons; Polish plurals ("1 osoba, 3 osoby, 5 osób") come from `polishPlural`
in `shared/i18n`.

On a phone the main menu moves to a tab bar at the bottom of the screen.

## PWA and the wall tablet

The app installs on phones and tablets (`vite-plugin-pwa`, manifest in `vite.config.ts`, icons in `public/`). The
service worker caches only the app itself and updates it on its own. It never answers the services' paths with
the app (`servicePaths` in `vite.config.ts`), above all `/auth/external/google`, which must reach Auth. Add a new
service's path prefix there. Data is never served offline.

`/wall` (`features/wall`) is the wall tablet (Q7, Q11): clock, date, the current household's active people and
placeholders for plugin tiles (weather, news), on a dark full-screen page without the app's menu. Open it from the
user menu ("Tryb tabletu") or, on an installed app, from the icon's long-press shortcut. The tablet signs in as an
ordinary user and stays signed in through the refresh token. The Screen Wake Lock API keeps the screen on; it needs
HTTPS, and where it is missing the page says so. The members are read again every 5 minutes.

## Dark mode

`useColorScheme` (`shared/composables`) follows the system and remembers a choice made with the button in the
header (system, light, dark) in this browser. It puts `app-dark` on `<html>`; PrimeVue (`darkModeSelector`) and
Tailwind (`dark:` variant in `main.css`) both use that class.
