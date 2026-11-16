import { describe, expect, it } from 'vitest'
import { frameEvent } from '../frameEvents'

// Which realtime events a frame hears (docs/homemix.md, "RealTime").
const frame = { installationId: 'i1', householdId: 'h1' }

describe('frameEvent', () => {
  it('passes its own installation on without the ids', () => {
    expect(
      frameEvent(
        { event: 'data.changed', data: { installationId: 'i1', scope: 'user', collection: 'c', key: 'k', by: 'u' } },
        frame,
      ),
    ).toEqual({ name: 'data.changed', data: { scope: 'user', collection: 'c', key: 'k', by: 'u' } })
    expect(
      frameEvent(
        { event: 'notification', data: { installationId: 'i1', title: 'T', body: null, path: null, tag: null } },
        frame,
      ),
    ).toEqual({ name: 'notification', data: { title: 'T', body: null, path: null, tag: null } })
  })

  it('leaves out other installations, installs and other households', () => {
    expect(
      frameEvent(
        { event: 'data.changed', data: { installationId: 'i2', scope: 'household', collection: 'c', key: 'k', by: 'u' } },
        frame,
      ),
    ).toBeNull()
    expect(
      frameEvent(
        { event: 'installation.changed', data: { installationId: 'i1', addonId: 'a', version: '1', change: 'installed' } },
        frame,
      ),
    ).toBeNull()
    expect(frameEvent({ event: 'permissions.changed', data: { householdId: 'h2', role: 'Admin' } }, frame)).toBeNull()
    expect(frameEvent({ event: 'permissions.changed', data: { householdId: 'h1', role: 'Admin' } }, frame)).toBe(
      'permissions',
    )
  })
})
