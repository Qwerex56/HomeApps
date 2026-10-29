import type { App } from 'vue'
import PrimeVue from 'primevue/config'
import ToastService from 'primevue/toastservice'
import ConfirmationService from 'primevue/confirmationservice'
import Aura from '@primeuix/themes/aura'
import { definePreset } from '@primeuix/themes'
import { primeVuePl } from '@/shared/i18n/primevue-pl'

// One theme for PrimeVue and Tailwind: tailwindcss-primeui exposes these tokens as classes (bg-primary, text-muted-color…).
const HomeAppsPreset = definePreset(Aura, {
  semantic: {
    primary: {
      50: '{indigo.50}',
      100: '{indigo.100}',
      200: '{indigo.200}',
      300: '{indigo.300}',
      400: '{indigo.400}',
      500: '{indigo.500}',
      600: '{indigo.600}',
      700: '{indigo.700}',
      800: '{indigo.800}',
      900: '{indigo.900}',
      950: '{indigo.950}',
    },
  },
})

export function installPrimeVue(app: App) {
  app.use(PrimeVue, {
    theme: {
      preset: HomeAppsPreset,
      options: {
        darkModeSelector: '.app-dark',
        cssLayer: { name: 'primevue', order: 'theme, base, primevue, components, utilities' },
      },
    },
    locale: primeVuePl,
  })
  app.use(ToastService)
  app.use(ConfirmationService)
}
