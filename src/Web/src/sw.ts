/// <reference lib="webworker" />
// The app's service worker (vite-plugin-pwa, injectManifest): the app shell offline, as the generated one did before,
// and Web Push from the Notifications service (docs/notifications.md).
import { clientsClaim } from 'workbox-core'
import { cleanupOutdatedCaches, createHandlerBoundToURL, precacheAndRoute } from 'workbox-precaching'
import { NavigationRoute, registerRoute } from 'workbox-routing'
import { servicePaths } from './app/servicePaths'

declare const self: ServiceWorkerGlobalScope & { __WB_MANIFEST: (string | { url: string; revision: string | null })[] }

// registerType 'autoUpdate': a new version takes over at once.
void self.skipWaiting()
clientsClaim()

cleanupOutdatedCaches()
precacheAndRoute(self.__WB_MANIFEST)
registerRoute(new NavigationRoute(createHandlerBoundToURL('/index.html'), { denylist: servicePaths }))

const isSafari = /Safari\//.test(navigator.userAgent) && !/Chrom(e|ium)|Edg\//.test(navigator.userAgent)

/** What Notifications sends (Notifications.Infrastructure.Push.WebPushSender). */
interface PushPayload {
  title: string
  body: string
  url: string
  source: string
  category: string
  householdId: string | null
  tag: string | null
}

self.addEventListener('push', (event) => {
  let payload: PushPayload
  try {
    payload = event.data?.json() as PushPayload
  } catch {
    return
  }
  if (!payload?.title) return

  event.waitUntil(
    (async () => {
      // Someone looking at that very page needs no banner; the page itself shows what is new. Not in Safari: it
      // cancels the subscription of a site whose pushes show nothing.
      const windows = await self.clients.matchAll({ type: 'window', includeUncontrolled: true })
      const target = new URL(payload.url, self.location.origin)
      const lookingAtIt = windows.some(
        (w) => w.focused && w.visibilityState === 'visible' && new URL(w.url).pathname === target.pathname,
      )
      if (lookingAtIt && !isSafari) {
        return
      }

      await self.registration.showNotification(payload.title, {
        body: payload.body,
        tag: payload.tag ?? undefined,
        icon: '/pwa-192.png',
        badge: '/pwa-192.png',
        lang: 'pl',
        data: { url: target.pathname + target.search + target.hash },
      })
    })(),
  )
})

self.addEventListener('notificationclick', (event) => {
  event.notification.close()
  const url: string = (event.notification.data as { url?: string } | null)?.url ?? '/'

  event.waitUntil(
    (async () => {
      const windows = await self.clients.matchAll({ type: 'window', includeUncontrolled: true })
      const open = windows.find((w) => new URL(w.url).origin === self.location.origin)
      if (open) {
        // The app routes itself (src/features/notifications/push.ts), without reloading the page.
        open.postMessage({ type: 'homeapps.navigate', url })
        await open.focus()
        return
      }
      await self.clients.openWindow(url)
    })(),
  )
})
