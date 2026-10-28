import axios from 'axios'

// The browser client of Auth (Auth:Clients). Sent on login, refresh, Google sign-in and invitations.
export const CLIENT_ID = 'homeapps-web'

// Same origin as the services (Traefik), so no base URL and no CORS; the refresh cookie (Path=/auth) goes along by itself.
export const http = axios.create({
  headers: { Accept: 'application/json' },
})

// The access token lives only in memory. The session store (F2) registers where to read it from.
let readAccessToken: () => string | null = () => null

export function useAccessToken(reader: () => string | null) {
  readAccessToken = reader
}

http.interceptors.request.use((config) => {
  const token = readAccessToken()
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})
