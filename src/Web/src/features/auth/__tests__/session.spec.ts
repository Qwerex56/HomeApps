import { beforeEach, describe, expect, it, vi } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { AxiosError, AxiosHeaders, type AxiosResponse, type InternalAxiosRequestConfig } from 'axios'
import { http } from '@/api/http'
import { authApi, type TokenResponse } from '../api'
import { useSessionStore } from '../session'

function token(sub: string, role = 'User') {
  const payload = btoa(JSON.stringify({ sub, email: `${sub}@home.pl`, role, auth_time: 1 }))
  return `x.${payload}.y`
}

function tokens(sub: string, role?: string): TokenResponse {
  return {
    accessToken: token(sub, role),
    accessTokenExpiresAt: new Date(Date.now() + 15 * 60_000).toISOString(),
    refreshTokenExpiresAt: new Date(Date.now() + 30 * 86_400_000).toISOString(),
  }
}

function httpError(status: number, config: InternalAxiosRequestConfig = { headers: new AxiosHeaders() }) {
  const response = { status, data: {}, headers: {}, config } as AxiosResponse
  return new AxiosError('failed', undefined, config, undefined, response)
}

beforeEach(() => {
  setActivePinia(createPinia())
  vi.restoreAllMocks()
})

describe('session', () => {
  it('starts signed in when the refresh cookie still works', async () => {
    vi.spyOn(authApi, 'refresh').mockResolvedValue(tokens('ania', 'Admin'))
    const session = useSessionStore()

    await session.start()

    expect(session.status).toBe('signedIn')
    expect(session.claims).toMatchObject({ sub: 'ania', role: 'Admin' })
    expect(session.isAdmin).toBe(true)
  })

  it('starts as a guest when Auth refuses the refresh', async () => {
    vi.spyOn(authApi, 'refresh').mockRejectedValue(httpError(401))
    const session = useSessionStore()

    await session.start()

    expect(session.status).toBe('guest')
    expect(session.accessToken).toBeNull()
  })

  it('sends one refresh for many callers', async () => {
    const refresh = vi.spyOn(authApi, 'refresh').mockResolvedValue(tokens('ania'))
    const session = useSessionStore()

    const results = await Promise.all([session.refresh(), session.refresh(), session.refresh()])

    expect(refresh).toHaveBeenCalledTimes(1)
    expect(new Set(results).size).toBe(1)
  })

  it('refreshes once and repeats a request that met an expired token', async () => {
    vi.spyOn(authApi, 'refresh').mockResolvedValue(tokens('fresh'))
    const session = useSessionStore()
    session.apply(tokens('stale'), false)
    const seen: string[] = []
    http.defaults.adapter = async (config) => {
      const auth = String(config.headers.Authorization)
      seen.push(auth)
      if (auth.includes(btoa(JSON.stringify({ sub: 'stale', email: 'stale@home.pl', role: 'User', auth_time: 1 })))) {
        throw httpError(401, config)
      }
      return { status: 200, statusText: 'OK', data: 'ok', headers: {}, config }
    }

    const response = await http.get('/users/me')

    expect(response.data).toBe('ok')
    expect(seen).toHaveLength(2)
    expect(session.claims?.sub).toBe('fresh')
  })

  it('does not refresh for a wrong password', async () => {
    const refresh = vi.spyOn(authApi, 'refresh')
    const session = useSessionStore()
    session.apply(tokens('ania'), false)
    http.defaults.adapter = async (config) => {
      throw httpError(401, config)
    }

    await expect(http.post('/auth/login', {})).rejects.toBeInstanceOf(AxiosError)
    expect(refresh).not.toHaveBeenCalled()
  })

  it('sends the ticket instead of the session token', async () => {
    const session = useSessionStore()
    session.apply(tokens('ania'), false)
    let auth = ''
    http.defaults.adapter = async (config) => {
      auth = String(config.headers.Authorization)
      return { status: 200, statusText: 'OK', data: {}, headers: {}, config }
    }

    await http.get('/users/me/reactivation', { ticket: 'reactivation-ticket' })

    expect(auth).toBe('Bearer reactivation-ticket')
  })
})

describe('session under the rate limit', () => {
  it('waits for Retry-After instead of signing out', async () => {
    vi.useFakeTimers()
    const limited = httpError(429)
    limited.response!.headers = { 'retry-after': '2' }
    const refresh = vi.spyOn(authApi, 'refresh').mockRejectedValueOnce(limited).mockResolvedValue(tokens('ania'))
    const session = useSessionStore()

    const started = session.start()
    await vi.advanceTimersByTimeAsync(2000)
    await started

    expect(refresh).toHaveBeenCalledTimes(2)
    expect(session.status).toBe('signedIn')
    vi.useRealTimers()
  })
})
