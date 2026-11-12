// The add-on side of the bridge: what an add-on calls instead of writing postMessage by hand.
import {
  PROTOCOL,
  isEvent,
  isResponse,
  type BridgeError,
  type Document,
  type EventName,
  type Events,
  type HomeMixContext,
  type Member,
  type Method,
  type Methods,
  type Scope,
  type Toast,
} from './protocol.js'

export class BridgeCallError extends Error {
  constructor(
    readonly code: string,
    message: string,
  ) {
    super(message)
  }
}

export interface HomeMix {
  context(): Promise<HomeMixContext>
  can(permission: string): Promise<boolean>
  resize(height?: number): Promise<void>
  navigate(path: string): Promise<void>
  data: {
    list<T = unknown>(collection: string, scope?: Scope): Promise<Document<T>[]>
    get<T = unknown>(collection: string, key: string, scope?: Scope): Promise<Document<T> | null>
    put<T = unknown>(
      collection: string,
      key: string,
      value: T,
      options?: { scope?: Scope; version?: number },
    ): Promise<Document<T>>
    delete(collection: string, key: string, options?: { scope?: Scope; version?: number }): Promise<void>
  }
  members(): Promise<Member[]>
  toast(message: string, severity?: Toast['severity']): Promise<void>
  on<E extends EventName>(event: E, listener: (data: Events[E]) => void): () => void
}

export interface ConnectOptions {
  /** The host's window; window.parent by default. */
  host?: Window
  /** The frame's own window (for listening); window by default. */
  self?: Window
  /** Milliseconds before a call without an answer fails. */
  timeout?: number
}

/** Connects to the host. One connection per page is enough; it is cheap to make more. */
export function connect(options: ConnectOptions = {}): HomeMix {
  const host = options.host ?? window.parent
  const self = options.self ?? window
  const timeout = options.timeout ?? 15_000
  let next = 1
  const pending = new Map<number, { resolve: (v: unknown) => void; reject: (e: unknown) => void }>()
  const listeners = new Map<EventName, Set<(data: unknown) => void>>()

  self.addEventListener('message', (event: MessageEvent) => {
    if (event.source !== host) return
    if (isResponse(event.data)) {
      const call = pending.get(event.data.id)
      if (!call) return
      pending.delete(event.data.id)
      const error = event.data.error as BridgeError | undefined
      if (error) call.reject(new BridgeCallError(error.code, error.message))
      else call.resolve(event.data.result)
    } else if (isEvent(event.data)) {
      listeners.get(event.data.event)?.forEach((listener) => listener(event.data.data))
    }
  })

  function call<M extends Method>(method: M, params?: Methods[M]['params']): Promise<Methods[M]['result']> {
    const id = next++
    return new Promise((resolve, reject) => {
      pending.set(id, { resolve: resolve as (v: unknown) => void, reject })
      setTimeout(() => {
        if (pending.delete(id)) reject(new BridgeCallError('homemix.timeout', `No answer to ${method}.`))
      }, timeout)
      host.postMessage({ homemix: PROTOCOL, id, method, params }, '*')
    })
  }

  return {
    context: () => call('context'),
    can: (permission) => call('can', { permission }),
    resize: async (height) => {
      // The body's own height: the document's is at least the frame's, so it would never shrink.
      await call('resize', { height: Math.ceil(height ?? self.document.body.getBoundingClientRect().height) })
    },
    navigate: async (path) => {
      await call('navigate', { path })
    },
    data: {
      list: (collection, scope) => call('data.list', { collection, scope }) as never,
      get: (collection, key, scope) => call('data.get', { collection, key, scope }) as never,
      put: (collection, key, value, o) => call('data.put', { collection, key, value, ...o }) as never,
      delete: async (collection, key, o) => {
        await call('data.delete', { collection, key, ...o })
      },
    },
    members: () => call('members'),
    toast: async (message, severity) => {
      await call('toast', { message, severity })
    },
    on(event, listener) {
      const set = listeners.get(event) ?? new Set()
      listeners.set(event, set)
      set.add(listener as (data: unknown) => void)
      return () => set.delete(listener as (data: unknown) => void)
    },
  }
}

/** Keeps the frame as tall as its content (a widget or page grows with it). */
export function autoResize(homemix: HomeMix, element: Element = document.body) {
  const observer = new ResizeObserver(() => void homemix.resize())
  observer.observe(element)
  return () => observer.disconnect()
}
