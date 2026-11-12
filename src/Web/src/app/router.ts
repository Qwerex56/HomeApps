import { createRouter, createWebHistory } from 'vue-router'
import { useSessionStore } from '@/features/auth/session'
import { rememberAfterSignIn } from '@/features/auth/useAfterSignIn'
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
    /** Server role Owner only: the person who runs the server (AUTH_BOOTSTRAP_EMAIL). */
    owner?: boolean
  }
}

// History mode: the URL fragment stays free for the result of Google sign-in (frontend.md, section 4).
// The routes the emails link to (/invite/:token, /reset-password, /confirm-password, /confirm-email) are fixed by Auth.
// No route may start with /auth, /users, /admin/users, /households, /invites, /admin/households, /plugins or /homemix: Traefik
// sends those paths to the services and plugins.
export const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      component: AppLayout,
      meta: { requiresAuth: true },
      children: [
        { path: '', name: 'home', component: () => import('@/features/home/HomePage.vue') },
        {
          path: 'account',
          component: () => import('@/features/account/pages/AccountPage.vue'),
          children: [
            {
              path: '',
              name: 'account-profile',
              component: () => import('@/features/account/pages/ProfilePage.vue'),
            },
            {
              path: 'security',
              name: 'account-security',
              component: () => import('@/features/account/pages/SecurityPage.vue'),
            },
            {
              path: 'sessions',
              name: 'account-sessions',
              component: () => import('@/features/account/pages/SessionsPage.vue'),
            },
            {
              path: 'notifications',
              name: 'account-notifications',
              component: () => import('@/features/notifications/pages/NotificationsPage.vue'),
            },
            {
              path: 'apps',
              name: 'account-apps',
              component: () => import('@/features/account/pages/ConnectedAppsPage.vue'),
            },
            {
              path: 'invitations',
              name: 'account-invitations',
              component: () => import('@/features/account/pages/InvitationsPage.vue'),
            },
            {
              path: 'history',
              name: 'account-history',
              component: () => import('@/features/account/pages/HistoryPage.vue'),
            },
            // "Moje aplikacje": HomeMix add-ons on my account (docs/homemix.md).
            {
              path: 'mix',
              name: 'account-mix',
              component: () => import('@/features/homemix/MyMixesPage.vue'),
            },
          ],
        },
        {
          path: 'me/mix/:addon/:path(.*)?',
          name: 'my-mix',
          props: true,
          component: () => import('@/features/homemix/MixPage.vue'),
        },
        // JARVIS in the app (docs/jarvis.md); /jarvis/api and /jarvis/mcp belong to the service.
        {
          path: 'jarvis/:conversationId?',
          name: 'jarvis',
          props: true,
          component: () => import('@/features/jarvis/JarvisPage.vue'),
        },
        // /homes, /join and /admin/homes: /households, /invites and /admin/households belong to the service.
        {
          path: 'homes',
          name: 'households',
          component: () => import('@/features/households/pages/HouseholdsPage.vue'),
        },
        {
          path: 'homes/:id',
          props: true,
          component: () => import('@/features/households/pages/HouseholdPage.vue'),
          children: [
            { path: '', name: 'household', redirect: { name: 'household-members' } },
            {
              path: 'members',
              name: 'household-members',
              component: () => import('@/features/households/pages/MembersTab.vue'),
            },
            {
              path: 'invites',
              name: 'household-invites',
              component: () => import('@/features/households/pages/InvitesTab.vue'),
            },
            {
              path: 'settings',
              name: 'household-settings',
              component: () => import('@/features/households/pages/SettingsTab.vue'),
            },
            // A plugin's pages (docs/plugins.md): the plugin decides who may see what.
            {
              path: 'apps/:plugin/:path(.*)?',
              name: 'household-app',
              props: true,
              component: () => import('@/features/plugins/PluginPage.vue'),
            },
            // A HomeMix add-on's pages, in a sandboxed frame (docs/homemix.md).
            {
              path: 'mix/:addon/:path(.*)?',
              name: 'household-mix',
              props: true,
              component: () => import('@/features/homemix/MixPage.vue'),
            },
          ],
        },
        {
          path: 'join/:code',
          name: 'join',
          props: true,
          component: () => import('@/features/households/pages/JoinPage.vue'),
        },
        // /admin/accounts, never /admin/users or /admin/households: those belong to the services.
        {
          path: 'admin',
          meta: { admin: true },
          component: () => import('@/features/admin/pages/AdminPage.vue'),
          children: [
            { path: '', redirect: { name: 'admin-users' } },
            {
              path: 'accounts',
              name: 'admin-users',
              component: () => import('@/features/admin/pages/UsersPage.vue'),
            },
            {
              path: 'accounts/:id',
              name: 'admin-user',
              props: true,
              component: () => import('@/features/admin/pages/UserPage.vue'),
            },
            {
              path: 'homes',
              name: 'admin-households',
              component: () => import('@/features/admin/pages/HouseholdsPage.vue'),
            },
            {
              path: 'plugins',
              name: 'admin-plugins',
              meta: { owner: true },
              component: () => import('@/features/admin/pages/ServerPluginsPage.vue'),
            },
          ],
        },
      ],
    },
    // The wall tablet (Q7): signed in like anyone, but without the app's header and menu.
    {
      path: '/wall',
      name: 'wall',
      meta: { requiresAuth: true },
      component: () => import('@/features/wall/WallPage.vue'),
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
        // OAuth consent (jarvis-v1.md 3.1): Auth's /auth/oauth/authorize redirects here; signing in comes first.
        {
          path: 'connect',
          name: 'connect',
          meta: { requiresAuth: true },
          component: () => import('@/features/auth/pages/ConnectPage.vue'),
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
    if (to.fullPath !== '/') rememberAfterSignIn(to.fullPath)
    return { name: 'login', query: to.fullPath === '/' ? {} : { redirect: to.fullPath } }
  }
  if (to.meta.guestOnly && signedIn) {
    return { name: 'home' }
  }
  if (to.matched.some((r) => r.meta.admin) && !session.isAdmin) {
    return { name: 'home' }
  }
  if (to.matched.some((r) => r.meta.owner) && !session.isOwner) {
    return { name: 'home' }
  }
})
