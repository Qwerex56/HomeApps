import { http } from '@/api/http'

// Apps connected through OAuth (jarvis-v1.md 3.1): the consent screen (/connect) and "Connected apps".

/** The query an app opens /auth/oauth/authorize with; the consent screen passes it back unchanged. */
export interface AuthorizationRequest {
  responseType: string | null
  clientId: string | null
  redirectUri: string | null
  codeChallenge: string | null
  codeChallengeMethod: string | null
  resource: string | null
  state: string | null
}

export interface ConsentPreview {
  clientId: string
  clientName: string
  redirectHost: string
  resource: string
}

export interface ConnectedApp {
  /** The app's session; disconnecting ends it. */
  id: string
  clientId: string
  name: string
  connectedAt: string
  lastUsedAt: string
}

export const appsKeys = {
  connected: ['auth', 'me', 'apps'] as const,
}

const one = (value: unknown) => (typeof value === 'string' ? value : null)

/** The OAuth parameters (snake_case) from the consent page's query. */
export function authorizationRequestFrom(query: Record<string, unknown>): AuthorizationRequest {
  return {
    responseType: one(query.response_type),
    clientId: one(query.client_id),
    redirectUri: one(query.redirect_uri),
    codeChallenge: one(query.code_challenge),
    codeChallengeMethod: one(query.code_challenge_method),
    resource: one(query.resource),
    state: one(query.state),
  }
}

export const appsApi = {
  async preview(request: AuthorizationRequest) {
    const params = {
      response_type: request.responseType,
      client_id: request.clientId,
      redirect_uri: request.redirectUri,
      code_challenge: request.codeChallenge,
      code_challenge_method: request.codeChallengeMethod,
      resource: request.resource,
      state: request.state,
    }
    return (await http.get<ConsentPreview>('/auth/oauth/consent', { params })).data
  },

  /** Where to send the browser: back to the app with a code, or with error=access_denied. */
  async decide(request: AuthorizationRequest, allow: boolean) {
    return (await http.post<{ location: string }>('/auth/oauth/consent', { ...request, allow })).data.location
  },

  async connected() {
    return (await http.get<ConnectedApp[]>('/auth/me/apps')).data
  },

  async disconnect(id: string) {
    await http.delete(`/auth/me/sessions/${id}`)
  },
}
