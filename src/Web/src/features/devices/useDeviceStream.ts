import { computed, onBeforeUnmount, onMounted, ref, shallowRef, toValue, watch, type MaybeRefOrGetter, type Ref } from 'vue'
import { useQueryClient, type QueryClient } from '@tanstack/vue-query'
import { useSessionStore } from '@/features/auth/session'
import { householdKeys } from '@/features/households/api'
import { deviceKeys, type Device, type DeviceEventItem, type DeviceStateInfo } from './api'
import { connectDeviceStreamOnce, DeviceStreamHttpError, type DeviceStreamEvent } from './stream'

export type DeviceStreamListener = (event: DeviceStreamEvent) => void

interface HouseholdStreamEntry {
  householdId: string
  subscribers: Set<{ listener?: DeviceStreamListener }>
  connected: Ref<boolean>
  liveLost: Ref<boolean>
  controller: AbortController | null
  reconnectTimer: ReturnType<typeof setTimeout> | null
  stoppedPermanently: boolean
  hasConnectedOnce: boolean
  start: () => void
  stop: () => void
}

const streams = new Map<string, HouseholdStreamEntry>()

function emptyState(deviceId: string, online = false): DeviceStateInfo {
  return {
    deviceId,
    values: {},
    valuesAt: null,
    online,
    silent: false,
    lowBattery: false,
    lastStatusAt: null,
    battery: null,
    voltage: null,
    rssi: null,
    uptime: null,
    firmware: null,
    error: null,
  }
}

function patchHouseholdCache(queryClient: QueryClient, householdId: string, event: DeviceStreamEvent) {
  if (event.type === 'ping') return
  if (event.type === 'changed') {
    void queryClient.invalidateQueries({ queryKey: deviceKeys.all })
    return
  }
  if (event.type === 'event') {
    const { deviceId, name, data, at, isLocation } = event.data
    queryClient.setQueryData<DeviceEventItem[]>(deviceKeys.events(deviceId), (prev) => {
      if (!prev) return prev
      const incoming: DeviceEventItem = {
        id: `${deviceId}-${at}-${name}-${prev.length}`,
        name,
        data,
        occurredAt: at,
        isLocation,
      }
      return [incoming, ...prev]
    })
    return
  }

  queryClient.setQueryData<Device[]>(deviceKeys.household(householdId), (devices) => {
    if (!devices) return devices
    return devices.map((device) => {
      if (device.id !== event.data.deviceId) return device
      const current = device.state ?? emptyState(device.id, device.online)
      if (event.type === 'online') {
        return {
          ...device,
          online: event.data.online,
          state: { ...current, online: event.data.online },
        }
      }
      if (event.type === 'state') {
        // A state frame means the device has reported: a pending setting it now reports is applied.
        const pending = current.pendingSettings
          ? Object.fromEntries(
              Object.entries(current.pendingSettings).filter(
                ([key, wanted]) =>
                  !(key in event.data.values) || JSON.stringify(event.data.values[key]) !== JSON.stringify(wanted),
              ),
            )
          : null
        return {
          ...device,
          lastSeenAt: event.data.at,
          state: {
            ...current,
            values: { ...current.values, ...event.data.values },
            valuesAt: event.data.at,
            pendingSettings: pending && Object.keys(pending).length ? pending : null,
          },
        }
      }
      if (event.type === 'status') {
        return {
          ...device,
          lastSeenAt: event.data.at,
          state: {
            ...current,
            silent: event.data.silent,
            lowBattery: event.data.lowBattery ?? current.lowBattery,
            lastStatusAt: event.data.at,
            battery: event.data.battery !== undefined ? event.data.battery : current.battery,
            voltage: event.data.voltage !== undefined ? event.data.voltage : current.voltage,
            rssi: event.data.rssi !== undefined ? event.data.rssi : current.rssi,
            uptime: event.data.uptime !== undefined ? event.data.uptime : current.uptime,
            firmware: event.data.firmware !== undefined ? event.data.firmware : current.firmware,
            error: event.data.error !== undefined ? event.data.error : current.error,
          },
        }
      }
      return device
    })
  })
}

