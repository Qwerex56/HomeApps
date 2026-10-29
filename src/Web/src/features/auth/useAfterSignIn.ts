import { useRoute, useRouter } from 'vue-router'

const STORAGE_KEY = 'homeapps.afterSignIn'

const safe = (path: unknown): path is string =>
  typeof path === 'string' && path.startsWith('/') && !path.startsWith('//')

/**
 * Remembers the page that sent a signed-out visitor to sign in. The ?redirect= query does not survive signing up,
 * Google or an invitation, so the path also waits in this tab's sessionStorage (e.g. /join/{code}).
 */
export function rememberAfterSignIn(path: string) {
  try {
    sessionStorage.setItem(STORAGE_KEY, path)
  } catch {
    // Without storage only ?redirect= on the login page brings the visitor back.
  }
}

function takeRemembered() {
  try {
    const path = sessionStorage.getItem(STORAGE_KEY)
    sessionStorage.removeItem(STORAGE_KEY)
    return path
  } catch {
    return null
  }
}

/** Where to go after signing in: back to the page that asked for it (?redirect= or remembered), else the dashboard. */
export function useAfterSignIn() {
  const route = useRoute()
  const router = useRouter()

  return () => {
    const remembered = takeRemembered()
    const redirect = route.query.redirect
    const target = safe(redirect) ? redirect : safe(remembered) ? remembered : '/'
    return router.replace(target)
  }
}
