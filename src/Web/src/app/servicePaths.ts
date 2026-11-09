// Paths that belong to the services behind Traefik. The service worker must never answer them with index.html,
// above all /auth/external/google, which is a full page navigation to Auth (frontend.md, section 4).
// Shared by vite.config.ts and the service worker (src/sw.ts).
export const servicePaths = [
  /^\/auth\//,
  /^\/\.well-known\//,
  /^\/users(\/|$)/,
  /^\/admin\/users(\/|$)/,
  /^\/households(\/|$)/,
  /^\/invites(\/|$)/,
  /^\/admin\/households(\/|$)/,
  /^\/notifications(\/|$)/,
  /^\/openapi\//,
  /^\/plugins\//,
]
