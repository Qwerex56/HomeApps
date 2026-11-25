// The bridge between an add-on's frame and HomeApps (docs/homemix.md, "The bridge"): plain objects over postMessage.
// The frame has an opaque origin, so both sides post with '*' and the host checks event.source against its frame.

export const PROTOCOL = 1

export type Capability = 'storage.household' | 'storage.user' | 'household.members' | 'notify' | 'devices' | 'devices.control'
export type Scope = 'household' | 'user'
export type HouseholdRole = 'Guest' | 'Member' | 'Admin' | 'Owner'
export type WidgetPlace = 'Household' | 'Wall' | 'Account'
export type WidgetSize = 'Small' | 'Medium' | 'Large'

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
  /** Set in a widget: which one, where, and the size it was given (one of its homemix.json sizes). */
  widget: { id: string; place: WidgetPlace; size: WidgetSize } | null
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
  /** ISO time after which the document is no longer returned; null or missing for one that does not expire. */
  expiresAt?: string | null
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

/**
 * A notification through HomeApps' Notifications (Web Push) to people of the household, or to the account's owner.
 * Without userIds it goes to everyone else in the household. path is a page of the add-on that a click opens; the same
 * tag replaces an earlier notification instead of adding one. Each person's settings still decide if it shows.
 */
export interface Push {
  title: string
  body: string
  userIds?: string[]
  path?: string
  tag?: string
}

/** A household device as the bridge shows it (capability devices); state is its last state when asked for. */
export interface Device {
  id: string
  name: string
  slug: string
  purpose: string
  online: boolean
  model: string | null
  roles: string[]
  capabilities: string[]
  state?: DeviceState | null
}

export interface DeviceState {
  deviceId: string
  /** Control and sensor names with their last values. */
  values: Record<string, unknown>
  valuesAt: string | null
  online: boolean
  silent: boolean
  lowBattery: boolean
}

/** Devices matched for a widget: best first, the one to show, and whether an admin still has to pick one. */
export interface DeviceList {
  devices: Device[]
  selected: string | null
  needsPick: boolean
}

export interface DeviceFilter {
  capability?: string
  role?: string
  model?: string
  /** Match with the manifest's devices rules of this widget id (a widget that declares `devices`). */
  widget?: string
  state?: boolean
}

/** Every method and its params and result. */
export interface Methods {
  context: { params: undefined; result: HomeMixContext }
  can: { params: { permission: string }; result: boolean }
  resize: { params: { height: number }; result: null }
  navigate: { params: { path: string }; result: null }
  open: { params: { path?: string }; result: null }
  'data.list': { params: { scope?: Scope; collection: string }; result: Document[] }
  'data.get': { params: { scope?: Scope; collection: string; key: string }; result: Document | null }
  'data.put': {
    params: { scope?: Scope; collection: string; key: string; value: unknown; version?: number; expiresAt?: string }
    result: Document
  }
  'data.delete': { params: { scope?: Scope; collection: string; key: string; version?: number }; result: null }
  members: { params: undefined; result: Member[] }
  toast: { params: Toast; result: null }
  'notify.push': { params: Push; result: { recipients: number } }
  'devices.list': { params: DeviceFilter | undefined; result: DeviceList }
  'devices.state': { params: { id: string }; result: DeviceState }
  'devices.set': { params: { id: string; control: string; value: unknown }; result: null }
}

export type Method = keyof Methods

export interface Events {
  theme: Theme
  /** A document of this installation was written or deleted, here, on another device or by JARVIS. */
  'data.changed': { scope: Scope; collection: string; key?: string; by?: string }
  /** The person's role in the household changed: their permissions now (useCan-style checks read them again). */
  'permissions.changed': { role: HouseholdRole | null; permissions: string[] }
  /** The installation was updated, blocked or removed; on an update or a block the host reloads or closes the frame. */
  'installation.changed': { change: 'updated' | 'removed' | 'blocked' | 'unblocked' | 'widgets'; version: string }
  /** A push() of this add-on reached the person while HomeApps is open (shown as a toast too). */
  /** A device of the household reported new values (capability devices); delivered by the host (H-9 wires the Devices stream). */
  'device.state': { deviceId: string; values: Record<string, unknown>; at: string }
  notification: { title: string; body: string | null; path: string | null; tag: string | null }
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
  'open',
  'data.list',
  'data.get',
  'data.put',
  'data.delete',
  'members',
  'toast',
  'notify.push',
  'devices.list',
  'devices.state',
  'devices.set',
]

/** The capability a call needs, or null when every add-on may make it. */
export function requiredCapability(method: Method, params: unknown): Capability | null {
  if (method.startsWith('data.')) {
    return (params as { scope?: Scope } | undefined)?.scope === 'user' ? 'storage.user' : 'storage.household'
  }
  if (method === 'members') return 'household.members'
  if (method === 'toast' || method === 'notify.push') return 'notify'
  if (method === 'devices.set') return 'devices.control'
  if (method.startsWith('devices.')) return 'devices'
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
