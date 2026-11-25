import { useQuery } from '@tanstack/vue-query'
import { computed, toValue, type MaybeRefOrGetter } from 'vue'
import { http } from '@/api/http'
import type { components } from '@/api/types/homemix'
import type { DeviceList, DeviceMatch, Document, Push, Scope } from '@home-apps/sdk'

type Schemas = components['schemas']
export type Installation = Schemas['InstallationView'] & {
  devicePicks?: { widgetId: string; deviceId: string }[]
}
export type Package = Schemas['PackageView']
export type Consent = { capabilities: string[]; network: string[]; jarvis: boolean }
export type MyAccess = Schemas['MyAccessView']
export type StoreEntry = Schemas['StoreEntryView']
export type MyListing = Schemas['MyListingView']
export type AdminInstallation = Schemas['AdminInstallationView']
export type Block = Schemas['BlockView']
export type AddonWidget = Schemas['AddonWidget'] & {
  devices?: DeviceMatch[]
}
export type WidgetChoice = Schemas['WidgetChoice']

/** Where add-ons are installed: a household, or the signed-in person's account. */
export type MixTarget = { kind: 'Household'; householdId: string } | { kind: 'User' }

export const mixKeys = {
  all: ['homemix'] as const,
  list: (target: MixTarget) =>
    target.kind === 'Household'
      ? (['homemix', 'household', target.householdId] as const)
      : (['homemix', 'me'] as const),
  access: (installationId: string) => ['homemix', 'access', installationId] as const,
  store: ['homemix', 'store'] as const,
  storeList: (query: StoreQuery) => ['homemix', 'store', 'list', query] as const,
  storeEntry: (key: string) => ['homemix', 'store', 'entry', key] as const,
  storeMine: ['homemix', 'store', 'mine'] as const,
  adminStore: ['homemix', 'admin', 'store'] as const,
  adminInstallations: ['homemix', 'admin', 'installations'] as const,
  blocks: ['homemix', 'admin', 'blocks'] as const,
}

const base = '/homemix/api/v1'

/** The store's tabs (docs/homemix.md, "Numbers and domki"); `all` is the default order. */
export type StoreTab = 'all' | 'featured' | 'popular' | 'hearts' | 'new'
export type StoreQuery = {
  tab: StoreTab
  q: string
  target: 'household' | 'user' | null
  jarvis: boolean
  offline: boolean
}
/** Entries a page; a shorter page is the last one. */
export const STORE_PAGE_SIZE = 24

function storeParams(query: StoreQuery, page: number) {
  return {
    tab: query.tab === 'all' ? undefined : query.tab,
    q: query.q.trim() || undefined,
    target: query.target ?? undefined,
    jarvis: query.jarvis || undefined,
    offline: query.offline || undefined,
    page: page > 1 ? page : undefined,
  }
}

function listUrl(target: MixTarget) {
  return target.kind === 'Household'
    ? `${base}/households/${target.householdId}/installations`
    : `${base}/me/installations`
}

function dataUrl(installationId: string, collection: string, key?: string) {
  const path = `${base}/installations/${installationId}/data/${encodeURIComponent(collection)}`
  return key === undefined ? path : `${path}/${encodeURIComponent(key)}`
}

