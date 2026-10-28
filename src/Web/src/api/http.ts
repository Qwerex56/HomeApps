import axios, { type InternalAxiosRequestConfig } from 'axios'

declare module 'axios' {
  interface AxiosRequestConfig {
    /** A sign-in ticket to send instead of the session's access token (reactivation, frontend.md 4). Never refreshed. */
    ticket?: string
    /** Set on the one retry after a refresh, so a second 401 is final. */
    retried?: boolean
  }
}

// The browser client of Auth (Auth:Clients). Sent on login, refresh, Google sign-in and invitations.
export const CLIENT_ID = 'homeapps-web'

// Same origin as the services (Traefik), so no base URL and no CORS; the refresh cookie (Path=/auth) goes along by itself.
export const http = axios.create({
  headers: { Accept: 'application/json' },
})

interface SessionConnection {
  /** The access token in memory, or null when signed out. */
  accessToken: () => string | null
  /** A new access token from Auth, or null when the session is over. */
  refresh: () => Promise<string | null>
}

let session: SessionConnection = { accessToken: () => null, refresh: async () => null }

/** The session store plugs itself in here (features/auth/session.ts); http.ts knows nothing about Pinia. */
export function connectSession(connection: SessionConnection) {
  session = connection
}

// Endpoints that answer 401 for a wrong password or an expired link, not for an expired access token.
const signInEndpoints =
  /^\/auth\/(login|register|refresh|logout|reactivation|external|password|invitations|me\/(password|email)\/confirm)\b/

http.interceptors.request.use((config) => {
  const token = config.ticket ?? session.accessToken()
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

// An expired access token: refresh once (one refresh for all requests and tabs, see session.ts) and repeat the request.
http.interceptors.response.use(undefined, async (error) => {
  const config = error?.config as InternalAxiosRequestConfig | undefined
  const retryable =
    error?.response?.status === 401 &&
    config &&
    !config.retried &&
    !config.ticket &&
    !signInEndpoints.test(config.url ?? '') &&
    session.accessToken() !== null
  if (!retryable) {
    throw error
  }

  const token = await session.refresh()
  if (!token) {
    throw error
  }
  return http({ ...config, retried: true })
})
