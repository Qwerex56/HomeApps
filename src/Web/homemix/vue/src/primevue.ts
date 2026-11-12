// The HomeApps preset for PrimeVue 4 in an add-on: Aura with HomeApps' indigo. useHomeMix then applies the host's own
// CSS variables on top, so light, dark and any later change of the theme follow HomeApps.
//   app.use(PrimeVue, { theme: { preset: HomeAppsPreset, options: { darkModeSelector: '.app-dark' } } })
import Aura from '@primeuix/themes/aura'
import { definePreset } from '@primeuix/themes'

export const HomeAppsPreset = definePreset(Aura, {
  semantic: {
    primary: Object.fromEntries(
      [50, 100, 200, 300, 400, 500, 600, 700, 800, 900, 950].map((n) => [n, `{indigo.${n}}`]),
    ),
  },
})
