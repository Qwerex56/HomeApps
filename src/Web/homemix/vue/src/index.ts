// @homemix/vue: Vue 3 composables over @homemix/sdk (docs/homemix.md, "Building one"). The add-on bundles its own Vue.
import {
  computed,
  onScopeDispose,
  ref,
  shallowRef,
  toValue,
  type ComputedRef,
  type MaybeRefOrGetter,
  type Ref,
} from 'vue'
import {
  BridgeCallError,
  applyLayout,
  applyTheme,
  autoResize,
  connect,
  type Document,
  type HomeMix,
  type HomeMixContext,
  type Scope,
  type WidgetPlace,
  type WidgetSize,
} from '@homemix/sdk'

let instance: HomeMix | null = null
const context = shallowRef<HomeMixContext | null>(null)
const ready = ref(false)
let loading: Promise<HomeMixContext> | null = null

export { applyLayout, applyTheme }

/**
 * The bridge and where the add-on runs, shared by every component. Applies the theme and the @homemix/css layout
 * classes (hm-widget, hm-size-*, hm-wall), follows theme changes and keeps the frame as tall as the page (pass
 * autoResize: false to size it yourself).
 */
export function useHomeMix(options: { autoResize?: boolean } = {}) {
  if (!instance) {
    instance = connect()
    instance.on('theme', (theme) => {
      applyTheme(theme)
      if (context.value) context.value = { ...context.value, theme }
    })
    // A new role in the household (realtime): useCan() and the context follow without a reload.
    instance.on('permissions.changed', ({ role, permissions }) => {
      const current = context.value
      if (!current) return
      context.value = {
        ...current,
        permissions,
        household: current.household && role ? { ...current.household, myRole: role } : current.household,
      }
    })
    if (options.autoResize !== false) autoResize(instance)
  }
  loading ??= instance.context().then((value) => {
    context.value = value
    applyTheme(value.theme)
    applyLayout(value)
    ready.value = true
    return value
  })
  return { homemix: instance, context, ready, loaded: loading }
}

/** Whether the person has one of the add-on's permissions (homemix.json permissions); follows a role change at once. */
export function useCan(permission: MaybeRefOrGetter<string>) {
  const { context } = useHomeMix()
  return computed(() => context.value?.permissions.includes(toValue(permission)) ?? false)
}

export interface Collection<T> {
  items: Ref<Document<T>[]>
  loading: Ref<boolean>
  error: Ref<BridgeCallError | null>
  refresh: () => Promise<void>
  /** Creates or replaces; passes the version it holds, so someone else's newer write is not lost (409 homemix.conflict). */
  put: (key: string, value: T) => Promise<Document<T>>
  remove: (key: string) => Promise<void>
}

/** A collection of documents, read at once and again whenever another frame or person here changes it. */
export function useCollection<T = unknown>(name: string, scope: Scope = 'household'): Collection<T> {
  const { homemix } = useHomeMix()
  const items = ref<Document<T>[]>([]) as Ref<Document<T>[]>
  const loading = ref(true)
  const error = ref<BridgeCallError | null>(null)

  async function refresh() {
    try {
      items.value = await homemix.data.list<T>(name, scope)
      error.value = null
    } catch (e) {
      error.value = e as BridgeCallError
    } finally {
      loading.value = false
    }
  }

  async function put(key: string, value: T) {
    const version = items.value.find((d) => d.key === key)?.version
    const document = await homemix.data.put<T>(name, key, value, { scope, version })
    items.value = [...items.value.filter((d) => d.key !== key), document].sort((a, b) => a.key.localeCompare(b.key))
    return document
  }

  async function remove(key: string) {
    const version = items.value.find((d) => d.key === key)?.version
    await homemix.data.delete(name, key, { scope, version })
    items.value = items.value.filter((d) => d.key !== key)
  }

  const stop = homemix.on('data.changed', (change) => {
    if (change.collection === name && change.scope === scope) void refresh()
  })
  onScopeDispose(stop)
  void refresh()
  return { items, loading, error, refresh, put, remove }
}

export interface Widget {
  /** Small, Medium or Large: the size admins chose for this place (Medium on a page). */
  size: ComputedRef<WidgetSize>
  /** Household, Wall or Account; null on a page. */
  place: ComputedRef<WidgetPlace | null>
  /** Shown as a widget (not a page). */
  isWidget: ComputedRef<boolean>
  /** On the wall tablet: big type, read from afar. */
  isWall: ComputedRef<boolean>
  /** Opens the add-on (or one of its pages) in full where the widget stands. */
  open: (path?: string) => Promise<void>
}

/** Where a widget stands and how big it is (homemix-2.md 3.3); the building blocks read their size from it. */
export function useWidget(): Widget {
  const { homemix, context } = useHomeMix()
  const widget = computed(() => context.value?.widget ?? null)
  return {
    size: computed(() => widget.value?.size ?? 'Medium'),
    place: computed(() => widget.value?.place ?? null),
    isWidget: computed(() => !!widget.value),
    isWall: computed(() => widget.value?.place === 'Wall'),
    open: (path) => homemix.open(path),
  }
}

export * from './blocks.js'
export type { Document, HomeMixContext, Scope, WidgetPlace, WidgetSize } from '@homemix/sdk'
