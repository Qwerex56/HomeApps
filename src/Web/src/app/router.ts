import { createRouter, createWebHistory } from 'vue-router'
import AppLayout from './layouts/AppLayout.vue'
import GuestLayout from './layouts/GuestLayout.vue'

// History mode: the URL fragment stays free for the result of Google sign-in (frontend.md, section 4).
// The routes the emails link to (/invite/:token, /reset-password, /confirm-password, /confirm-email) are fixed by Auth.
// Guards (signed in, server role) come with the session in F2.
export const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      component: AppLayout,
      children: [{ path: '', name: 'home', component: () => import('@/features/home/HomePage.vue') }],
    },
    {
      path: '/',
      component: GuestLayout,
      children: [{ path: ':pathMatch(.*)*', name: 'not-found', component: () => import('./NotFoundPage.vue') }],
    },
  ],
})
