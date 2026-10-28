import { computed, ref } from 'vue'
import { defineStore } from 'pinia'
import { connectSession } from '@/api/http'
import { toProblem } from '@/api/problem'
import { queryClient } from '@/api/queryClient'
import { authApi, type TokenResponse } from './api'
import { readClaims } from './claims'

export type SessionStatus = 'unknown' | 'guest' | 'signedIn'

// Refresh a minute before the access token expires, so requests rarely meet a 401.
const REFRESH_AHEAD_MS = 60_000
const LOCK_NAME = 'homeapps.refresh'
const RATE_LIMIT_ATTEMPTS = 4

type TabMessage = { type: 'tokens'; tokens: TokenResponse } | { type: 'signedOut' }

/**
 * The session (frontend.md 4): the access token only in memory, the refresh token in Auth's HttpOnly cookie.
 *
 * A refresh token works once and Auth has no grace window, so two tabs refreshing at the same moment would look like
 * a stolen token and end the session everywhere (Q3). Every refresh therefore runs under one Web Lock shared by all
 * tabs, and the tab that refreshed hands the new access token to the others over a BroadcastChannel.
 */
export const useSessionStore = defineStore('session', () => {
  const accessToken = ref<string | null>(null)
  const expiresAt = ref(0)
  const status = ref<SessionStatus>('unknown')
  // Bumped on every new token, so a tab that waited for the lock sees that another tab already refreshed.
  const generation = ref(0)

  const claims = computed(() => (accessToken.value ? readClaims(accessToken.value) : null))
  const isAdmin = computed(() => claims.value?.role === 'Admin' || claims.value?.role === 'Owner')

  const channel = typeof BroadcastChannel === 'undefined' ? null : new BroadcastChannel('homeapps.session')
  let timer: ReturnType<typeof setTimeout> | undefined
  let inFlight: Promise<string | null> | null = null
  let started: Promise<void> | null = null

  function apply(tokens: TokenResponse, tell = true) {
    accessToken.value = tokens.accessToken
    expiresAt.value = Date.parse(tokens.accessTokenExpiresAt)
    status.value = 'signedIn'
    generation.value++
    clearTimeout(timer)
    timer = setTimeout(() => void refresh(), Math.max(0, expiresAt.value - Date.now() - REFRESH_AHEAD_MS))
    if (tell) {
      channel?.postMessage({ type: 'tokens', tokens } satisfies TabMessage)
    }
  }

  function clear(tell = true) {
    const wasSignedIn = status.value === 'signedIn'
    accessToken.value = null
    expiresAt.value = 0
    status.value = 'guest'
    clearTimeout(timer)
    queryClient.clear()
    if (tell && wasSignedIn) {
      channel?.postMessage({ type: 'signedOut' } satisfies TabMessage)
    }
  }

  /** A new access token, or null when the session is over. One refresh at a time in this tab and across tabs. */
  function refresh(): Promise<string | null> {
    if (inFlight) {
      return inFlight
    }

    const seen = generation.value
    inFlight = withLock(async () => {
      if (generation.value !== seen && accessToken.value && expiresAt.value - Date.now() > REFRESH_AHEAD_MS) {
        return accessToken.value
      }
      for (let attempt = 1; ; attempt++) {
        try {
          const tokens = await authApi.refresh()
          apply(tokens)
          return tokens.accessToken
        } catch (error) {
          const problem = toProblem(error)
          if (problem.status === 400 || problem.status === 401) {
            clear()
            return null
          }
          // Auth counts refreshes against its anonymous limit per IP; too many reloads must not sign anyone out.
          if (problem.status === 429 && attempt < RATE_LIMIT_ATTEMPTS) {
            await sleep(Math.min(problem.retryAfter ?? 5, 30) * 1000)
            continue
          }
          throw error
        }
      }
    }).finally(() => (inFlight = null))
    return inFlight
  }

  /** On page load: the refresh cookie decides whether we are signed in. Guards wait for this. */
  function start(): Promise<void> {
    started ??= refresh().then(
      () => undefined,
      () => clear(false),
    )
    return started
  }

  async function signOut(everywhere = false) {
    try {
      await (everywhere ? authApi.logoutEverywhere() : authApi.logout())
    } finally {
      clear()
    }
  }

  channel?.addEventListener('message', (event: MessageEvent<TabMessage>) => {
    if (event.data.type === 'tokens') {
      apply(event.data.tokens, false)
    } else {
      clear(false)
    }
  })

  connectSession({ accessToken: () => accessToken.value, refresh })

  return { accessToken, status, claims, isAdmin, apply, refresh, start, signOut }
})

const sleep = (ms: number) => new Promise((resolve) => setTimeout(resolve, ms))

function withLock<T>(task: () => Promise<T>): Promise<T> {
  if (typeof navigator === 'undefined' || !navigator.locks) {
    return task()
  }
  return navigator.locks.request(LOCK_NAME, task) as Promise<T>
}
