import { afterEach, describe, expect, it, vi } from 'vitest'
import { notificationsApi } from '../api'
import { applicationServerKey, disablePush, enablePush, isIos, pushSupport } from '../push'

const IPHONE = 'Mozilla/5.0 (iPhone; CPU iPhone OS 17_5 like Mac OS X) AppleWebKit/605.1.15 Version/17.5 Mobile/15E148'
const KEY = 'BEl62iUYgUivxIkv69yViEuiBIa-Ib9-SkvMeAtA3LFgDzkrxZJjSgSnfckjBJuBkr3qBUYIHBQFLXYp5Nksh8U'

function subscription(endpoint: string, key: Uint8Array | null) {
  return {
    endpoint,
    options: { applicationServerKey: key?.buffer ?? null },
    toJSON: () => ({ endpoint, keys: { p256dh: 'p', auth: 'a' } }),
    unsubscribe: vi.fn().mockResolvedValue(true),
  }
}

function installPush(existing: ReturnType<typeof subscription> | null, permission: NotificationPermission = 'granted') {
  const created = subscription('https://push.example/new', applicationServerKey(KEY))
  const pushManager = {
    getSubscription: vi.fn().mockResolvedValue(existing),
    subscribe: vi.fn().mockResolvedValue(created),
  }
  const registration = { pushManager }
  vi.stubGlobal('Notification', { permission, requestPermission: vi.fn().mockResolvedValue(permission) })
  vi.stubGlobal('PushManager', class {})
  Object.defineProperty(navigator, 'serviceWorker', {
    configurable: true,
    value: { ready: Promise.resolve(registration), getRegistration: vi.fn().mockResolvedValue(registration) },
  })
  return { pushManager, created }
}

afterEach(() => {
  vi.unstubAllGlobals()
  vi.restoreAllMocks()
  // @ts-expect-error jsdom has no service worker; the tests add one.
  delete navigator.serviceWorker
})

describe('push support', () => {
  it('needs the app on the home screen on an iPhone', () => {
    expect(isIos(IPHONE)).toBe(true)
    expect(isIos('Mozilla/5.0 (X11; Linux x86_64) Chrome/130.0')).toBe(false)
  })

  it('is unsupported without service workers', () => {
    expect(pushSupport()).toBe('unsupported')
  })

  it('is supported with service workers, PushManager and Notification', () => {
    installPush(null)
    expect(pushSupport()).toBe('supported')
  })
})

describe('applicationServerKey', () => {
  it('decodes the base64url VAPID key into the 65 bytes of a P-256 point', () => {
    const key = applicationServerKey(KEY)
    expect(key).toHaveLength(65)
    expect(key[0]).toBe(4)
  })
})

describe('enablePush', () => {
  it('subscribes with the instance key and registers the device', async () => {
    const { pushManager, created } = installPush(null)
    vi.spyOn(notificationsApi, 'vapidPublicKey').mockResolvedValue(KEY)
    const register = vi.spyOn(notificationsApi, 'registerDevice').mockResolvedValue('id')

    expect(await enablePush()).toBe(true)

    expect(pushManager.subscribe).toHaveBeenCalledWith({
      userVisibleOnly: true,
      applicationServerKey: expect.any(Uint8Array),
    })
    expect(register).toHaveBeenCalledWith(created.toJSON())
  })

  it('replaces a subscription made with an old key', async () => {
    const old = subscription('https://push.example/old', new Uint8Array(65))
    const { pushManager } = installPush(old)
    vi.spyOn(notificationsApi, 'vapidPublicKey').mockResolvedValue(KEY)
    vi.spyOn(notificationsApi, 'registerDevice').mockResolvedValue('id')

    await enablePush()

    expect(old.unsubscribe).toHaveBeenCalled()
    expect(pushManager.subscribe).toHaveBeenCalled()
  })

  it('keeps a subscription made with the current key', async () => {
    const same = subscription('https://push.example/same', applicationServerKey(KEY))
    const { pushManager } = installPush(same)
    vi.spyOn(notificationsApi, 'vapidPublicKey').mockResolvedValue(KEY)
    const register = vi.spyOn(notificationsApi, 'registerDevice').mockResolvedValue('id')

    await enablePush()

    expect(pushManager.subscribe).not.toHaveBeenCalled()
    expect(register).toHaveBeenCalledWith(same.toJSON())
  })

  it('stops when the person refuses the permission', async () => {
    installPush(null, 'denied')
    const register = vi.spyOn(notificationsApi, 'registerDevice')

    expect(await enablePush()).toBe(false)
    expect(register).not.toHaveBeenCalled()
  })
})

describe('disablePush', () => {
  it('forgets the device and drops the subscription', async () => {
    const current = subscription('https://push.example/mine', applicationServerKey(KEY))
    installPush(current)
    const forget = vi.spyOn(notificationsApi, 'forgetDevice').mockResolvedValue()

    await disablePush()

    expect(forget).toHaveBeenCalledWith('https://push.example/mine')
    expect(current.unsubscribe).toHaveBeenCalled()
  })
})
