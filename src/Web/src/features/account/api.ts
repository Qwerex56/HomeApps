import { computed } from 'vue'
import { useQuery } from '@tanstack/vue-query'
import { http } from '@/api/http'
import { toProblem } from '@/api/problem'
import type { components } from '@/api/types/accounts'
import { useSessionStore } from '@/features/auth/session'

export type ProfileInput = components['schemas']['ProfileInput']
export type AccountStatus = components['schemas']['AccountStatus']

// Response types are written by hand until the services declare them in OpenAPI (docs/web.md).
export interface Profile {
  displayName: string
  firstName: string | null
  lastName: string | null
  preferredName: string | null
  avatarUrl: string | null
  locale: string
  timeZone: string
  dateOfBirth: string | null
}

export interface MyAccount {
  id: string
  email: string
  role: 'User' | 'Admin' | 'Owner'
  status: AccountStatus
  profile: Profile
  createdAt: string
  updatedAt: string
}

export type HistoryAction =
  'Registered' | 'ProfileUpdated' | 'Disabled' | 'Enabled' | 'ReactivatedBySelf' | 'RoleSynced' | 'EmailSynced'

export interface MyHistoryEntry {
  at: string
  action: HistoryAction
}

export const accountKeys = {
  me: ['accounts', 'me'] as const,
  history: ['accounts', 'me', 'history'] as const,
}

export const accountApi = {
  /** UC-02: the whole profile is replaced (Q5), so the form always sends every field. */
  async updateProfile(profile: ProfileInput) {
    await http.put('/users/me/profile', profile)
  },

  async history() {
    return (await http.get<MyHistoryEntry[]>('/users/me/history')).data
  },

  /** UC-03: Accounts tells Auth, which ends every session; signing in again offers to bring the account back. */
  async disable(reason: string | null) {
    await http.post('/users/me/disable', { reason })
  },
}

/**
 * My account from Accounts. Right after sign-up it answers 404 user.pending until Auth's event arrives (docs/accounts.md),
 * so that one error is retried for about half a minute; `pending` tells the UI to show "preparing your account".
 */
export function useMyAccount() {
  const session = useSessionStore()
  const query = useQuery({
    queryKey: accountKeys.me,
    queryFn: async () => (await http.get<MyAccount>('/users/me')).data,
    enabled: computed(() => session.status === 'signedIn'),
    retry: (failures, error) => toProblem(error).code === 'user.pending' && failures < 15,
    retryDelay: 2000,
  })
  const pending = computed(
    () => query.failureReason.value != null && toProblem(query.failureReason.value).code === 'user.pending',
  )
  return { ...query, pending }
}

/** UC-05: the reactivation form, read and sent with the reactivation ticket instead of a session. */
export const reactivationApi = {
  async form(ticket: string) {
    return (await http.get<Profile>('/users/me/reactivation', { ticket })).data
  },

  async reactivate(ticket: string, profile: ProfileInput) {
    await http.post('/users/me/reactivate', profile, { ticket })
  },
}
