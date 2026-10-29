import { defineAsyncComponent, type App, type Component } from 'vue'
import * as Vue from 'vue'
import * as VueRouter from 'vue-router'
import * as VueI18n from 'vue-i18n'
import * as Pinia from 'pinia'
import * as VueQuery from '@tanstack/vue-query'
declare global {
  // Keys: SHARED_MODULES in plugin-sdk/src/vite.ts.
  var __HOMEAPPS_SHARED__: Record<string, unknown> | undefined
}

/**
 * The app's own copies of the modules plugins share (plugin-sdk/src/vite.ts): a plugin's build reads them from here,
 * so there is one Vue, one router, one i18n and one query cache.
 */
export function sharePluginModules() {
  globalThis.__HOMEAPPS_SHARED__ = {
    vue: Vue,
    'vue-router': VueRouter,
    'vue-i18n': VueI18n,
    pinia: Pinia,
    '@tanstack/vue-query': VueQuery,
  }
}

/**
 * PrimeVue components a plugin uses by name in its templates (<Button>, <DatePicker>…), with the app's theme. Loaded
 * on first use, so the app's bundle does not grow. A plugin needs nothing else for them (docs/plugins.md, "UI").
 */
const components: Record<string, () => Promise<{ default: Component }>> = {
  Avatar: () => import('primevue/avatar'),
  Badge: () => import('primevue/badge'),
  Button: () => import('primevue/button'),
  Card: () => import('primevue/card'),
  Checkbox: () => import('primevue/checkbox'),
  Chip: () => import('primevue/chip'),
  DatePicker: () => import('primevue/datepicker'),
  Dialog: () => import('primevue/dialog'),
  Divider: () => import('primevue/divider'),
  Drawer: () => import('primevue/drawer'),
  InputNumber: () => import('primevue/inputnumber'),
  InputText: () => import('primevue/inputtext'),
  Menu: () => import('primevue/menu'),
  Message: () => import('primevue/message'),
  ProgressSpinner: () => import('primevue/progressspinner'),
  Select: () => import('primevue/select'),
  SelectButton: () => import('primevue/selectbutton'),
  Skeleton: () => import('primevue/skeleton'),
  Tag: () => import('primevue/tag'),
  Textarea: () => import('primevue/textarea'),
  ToggleSwitch: () => import('primevue/toggleswitch'),
}

export const PLUGIN_COMPONENTS = Object.keys(components)

export function registerPluginComponents(app: App) {
  for (const [name, load] of Object.entries(components)) {
    app.component(name, defineAsyncComponent(load))
  }
}
