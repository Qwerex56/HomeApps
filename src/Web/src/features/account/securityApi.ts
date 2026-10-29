import { CLIENT_ID, http } from '@/api/http'

// What Auth keeps about me: the email, the sign-in methods and the sessions (/auth/me).

export interface LinkedLogin {
  provider: 'Google' | 'Apple'
  email: string
  linkedAt: string
}

export interface Me {
  id: string
  email: string
  role: 'User' | 'Admin' | 'Owner'
  hasPassword: boolean
  logins: LinkedLogin[]
}

export interface Session {
  id: string
  clientId: string
  userAgent: string | null
  method: 'Password' | 'Google' | 'Apple'
  startedAt: string
  lastUsedAt: string
}

export interface Invitation {
  id: string
  email: string | null
  code: string
  kind: 'Invite' | 'Bootstrap'
  status: 'Pending' | 'Accepted' | 'Revoked' | 'Expired'
  invitedByEmail: string | null
  createdAt: string
  expiresAt: string
  acceptedAt: string | null
}

export const securityKeys = {
  me: ['auth', 'me'] as const,
  sessions: ['auth', 'me', 'sessions'] as const,
  invitations: ['auth', 'invitations'] as const,
}

export const securityApi = {
  async me() {
    return (await http.get<Me>('/auth/me')).data
  },

  /** 202: the change waits for the link in the email (/confirm-password). Without a password only `newPassword`. */
  async changePassword(currentPassword: string | null, newPassword: string) {
    await http.put('/auth/me/password', { currentPassword, newPassword })
  },

  /** 202: the change waits for the link sent to the new address (/confirm-email). */
  async changeEmail(newEmail: string, currentPassword: string | null) {
    await http.put('/auth/me/email', { newEmail, currentPassword })
  },

  /**
   * A-11: linking Google is a page navigation too, but it must carry the session, so Auth hands out the URL first (Q15).
   * Auth comes back to `returnPath` with `#linked=google` or `#error=…`.
   */
  async googleLinkUrl(returnPath: string) {
    return (
      await http.post<{ url: string }>('/auth/me/logins/google/link', { clientId: CLIENT_ID, returnUrl: returnPath })
    ).data.url
  },

  async unlink(provider: 'google' | 'apple') {
    await http.delete(`/auth/me/logins/${provider}`)
  },

  async sessions() {
    return (await http.get<Session[]>('/auth/me/sessions')).data
  },

  async revokeSession(id: string) {
    await http.delete(`/auth/me/sessions/${id}`)
  },

  /** D7: every active account may invite people to the app; the list shows the invitations I sent. */
  async invitations() {
    return (await http.get<Invitation[]>('/auth/invitations')).data
  },

  async invite(email: string) {
    await http.post('/auth/invitations', { email })
  },

  async resendInvitation(id: string) {
    await http.post(`/auth/invitations/${id}/resend`)
  },

  async revokeInvitation(id: string) {
    await http.delete(`/auth/invitations/${id}`)
  },
}

/** "Chrome on macOS" from a User-Agent; good enough to recognise one's own devices (D6 keeps only the UA). */
export function describeUserAgent(userAgent: string | null): string | null {
  if (!userAgent) return null
  const browser = (
    [
      [/Edg\//, 'Edge'],
      [/OPR\/|Opera/, 'Opera'],
      [/Firefox\//, 'Firefox'],
      [/Chrome\//, 'Chrome'],
      [/Safari\//, 'Safari'],
    ] as const
  ).find(([pattern]) => pattern.test(userAgent))?.[1]
  const system = (
    [
      [/iPhone|iPad/, 'iOS'],
      [/Android/, 'Android'],
      [/Mac OS X|Macintosh/, 'macOS'],
      [/Windows/, 'Windows'],
      [/Linux/, 'Linux'],
    ] as const
  ).find(([pattern]) => pattern.test(userAgent))?.[1]
  if (browser && system) return `${browser}, ${system}`
  return browser ?? system ?? userAgent.slice(0, 60)
}
