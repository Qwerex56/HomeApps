import { onBeforeUnmount, onMounted, type Ref } from 'vue'
import { useRouter } from 'vue-router'
import { useToast } from 'primevue/usetoast'
import { emit, serveFrame, type Handlers } from '@homemix/sdk/host'
import type { Document, HomeMixContext, HouseholdRole, Scope, WidgetPlace } from '@homemix/sdk'
import { toProblem } from '@/api/problem'
import { errorMessage } from '@/api/errorMessage'
import { householdsApi } from '@/features/households/api'
import { homemixApi, type Installation } from './api'
import { currentTheme } from './theme'
import { localized } from '@/features/plugins/registry'

export interface MixHostOptions {
  installation: Installation
  household: { id: string; name: string; myRole: HouseholdRole } | null
  user: { id: string; name: string }
  page: string
  widget: { id: string; place: WidgetPlace } | null
  onResize: (height: number) => void
}

// Frames of the same installation hear about each other's writes (data.changed), e.g. a widget and the page.
const frames = new Map<string, Set<() => Window | null | undefined>>()

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

  function changed(scope: Scope, collection: string) {
    for (const other of frames.get(options().installation.id) ?? []) {
      emit(other(), 'data.changed', { scope, collection })
    }
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
    async navigate({ path }) {
      const o = options()
      const clean = String(path ?? '').replace(/^\/+/, '')
      if (!PAGE_PATH.test(clean) || clean.includes('..')) return null
      await router.push(
        o.household
          ? { name: 'household-mix', params: { id: o.household.id, addon: o.installation.addonId, path: clean } }
          : { name: 'my-mix', params: { addon: o.installation.addonId, path: clean } },
      )
      return null
    },
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
      changed(scope ?? 'household', collection)
      return document
    },
    async 'data.delete'({ collection, key, scope, version }) {
      await homemixApi.data.delete(options().installation.id, collection, key, scope, version)
      changed(scope ?? 'household', collection)
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
