<script setup lang="ts">
import { defineAsyncComponent, onErrorCaptured, provide, ref, shallowRef, watch, type Component } from 'vue'
import { useI18n } from 'vue-i18n'
import Message from 'primevue/message'
import { HOST_KEY, type PluginHousehold, type PluginModule } from '@homeapps/plugin-sdk'
import type { LoadedPlugin } from './registry'
import { createHost } from './host'

type Lazy = NonNullable<PluginModule['settings']>

// One piece of a plugin's UI (page, widget or settings) with its HostApi. A plugin that throws breaks only itself.
const props = defineProps<{
  plugin: LoadedPlugin
  component: Lazy
  household: PluginHousehold | null
  componentProps?: Record<string, unknown>
}>()
const { t } = useI18n()

provide(
  HOST_KEY,
  createHost(props.plugin.manifest, () => props.household),
)

function resolve(component: Lazy): Component {
  return typeof component === 'function' && !('setup' in component) && !('render' in component)
    ? defineAsyncComponent(async () => {
        const loaded = await (component as () => Promise<Component | { default: Component }>)()
        return 'default' in loaded ? (loaded.default as Component) : loaded
      })
    : (component as Component)
}

const view = shallowRef<Component>(resolve(props.component))
watch(
  () => props.component,
  (component) => (view.value = resolve(component)),
)

const failed = ref(false)
onErrorCaptured((error) => {
  console.error(`Plugin ${props.plugin.manifest.id} failed.`, error)
  failed.value = true
  return false
})
</script>

<template>
  <Message v-if="failed" severity="warn" :closable="false">{{ t('plugins.failed') }}</Message>
  <component :is="view" v-else v-bind="componentProps" />
</template>
