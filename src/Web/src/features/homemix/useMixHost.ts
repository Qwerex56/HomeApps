import { onBeforeUnmount, onMounted, type Ref } from 'vue'
import { useRouter } from 'vue-router'
import { useToast } from 'primevue/usetoast'
import { emit, serveFrame, type Handlers } from '@home-apps/sdk/host'
import type { Document, HomeMixContext, HouseholdRole, Scope, WidgetPlace, WidgetSize } from '@home-apps/sdk'
import { toProblem } from '@/api/problem'
import { errorMessage } from '@/api/errorMessage'
import { householdsApi } from '@/features/households/api'
import { homemixApi, type Installation } from './api'
import { currentTheme } from './theme'
import { localized } from '@/features/plugins/registry'
import { frameEvent } from './frameEvents'
import { useMixRealtime } from './realtime'

export interface MixHostOptions {
  installation: Installation
  household: { id: string; name: string; myRole: HouseholdRole } | null
  user: { id: string; name: string }
  page: string
  widget: { id: string; place: WidgetPlace; size: WidgetSize } | null
  /** The frame is on the wall tablet, so navigate and open stay there. */
  wall: boolean
  onResize: (height: number) => void
}

// Frames of the same installation hear about each other's writes (data.changed) at once, e.g. a widget and the page;
// the same write coming back over realtime shortly after is not told twice.
const frames = new Map<string, Set<() => Window | null | undefined>>()
const recentWrites = new Map<string, number>()
const ECHO_MS = 2000

const PAGE_PATH = /^[A-Za-z0-9._/-]{0,200}$/

/**
 * The host side of the bridge for one frame (docs/homemix.md, "The bridge"): answers its calls with the person's
 * token on the server side, never in the frame. Only what the "never" list allows leaves this file.
 */
