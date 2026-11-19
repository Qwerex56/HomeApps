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

describe('triggers in the manifest', () => {
  const trigger = { on: 'state', capability: 'weather', field: 'temperature', script: 'triggers/cold.js' }
  const withTriggers = (triggers: unknown, capabilities: string[] = ['devices', 'devices.control']) => ({ ...base, capabilities, triggers })

  it('accepts state and event triggers matched like widgets', () => {
    const triggers = [trigger, { on: 'event', role: 'doorbell', name: 'pressed', script: 'triggers/bell.js', id: 'dzwonek' }, { on: 'event', model: 'a/b', script: 'triggers/c.js' }]
    expect(checkManifest(withTriggers(triggers))).toEqual([])
  })

  it('needs devices, at most ten, unique ids and a match', () => {
    expect(checkManifest(withTriggers([trigger], []))).toHaveLength(1)
    const eleven = Array.from({ length: 11 }, (_, i) => ({ ...trigger, script: `triggers/t${i}.js` }))
    expect(checkManifest(withTriggers(eleven))).toEqual(['triggers: at most 10.'])
    expect(checkManifest(withTriggers([trigger, { ...trigger, role: 'x' }])).join()).toContain('used twice')
    expect(checkManifest(withTriggers([{ on: 'state', script: 'triggers/a.js' }])).join()).toContain('needs a capability')
  })

  it('refuses a wrong kind, script path, or field/name on the wrong kind', () => {
    expect(checkManifest(withTriggers([{ ...trigger, on: 'tick' }])).join()).toContain('on must be')
    expect(checkManifest(withTriggers([{ ...trigger, script: 'tools/a.js' }])).join()).toContain('script must be')
    expect(checkManifest(withTriggers([{ ...trigger, script: 'triggers/../a.js' }])).join()).toContain('script must be')
    expect(checkManifest(withTriggers([{ ...trigger, name: 'x' }])).join()).toContain('name is for')
    expect(checkManifest(withTriggers([{ ...trigger, on: 'event' }])).join()).toContain('field is for')
    expect(checkManifest(withTriggers({}))).toEqual(['triggers must be an array.'])
  })
})
