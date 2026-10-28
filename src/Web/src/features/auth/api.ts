import { CLIENT_ID, http } from '@/api/http'
import type { components } from '@/api/types/auth'

type Schemas = components['schemas']

/** What every sign-in answers (Auth.Api TokenResponse); the refresh token stays in the HttpOnly cookie. */
export interface TokenResponse {
  accessToken: string
  accessTokenExpiresAt: string
  refreshTokenExpiresAt: string
}

export type RegisterInput = Omit<Schemas['RegisterRequest'], 'clientId'>

export const authApi = {
  async login(email: string, password: string) {
    return (await http.post<TokenResponse>('/auth/login', { email, password, clientId: CLIENT_ID })).data
  },

  async register(input: RegisterInput) {
    return (await http.post<TokenResponse>('/auth/register', { ...input, clientId: CLIENT_ID })).data
  },

  async refresh() {
    return (await http.post<TokenResponse>('/auth/refresh', { clientId: CLIENT_ID })).data
  },

  async logout() {
    await http.post('/auth/logout', {})
  },

  async logoutEverywhere() {
    await http.post('/auth/logout-all')
  },

  /** A-09, last step: the reactivation ticket becomes a normal session. 409 reactivation.pending until Auth hears from Accounts. */
  async completeReactivation(ticket: string) {
    return (await http.post<TokenResponse>('/auth/reactivation/complete', { ticket, clientId: CLIENT_ID })).data
  },

  /** A-15 "Yes": sign in to the existing account with its password; Google gets linked to it. */
  async confirmLink(ticket: string, password: string) {
    return (await http.post<TokenResponse>('/auth/external/link/confirm', { ticket, password, clientId: CLIENT_ID }))
      .data
  },

  async declineLink(ticket: string) {
    await http.post('/auth/external/link/decline', { ticket })
  },
}

export const SIGN_IN_CALLBACK_PATH = '/signin/callback'

/**
 * A-05: Google sign-in is a full page navigation to Auth, which comes back to SIGN_IN_CALLBACK_PATH with the result in
 * the URL fragment. The callback cannot live under /auth: Traefik sends that whole path to Auth.
 */
export function googleSignInUrl(invitation?: string) {
  const query = new URLSearchParams({ returnUrl: SIGN_IN_CALLBACK_PATH, client_id: CLIENT_ID, locale: 'pl' })
  if (invitation) {
    query.set('invitation', invitation)
  }
  return `/auth/external/google?${query}`
}
