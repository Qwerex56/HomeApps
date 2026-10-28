import { createRouter, createWebHistory } from 'vue-router'
import { useSessionStore } from '@/features/auth/session'
import AppLayout from './layouts/AppLayout.vue'
import GuestLayout from './layouts/GuestLayout.vue'

declare module 'vue-router' {
  interface RouteMeta {
    /** Needs a session; set on the AppLayout route, so every page inside it needs one. */
    requiresAuth?: boolean
    /** Pages for signed-out visitors only (sign-in, sign-up); a signed-in user goes to the dashboard. */
    guestOnly?: boolean
    /** Server role Admin or Owner (the claim in the token; Accounts and Auth check it again). */
    admin?: boolean
  }
}

// History mode: the URL fragment stays free for the result of Google sign-in (frontend.md, section 4).
// The routes the emails link to (/invite/:token, /reset-password, /confirm-password, /confirm-email) are fixed by Auth.
// No route may start with /auth, /users or /admin/users: Traefik sends those paths to the services.
export const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      component: AppLayout,
      meta: { requiresAuth: true },
      children: [{ path: '', name: 'home', component: () => import('@/features/home/HomePage.vue') }],
    },
    {
      path: '/',
      component: GuestLayout,
      children: [
        {
          path: 'login',
          name: 'login',
          meta: { guestOnly: true },
          component: () => import('@/features/auth/pages/LoginPage.vue'),
        },
        {
          path: 'register',
          name: 'register',
          meta: { guestOnly: true },
          component: () => import('@/features/auth/pages/RegisterPage.vue'),
        },
        {
          path: 'signin/callback',
          name: 'signin-callback',
          component: () => import('@/features/auth/pages/SignInCallbackPage.vue'),
        },
        {
          path: 'signin/link',
          name: 'link-account',
          meta: { guestOnly: true },
          component: () => import('@/features/auth/pages/LinkAccountPage.vue'),
        },
        {
          path: 'signin/reactivate',
          name: 'reactivate',
          meta: { guestOnly: true },
          component: () => import('@/features/auth/pages/ReactivatePage.vue'),
        },
        // The links from Auth's emails: these paths are fixed by Auth's email templates.
        { path: 'invite/:token', name: 'invite', component: () => import('@/features/auth/pages/InvitePage.vue') },
        {
          path: 'forgot-password',
          name: 'forgot-password',
          component: () => import('@/features/auth/pages/ForgotPasswordPage.vue'),
        },
        {
          path: 'reset-password',
          name: 'reset-password',
          component: () => import('@/features/auth/pages/ResetPasswordPage.vue'),
        },
        {
          path: 'confirm-password',
          name: 'confirm-password',
          component: () => import('@/features/auth/pages/ConfirmLinkPage.vue'),
          props: { kind: 'password' },
        },
        {
          path: 'confirm-email',
          name: 'confirm-email',
          component: () => import('@/features/auth/pages/ConfirmLinkPage.vue'),
          props: { kind: 'email' },
        },
        { path: ':pathMatch(.*)*', name: 'not-found', component: () => import('./NotFoundPage.vue') },
      ],
    },
  ],
})

// Pages need a token unless they say otherwise, like the endpoints in the services (ADR 0005).
router.beforeEach(async (to) => {
  const session = useSessionStore()
  await session.start()

  const signedIn = session.status === 'signedIn'
  if (to.matched.some((r) => r.meta.requiresAuth) && !signedIn) {
    return { name: 'login', query: to.fullPath === '/' ? {} : { redirect: to.fullPath } }
  }
  if (to.meta.guestOnly && signedIn) {
    return { name: 'home' }
  }
  if (to.matched.some((r) => r.meta.admin) && !session.isAdmin) {
    return { name: 'home' }
  }
})
