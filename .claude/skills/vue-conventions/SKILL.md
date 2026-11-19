---
name: vue-conventions
description: HomeApps web app conventions (Vue 3.5 + TypeScript + Vite, TanStack Query + Axios, Pinia only for the session, PrimeVue 4.5 + Tailwind 4, vue-i18n in Polish). Use when writing, changing or reviewing code in src/Web.
---

# HomeApps Vue conventions

`src/Web` (see `docs/web.md`): Vue 3.5, TypeScript ~5.9 strict (`vue-tsc`), Vite 8, Vue Router 5 (history mode),
Pinia 4 **only for the session** (`features/auth/session.ts`), server data through **TanStack Query** (`@tanstack/vue-query`)
and **Axios** (`api/http.ts`), UI from **PrimeVue ~4.5** (MIT; never upgrade to 5, it is not MIT) + **Tailwind 4**
(`tailwindcss-primeui`), primeicons 7, **vue-i18n** with Polish only (`shared/i18n/pl.json`), PWA (`sw.ts`),
SignalR for realtime. Plugins/HomeMix live under `homemix/` and `plugin-sdk/`, with their own rules.

Copy the newest feature: `src/features/devices/`.

## 1. Folder layout: by feature

```
src/
  api/          http.ts (axios + token refresh), problem.ts, errorMessage.ts, queryClient.ts, types/<service>.ts (generated)
  app/          App.vue, router.ts, layouts/, servicePaths.ts
  features/<feature>/
    api.ts               types + <feature>Keys + <feature>Api
    pages/*Page.vue|*Tab.vue
    components/*.vue
    use*.ts              feature composables
    __tests__/*.spec.ts
  shared/       components/ (FormField, SafeMarkdown…), composables/ (useSubmit…), i18n/
```

## 2. The feature's api.ts

One file per feature with the types, the query keys and the calls. No calls from components through `http` directly.

```ts
import { http } from '@/api/http'

export interface Device { id: string; name: string; kind: 'Computer' | 'Satellite'; householdId: string | null; ... }

export const deviceKeys = {
  all: ['devices'] as const,
  mine: ['devices', 'mine'] as const,
  household: (id: string) => ['devices', 'household', id] as const,
}

export const devicesApi = {
  async household(householdId: string) {
    return (await http.get<Device[]>(`/devices/households/${householdId}`)).data
  },
  async addSatellite(householdId: string, name: string) {
    return (await http.post<PairingCode>(`/devices/households/${householdId}`, { name })).data
  },
}
```

- Request types come from the generated OpenAPI types when they exist:
  `type ProfileInput = components['schemas']['ProfileInput']` (`import type { components } from '@/api/types/accounts'`).
  Regenerate with `npm run api:types` against running services; the generated files are committed, never hand-edited.
  Response types are hand-written while an endpoint lacks `.Produces<T>()`.
- Same origin as the services (Traefik): relative URLs, no base URL, no CORS. `http.ts` adds the token and refreshes
  once on 401; don't add your own auth headers.
- No route path may start with a service prefix (`/auth`, `/users`, `/admin/users`, `/households`, `/invites`,
  `/admin/households`, `/plugins`, `/homemix`, `/devices` …): Traefik sends those to the services.

## 3. Components: `<script setup lang="ts">`

```vue
<script setup lang="ts">
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useQuery, useQueryClient } from '@tanstack/vue-query'
import Card from 'primevue/card'
import Message from 'primevue/message'
import Skeleton from 'primevue/skeleton'
import { useSubmit } from '@/shared/composables/useSubmit'
import { errorMessage } from '@/api/errorMessage'
import { toProblem } from '@/api/problem'
import { deviceKeys, devicesApi, type PairingCode } from '../api'

const props = defineProps<{ household: Household; can: HouseholdPermissions }>()
const emit = defineEmits<{ code: [value: PairingCode] }>()
const { t } = useI18n()
const queryClient = useQueryClient()
const id = computed(() => props.household.id)

const { data: devices, error, isPending } = useQuery({
  queryKey: computed(() => deviceKeys.household(id.value)),   // reactive key: refetches when the prop changes
  queryFn: () => devicesApi.household(id.value),
})
</script>
```

