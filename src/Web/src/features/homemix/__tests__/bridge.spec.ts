import { describe, expect, it, vi } from 'vitest'
import { HomeMixError, serveFrame, type Handlers } from '@homemix/sdk/host'
import { BridgeCallError, checkManifest, connect } from '@homemix/sdk'

// The bridge without a browser: two EventTargets play the host's and the frame's windows.
function windows() {
  const host = new EventTarget() as EventTarget & { postMessage: (data: unknown) => void }
  const frame = new EventTarget() as EventTarget & { postMessage: (data: unknown) => void }
  const deliver = (to: EventTarget, from: unknown) => (data: unknown) =>
    queueMicrotask(() => to.dispatchEvent(Object.assign(new Event('message'), { data, source: from })))
  host.postMessage = deliver(host, frame)
  frame.postMessage = deliver(frame, host)
  return { host, frame }
}

function handlers(overrides: Partial<Handlers> = {}): Handlers {
  const none = () => {
    throw new Error('not expected')
  }
  return {
    context: none,
    can: ({ permission }) => permission === 'plan.write',
    resize: () => null,
    navigate: () => null,
    'data.list': () => [],
    'data.get': () => null,
    'data.put': ({ key, value }) => ({ key, value, version: 1, updatedBy: 'u', updatedAt: '2026-11-12T00:00:00Z' }),
    'data.delete': () => null,
    members: () => [],
    toast: () => null,
    ...overrides,
  } as Handlers
}

function setup(capabilities: string[], overrides: Partial<Handlers> = {}) {
  const { host, frame } = windows()
  serveFrame({
    frame: () => frame as unknown as Window,
    capabilities,
    handlers: handlers(overrides),
    window: host as unknown as Window,
  })
  return connect({ host: host as unknown as Window, self: frame as unknown as Window, timeout: 200 })
}

describe('HomeMix bridge', () => {
  it('answers calls the add-on was granted', async () => {
    const homemix = setup(['storage.household'])

    const document = await homemix.data.put('sessions', 'a1', { minutes: 25 })

    expect(document).toMatchObject({ key: 'a1', value: { minutes: 25 }, version: 1 })
    expect(await homemix.can('plan.write')).toBe(true)
  })

  it('refuses a scope without its capability', async () => {
    const homemix = setup(['storage.household'])

    await expect(homemix.data.list('prefs', 'user')).rejects.toMatchObject({ code: 'homemix.capability_missing' })
    await expect(homemix.members()).rejects.toBeInstanceOf(BridgeCallError)
  })

  it('passes a handler error code on', async () => {
    const homemix = setup(['storage.household'], {
      'data.put': () => {
        throw new HomeMixError('homemix.conflict', 'Someone changed this document first.')
      },
    })

    await expect(homemix.data.put('a', 'b', 1, { version: 1 })).rejects.toMatchObject({ code: 'homemix.conflict' })
  })

  it('ignores messages from any other window', async () => {
    const { host } = windows()
    const stranger = windows().frame
    const handler = vi.fn(() => [])
    serveFrame({
      frame: () => stranger as unknown as Window,
      capabilities: ['storage.household'],
      handlers: handlers({ 'data.list': handler }),
      window: host as unknown as Window,
    })
    const homemix = connect({
      host: host as unknown as Window,
      self: windows().frame as unknown as Window,
      timeout: 50,
    })

    await expect(homemix.data.list('x')).rejects.toMatchObject({ code: 'homemix.timeout' })
    expect(handler).not.toHaveBeenCalled()
  })
})

describe('homemix.json check', () => {
  const valid = {
    homemixVersion: 1,
    id: 'pomodoro',
    version: '1.0.0',
    requires: { homeApps: '2.3.0' },
    name: { pl: 'Pomodoro' },
    entry: 'index.html',
    network: ['https://api.open-meteo.com'],
  }

  it('accepts a valid manifest', () => expect(checkManifest(valid)).toEqual([]))

  it('reports every problem like the server', () => {
    const problems = checkManifest({
      ...valid,
      id: 'jarvis',
      version: 'one',
      network: ['http://x.com/a'],
      entry: '../a.html',
    })
    expect(problems).toHaveLength(4)
  })
})
