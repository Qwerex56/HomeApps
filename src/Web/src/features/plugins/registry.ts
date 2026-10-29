import { computed, type MaybeRefOrGetter, toValue } from 'vue'
import { useQueries, useQuery } from '@tanstack/vue-query'
import { MANIFEST_VERSION, type PluginEnablement, type PluginManifest, type PluginModule } from '@homeapps/plugin-sdk'
import { http } from '@/api/http'
import { i18n } from '@/shared/i18n'

/** HomeApps' version (Directory.Build.props, mirrored in package.json). */
export const APP_VERSION = __APP_VERSION__

export interface LoadedPlugin {
  manifest: PluginManifest
  /** The UI module, or null for a plugin without UI or one whose module failed to load. */
  module: PluginModule | null
  /** Why the plugin cannot be shown, for the settings list. */
  problem: 'incompatible' | 'unavailable' | null
}

export const pluginKeys = {
  all: ['plugins'] as const,
  enablement: (pluginId: string, householdId: string) => ['plugins', pluginId, 'enablement', householdId] as const,
}

/** Compares a.b.c versions; a pre-release counts as its release. */
export function versionAtLeast(version: string, minimum: string) {
  const parse = (v: string) =>
    v
      .split('-')[0]!
      .split('.')
      .map((n) => Number(n) || 0)
  const [a, b] = [parse(version), parse(minimum)]
  for (let i = 0; i < 3; i++) {
    if ((a[i] ?? 0) !== (b[i] ?? 0)) return (a[i] ?? 0) > (b[i] ?? 0)
  }
  return true
}

/** The text in the app's language. */
export function localized(text: { pl: string; en: string } | null | undefined) {
  return text?.pl ?? ''
}

// Plain imports of a URL; a seam so tests can hand in modules.
let importModule = (url: string): Promise<{ default: PluginModule }> => import(/* @vite-ignore */ url)

export function setPluginImporter(importer: typeof importModule) {
  importModule = importer
}

function addStylesheet(href: string) {
  if (document.querySelector(`link[data-plugin-style="${href}"]`)) return
  const link = document.createElement('link')
  link.rel = 'stylesheet'
  link.href = href
  link.dataset.pluginStyle = href
  document.head.append(link)
}

async function loadPlugin(id: string): Promise<LoadedPlugin | null> {
  let manifest: PluginManifest
  try {
    manifest = (await http.get<PluginManifest>(`/plugins/${id}/manifest`)).data
  } catch {
    // Listed but not running: leave it out instead of breaking the app.
    return null
  }

  if (manifest.manifestVersion !== MANIFEST_VERSION || !versionAtLeast(APP_VERSION, manifest.requires.homeApps)) {
    return { manifest, module: null, problem: 'incompatible' }
  }
  if (!manifest.ui) {
    return { manifest, module: null, problem: null }
  }

  try {
    const module = (await importModule(manifest.ui.entry)).default
    addStylesheet(manifest.ui.style)
    if (module.messages?.pl) {
      i18n.global.mergeLocaleMessage('pl', { plugins: { [manifest.id]: module.messages.pl } } as never)
    }
    return { manifest, module, problem: null }
  } catch (error) {
    console.error(`Plugin ${id}: the UI module did not load.`, error)
    return { manifest, module: null, problem: 'unavailable' }
  }
}

/** The instance's plugins (/plugins.json, from HOMEAPPS_PLUGINS) with their manifests and UI modules. */
export async function loadPlugins(): Promise<LoadedPlugin[]> {
  let ids: string[] = []
  try {
    const response = await fetch('/plugins.json', { headers: { Accept: 'application/json' } })
    ids = response.ok ? ((await response.json()) as string[]) : []
  } catch {
    ids = []
  }
  const loaded = await Promise.all([...new Set(ids)].map(loadPlugin))
  return loaded.filter((p): p is LoadedPlugin => p !== null)
}

/** Loaded once per page load: a new plugin version shows after a reload, like a new app version. */
export function usePlugins() {
  return useQuery({ queryKey: pluginKeys.all, queryFn: loadPlugins, staleTime: Infinity, gcTime: Infinity })
}

export const pluginsApi = {
  async enablement(manifest: PluginManifest, householdId: string) {
    return (await http.get<PluginEnablement>(enablementPath(manifest, householdId))).data
  },
  async setEnabled(manifest: PluginManifest, householdId: string, enabled: boolean) {
    return (await http.put<PluginEnablement>(enablementPath(manifest, householdId), { enabled })).data
  },
}

function enablementPath(manifest: PluginManifest, householdId: string) {
  return manifest.enablement!.path.replace('{householdId}', householdId)
}

/**
 * The plugins that are on in a household, with their state. A plugin without a switch (enablement null) is always
 * on; one whose switch cannot be read counts as off.
 */
export function useHouseholdPlugins(householdId: MaybeRefOrGetter<string | null | undefined>) {
  const { data: plugins, isPending } = usePlugins()
  const switchable = computed(() => (plugins.value ?? []).filter((p) => p.manifest.enablement && !p.problem))

  const states = useQueries({
    queries: computed(() =>
      switchable.value.map((plugin) => ({
        queryKey: pluginKeys.enablement(plugin.manifest.id, toValue(householdId) ?? ''),
        queryFn: () => pluginsApi.enablement(plugin.manifest, toValue(householdId)!),
        enabled: !!toValue(householdId),
        retry: false,
      })),
    ),
  })

  const enabled = computed(() => {
    const on = new Set(switchable.value.filter((_, i) => states.value[i]?.data?.enabled).map((p) => p.manifest.id))
    return (plugins.value ?? []).filter(
      (p) => !p.problem && p.module && (p.manifest.enablement ? on.has(p.manifest.id) : true),
    )
  })

  return { plugins, enabled, isPending }
}
