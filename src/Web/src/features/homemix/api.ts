import { useQuery } from '@tanstack/vue-query'
import { computed, toValue, type MaybeRefOrGetter } from 'vue'
import { http } from '@/api/http'
import type { components } from '@/api/types/homemix'
import type { Document, Scope } from '@homemix/sdk'

type Schemas = components['schemas']
export type Installation = Schemas['InstallationView']
export type Package = Schemas['PackageView']
export type Consent = { capabilities: string[]; network: string[]; jarvis: boolean }
export type MyAccess = Schemas['MyAccessView']

/** Where add-ons are installed: a household, or the signed-in person's account. */
export type MixTarget = { kind: 'Household'; householdId: string } | { kind: 'User' }

export const mixKeys = {
  all: ['homemix'] as const,
  list: (target: MixTarget) =>
    target.kind === 'Household'
      ? (['homemix', 'household', target.householdId] as const)
      : (['homemix', 'me'] as const),
  access: (installationId: string) => ['homemix', 'access', installationId] as const,
}

const base = '/homemix/api'

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
  async addZip(file: File) {
    const form = new FormData()
    form.append('file', file)
    return (await http.post<Package>(`${base}/packages`, form)).data
  },
  async addGitHub(repository: string, tag?: string) {
    return (await http.post<Package>(`${base}/packages/github`, { repository, tag: tag || null })).data
  },
  async install(target: MixTarget, sha256: string, consent: Consent) {
    return (await http.post<Installation>(listUrl(target), { sha256, consent })).data
  },
  async update(installationId: string, sha256: string, consent?: Consent) {
    return (
      await http.put<Installation>(`${base}/installations/${installationId}`, { sha256, consent: consent ?? null })
    ).data
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
