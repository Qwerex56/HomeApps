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

/** 2.13: one entry of the notification center; url is a path of the web app. */
export interface InAppNotification {
  id: string
  source: string
  category: string
  tag: string | null
  title: string
  body: string
  url: string
  householdId: string | null
  createdAt: string
  readAt: string | null
}
export interface NotificationList {
  unreadCount: number
  items: InAppNotification[]
}

export const notificationKeys = {
  settings: ['notifications', 'settings'] as const,
  inApp: ['notifications', 'in-app'] as const,
}

export const notificationsApi = {
  /** The notification center: the last 30 days, newest first. */
  async inApp(limit = 50) {
    return (await http.get<NotificationList>('/notifications/v1/in-app', { params: { limit } })).data
  },

  async markRead(id: string) {
    await http.post(`/notifications/v1/in-app/${id}/read`)
  },

  async markAllRead() {
    await http.post('/notifications/v1/in-app/read-all')
  },

  async vapidPublicKey() {
    return (await http.get<{ publicKey: string }>('/notifications/v1/vapid-public-key')).data.publicKey
  },

  /** The browser's PushSubscription.toJSON(); the User-Agent goes along by itself (D6). */
  async registerDevice(subscription: PushSubscriptionJSON) {
    return (await http.put<{ id: string }>('/notifications/v1/devices', subscription)).data.id
  },

  async forgetDevice(endpoint: string) {
    await http.delete('/notifications/v1/devices', { params: { endpoint } })
  },

  async removeDevice(id: string) {
    await http.delete(`/notifications/v1/devices/${id}`)
  },

  async settings() {
    return (await http.get<NotificationSettings>('/notifications/v1/settings')).data
  },

  async changeSettings(showPreview: boolean, quietHours: QuietHours | null) {
    await http.put('/notifications/v1/settings', { showPreview, quietHours })
  },

  async mute(householdId: string, source: string | null) {
    await http.put('/notifications/v1/mutes', { householdId, source })
  },

  async unmute(householdId: string, source: string | null) {
    await http.delete('/notifications/v1/mutes', { params: source ? { householdId, source } : { householdId } })
  },

  /** 409 notifications.no_devices when no device has notifications on. */
  async test() {
    return (await http.post<DeliveryReport>('/notifications/v1/test')).data
  },
}