export function useMixHost(frame: Ref<HTMLIFrameElement | undefined>, options: () => MixHostOptions) {
  const router = useRouter()
  const toast = useToast()
  const window_ = () => frame.value?.contentWindow
  let access: Promise<{ permissions: string[] }> | null = null
  let stop: (() => void) | null = null
  let themeObserver: MutationObserver | null = null

  function myAccess() {
    access ??= homemixApi.access(options().installation.id).catch((error) => {
      access = null
      throw error
    })
    return access
  }

  function changed(scope: Scope, collection: string, key: string) {
    const id = options().installation.id
    recentWrites.set(`${id}/${scope}/${collection}/${key}`, Date.now())
    for (const other of frames.get(id) ?? []) {
      emit(other(), 'data.changed', { scope, collection, key, by: options().user.id })
    }
  }

  // Realtime (docs/homemix.md, "RealTime"): writes from other devices and JARVIS, a new role, an update or removal,
  // a notification. The frame only hears about its own installation and household.
  useMixRealtime(
    () => options().household?.id,
    (event) => {
      const o = options()
      const routed = frameEvent(event, { installationId: o.installation.id, householdId: o.household?.id ?? null })
      if (!routed) return
      if (routed === 'permissions') {
        access = null
        void myAccess()
          .then(({ permissions }) =>
            emit(window_(), 'permissions.changed', {
              role: (event.data as { role: HouseholdRole | null }).role,
              permissions,
            }),
          )
          .catch(() => undefined)
        return
      }
      if (routed.name === 'data.changed') {
        const { scope, collection, key } = routed.data as { scope: Scope; collection: string; key?: string }
        const at = recentWrites.get(`${o.installation.id}/${scope}/${collection}/${key}`)
        if (at && Date.now() - at < ECHO_MS) return
      }
      emit(window_(), routed.name, routed.data)
    },
  )

  // navigate (a page to another) and open (a widget to its add-on) both show one of the add-on's pages: on the wall
  // tablet in full there, otherwise in the household or on my account.
  async function show(path: unknown) {
    const o = options()
    const clean = String(path ?? '').replace(/^\/+/, '')
    if (!PAGE_PATH.test(clean) || clean.includes('..')) return null
    const addon = o.installation.addonId
    await router.push(
      o.wall
        ? { name: 'wall-mix', params: { addon, path: clean } }
        : o.household
          ? { name: 'household-mix', params: { id: o.household.id, addon, path: clean } }
          : { name: 'my-mix', params: { addon, path: clean } },
    )
    return null
  }

  const handlers: Handlers = {
    async context(): Promise<HomeMixContext> {
      const o = options()
      return {
        addon: { id: o.installation.addonId, name: localized(o.installation.name), version: o.installation.version },
        installation: { id: o.installation.id, target: o.installation.targetKind },
        household: o.household,
        user: o.user,
        permissions: (await myAccess()).permissions,
        capabilities: o.installation.capabilities as HomeMixContext['capabilities'],
        language: 'pl',
        theme: currentTheme(),
        page: o.page,
        widget: o.widget,
      }
    },
    async can({ permission }) {
      return (await myAccess()).permissions.includes(permission)
    },
    resize({ height }) {
      options().onResize(Math.min(Math.max(Number(height) || 0, 40), 4000))
      return null
    },
    navigate: ({ path }) => show(path),
    open: ({ path }) => show(path),
    'data.list': ({ collection, scope }) =>
      homemixApi.data.list(options().installation.id, collection, scope) as Promise<Document[]>,
    'data.get': ({ collection, key, scope }) =>
      homemixApi.data.get(options().installation.id, collection, key, scope) as Promise<Document | null>,
    async 'data.put'({ collection, key, value, scope, version }) {
      const document = (await homemixApi.data.put(
        options().installation.id,
        collection,
        key,
        value,
        scope,
        version,
      )) as Document
      changed(scope ?? 'household', collection, key)
      return document
    },
    async 'data.delete'({ collection, key, scope, version }) {
      await homemixApi.data.delete(options().installation.id, collection, key, scope, version)
      changed(scope ?? 'household', collection, key)
      return null
    },
    async members() {
      const o = options()
      if (!o.household) return [{ id: o.user.id, name: o.user.name, avatarUrl: null, role: 'Owner' }]
      // Names and avatars only: never emails or anything else of the accounts.
      return (await householdsApi.members(o.household.id))
        .filter((m) => m.isActive && m.role)
        .map((m) => ({ id: m.userId, name: m.displayName, avatarUrl: m.avatarUrl, role: m.role! }))
    },
    toast({ message, severity }) {
      toast.add({
        severity: severity ?? 'info',
        summary: localized(options().installation.name),
        detail: String(message ?? '').slice(0, 300),
        life: 4000,
      })
      return null
    },
    'devices.list': (filter) => homemixApi.devices.list(options().installation.id, { ...filter }),
    'devices.state': ({ id }) => homemixApi.devices.state(options().installation.id, id),
    async 'devices.set'({ id, control, value }) {
      await homemixApi.devices.set(options().installation.id, id, control, value)
      return null
    },
    // TODO(H-9): 'device.state' events (Devices SSE, GET /devices/households/{id}/stream), opened only when `devices` is granted.
    'notify.push': (notification) => homemixApi.notify(options().installation.id, notification),
  }

  onMounted(() => {
    const id = options().installation.id
    const set = frames.get(id) ?? new Set()
    frames.set(id, set)
    set.add(window_)
    stop = serveFrame({
      frame: window_,
      capabilities: options().installation.capabilities,
      handlers,
      toError: (error) => {
        const problem = toProblem(error)
        return { code: problem.code ?? 'homemix.failed', message: problem.detail ?? errorMessage(problem) }
      },
    })
    themeObserver = new MutationObserver(() => emit(window_(), 'theme', currentTheme()))
    themeObserver.observe(document.documentElement, { attributes: true, attributeFilter: ['class'] })
  })

  onBeforeUnmount(() => {
    stop?.()
    themeObserver?.disconnect()
    frames.get(options().installation.id)?.delete(window_)
  })
}
