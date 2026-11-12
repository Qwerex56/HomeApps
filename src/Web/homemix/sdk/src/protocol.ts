// The bridge between an add-on's frame and HomeApps (docs/homemix.md, "The bridge"): plain objects over postMessage.
// The frame has an opaque origin, so both sides post with '*' and the host checks event.source against its frame.

export const PROTOCOL = 1

export type Capability = 'storage.household' | 'storage.user' | 'household.members' | 'notify'
export type Scope = 'household' | 'user'
export type HouseholdRole = 'Guest' | 'Member' | 'Admin' | 'Owner'
export type WidgetPlace = 'Household' | 'Wall' | 'Account'

/** What the add-on knows about where it runs. Never tokens, emails, server roles or instance configuration. */
export interface HomeMixContext {
  addon: { id: string; name: string; version: string }
  installation: { id: string; target: 'Household' | 'User' }
  household: { id: string; name: string; myRole: HouseholdRole } | null
  user: { id: string; name: string }
  /** The add-on's permissions (homemix.json) the person has. */
  permissions: string[]
  capabilities: Capability[]
  language: 'pl' | 'en'
  theme: Theme
  /** The page path inside the add-on, or the widget's id and place. */
  page: string
  widget: { id: string; place: WidgetPlace } | null
}

export interface Theme {
  dark: boolean
  /** CSS custom properties of HomeApps' theme, e.g. --p-primary-color. */
  vars: Record<string, string>
}

export interface Document<T = unknown> {
  key: string
  value: T
  version: number
  updatedBy: string
  updatedAt: string
}

export interface Member {
  id: string
  name: string
  avatarUrl: string | null
  role: HouseholdRole
}

export interface Toast {
  message: string
  severity?: 'success' | 'info' | 'warn' | 'error'
}

/** Every method and its params and result. */
export interface Methods {
  context: { params: undefined; result: HomeMixContext }
  can: { params: { permission: string }; result: boolean }
  resize: { params: { height: number }; result: null }
  navigate: { params: { path: string }; result: null }
  'data.list': { params: { scope?: Scope; collection: string }; result: Document[] }
  'data.get': { params: { scope?: Scope; collection: string; key: string }; result: Document | null }
  'data.put': {
    params: { scope?: Scope; collection: string; key: string; value: unknown; version?: number }
    result: Document
  }
  'data.delete': { params: { scope?: Scope; collection: string; key: string; version?: number }; result: null }
  members: { params: undefined; result: Member[] }
  toast: { params: Toast; result: null }
}

export type Method = keyof Methods

export interface Events {
  theme: Theme
  'data.changed': { scope: Scope; collection: string }
}

export type EventName = keyof Events

export interface RequestMessage<M extends Method = Method> {
  homemix: typeof PROTOCOL
  id: number
  method: M
  params?: Methods[M]['params']
}

export interface BridgeError {
  code: string
  message: string
}

export interface ResponseMessage {
  homemix: typeof PROTOCOL
  id: number
  result?: unknown
  error?: BridgeError
}

export interface EventMessage<E extends EventName = EventName> {
  homemix: typeof PROTOCOL
  event: E
  data: Events[E]
}

export const METHODS: readonly Method[] = [
  'context',
  'can',
  'resize',
  'navigate',
  'data.list',
  'data.get',
  'data.put',
  'data.delete',
  'members',
  'toast',
]

/** The capability a call needs, or null when every add-on may make it. */
export function requiredCapability(method: Method, params: unknown): Capability | null {
  if (method.startsWith('data.')) {
    return (params as { scope?: Scope } | undefined)?.scope === 'user' ? 'storage.user' : 'storage.household'
  }
  if (method === 'members') return 'household.members'
  if (method === 'toast') return 'notify'
  return null
}

export function isRequest(data: unknown): data is RequestMessage {
  const m = data as RequestMessage | null
  return !!m && m.homemix === PROTOCOL && typeof m.id === 'number' && METHODS.includes(m.method)
}

export function isResponse(data: unknown): data is ResponseMessage {
  const m = data as ResponseMessage | null
  return !!m && m.homemix === PROTOCOL && typeof m.id === 'number' && !('method' in m)
}

export function isEvent(data: unknown): data is EventMessage {
  const m = data as EventMessage | null
  return !!m && m.homemix === PROTOCOL && typeof m.event === 'string'
}
