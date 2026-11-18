import { http } from '@/api/http'

// The Devices service (docs/devices.md): my computers, the household's satellites and their pairing codes.

export interface DeviceAction {
  name: string
  description: string
  readOnly: boolean
  /** The device asks before JARVIS runs it. */
  confirm: boolean
  /** "Nie pytaj": JARVIS runs it without asking (S6). */
  autoApprove: boolean
}

export interface Device {
  id: string
  name: string
  kind: 'Computer' | 'Satellite'
  householdId: string | null
  paired: boolean
  online: boolean
  removed: boolean
  createdAt: string
  lastSeenAt: string
  actions: DeviceAction[]
}

/** A one-time code for a satellite: 10 minutes, shown once. */
export interface PairingCode {
  deviceId: string
  name: string
  code: string
  expiresAt: string
}

export const deviceKeys = {
  all: ['devices'] as const,
  mine: ['devices', 'mine'] as const,
  household: (id: string) => ['devices', 'household', id] as const,
}

export const devicesApi = {
  async mine() {
    return (await http.get<Device[]>('/devices')).data
  },

  async household(householdId: string) {
    return (await http.get<Device[]>(`/devices/households/${householdId}`)).data
  },

  /** A household admin adds a satellite and gets its first pairing code. */
  async addSatellite(householdId: string, name: string) {
    return (await http.post<PairingCode>(`/devices/households/${householdId}`, { name })).data
  },

  /** A new code; the satellite's old pairing ends. */
  async newCode(deviceId: string) {
    return (await http.post<PairingCode>(`/devices/${deviceId}/pairing-code`)).data
  },

  /** Turned off, not deleted: its link drops and it may not connect again until restored. */
  async remove(deviceId: string) {
    await http.delete(`/devices/${deviceId}`)
  },

  async restore(deviceId: string) {
    await http.post(`/devices/${deviceId}/restore`)
  },

  async allow(deviceId: string, action: string, allowed: boolean) {
    await http.put(`/devices/${deviceId}/actions/${encodeURIComponent(action)}/allow`, { allowed })
  },
}

/** Pairing codes are 8 characters, shown as ABCD-EFGH; case and the dash do not matter to the service. */
export function formatPairingCode(code: string) {
  const plain = code.replace(/[^0-9a-z]/gi, '').toUpperCase()
  return plain.length === 8 ? `${plain.slice(0, 4)}-${plain.slice(4)}` : code.toUpperCase()
}

/** Where a device stands, for its badge: removed, waiting for its code, online or offline. */
export function deviceState(device: Device): 'removed' | 'unpaired' | 'online' | 'offline' {
  if (device.removed) return 'removed'
  if (!device.paired) return 'unpaired'
  return device.online ? 'online' : 'offline'
}