export const homemixApi = {
  async list(target: MixTarget) {
    return (await http.get<Installation[]>(listUrl(target))).data
  },
  /** My account's add-ons on the quick links bar, in this order (UI 2.4.1, at most 8). */
  async pin(installationIds: string[]) {
    return (await http.put<Installation[]>(`${base}/me/pins`, { installations: installationIds })).data
  },
  async addZip(file: File) {
    const form = new FormData()
    form.append('file', file)
    return (await http.post<Package>(`${base}/packages`, form)).data
  },
  async addGitHub(repository: string, tag?: string) {
    return (await http.post<Package>(`${base}/packages/github`, { repository, tag: tag || null })).data
  },
  async addOfficial(addonId: string) {
    return (await http.post<Package>(`${base}/packages/official/${encodeURIComponent(addonId)}`)).data
  },
  store: {
    async list(query: StoreQuery, page = 1) {
      return (await http.get<StoreEntry[]>(`${base}/store`, { params: storeParams(query, page) })).data
    },
    async entry(key: string) {
      return (await http.get<StoreEntry>(`${base}/store/${encodeURIComponent(key)}`)).data
    },
    async mine() {
      return (await http.get<MyListing[]>(`${base}/store/mine`)).data
    },
    async submit(repository: string) {
      return (await http.post<StoreEntry>(`${base}/store`, { repository })).data
    },
    async refresh(key: string) {
      return (await http.post<StoreEntry>(`${base}/store/${encodeURIComponent(key)}/refresh`)).data
    },
    async remove(key: string) {
      await http.delete(`${base}/store/${encodeURIComponent(key)}`)
    },
    /** A "domek": given once per person, taken back with `on` false. */
    async heart(key: string, on: boolean) {
      const url = `${base}/store/${encodeURIComponent(key)}/heart`
      await (on ? http.put(url) : http.delete(url))
    },
  },
  devices: {
    list: (installationId: string, filter: Record<string, unknown> = {}) =>
      http
        .get<DeviceList>(`${base}/installations/${encodeURIComponent(installationId)}/devices`, { params: filter })
        .then((r) => r.data),
    state: (installationId: string, deviceId: string) =>
      http.get(`${base}/installations/${encodeURIComponent(installationId)}/devices/${encodeURIComponent(deviceId)}/state`).then((r) => r.data),
    set: (installationId: string, deviceId: string, control: string, value: unknown) =>
      http.post(`${base}/installations/${encodeURIComponent(installationId)}/devices/${encodeURIComponent(deviceId)}/set`, { control, value }),
    chooseWidgetDevice: (installationId: string, widgetId: string, device: string | null) =>
      device
        ? http
            .put<Installation>(
              `${base}/installations/${encodeURIComponent(installationId)}/widget-devices/${encodeURIComponent(widgetId)}`,
              { device },
            )
            .then((r) => r.data)
        : http
            .delete<Installation>(
              `${base}/installations/${encodeURIComponent(installationId)}/widget-devices/${encodeURIComponent(widgetId)}`,
            )
            .then((r) => r.data),
  },
  async notify(installationId: string, notification: Push) {
    return (await http.post<{ recipients: number }>(`${base}/installations/${installationId}/notify`, notification)).data
  },
  admin: {
    async installations() {
      return (await http.get<AdminInstallation[]>(`${base}/admin/installations`)).data
    },
    async blocks() {
      return (await http.get<Block[]>(`${base}/admin/blocks`)).data
    },
    async block(value: string, reason: string | null) {
      return (await http.put<Block>(`${base}/admin/blocks/${encodeURIComponent(value)}`, { reason })).data
    },
    async unblock(value: string) {
      await http.delete(`${base}/admin/blocks/${encodeURIComponent(value)}`)
    },
    async store() {
      return (await http.get<StoreEntry[]>(`${base}/admin/store`)).data
    },
    async hide(key: string, hidden: boolean) {
      return (await http.put<StoreEntry>(`${base}/admin/store/${encodeURIComponent(key)}/hidden`, { hidden })).data
    },
  },
  async install(target: MixTarget, sha256: string, consent: Consent) {
    return (await http.post<Installation>(listUrl(target), { sha256, consent })).data
  },
  async update(installationId: string, sha256: string, consent?: Consent) {
    return (
      await http.put<Installation>(`${base}/installations/${installationId}`, { sha256, consent: consent ?? null })
    ).data
  },
  /** Which widget of the add-on stands in a place and in which size; widget null turns it off there. */
  async chooseWidget(installationId: string, place: string, widget: string | null, size: string | null, order = 0) {
    return (
      await http.put<Installation>(`${base}/installations/${installationId}/widgets/${place}`, { widget, size, order })
    ).data
  },
  /** Back to the default: every widget that may stand in the place, in its own size. */
  async resetWidget(installationId: string, place: string) {
    return (await http.delete<Installation>(`${base}/installations/${installationId}/widgets/${place}`)).data
  },
  async remove(installationId: string) {
    await http.delete(`${base}/installations/${installationId}`)
  },
  async access(installationId: string) {
    return (await http.get<MyAccess>(`${base}/installations/${installationId}/me`)).data
  },
  data: {
    async list(installationId: string, collection: string, scope: Scope = 'household') {
      return (await http.get<Document[]>(dataUrl(installationId, collection), { params: { scope } })).data
    },
    async get(installationId: string, collection: string, key: string, scope: Scope = 'household') {
      try {
        return (await http.get<Document>(dataUrl(installationId, collection, key), { params: { scope } })).data
      } catch (error) {
        if ((error as { response?: { status?: number } }).response?.status === 404) {
          const code = (error as { response?: { data?: { code?: string } } }).response?.data?.code
          if (code === 'homemix.document_not_found') return null
        }
        throw error
      }
    },
    async put(
      installationId: string,
      collection: string,
      key: string,
      value: unknown,
      scope: Scope = 'household',
      version?: number,
    ) {
      return (
        await http.put<Document>(
          dataUrl(installationId, collection, key),
          { value, version: version ?? null },
          { params: { scope } },
        )
      ).data
    },
    async delete(
      installationId: string,
      collection: string,
      key: string,
      scope: Scope = 'household',
      version?: number,
    ) {
      await http.delete(dataUrl(installationId, collection, key), { params: { scope, version } })
    },
  },
}

/** The add-ons installed here (a household's for its members, the person's own). */
export function useInstallations(target: MaybeRefOrGetter<MixTarget | null>) {
  return useQuery({
    queryKey: computed(() => (toValue(target) ? mixKeys.list(toValue(target)!) : ['homemix', 'none'])),
    queryFn: () => homemixApi.list(toValue(target)!),
    enabled: computed(() => !!toValue(target)),
    staleTime: 60_000,
  })
}

/** Consent to everything a package asks for. */
export function fullConsent(pkg: Pick<Package, 'capabilities' | 'network'>, jarvis: boolean): Consent {
  return { capabilities: [...pkg.capabilities], network: [...pkg.network], jarvis }
}

export function hostOf(origin: string) {
  try {
    return new URL(origin).host
  } catch {
    return origin
  }
}
