import { onBeforeUnmount, onMounted, toValue, watch, type MaybeRefOrGetter } from 'vue'
import { HubConnectionBuilder, HubConnectionState, LogLevel, type HubConnection } from '@microsoft/signalr'
import { useQueryClient, type QueryClient } from '@tanstack/vue-query'
import { useToast } from 'primevue/usetoast'
import type { ToastServiceMethods } from 'primevue/toastservice'
import { useSessionStore } from '@/features/auth/session'
import { householdKeys } from '@/features/households/api'
import { mixKeys } from './api'

/** The events of /homemix/hub (docs/homemix.md, "RealTime"); every one names its installation or household. */
export type MixEvent =
  | {
      event: 'data.changed'
      data: { installationId: string; scope: 'household' | 'user'; collection: string; key: string; by: string }
    }
  | {
      event: 'installation.changed'
      data: {
        installationId: string
        addonId: string
        version: string
        change: 'installed' | 'updated' | 'removed' | 'blocked' | 'unblocked' | 'widgets'
      }
    }
  | {
      event: 'notification'
      data: { installationId: string; title: string; body: string | null; path: string | null; tag: string | null }
    }
  | { event: 'permissions.changed'; data: { householdId: string; role: string | null } }
  | { event: 'board.changed'; data: { householdId: string; place: 'Household' | 'Wall' } }

type Listener = (event: MixEvent) => void

// One connection per tab while anything HomeMix is on screen (or the app's layout keeps it for toasts). It is in the
// person's group from the start and in a household's group while a frame of that household is shown.
let connection: HubConnection | null = null
let starting: Promise<void> | null = null
let users = 0
let stopTimer: ReturnType<typeof setTimeout> | undefined
const watched = new Map<string, number>()
const listeners = new Set<Listener>()
let queryClient: QueryClient | null = null
let toast: ToastServiceMethods | null = null

function hub(token: () => Promise<string>) {
  const built = new HubConnectionBuilder()
    .withUrl('/homemix/hub', { accessTokenFactory: token })
    .withAutomaticReconnect()
    .configureLogging(LogLevel.Warning)
    .build()
  built.on('event', (event: MixEvent['event'], data: MixEvent['data']) => dispatch({ event, data } as MixEvent))
  // Groups are per connection: after a reconnect the households are watched again.
  built.onreconnected(() => joinAll(built))
  return built
}

/** What the app itself does on an event, once per tab; frames get theirs through their listeners. */
function dispatch(event: MixEvent) {
  if (event.event === 'installation.changed') void queryClient?.invalidateQueries({ queryKey: mixKeys.all })
  if (event.event === 'board.changed')
    void queryClient?.invalidateQueries({ queryKey: ['homemix', 'boards', event.data.householdId, event.data.place] })
  if (event.event === 'permissions.changed') {
    void queryClient?.invalidateQueries({ queryKey: householdKeys.all })
    void queryClient?.invalidateQueries({ queryKey: mixKeys.all })
  }
  if (event.event === 'notification') {
    toast?.add({ severity: 'info', summary: event.data.title, detail: event.data.body ?? undefined, life: 6000 })
  }
  for (const listener of listeners) listener(event)
}

function joinAll(on: HubConnection) {
  for (const id of watched.keys()) void on.invoke('WatchHousehold', id).catch(() => undefined)
}

async function start(token: () => Promise<string>) {
  clearTimeout(stopTimer)
  const current = (connection ??= hub(token))
  if (current.state === HubConnectionState.Disconnected) {
    starting = current
      .start()
      .then(() => joinAll(current))
      .catch(() => undefined)
  }
  await starting
}

function stopSoon() {
  // A route change unmounts one frame and mounts the next: keep the connection over that gap.
  clearTimeout(stopTimer)
  stopTimer = setTimeout(() => {
    if (users > 0 || !connection) return
    void connection.stop()
    connection = null
    watched.clear()
  }, 5000)
}

function watchHousehold(id: string) {
  const count = watched.get(id) ?? 0
  watched.set(id, count + 1)
  // Not connected yet: start() joins every watched household once it is.
  if (count === 0 && connection?.state === HubConnectionState.Connected)
    void connection.invoke('WatchHousehold', id).catch(() => undefined)
}

function unwatchHousehold(id: string) {
  const count = (watched.get(id) ?? 1) - 1
  if (count > 0) {
    watched.set(id, count)
    return
  }
  watched.delete(id)
  if (connection?.state === HubConnectionState.Connected) void connection.invoke('UnwatchHousehold', id).catch(() => undefined)
}

/**
 * Keeps the realtime connection for this component (call in setup): while it is mounted the tab hears the person's
 * events and, with `household`, that household's. `listener` gets every event; the caller filters what is its own.
 */
export function useMixRealtime(household?: MaybeRefOrGetter<string | null | undefined>, listener?: Listener) {
  const session = useSessionStore()
  queryClient ??= useQueryClient()
  toast ??= useToast()
  const token = async () => session.accessToken ?? (await session.refresh()) ?? ''

  watch(
    () => toValue(household),
    (now, before) => {
      if (before) unwatchHousehold(before)
      if (now) watchHousehold(now)
    },
    { immediate: true },
  )

  onMounted(() => {
    users++
    if (listener) listeners.add(listener)
    void start(token)
  })

  onBeforeUnmount(() => {
    users--
    if (listener) listeners.delete(listener)
    const id = toValue(household)
    if (id) unwatchHousehold(id)
    stopSoon()
  })
}