- Type-based `defineProps<…>()` / `defineEmits<…>()`; no runtime prop objects, no `any`.
- PrimeVue components imported per file (`import Button from 'primevue/button'`), icons `pi pi-*`.
- Layout and spacing with Tailwind utilities; colours from PrimeVue tokens (`text-muted-color`, `surface-*`,
  `dark:` variants). No scoped CSS unless Tailwind can't do it.
- Every visible text through `t('feature.key')` in `shared/i18n/pl.json`; no hard-coded Polish in templates.

## 4. Every list/detail shows four states

```vue
<Message v-if="error" severity="error" :closable="false">{{ errorMessage(toProblem(error)) }}</Message>
<Skeleton v-else-if="isPending" height="4rem" />
<p v-else-if="!devices?.length" class="text-muted-color">{{ t('devices.noSatellites') }}</p>
<ul v-else> ... </ul>
```

Error, loading, empty, data. Actions the user may not take are hidden (`v-if="can.edit"`), and the server still checks.

## 5. Forms: `useSubmit` + `FormField`

```ts
const form = useSubmit()
async function add() {
  const ok = await form.submit(async () => { code.value = await devicesApi.addSatellite(id.value, name.value.trim()) })
  if (!ok) return
  name.value = ''
  await queryClient.invalidateQueries({ queryKey: deviceKeys.all })
}
```

```vue
<form novalidate @submit.prevent="add">
  <Message v-if="form.formError.value" severity="error" :closable="false">{{ form.formError.value }}</Message>
  <FormField id="satelliteName" :label="t('devices.name')" :errors="form.fieldErrors.value.name">
    <template #default="{ invalid }"><InputText id="satelliteName" v-model="name" :invalid="invalid" /></template>
  </FormField>
  <Button type="submit" :loading="form.busy.value" :disabled="!name.trim()" />
</form>
```

- Plain `ref`s + server validation (no VeeValidate/Zod). `useSubmit` maps the ProblemDetails: field errors go under
  the fields, other errors into `formError`.
- After a change, invalidate the feature's keys (`deviceKeys.all`); don't patch the cache by hand unless needed.
- Disable the submit button while busy; destructive actions confirm through PrimeVue `useConfirm()`; results toast
  through `useToast()`.

## 6. Errors

`toProblem(error)` turns any Axios error into `Problem { status, code, detail, fieldErrors, retryAfter … }`;
`errorMessage(problem)` shows `t('errors.<code>')` when the key exists, else the service's detail. Every new backend
`AppError` code the user can hit gets a Polish message in `pl.json` under `errors.<service>.<code>`.

## 7. State

- Server data: TanStack Query only (default `staleTime` 30 s, retries only on network/5xx).
- Pinia only for app-wide client state; today that is the session. A new store needs a reason.
- Shared per-feature client state: a composable (`useCurrentHousehold`).
- Never store tokens in localStorage; the session keeps the access token in memory and the refresh token in a cookie.

## 8. Routing

Pages are lazy (`component: () => import('@/features/x/pages/XPage.vue')`) under the `AppLayout` route
(`meta.requiresAuth`). Use `meta.admin` / `meta.owner` for server-role pages; household roles are checked in the page
from `householdPermissions(role)`.

## 9. Leaks and reactivity

- Anything you start, you stop: `setInterval`, `addEventListener`, SignalR handlers and `BroadcastChannel`s are
  cleaned up in `onBeforeUnmount`/`onUnmounted` (see `features/homemix/useMixHost.ts`).
- Don't destructure `props` (loses reactivity); use `computed(() => props.x)` or `toRef`.
- `watch` with `{ immediate: true }` instead of calling the same code in `onMounted` and a watcher.
- Untrusted Markdown only through `SafeMarkdown.vue`; never `v-html` with server data.

## Before you finish

`npm run type-check`, `npx eslint .` (no `--fix` unless you mean it), `npm run test:run`, and `npm run build`
for a larger change. All in `src/Web`.
