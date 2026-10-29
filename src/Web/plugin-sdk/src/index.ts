import { inject, type Component, type ComputedRef, type InjectionKey } from 'vue'
import type { AxiosInstance } from 'axios'
import type { HouseholdRole, PluginManifest } from './manifest'

export * from './manifest'

/** The household a plugin page or widget is shown in. */
export interface PluginHousehold {
  id: string
  name: string
  /** null for a server admin looking at a household they are not in. */
  myRole: HouseholdRole | null
}

/**
 * What the web app gives a plugin (docs/plugins.md, "UI"). Read it with useHost() inside the plugin's components.
 * The plugin never signs in by itself: http carries the user's token and refreshes it like the rest of the app.
 */
export interface HostApi {
  /** HomeApps' version, e.g. 2.1.0. */
  version: string
  manifest: PluginManifest
  /** Axios with the access token; paths are absolute on the HomeApps host, e.g. `${apiBase}/households/...`. */
  http: AxiosInstance
  /** manifest.endpoints.api, e.g. /plugins/todo/api. */
  apiBase: string
  /** The household on screen; null on pages outside a household. */
  household: ComputedRef<PluginHousehold | null>
  user: ComputedRef<{ id: string; email: string | null } | null>
  /** Whether the current role in this household has a permission from the manifest. */
  can: (permission: string) => boolean
  /** Translates `key` from the plugin's own messages (definePlugin({ messages })). */
  t: (key: string, values?: Record<string, unknown>) => string
  /** A toast like the rest of the app. */
  notify: (severity: 'success' | 'info' | 'warn' | 'error', summary: string) => void
  /** The text to show for a failed request: the service's translated code or detail. */
  errorMessage: (error: unknown) => string
}

/** Props of every widget component. */
export interface WidgetProps {
  householdId: string
  place: 'Household' | 'Wall' | 'Dashboard'
}

type Lazy = Component | (() => Promise<Component | { default: Component }>)

/** What the plugin's ui/entry.js exports by default. Keys of routes and widgets match the manifest. */
export interface PluginModule {
  id: string
  /** Pages by PluginRoute.path ('' for the main page). */
  routes?: Record<string, Lazy>
  /** Widgets by PluginWidget.id. They get WidgetProps. */
  widgets?: Record<string, Lazy>
  /** The plugin's section in the household's settings, when manifest.ui.settings is true. */
  settings?: Lazy
  /** Messages by locale; the web app is Polish only for now, so pl is required. */
  messages?: { pl: Record<string, unknown>; en?: Record<string, unknown> }
}

export function definePlugin(module: PluginModule): PluginModule {
  return module
}

/** Where the web app provides the HostApi. A string, so the app and every plugin agree on it. */
export const HOST_KEY = 'homeapps:plugin-host' as unknown as InjectionKey<HostApi>

export function useHost(): HostApi {
  const host = inject(HOST_KEY)
  if (!host) {
    throw new Error('useHost() works only inside a component the HomeApps web app renders for a plugin.')
  }
  return host
}
