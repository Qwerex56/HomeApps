import { computed } from 'vue'
import { useQuery } from '@tanstack/vue-query'
import type { PluginManifest } from '@homeapps/plugin-sdk'
import { APP_VERSION, loadPluginIds, usePlugins, versionAtLeast, type LoadedPlugin } from '@/features/plugins/registry'

/** What the web app could do with the plugin's UI module. */
export type PluginUi = 'loaded' | 'none' | 'incompatible' | 'unavailable' | 'notRunning'
export type PluginHealth = 'healthy' | 'unhealthy' | 'unknown'

/** A plugin listed in HOMEAPPS_PLUGINS, as the Owner's read-only "Pluginy serwera" view shows it. */
export interface ServerPlugin {
  id: string
  /** Null when /plugins/{id}/manifest did not answer: listed but not running. */
  manifest: PluginManifest | null
  compatible: boolean
  health: PluginHealth
  ui: PluginUi
}

export const serverPluginKeys = { all: ['plugins', 'server'] as const }

/** GET endpoints.health; any 2xx is healthy. Unknown when the manifest names no health endpoint. */
export async function checkHealth(path: string | null | undefined): Promise<PluginHealth> {
  if (!path) return 'unknown'
  try {
    const response = await fetch(path, { headers: { Accept: 'application/json' }, cache: 'no-store' })
    return response.ok ? 'healthy' : 'unhealthy'
  } catch {
    return 'unhealthy'
  }
}

function uiOf(plugin: LoadedPlugin): PluginUi {
  if (plugin.problem) return plugin.problem
  return plugin.manifest.ui ? 'loaded' : 'none'
}

/** Every listed id, the ones without a manifest included, with a fresh health check. */
export async function describeServerPlugins(ids: string[], loaded: LoadedPlugin[]): Promise<ServerPlugin[]> {
  return Promise.all(
    ids.map(async (id): Promise<ServerPlugin> => {
      const plugin = loaded.find((p) => p.manifest.id === id)
      if (!plugin) return { id, manifest: null, compatible: false, health: 'unhealthy', ui: 'notRunning' }
      return {
        id,
        manifest: plugin.manifest,
        compatible: versionAtLeast(APP_VERSION, plugin.manifest.requires.homeApps),
        health: await checkHealth(plugin.manifest.endpoints.health),
        ui: uiOf(plugin),
      }
    }),
  )
}

/** The view's list; UI states come from this page load (usePlugins), health is checked again on refetch. */
export function useServerPlugins() {
  const { data: loaded } = usePlugins()
  return useQuery({
    queryKey: serverPluginKeys.all,
    queryFn: async () => describeServerPlugins(await loadPluginIds(), loaded.value ?? []),
    enabled: computed(() => !!loaded.value),
    staleTime: 0,
  })
}
