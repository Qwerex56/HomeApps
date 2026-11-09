import { http } from '@/api/http'
import type { components } from '@/api/types/notifications'

// The Notifications service (docs/notifications.md): my devices, settings and mutes. Always the signed-in person.

type Schemas = components['schemas']
/** from/to as "22:00"; timeZone IANA, e.g. Europe/Warsaw. */
export type QuietHours = Schemas['QuietHoursView']
/** source: a plugin id such as "chat"; null for the whole household. */
export type Mute = Schemas['MuteView']
export type Device = Schemas['DeviceView']
export type NotificationSettings = Schemas['NotificationSettingsView']
export type DeliveryReport = Schemas['DeliveryReport']

export const notificationKeys = {
  settings: ['notifications', 'settings'] as const,
}

export const notificationsApi = {
  async vapidPublicKey() {
    return (await http.get<{ publicKey: string }>('/notifications/vapid-public-key')).data.publicKey
  },

  /** The browser's PushSubscription.toJSON(); the User-Agent goes along by itself (D6). */
  async registerDevice(subscription: PushSubscriptionJSON) {
    return (await http.put<{ id: string }>('/notifications/devices', subscription)).data.id
  },

  async forgetDevice(endpoint: string) {
    await http.delete('/notifications/devices', { params: { endpoint } })
  },

  async removeDevice(id: string) {
    await http.delete(`/notifications/devices/${id}`)
  },

  async settings() {
    return (await http.get<NotificationSettings>('/notifications/settings')).data
  },

  async changeSettings(showPreview: boolean, quietHours: QuietHours | null) {
    await http.put('/notifications/settings', { showPreview, quietHours })
  },

  async mute(householdId: string, source: string | null) {
    await http.put('/notifications/mutes', { householdId, source })
  },

  async unmute(householdId: string, source: string | null) {
    await http.delete('/notifications/mutes', { params: source ? { householdId, source } : { householdId } })
  },

  /** 409 notifications.no_devices when no device has notifications on. */
  async test() {
    return (await http.post<DeliveryReport>('/notifications/test')).data
  },
}
