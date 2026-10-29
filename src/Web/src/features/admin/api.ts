import { http } from '@/api/http'
import type { components } from '@/api/types/accounts'
import type { HistoryAction, Profile, ProfileInput } from '@/features/account/api'

type Schemas = components['schemas']
export type AccountStatus = NonNullable<Schemas['AccountStatus']>
export type ServerRole = NonNullable<Schemas['ServerRole']>
export type UserSort = NonNullable<Schemas['UserSort']>

// Response types are written by hand until Accounts declares them in OpenAPI (docs/web.md).
export interface UserListItem {
  id: string
  displayName: string
  email: string
  role: ServerRole
  status: AccountStatus
  createdAt: string
  lastSeenAt: string | null
}

export interface UserPage {
  items: UserListItem[]
  total: number
  page: number
  pageSize: number
}

export interface UserFilter {
  status?: AccountStatus | null
  role?: ServerRole | null
  q?: string
  page: number
  pageSize: number
  sort: UserSort
  desc: boolean
}

export interface Registration {
  source: 'Password' | 'Google' | 'Apple' | 'Bootstrap'
  registeredAt: string
  viaReferral: boolean
  invitedByEmail: string | null
  invitedEmail: string | null
  clientApp: string | null
  userAgent: string | null
  locale: string | null
}

export interface Disabling {
  at: string
  bySelf: boolean
  reason: string | null
  byUserId: string | null
}

export interface User {
  id: string
  email: string
  role: ServerRole
  status: AccountStatus
  profile: Profile
  disabling: Disabling | null
  registration: Registration
  createdAt: string
  updatedAt: string
  lastSeenAt: string | null
}

export interface HistoryEntry {
  at: string
  action: HistoryAction
  actorId: string | null
  /** Null for changes that came from Auth or from an erased account. */
  actorName: string | null
  reason: string | null
  details: string | null
}

/** Accounts returns the history 100 entries at a time (Q6); older ones with ?before. */
export const HISTORY_PAGE = 100

export const adminKeys = {
  users: ['admin', 'users'] as const,
  list: (filter: UserFilter) => ['admin', 'users', 'list', filter] as const,
  user: (id: string) => ['admin', 'users', id] as const,
  history: (id: string) => ['admin', 'users', id, 'history'] as const,
}

/** UC-10 to UC-17 from Accounts, A-22 and A-23 from Auth. Both services check the caller's role again (R1–R5, R18). */
export const adminApi = {
  async list(filter: UserFilter) {
    const params = Object.fromEntries(Object.entries(filter).filter(([, value]) => value != null && value !== ''))
    return (await http.get<UserPage>('/admin/users', { params })).data
  },

  async get(id: string) {
    return (await http.get<User>(`/admin/users/${id}`)).data
  },

  async updateProfile(id: string, profile: ProfileInput) {
    await http.put(`/admin/users/${id}/profile`, profile)
  },

  async disable(id: string, reason: string | null) {
    await http.post(`/admin/users/${id}/disable`, { reason })
  },

  async enable(id: string) {
    await http.post(`/admin/users/${id}/enable`)
  },

  async history(id: string, before?: string) {
    return (await http.get<HistoryEntry[]>(`/admin/users/${id}/history`, { params: before ? { before } : {} })).data
  },

  /** UC-17: Owner only, a disabled account only, confirmed by typing its email (the body keeps it out of URLs). */
  async erase(id: string, confirmEmail: string) {
    await http.delete(`/admin/users/${id}`, { data: { confirmEmail } })
  },

  /** A-22: ends every session of the user and sends them a reset link. */
  async resetPassword(id: string) {
    await http.post(`/auth/admin/identities/${id}/password-reset`)
  },

  /** A-23: the Owner grants or removes Admin. Accounts learns it from Auth's event, so its copy follows a moment later. */
  async changeRole(id: string, role: Exclude<ServerRole, 'Owner'>) {
    await http.put(`/auth/admin/identities/${id}/role`, { role })
  },
}

const rank: Record<ServerRole, number> = { User: 0, Admin: 1, Owner: 2 }

/**
 * What the signed-in admin may do with an account, after R1, R3, R5 and R18. Only for showing the right buttons:
 * the services decide.
 */
export function permissions(me: { id: string; role: ServerRole }, user: Pick<User, 'id' | 'role' | 'status'>) {
  const self = me.id === user.id
  const higher = rank[me.role] > rank[user.role]
  return {
    /** R1: everyone manages their own account, a higher role someone else's; the Owner's is changed by nobody else. */
    manage: self || higher,
    /** R3: the Owner's account is never disabled. */
    disable: higher && user.status === 'Active',
    enable: higher && user.status === 'Disabled',
    resetPassword: higher,
    /** R18: only the Owner, never their own role and never to or from Owner. */
    changeRole: me.role === 'Owner' && !self && user.role !== 'Owner',
    /** R5: only the Owner, only an already disabled account. */
    erase: me.role === 'Owner' && !self && user.status === 'Disabled',
  }
}
