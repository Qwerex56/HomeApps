import { describe, expect, it } from 'vitest'
import { nextTick } from 'vue'
import { useColorScheme } from '../useColorScheme'

describe('useColorScheme', () => {
  it('puts the dark class on <html> and remembers the choice', async () => {
    const { setScheme, isDark } = useColorScheme()

    setScheme('dark')
    await nextTick()
    expect(isDark.value).toBe(true)
    expect(document.documentElement.classList.contains('app-dark')).toBe(true)
    expect(localStorage.getItem('homeapps.colorScheme')).toBe('dark')

    setScheme('light')
    await nextTick()
    expect(document.documentElement.classList.contains('app-dark')).toBe(false)
  })
})