function getOrCreateEntry(
  householdId: string,
  queryClient: QueryClient,
  accessToken: () => string | null,
  refresh: () => Promise<string | null>,
): HouseholdStreamEntry {
  const existing = streams.get(householdId)
  if (existing) return existing

  const connected = ref(false)
  const liveLost = ref(false)
  const subscribers = new Set<{ listener?: DeviceStreamListener }>()

  const entry: HouseholdStreamEntry = {
    householdId,
    subscribers,
    connected,
    liveLost,
    controller: null,
    reconnectTimer: null,
    stoppedPermanently: false,
    hasConnectedOnce: false,
    start() {
      if (entry.stoppedPermanently || entry.controller || subscribers.size === 0) return
      if (typeof document !== 'undefined' && document.visibilityState === 'hidden') return
      if (entry.reconnectTimer) {
        clearTimeout(entry.reconnectTimer)
        entry.reconnectTimer = null
      }

      const controller = new AbortController()
      entry.controller = controller
      const wasReconnect = entry.hasConnectedOnce

      void connectDeviceStreamOnce({
        householdId,
        accessToken,
        refresh,
        signal: controller.signal,
        onOpen() {
          connected.value = true
          liveLost.value = false
          entry.hasConnectedOnce = true
          if (wasReconnect) {
            void queryClient.invalidateQueries({ queryKey: deviceKeys.household(householdId) })
          }
        },
        onEvent(event) {
          patchHouseholdCache(queryClient, householdId, event)
          for (const sub of subscribers) sub.listener?.(event)
        },
      })
        .then(() => {
          connected.value = false
          if (entry.controller === controller) entry.controller = null
          if (!controller.signal.aborted && subscribers.size > 0 && !entry.stoppedPermanently) {
            // Normal 15-minute stream completion: silent immediate reconnect + refetch on open.
            entry.start()
          }
        })
        .catch((error) => {
          connected.value = false
          if (entry.controller === controller) entry.controller = null
          if (controller.signal.aborted) return
          if (error instanceof DeviceStreamHttpError && (error.status === 403 || error.status === 404)) {
            entry.stoppedPermanently = true
            liveLost.value = true
            if (error.status === 403) {
              void queryClient.invalidateQueries({ queryKey: householdKeys.all })
            }
            return
          }
          liveLost.value = true
          if (subscribers.size > 0 && !entry.stoppedPermanently) {
            entry.reconnectTimer = setTimeout(() => {
              entry.reconnectTimer = null
              entry.start()
            }, 3000)
          }
        })
    },
    stop() {
      if (entry.reconnectTimer) {
        clearTimeout(entry.reconnectTimer)
        entry.reconnectTimer = null
      }
      if (entry.controller) {
        entry.controller.abort()
        entry.controller = null
      }
      connected.value = false
    },
  }

  streams.set(householdId, entry)
  return entry
}

/**
 * Connects to the household's Devices SSE stream while mounted and the browser tab is visible.
 * Patches TanStack Query cache on `state`, `online`, `status`, `event`, and invalidates on `changed` or reconnect.
 */
export function useDeviceStream(
  householdId: MaybeRefOrGetter<string | null | undefined>,
  options?: {
    enabled?: MaybeRefOrGetter<boolean>
    onEvent?: DeviceStreamListener
  },
) {
  const session = useSessionStore()
  const queryClient = useQueryClient()
  const subscriber = { listener: options?.onEvent }
  let currentEntry: HouseholdStreamEntry | null = null
  let mounted = false

  const activeEntry = shallowRef<HouseholdStreamEntry | null>(null)
  const connected = computed(() => activeEntry.value?.connected.value ?? false)
  const liveLost = computed(() => activeEntry.value?.liveLost.value ?? false)

  function detach() {
    if (!currentEntry) return
    currentEntry.subscribers.delete(subscriber)
    if (currentEntry.subscribers.size === 0) {
      currentEntry.stop()
      streams.delete(currentEntry.householdId)
    }
    currentEntry = null
    activeEntry.value = null
  }

  function sync() {
    const id = toValue(householdId)
    const enabled = options?.enabled === undefined ? true : toValue(options.enabled)
    if (!mounted || !id || !enabled) {
      detach()
      return
    }
    if (currentEntry && currentEntry.householdId === id) {
      currentEntry.start()
      return
    }
    detach()
    const entry = getOrCreateEntry(
      id,
      queryClient,
      () => session.accessToken,
      () => session.refresh(),
    )
    currentEntry = entry
    activeEntry.value = entry
    entry.subscribers.add(subscriber)
    entry.start()
  }

  function onVisibilityChange() {
    if (!currentEntry) return
    if (typeof document !== 'undefined' && document.visibilityState === 'hidden') {
      currentEntry.stop()
    } else {
      currentEntry.start()
    }
  }

  watch(
    () => [toValue(householdId), options?.enabled === undefined ? true : toValue(options.enabled)] as const,
    () => sync(),
  )

  onMounted(() => {
    mounted = true
    if (typeof document !== 'undefined') {
      document.addEventListener('visibilitychange', onVisibilityChange)
    }
    sync()
  })

  onBeforeUnmount(() => {
    mounted = false
    if (typeof document !== 'undefined') {
      document.removeEventListener('visibilitychange', onVisibilityChange)
    }
    detach()
  })

  return { connected, liveLost }
}
