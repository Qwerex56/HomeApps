// Server-Sent Events reader for /devices/households/{id}/stream over fetch + ReadableStream with Bearer header.
// No external library and no Pinia: the caller supplies accessToken() and refresh().

export type DeviceStreamChangedReason =
  | 'renamed'
  | 'jarvis'
  | 'notes'
  | 'removed'
  | 'restored'
  | 'paired'
  | 'descriptor'

export type DeviceStreamEvent =
  | { type: 'ping' }
  | { type: 'state'; data: { deviceId: string; values: Record<string, unknown>; at: string } }
  | { type: 'online'; data: { deviceId: string; online: boolean } }
  | {
      type: 'status'
      data: {
        deviceId: string
        silent: boolean
        lowBattery?: boolean
        battery?: number | null
        voltage?: number | null
        rssi?: number | null
        uptime?: number | null
        firmware?: string | null
        error?: string | null
        at: string
      }
    }
  | {
      type: 'event'
      data: { deviceId: string; name: string; data: unknown; at: string; isLocation: boolean }
    }
  | {
      type: 'changed'
      data: { deviceId: string; reason: DeviceStreamChangedReason }
    }

export class DeviceStreamHttpError extends Error {
  constructor(readonly status: number) {
    super(`Device stream failed with status ${status}`)
  }
}

/** Parses SSE blocks from a ReadableStream of bytes; handles arbitrary chunk boundaries and CRLF. */
export async function* readDeviceStreamEvents(
  body: ReadableStream<Uint8Array>,
): AsyncGenerator<DeviceStreamEvent> {
  const reader = body.getReader()
  const decoder = new TextDecoder()
  let buffer = ''
  try {
    for (;;) {
      const { value, done } = await reader.read()
      buffer = (buffer + decoder.decode(value, { stream: !done })).replace(/\r\n/g, '\n')
      let end: number
      while ((end = buffer.indexOf('\n\n')) >= 0) {
        const parsed = parseSseBlock(buffer.slice(0, end))
        buffer = buffer.slice(end + 2)
        if (parsed) yield parsed
      }
      if (done) {
        const parsed = parseSseBlock(buffer)
        if (parsed) yield parsed
        return
      }
    }
  } finally {
    reader.releaseLock()
  }
}

export function parseSseBlock(block: string): DeviceStreamEvent | null {
  let event = 'message'
  const dataLines: string[] = []
  for (const line of block.split('\n')) {
    if (!line || line.startsWith(':')) continue
    if (line.startsWith('event:')) event = line.slice(6).trim()
    else if (line.startsWith('data:')) dataLines.push(line.slice(5).replace(/^ /, ''))
  }
  if (event === 'ping') return { type: 'ping' }
  if (dataLines.length === 0) return null
  let parsed: unknown
  try {
    parsed = JSON.parse(dataLines.join('\n'))
  } catch {
    return null
  }
  if (!parsed || typeof parsed !== 'object') return null
  switch (event) {
    case 'state':
    case 'online':
    case 'status':
    case 'event':
    case 'changed':
      return { type: event, data: parsed as never }
    default:
      return null
  }
}

export interface ConnectDeviceStreamOptions {
  householdId: string
  accessToken: () => string | null
  refresh: () => Promise<string | null>
  signal: AbortSignal
  fetchImpl?: typeof fetch
  onOpen?: () => void
  onEvent: (event: DeviceStreamEvent) => void
}

/**
 * Opens one SSE request to `/devices/v1/households/{id}/stream` with Bearer token (refreshing once on 401)
 * and forwards every event until the server closes the stream (~15 min) or `signal` aborts.
 */
export async function connectDeviceStreamOnce(options: ConnectDeviceStreamOptions): Promise<void> {
  const fetchFn = options.fetchImpl ?? fetch
  const url = `/devices/v1/households/${encodeURIComponent(options.householdId)}/stream`
  const open = (token: string | null) =>
    fetchFn(url, {
      method: 'GET',
      headers: {
        Accept: 'text/event-stream',
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
      },
      signal: options.signal,
    })

  let response = await open(options.accessToken())
  if (response.status === 401 && !options.signal.aborted) {
    const refreshed = await options.refresh()
    if (refreshed) response = await open(refreshed)
  }
  if (!response.ok || !response.body) {
    throw new DeviceStreamHttpError(response.status)
  }

  options.onOpen?.()
  for await (const event of readDeviceStreamEvents(response.body)) {
    if (options.signal.aborted) return
    options.onEvent(event)
  }
}
