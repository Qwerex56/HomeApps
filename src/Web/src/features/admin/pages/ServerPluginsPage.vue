<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import Card from 'primevue/card'
import Button from 'primevue/button'
import Tag from 'primevue/tag'
import Message from 'primevue/message'
import Skeleton from 'primevue/skeleton'
import { APP_VERSION, localized } from '@/features/plugins/registry'
import { useServerPlugins, type PluginHealth, type PluginUi } from '../serverPlugins'

// W8: plugins come with the server (HOMEAPPS_PLUGINS + compose + restart); the Owner only looks at them here.
const { t } = useI18n()
const { data: plugins, isPending, isFetching, refetch } = useServerPlugins()

const healthSeverity: Record<PluginHealth, string> = { healthy: 'success', unhealthy: 'danger', unknown: 'secondary' }
const uiSeverity: Record<PluginUi, string> = {
  loaded: 'success',
  none: 'secondary',
  incompatible: 'warn',
  unavailable: 'danger',
  notRunning: 'danger',
}
</script>

<template>
  <Card>
    <template #title>
      <div class="flex items-center justify-between gap-2">
        <span>{{ t('admin.plugins.title') }}</span>
        <Button
          icon="pi pi-refresh"
          text
          rounded
          :loading="isFetching"
          :aria-label="t('admin.plugins.refresh')"
          @click="refetch()"
        />
      </div>
    </template>
    <template #subtitle>{{ t('admin.plugins.subtitle', { version: APP_VERSION }) }}</template>
    <template #content>
      <Skeleton v-if="isPending" height="6rem" />
      <Message v-else-if="!plugins?.length" severity="secondary" :closable="false">
        {{ t('admin.plugins.empty') }}
      </Message>
      <ul v-else class="flex flex-col divide-y divide-surface-200 dark:divide-surface-700">
        <li
          v-for="plugin in plugins"
          :key="plugin.id"
          class="flex flex-wrap items-center gap-3 py-3 first:pt-0 last:pb-0"
        >
          <i :class="['pi', plugin.manifest?.icon ?? 'pi-box', 'text-xl text-primary']" aria-hidden="true" />
          <div class="min-w-0 flex-1">
            <div class="font-medium">
              {{ plugin.manifest ? localized(plugin.manifest.name) : plugin.id }}
              <span class="text-sm text-muted-color">{{ plugin.id }}</span>
            </div>
            <div v-if="plugin.manifest" class="text-sm text-muted-color">
              {{ t('admin.plugins.version', { version: plugin.manifest.version }) }} ·
              {{ t('admin.plugins.requires', { version: plugin.manifest.requires.homeApps }) }}
              <span v-if="!plugin.compatible" class="text-orange-500">({{ t('admin.plugins.tooOld') }})</span>
            </div>
          </div>
          <div class="flex flex-wrap gap-2">
            <Tag :value="t(`admin.plugins.health.${plugin.health}`)" :severity="healthSeverity[plugin.health]" />
            <Tag :value="t(`admin.plugins.ui.${plugin.ui}`)" :severity="uiSeverity[plugin.ui]" />
          </div>
        </li>
      </ul>
      <p class="mt-4 text-sm text-muted-color">{{ t('admin.plugins.howToChange') }}</p>
    </template>
  </Card>
</template>
