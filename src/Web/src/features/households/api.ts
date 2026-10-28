import { http } from '@/api/http'
import type { components } from '@/api/types/households'

type Schemas = components['schemas']
export type HouseholdRole = NonNullable<Schemas['HouseholdRole']>
export type InviteValidity = NonNullable<Schemas['InviteValidity']>
export type MyHousehold = Schemas['MyHouseholdView']
export type Household = Schemas['HouseholdView']
export type Member = Schemas['MemberView']
export type MyMembership = Schemas['MyMembershipView']
export type Invite = Schemas['InviteView']
export type InvitePreview = Schemas['InvitePreviewView']
export type AdminHousehold = Schemas['AdminHouseholdView']

export const VALIDITIES: InviteValidity[] = [
  'ThirtyMinutes',
  'OneHour',
  'SixHours',
  'TwelveHours',
  'OneDay',
  'SevenDays',
]

export const householdKeys = {
  all: ['households'] as const,
  mine: ['households', 'mine'] as const,
  one: (id: string) => ['households', id] as const,
  members: (id: string) => ['households', id, 'members'] as const,
  me: (id: string) => ['households', id, 'me'] as const,
  invites: (id: string) => ['households', id, 'invites'] as const,
  preview: (code: string) => ['households', 'invite', code] as const,
  admin: (q: string) => ['households', 'admin', q] as const,
}

/** docs/households.md. Someone who is not a member gets 404, never 403. */
export const householdsApi = {
  async mine() {
    return (await http.get<MyHousehold[]>('/households')).data
  },

  async create(name: string, description: string | null) {
    return (await http.post<{ id: string }>('/households', { name, description })).data.id
  },

  async get(id: string) {
    return (await http.get<Household>(`/households/${id}`)).data
  },

  async update(id: string, name: string, description: string | null) {
    await http.put(`/households/${id}`, { name, description })
  },

  async archive(id: string) {
    await http.post(`/households/${id}/archive`)
  },

  async restore(id: string) {
    await http.post(`/households/${id}/restore`)
  },

  async acknowledgeOwnership(id: string) {
    await http.post(`/households/${id}/ownership/acknowledge`)
  },

  async newGuestLink(id: string) {
    return (await http.post<{ code: string }>(`/households/${id}/guest-link`)).data.code
  },

  async disableGuestLink(id: string) {
    await http.delete(`/households/${id}/guest-link`)
  },

  async members(id: string) {
    return (await http.get<Member[]>(`/households/${id}/members`)).data
  },

  async myMembership(id: string) {
    return (await http.get<MyMembership>(`/households/${id}/members/me`)).data
  },

  async setMyNickname(id: string, nickname: string | null) {
    await http.put(`/households/${id}/members/me/nickname`, { nickname })
  },

  async leave(id: string) {
    await http.delete(`/households/${id}/members/me`)
  },

  async setNickname(id: string, userId: string, nickname: string | null) {
    await http.put(`/households/${id}/members/${userId}/nickname`, { nickname })
  },

  async setRole(id: string, userId: string, role: Exclude<HouseholdRole, 'Owner'>) {
    await http.put(`/households/${id}/members/${userId}/role`, { role })
  },

  async setAccess(id: string, userId: string, until: string | null) {
    await http.put(`/households/${id}/members/${userId}/access`, { until })
  },

  async remove(id: string, userId: string) {
    await http.delete(`/households/${id}/members/${userId}`)
  },

  async transfer(id: string, userId: string) {
    await http.post(`/households/${id}/ownership`, { userId })
  },

  async invites(id: string) {
    return (await http.get<Invite[]>(`/households/${id}/invites`)).data
  },

  async invite(id: string, role: 'Member' | 'Guest', validity: InviteValidity, guestAccessUntil: string | null) {
    await http.post(`/households/${id}/invites`, { role, validity, guestAccessUntil })
  },

  async revokeInvite(id: string, inviteId: string) {
    await http.delete(`/households/${id}/invites/${inviteId}`)
  },

  async preview(code: string) {
    return (await http.get<InvitePreview>(`/invites/${encodeURIComponent(code)}`)).data
  },

  async accept(code: string) {
    return (await http.post<{ id: string }>(`/invites/${encodeURIComponent(code)}/accept`)).data.id
  },

  /** Q7: every household, newest first, 50 at a time; `before` is the last createdAt shown. */
  async all(q: string, before?: string) {
    const params = { ...(q ? { q } : {}), ...(before ? { before } : {}) }
    return (await http.get<AdminHousehold[]>('/admin/households', { params })).data
  },
}

export const ADMIN_PAGE = 50

const rank: Record<HouseholdRole, number> = { Guest: 0, Member: 1, Admin: 2, Owner: 3 }

/**
 * What my household role lets me do, after docs/households.md. Only for showing the right buttons. A server admin
 * who is not a member (no role) may look at the members (Q7).
 */
export function householdPermissions(myRole: HouseholdRole | null | undefined, serverAdmin = false) {
  const r = myRole ? rank[myRole] : -1
  return {
    seeMembers: r >= rank.Member || (serverAdmin && !myRole),
    member: r >= 0,
    invite: r >= rank.Member,
    edit: r >= rank.Admin,
    guestLink: r >= rank.Admin,
    changeRoles: myRole === 'Owner',
    transfer: myRole === 'Owner',
    archive: myRole === 'Owner',
    leave: r >= 0 && myRole !== 'Owner',
    /** Remove or rename someone below my role. */
    manage: (target: HouseholdRole) => r >= rank.Admin && rank[target] < r,
  }
}

export type HouseholdPermissions = ReturnType<typeof householdPermissions>

/** Codes are shown as ABCDE-FGHJK; case and the dash do not matter to the service. */
export function formatCode(code: string) {
  const plain = code.replace(/[^0-9a-z]/gi, '').toUpperCase()
  return plain.length === 10 ? `${plain.slice(0, 5)}-${plain.slice(5)}` : code.toUpperCase()
}

export function joinUrl(code: string) {
  return `${window.location.origin}/join/${formatCode(code)}`
}
