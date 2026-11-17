import { computed } from 'vue'
import type { RouteLocationRaw } from 'vue-router'
import { useInstallations, type Installation } from './api'

/** The most add-ons on the quick links bar (UI 2.4.1, P5). */
export const MAX_PINS = 8

/** Where an add-on of my account opens: its first page. */
export function appLink(installation: Installation): RouteLocationRaw {
  return { name: 'my-mix', params: { addon: installation.addonId, path: installation.pages[0]?.path ?? '' } }
}

/** My account's add-ons, and those on the quick links bar in their order. */
export function useMyApps() {
  const { data, isPending } = useInstallations({ kind: 'User' })
  const apps = computed(() => data.value ?? [])
  const pinned = computed(() => apps.value.filter((a) => a.pin != null).sort((a, b) => Number(a.pin) - Number(b.pin)))
  return { apps, pinned, isPending }
}
