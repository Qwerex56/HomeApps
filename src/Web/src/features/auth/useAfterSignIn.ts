import { useRoute, useRouter } from 'vue-router'

/** Where to go after signing in: back to the page that asked for it (?redirect=), else the dashboard. */
export function useAfterSignIn() {
  const route = useRoute()
  const router = useRouter()

  return () => {
    const redirect = route.query.redirect
    const target =
      typeof redirect === 'string' && redirect.startsWith('/') && !redirect.startsWith('//') ? redirect : '/'
    return router.replace(target)
  }
}
