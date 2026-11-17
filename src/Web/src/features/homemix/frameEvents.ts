import type { EventName, Events } from '@home-apps/sdk'
import type { MixEvent } from './realtime'

/** What a frame of this installation hears of a realtime event, or null when it is not its own. */
export function frameEvent(
  event: MixEvent,
  frame: { installationId: string; householdId: string | null },
): { name: EventName; data: Events[EventName] } | 'permissions' | null {
  switch (event.event) {
    case 'data.changed': {
      if (event.data.installationId !== frame.installationId) return null
      const { scope, collection, key, by } = event.data
      return { name: 'data.changed', data: { scope, collection, key, by } }
    }
    case 'installation.changed': {
      if (event.data.installationId !== frame.installationId || event.data.change === 'installed') return null
      const { change, version } = event.data
      return { name: 'installation.changed', data: { change, version } }
    }
    case 'notification': {
      if (event.data.installationId !== frame.installationId) return null
      const { title, body, path, tag } = event.data
      return { name: 'notification', data: { title, body, path, tag } }
    }
    case 'board.changed':
      // The household's layout is the app's own business.
      return null
    case 'permissions.changed':
      // The new permissions are asked for by the host before the frame hears about them.
      return event.data.householdId === frame.householdId ? 'permissions' : null
  }
}
