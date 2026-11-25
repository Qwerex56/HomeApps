import { http } from '@/api/http'
import type { AssistantToolSource } from './assistantFlags'

// The Devices service (docs/devices.md, docs/devices-protocol.md) and the household assistant settings in Households.

export interface DeviceAction {
  name: string
  description: string
  readOnly: boolean
  /** The device asks before JARVIS runs it. */
  confirm: boolean
  /** "Nie pytaj": JARVIS runs it without asking (S6). */
  autoApprove: boolean
}

export interface JsonSchema {
  type?: 'boolean' | 'integer' | 'number' | 'string' | 'object' | 'array'
  enum?: (string | number | boolean)[]
  const?: string | number | boolean
  minimum?: number
  maximum?: number
  minLength?: number
  maxLength?: number
}

export interface DescriptorControl {
  name: string
  label: string | null
  description: string | null
  value: JsonSchema
  widget: 'switch' | 'slider' | 'select' | 'button' | null
}

export interface DescriptorSensor {
  name: string
  label: string | null
  value: JsonSchema
  unit: string | null
}

export interface DescriptorEvent {
  name: string
  label: string | null
  notify: boolean
  isLocation: boolean
}

export interface DeviceDescriptor {
  described: boolean
  model: string | null
  version: string | null
  roles: string[]
  capabilities: string[]
  warnings: string[]
  controls: DescriptorControl[]
  sensors: DescriptorSensor[]
  events: DescriptorEvent[]
  jarvis?: string | null
  sleepy?: boolean
}

export interface DeviceStateInfo {
  deviceId: string
  values: Record<string, unknown>
  valuesAt: string | null
  online: boolean
  silent: boolean
  lowBattery: boolean
  lastStatusAt: string | null
  battery: number | null
  voltage: number | null
  rssi: number | null
  uptime: number | null
  firmware: string | null
  error: string | null
}

export interface DeviceEventItem {
  id: string
  name: string
  data: unknown
  occurredAt: string
  isLocation: boolean
}

export type DevicePurpose = 'assistant' | 'smart_home' | 'computer'

export interface Device {
  id: string
  name: string
  slug?: string
  kind: 'Computer' | 'Satellite'
  purpose?: DevicePurpose
  householdId: string | null
  paired: boolean
  online: boolean
  removed: boolean
  createdAt: string
  lastSeenAt: string
  actions: DeviceAction[]
  descriptor?: DeviceDescriptor
  adminNotes?: string | null
  jarvisMayControl?: boolean
  jarvisAsks?: boolean
  state?: DeviceStateInfo | null
}

/** A one-time code for a household device: 10 minutes, shown once. */
export interface PairingCode {
  deviceId: string
  name: string
  code: string
  expiresAt: string
}

export type AssistantAccessLevel = 'ReadOnly' | 'Member'

export interface HouseholdAssistant {
  enabled: boolean
  name: string
  accessLevel: AssistantAccessLevel
  toolSources: string
  eventDays: number
  locationEventDays: number
}

export const deviceKeys = {
  all: ['devices'] as const,
  mine: ['devices', 'mine'] as const,
  household: (id: string) => ['devices', 'household', id] as const,
  events: (id: string) => ['devices', 'events', id] as const,
}

export const assistantKeys = {
  all: ['assistant'] as const,
  household: (id: string) => ['assistant', 'household', id] as const,
}

export const EVENTS_PAGE_SIZE = 50

export const devicesApi = {
  async mine() {
    return (await http.get<Device[]>('/devices/v1')).data
  },

  async household(householdId: string, state = true) {
    return (await http.get<Device[]>(`/devices/v1/households/${householdId}`, { params: state ? { state: true } : {} }))
      .data
  },

  /** A household admin adds a device and gets its first pairing code. */
  async addSatellite(householdId: string, name: string) {
    return (await http.post<PairingCode>(`/devices/v1/households/${householdId}`, { name })).data
  },

  /** A new code; the device's old pairing ends. */
  async newCode(deviceId: string) {
    return (await http.post<PairingCode>(`/devices/v1/${deviceId}/pairing-code`)).data
  },

  /** Turned off, not deleted: its link drops and it may not connect again until restored. */
  async remove(deviceId: string) {
    await http.delete(`/devices/v1/${deviceId}`)
  },

  async restore(deviceId: string) {
    await http.post(`/devices/v1/${deviceId}/restore`)
  },

  async allow(deviceId: string, action: string, allowed: boolean) {
    await http.put(`/devices/v1/${deviceId}/actions/${encodeURIComponent(action)}/allow`, { allowed })
  },

  async rename(deviceId: string, name: string) {
    await http.put(`/devices/v1/${deviceId}/name`, { name })
  },

  async setNotes(deviceId: string, notes: string | null) {
    await http.put(`/devices/v1/${deviceId}/notes`, { notes })
  },

  async setJarvis(deviceId: string, mayControl: boolean, asks: boolean) {
    await http.put(`/devices/v1/${deviceId}/jarvis`, { mayControl, asks })
  },

  async setControl(deviceId: string, control: string, value: unknown) {
    await http.post(`/devices/v1/${deviceId}/set`, { control, value })
  },

  async state(deviceId: string) {
    return (await http.get<DeviceStateInfo>(`/devices/v1/${deviceId}/state`)).data
  },

  async events(deviceId: string, skip = 0, limit = EVENTS_PAGE_SIZE) {
    return (await http.get<DeviceEventItem[]>(`/devices/v1/${deviceId}/events`, { params: { skip, limit } })).data
  },

  async assistant(householdId: string) {
    return (await http.get<HouseholdAssistant>(`/households/v1/${householdId}/assistant`)).data
  },

  async updateAssistant(
    householdId: string,
    settings: {
      enabled: boolean
      name: string
      accessLevel: AssistantAccessLevel
      toolSources: string | AssistantToolSource[]
      eventDays: number
      locationEventDays: number
    },
  ) {
    return (await http.put<HouseholdAssistant>(`/households/v1/${householdId}/assistant`, settings)).data
  },
}

/** Pairing codes are 8 characters, shown as ABCD-EFGH; case and the dash do not matter to the service. */
export function formatPairingCode(code: string) {
  const plain = code.replace(/[^0-9a-z]/gi, '').toUpperCase()
  return plain.length === 8 ? `${plain.slice(0, 4)}-${plain.slice(4)}` : code.toUpperCase()
}

export type DeviceStatusBadge = 'removed' | 'unpaired' | 'silent' | 'online' | 'offline'

/** Where a device stands, for its badge: removed, waiting for its code, silent (missing heartbeat), online or offline. */
export function deviceState(device: Device): DeviceStatusBadge {
  if (device.removed) return 'removed'
  if (!device.paired) return 'unpaired'
  if (device.state?.silent) return 'silent'
  return device.online ? 'online' : 'offline'
}

