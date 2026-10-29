import { computed, ref, watchEffect } from 'vue'

export type ColorScheme = 'system' | 'light' | 'dark'

const STORAGE_KEY = 'homeapps.colorScheme'
const DARK_CLASS = 'app-dark'

const scheme = ref<ColorScheme>(readStored())
const systemDark = ref(false)
let started = false
const isDark = computed(() => scheme.value === 'dark' || (scheme.value === 'system' && systemDark.value))

// Follows the system by default and remembers the user's choice in this browser only (Q8).
export function useColorScheme() {
  if (!started) {
    started = true
    const media = window.matchMedia?.('(prefers-color-scheme: dark)')
    systemDark.value = media?.matches ?? false
    media?.addEventListener('change', (e) => (systemDark.value = e.matches))
    watchEffect(() => document.documentElement.classList.toggle(DARK_CLASS, isDark.value))
  }

  return {
    scheme: computed(() => scheme.value),
    isDark,
    setScheme(value: ColorScheme) {
      scheme.value = value
      try {
        localStorage.setItem(STORAGE_KEY, value)
      } catch {
        // Private mode or blocked storage: the choice lasts until the page closes.
      }
    },
  }
}

function readStored(): ColorScheme {
  try {
    const stored = localStorage.getItem(STORAGE_KEY)
    return stored === 'light' || stored === 'dark' ? stored : 'system'
  } catch {
    return 'system'
  }
}
