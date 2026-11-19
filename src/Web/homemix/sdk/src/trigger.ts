// The ctx of a trigger script (triggers/{name}.js), which the HomeMix server runs in Jint when a household device changes
// state or publishes an event (docs/homemix-authoring.md, "Triggers"). No person is logged in: the script acts as the
// add-on within its consent and the rights of whoever installed it, so `data`, `members` and `fetch` are not offered.
import type { HomeMixContext } from './protocol.js'

export interface TriggerDevice {
  id: string
  name: string
  slug: string
  model: string | null
  roles: string[]
  capabilities: string[]
}

/** What `ctx.trigger` says: which device, and what changed or happened. */
export interface TriggerInfo {
  /** The trigger's id from homemix.json. */
  id: string
  on: 'state' | 'event'
  device: TriggerDevice
  /** state: only the values that changed, with their new values. */
  changed?: Record<string, unknown>
  /** event: its name and data (null when it has none). */
  event?: { name: string; payload: unknown }
  at: string
}

export interface TriggerContext {
  /** As in a tool, with the household and the installer as the user; no theme, page or widget. */
  context(): Promise<HomeMixContext>
  can(permission: string): Promise<boolean>
  trigger: TriggerInfo
  devices: {
    /** The household's devices, filtered like a widget's; `state` holds the last values when asked for. */
    list(filter?: { capability?: string; role?: string; model?: string; state?: boolean }): Promise<(TriggerDevice & { online: boolean; state?: Record<string, unknown> | null })[]>
    state(id: string): Promise<Record<string, unknown> | null>
    /** Needs devices.control; fails with Devices' own error (devices.offline, devices.read_only, ...). */
    set(id: string, control: string, value: unknown): Promise<void>
  }
}

/** What triggers/{name}.js exports as default: it gets ctx.trigger and the ctx; its answer is ignored. */
export type TriggerScript = (trigger: TriggerInfo, ctx: TriggerContext) => unknown
