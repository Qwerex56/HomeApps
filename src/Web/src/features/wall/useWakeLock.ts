import { onBeforeUnmount, onMounted, ref } from 'vue'

/**
 * Keeps the screen on while the page is visible (Screen Wake Lock API). The browser drops the lock whenever the page
 * is hidden, so it is taken again on return. `awake` is false where the API is missing or refused (e.g. plain http).
 */
export function useWakeLock() {
  const awake = ref(false)
  let lock: WakeLockSentinel | null = null

  async function acquire() {
    if (!('wakeLock' in navigator) || document.visibilityState !== 'visible') return
    try {
      lock = await navigator.wakeLock.request('screen')
      awake.value = true
      lock.addEventListener('release', () => (awake.value = false))
    } catch {
      awake.value = false
    }
  }

  const onVisibility = () => void acquire()

  onMounted(() => {
    void acquire()
    document.addEventListener('visibilitychange', onVisibility)
  })
  onBeforeUnmount(() => {
    document.removeEventListener('visibilitychange', onVisibility)
    void lock?.release()
  })

  return awake
}
