import { describe, expect, it } from 'vitest'
import { deviceState, formatPairingCode, type Device } from '../api'

const device = (overrides: Partial<Device>): Device => ({
  id: '1',
  name: 'salon',
  kind: 'Satellite',
  householdId: 'h',
  paired: true,
  online: false,
  removed: false,
  createdAt: '2026-11-18T10:00:00Z',
  lastSeenAt: '2026-11-18T10:00:00Z',
  actions: [],
  ...overrides,
})

describe('formatPairingCode', () => {
  it('shows codes as ABCD-EFGH whatever was typed', () => {
    expect(formatPairingCode('abcdefgh')).toBe('ABCD-EFGH')
    expect(formatPairingCode('ABCD-EFGH')).toBe('ABCD-EFGH')
  })
})

describe('deviceState', () => {
  it('puts removed before anything else, then a missing pairing, then the link', () => {
    expect(deviceState(device({ removed: true, online: true }))).toBe('removed')
    expect(deviceState(device({ paired: false }))).toBe('unpaired')
    expect(deviceState(device({ online: true }))).toBe('online')
    expect(deviceState(device({}))).toBe('offline')
  })
})
