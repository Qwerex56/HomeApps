export type ServerRole = 'User' | 'Admin' | 'Owner'

/** The claims the UI reads from the access token. Only for display: every service checks the token itself. */
export interface Claims {
  sub: string
  email: string
  role: ServerRole
  /** When the user last really signed in (seconds), for reauthentication (R8). */
  authTime: number
}

export function readClaims(accessToken: string): Claims | null {
  try {
    const payload = accessToken.split('.')[1]!.replace(/-/g, '+').replace(/_/g, '/')
    const json = new TextDecoder().decode(Uint8Array.from(atob(payload), (c) => c.charCodeAt(0)))
    const claims = JSON.parse(json)
    return { sub: claims.sub, email: claims.email, role: claims.role, authTime: Number(claims.auth_time) }
  } catch {
    return null
  }
}
