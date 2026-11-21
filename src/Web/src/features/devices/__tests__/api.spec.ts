import { describe, expect, it } from 'vitest'
import { i18n } from '@/shared/i18n'
import { deviceState, formatPairingCode, type Device } from '../api'
import { parseToolSources, serializeToolSources } from '../assistantFlags'
import { connectDeviceStreamOnce, readDeviceStreamEvents, type DeviceStreamEvent } from '../stream'

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
  it('puts removed before anything else, then unpaired, then silent, then online/offline', () => {
    expect(deviceState(device({ removed: true, online: true }))).toBe('removed')
    expect(deviceState(device({ paired: false }))).toBe('unpaired')
    expect(
      deviceState(
        device({
          online: true,
          state: {
            deviceId: '1',
            values: {},
            valuesAt: null,
            online: true,
            silent: true,
            lowBattery: false,
            lastStatusAt: null,
            battery: null,
            voltage: null,
            rssi: null,
            uptime: null,
            firmware: null,
            error: null,
          },
        }),
      ),
    ).toBe('silent')
    expect(deviceState(device({ online: true }))).toBe('online')
    expect(deviceState(device({}))).toBe('offline')
  })
})

describe('assistantFlags', () => {
  it('parses and serializes None, All, and comma-separated flag combinations', () => {
    expect(parseToolSources(null)).toEqual([])
    expect(parseToolSources('None')).toEqual([])
    expect(parseToolSources('All')).toEqual(['Plugins', 'HomeMix', 'Devices'])
    expect(parseToolSources('Plugins, HomeMix')).toEqual(['Plugins', 'HomeMix'])
    expect(parseToolSources('Devices')).toEqual(['Devices'])

    expect(serializeToolSources([])).toBe('None')
    expect(serializeToolSources(['Plugins', 'HomeMix', 'Devices'])).toBe('All')
    expect(serializeToolSources(['HomeMix', 'Plugins'])).toBe('Plugins, HomeMix')
    expect(serializeToolSources(['Devices'])).toBe('Devices')
  })
})

describe('stream reader (readDeviceStreamEvents & connectDeviceStreamOnce)', () => {
  function streamFromChunks(chunks: string[]): ReadableStream<Uint8Array> {
    const encoder = new TextEncoder()
    let index = 0
    return new ReadableStream<Uint8Array>({
      pull(controller) {
        if (index < chunks.length) {
          controller.enqueue(encoder.encode(chunks[index++]!))
        } else {
          controller.close()
        }
      },
    })
  }

  it('handles chunk boundaries, ping, state, online, status (+lowBattery), event, and changed', async () => {
    const body = streamFromChunks([
      'event: ping\ndata: {}\n\nevent: sta',
      'te\ndata: {"deviceId":"d1","values":{"level":2},"at":"2026-11-21T12:00:00Z"}\n\n',
      'event: online\r\ndata: {"deviceId":"d1","online":true}\r\n\r\n',
      'event: status\ndata: {"deviceId":"d1","silent":false,"lowBattery":true,"battery":18,"at":"2026-11-21T12:00:00Z"}\n\n',
      'event: event\ndata: {"deviceId":"d1","name":"button","data":{"count":1},"at":"2026-11-21T12:00:00Z","isLocation":false}\n\n',
      'event: changed\ndata: {"deviceId":"d1","reason":"paired"}\n\n',
    ])

    const received: DeviceStreamEvent[] = []
    for await (const ev of readDeviceStreamEvents(body)) {
      received.push(ev)
    }

    expect(received).toEqual([
      { type: 'ping' },
      { type: 'state', data: { deviceId: 'd1', values: { level: 2 }, at: '2026-11-21T12:00:00Z' } },
      { type: 'online', data: { deviceId: 'd1', online: true } },
      {
        type: 'status',
        data: { deviceId: 'd1', silent: false, lowBattery: true, battery: 18, at: '2026-11-21T12:00:00Z' },
      },
      {
        type: 'event',
        data: { deviceId: 'd1', name: 'button', data: { count: 1 }, at: '2026-11-21T12:00:00Z', isLocation: false },
      },
      { type: 'changed', data: { deviceId: 'd1', reason: 'paired' } },
    ])
  })

  it('sends Bearer header and refreshes once on 401', async () => {
    const calls: string[] = []
    const fakeFetch: typeof fetch = async (_input, init) => {
      const auth = (init?.headers as Record<string, string>)?.Authorization ?? ''
      calls.push(auth)
      if (calls.length === 1) {
        return new Response(null, { status: 401 })
      }
      return new Response(streamFromChunks(['event: online\ndata: {"deviceId":"d1","online":true}\n\n']), {
        status: 200,
      })
    }

    const events: DeviceStreamEvent[] = []
    await connectDeviceStreamOnce({
      householdId: 'h1',
      accessToken: () => 'expired-token',
      refresh: async () => 'fresh-token',
      signal: new AbortController().signal,
      fetchImpl: fakeFetch,
      onEvent: (e) => events.push(e),
    })

    expect(calls).toEqual(['Bearer expired-token', 'Bearer fresh-token'])
    expect(events).toEqual([{ type: 'online', data: { deviceId: 'd1', online: true } }])
  })
})

describe('pl.json error keys for H-9', () => {
  const requiredCodes = [
    'devices.name_taken',
    'devices.invalid_name',
    'devices.invalid_notes',
    'devices.not_found',
    'devices.forbidden',
    'devices.read_only',
    'devices.members_only',
    'devices.members_unavailable',
    'devices.offline',
    'devices.no_answer',
    'devices.action_failed',
    'devices.control_not_found',
    'devices.invalid_frame',
    'devices.disabled',
    'devices.jarvis_not_allowed',
    'devices.pairing_expired',
    'devices.not_paired',
    'rate_limited',
    'household.assistant_name_invalid',
    'household.assistant_retention_invalid',
    'household.assistant_settings_invalid',
    'homemix.device_not_found',
    'homemix.device_not_a_match',
    'homemix.devices_household_only',
    'homemix.widget_without_devices',
  ]

  it('has a Polish translation for every H-9 error code', () => {
    for (const code of requiredCodes) {
      expect(i18n.global.te(`errors.${code}`), `missing errors.${code}`).toBe(true)
    }
  })
})

