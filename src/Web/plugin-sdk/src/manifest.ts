// The manifest as GET /plugins/{id}/manifest serves it (HomeApps.Plugins.Manifest.PluginManifestDocument, docs/plugins.md).

export type HouseholdRole = 'Guest' | 'Member' | 'Admin' | 'Owner'
export type DataScope = 'User' | 'Household' | 'Application' | 'HouseholdUser'
export type WidgetSize = 'Small' | 'Medium' | 'Large'
export type WidgetPlace = 'Household' | 'Wall' | 'Dashboard'

export interface LocalizedText {
  pl: string
  en: string
}

export interface PluginPermission {
  id: string
  minRole: HouseholdRole
  description: LocalizedText | null
}

export interface PluginRoute {
  /** Relative to /homes/{householdId}/apps/{pluginId}; empty for the main page. */
  path: string
  title: LocalizedText
  menu: boolean
}

export interface PluginWidget {
  id: string
  title: LocalizedText
  size: WidgetSize
  places: WidgetPlace[]
}

export interface PluginManifest {
  manifestVersion: number
  id: string
  version: string
  requires: { homeApps: string }
  name: LocalizedText
  description: LocalizedText | null
  /** A PrimeIcons class, e.g. pi-check-square. */
  icon: string
  scopes: DataScope[]
  enablement: { scope: 'Household'; minRole: HouseholdRole; path: string } | null
  permissions: PluginPermission[]
  endpoints: { api: string; openApi: string; mcp: string | null; health: string }
  ui: {
    entry: string
    style: string
    routes: PluginRoute[]
    widgets: PluginWidget[]
    settings: boolean
  } | null
  display: { id: string; title: LocalizedText; path: string; refreshSeconds: number }[]
  mcp: { tools: string[] } | null
  events: { publishes: string[]; consumes: string[] }
}

export interface PluginEnablement {
  householdId: string
  enabled: boolean
  changedBy: string | null
  changedAt: string | null
}

/** The manifest versions this SDK understands. */
export const MANIFEST_VERSION = 1

const roleRank: Record<HouseholdRole, number> = { Guest: 0, Member: 1, Admin: 2, Owner: 3 }

/** Whether a role has a permission from the manifest; an unknown permission is never granted. */
export function hasPermission(manifest: PluginManifest, role: HouseholdRole | null | undefined, permission: string) {
  const declared = manifest.permissions.find((p) => p.id === permission)
  return !!role && !!declared && roleRank[role] >= roleRank[declared.minRole]
}

export function hasRole(role: HouseholdRole | null | undefined, minimum: HouseholdRole) {
  return !!role && roleRank[role] >= roleRank[minimum]
}
