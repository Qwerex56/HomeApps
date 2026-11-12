import type { Theme } from '@homemix/sdk'

// The theme an add-on gets (docs/homemix.md, "The bridge"): PrimeVue's tokens as CSS custom properties, read from the
// page so they follow light and dark. @homemix/vue applies them in the frame.
export const THEME_VARS = [
  '--p-primary-color',
  '--p-primary-contrast-color',
  '--p-primary-hover-color',
  '--p-text-color',
  '--p-text-muted-color',
  '--p-content-background',
  '--p-content-border-color',
  '--p-content-hover-background',
  '--p-highlight-background',
  '--p-highlight-color',
  '--p-form-field-background',
  '--p-form-field-border-color',
  '--p-border-radius-md',
  '--p-border-radius-xl',
  ...[0, 50, 100, 200, 300, 400, 500, 600, 700, 800, 900, 950].map((n) => `--p-surface-${n}`),
  ...[50, 100, 200, 300, 400, 500, 600, 700, 800, 900, 950].map((n) => `--p-primary-${n}`),
]

export function currentTheme(root: HTMLElement = document.documentElement): Theme {
  const style = getComputedStyle(root)
  const vars: Record<string, string> = {}
  for (const name of THEME_VARS) {
    const value = style.getPropertyValue(name).trim()
    if (value) vars[name] = value
  }
  return { dark: root.classList.contains('app-dark'), vars }
}
