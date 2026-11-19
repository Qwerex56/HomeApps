import { describe, expect, it } from 'vitest'
import { checkManifest, requiredCapability } from '@home-apps/sdk'

const base = {
  homemixVersion: 1,
  id: 'stacja',
  version: '1.0.0',
  requires: { homeApps: '2.3.0' },
  name: { pl: 'Stacja' },
  entry: 'index.html',
}

describe('devices in the bridge and the manifest', () => {
  it('needs devices to read and devices.control to set', () => {
    expect(requiredCapability('devices.list', undefined)).toBe('devices')
    expect(requiredCapability('devices.state', { id: 'x' })).toBe('devices')
    expect(requiredCapability('devices.set', { id: 'x' })).toBe('devices.control')
  })

  it('accepts widget device matching with the capability', () => {
    const m = { ...base, capabilities: ['devices'], widgets: [{ id: 'w', title: { pl: 'W' }, devices: [{ capability: 'weather' }, { role: 'x', pick: 'manual' }, { capability: 'qwe-rex/akwarium' }] }] }
    expect(checkManifest(m)).toEqual([])
  })

  it('refuses matching without devices, empty or bad entries, and control without read', () => {
    const widget = (devices: unknown[]) => ({ ...base, capabilities: [], widgets: [{ id: 'w', title: { pl: 'W' }, devices }] })
    expect(checkManifest(widget([{ capability: 'weather' }]))).toHaveLength(1)
    expect(checkManifest({ ...widget([{}]), capabilities: ['devices'] })).toHaveLength(1)
    expect(checkManifest({ ...widget([{ capability: 'W' }]), capabilities: ['devices'] })).toHaveLength(1)
    expect(checkManifest({ ...widget([{ capability: 'qwe-rex/Akwarium' }]), capabilities: ['devices'] })).toHaveLength(1)
    expect(checkManifest({ ...base, capabilities: ['devices.control'] })).toHaveLength(1)
  })
})
