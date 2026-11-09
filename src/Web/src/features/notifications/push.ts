import type { Router } from 'vue-router'
import { notificationsApi } from './api'

/**
 * Web Push in this browser (docs/notifications.md). The browser asks for permission only after a click, never on its
 * own, and an iPhone or iPad delivers notifications only to the app added to the home screen (iOS 16.4+).
 */
export type PushSupport = 'supported' | 'needs-install' | 'unsupported'

export function isIos(userAgent = navigator.userAgent) {
  return /iPhone|iPad|iPod/.test(userAgent) || (/Macintosh/.test(userAgent) && navigator.maxTouchPoints > 1)
}

export function isInstalled() {
  return (
    window.matchMedia?.('(display-mode: standalone)').matches === true ||
    (navigator as Navigator & { standalone?: boolean }).standalone === true
  )
}

export function pushSupport(): PushSupport {
  const apis = 'serviceWorker' in navigator && 'PushManager' in window && 'Notification' in window
  if (isIos() && !isInstalled()) return 'needs-install'
  return apis ? 'supported' : 'unsupported'
}

/** The VAPID key from base64url to the bytes PushManager.subscribe() wants. */
export function applicationServerKey(base64Url: string) {
  const base64 = base64Url
    .replace(/-/g, '+')
    .replace(/_/g, '/')
    .padEnd(Math.ceil(base64Url.length / 4) * 4, '=')
  return Uint8Array.from(atob(base64), (c) => c.charCodeAt(0))
}

async function registration() {
  return (await navigator.serviceWorker?.getRegistration()) ?? null
}

/** This browser's subscription, or null when notifications are off here. */
export async function currentSubscription() {
  if (pushSupport() !== 'supported') return null
  return (await (await registration())?.pushManager.getSubscription()) ?? null
}

/**
 * Turns notifications on in this browser for the signed-in person: permission, subscription with the instance's key,
 * then the device in Notifications. Returns false when the person refused the permission.
 */
export async function enablePush() {
  if ((await Notification.requestPermission()) !== 'granted') return false
  const worker = await navigator.serviceWorker.ready
  const key = applicationServerKey(await notificationsApi.vapidPublicKey())
  let subscription = await worker.pushManager.getSubscription()
  const sameKey =
    subscription?.options.applicationServerKey &&
    new Uint8Array(subscription.options.applicationServerKey).every((byte, i) => byte === key[i])
  if (subscription && !sameKey) {
    // The instance has new VAPID keys: the old subscription can no longer be used.
    await subscription.unsubscribe()
    subscription = null
  }
  subscription ??= await worker.pushManager.subscribe({ userVisibleOnly: true, applicationServerKey: key })
  await notificationsApi.registerDevice(subscription.toJSON())
  return true
}

/** Turns notifications off in this browser: Notifications forgets the device, the browser drops the subscription. */
export async function disablePush() {
  const subscription = await currentSubscription()
  if (!subscription) return
  try {
    await notificationsApi.forgetDevice(subscription.endpoint)
  } finally {
    await subscription.unsubscribe()
  }
}

/**
 * After sign-in: a subscription this browser still has is registered again, so its keys stay current and a browser
 * shared by two people notifies the one signed in now. Never asks for permission.
 */
export async function syncPush() {
  try {
    const subscription = Notification.permission === 'granted' ? await currentSubscription() : null
    if (subscription) await notificationsApi.registerDevice(subscription.toJSON())
  } catch {
    // Best effort: the settings page shows the real state.
  }
}

/** Before signing out: the next person on this browser must not get my notifications. */
export async function forgetPushBeforeSignOut() {
  try {
    await disablePush()
  } catch {
    // Signing out must not fail because of notifications.
  }
}

/** A click on a notification while the app is open: the service worker asks the app to show that page. */
export function listenForNotificationClicks(router: Router) {
  navigator.serviceWorker?.addEventListener('message', (event: MessageEvent) => {
    const data = event.data as { type?: string; url?: string } | null
    if (data?.type === 'homeapps.navigate' && typeof data.url === 'string' && data.url.startsWith('/')) {
      void router.push(data.url)
    }
  })
}
