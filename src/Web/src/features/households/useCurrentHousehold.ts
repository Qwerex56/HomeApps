import { computed, ref, watch } from 'vue'
import { useQuery } from '@tanstack/vue-query'
import { useSessionStore } from '@/features/auth/session'
import { householdKeys, householdsApi } from './api'

const STORAGE_KEY = 'homeapps.household'

// One choice for the whole app (Q6), remembered per person in this browser.
const chosen = ref<string | null>(null)
let loadedFor: string | null = null

function storageKey(userId: string) {
  return `${STORAGE_KEY}.${userId}`
}

/** My households (H2). Shared by the header, the dashboard and the household pages. */
export function useMyHouseholds() {
  const session = useSessionStore()
  return useQuery({
    queryKey: householdKeys.mine,
    queryFn: householdsApi.mine,
    enabled: computed(() => session.status === 'signedIn'),
  })
}

/**
 * The household the app shows (Q6): the one picked last, otherwise the first. Choosing one that is gone (left,
 * archived) quietly falls back to the first.
 */
export function useCurrentHousehold() {
  const session = useSessionStore()
  const { data: households, isPending } = useMyHouseholds()

  watch(
    () => session.claims?.sub ?? null,
    (userId) => {
      if (userId === loadedFor) return
      loadedFor = userId
      try {
        chosen.value = userId ? localStorage.getItem(storageKey(userId)) : null
      } catch {
        chosen.value = null
      }
    },
    { immediate: true },
  )

  const current = computed(() => {
    const list = households.value ?? []
    return list.find((h) => h.id === chosen.value) ?? list[0] ?? null
  })

  function select(id: string) {
    chosen.value = id
    const userId = session.claims?.sub
    if (!userId) return
    try {
      localStorage.setItem(storageKey(userId), id)
    } catch {
      // Private mode: the choice lasts until the page is closed.
    }
  }

  return { households, current, isPending, select }
}
