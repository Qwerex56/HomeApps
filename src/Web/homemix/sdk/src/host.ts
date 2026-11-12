// The host side of the bridge, without a framework: HomeApps' web app and the `homemix dev` fake host both use it.
import {
  PROTOCOL,
  isRequest,
  requiredCapability,
  type BridgeError,
  type Capability,
  type EventName,
  type Events,
  type Method,
  type Methods,
} from './protocol.js'

export type Handlers = {
  [M in Method]: (params: Methods[M]['params']) => Methods[M]['result'] | Promise<Methods[M]['result']>
}

export interface HostOptions {
  /** The frame's window; messages from anything else are ignored. */
  frame: () => Window | null | undefined
  /** What the installer granted. */
  capabilities: readonly string[]
  handlers: Handlers
  /** Turns a thrown error into what the frame sees; by default the message only. */
  toError?: (error: unknown) => BridgeError
  /** The window to listen on; the global one by default. */
  window?: Window
}

/** Error thrown by a handler with a code the add-on can act on. */
export class HomeMixError extends Error {
  constructor(
    readonly code: string,
    message: string,
  ) {
    super(message)
  }
}

/** Answers one frame's calls; returns a function that stops listening. */
export function serveFrame(options: HostOptions): () => void {
  const target = options.window ?? window
  const toError =
    options.toError ??
    ((e: unknown): BridgeError =>
      e instanceof HomeMixError
        ? { code: e.code, message: e.message }
        : { code: 'homemix.failed', message: e instanceof Error ? e.message : String(e) })

  async function onMessage(event: MessageEvent) {
    const frame = options.frame()
    if (!frame || event.source !== frame || !isRequest(event.data)) {
      return
    }

    const { id, method, params } = event.data
    const reply = (body: { result?: unknown; error?: BridgeError }) =>
      frame.postMessage({ homemix: PROTOCOL, id, ...body }, '*')

    const needed = requiredCapability(method, params)
    if (needed && !options.capabilities.includes(needed)) {
      reply({ error: { code: 'homemix.capability_missing', message: `This add-on was not granted ${needed}.` } })
      return
    }

    try {
      const handler = options.handlers[method] as (p: unknown) => unknown
      reply({ result: (await handler(params)) ?? null })
    } catch (error) {
      reply({ error: toError(error) })
    }
  }

  target.addEventListener('message', onMessage)
  return () => target.removeEventListener('message', onMessage)
}

/** Sends an event the frame did not ask for (theme, data.changed). */
export function emit<E extends EventName>(frame: Window | null | undefined, event: E, data: Events[E]) {
  frame?.postMessage({ homemix: PROTOCOL, event, data }, '*')
}

export type { Capability }
