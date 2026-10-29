<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useQueryClient } from '@tanstack/vue-query'
import { useToast } from 'primevue/usetoast'
import Card from 'primevue/card'
import ToggleSwitch from 'primevue/toggleswitch'
import Tag from 'primevue/tag'
import { hasRole } from '@homeapps/plugin-sdk'
import { errorMessage } from '@/api/errorMessage'
import { toProblem } from '@/api/problem'
import type { Household } from '@/features/households/api'
import PluginOutlet from './PluginOutlet.vue'
import { localized, pluginKeys, pluginsApi, useHouseholdPlugins, type LoadedPlugin } from './registry'

// "Aplikacje" in a household's settings: switch each plugin on or off (manifest.enablement.minRole) and, while it is
// on, the plugin's own settings section.
const props = defineProps<{ household: Household }>()
const { t } = useI18n()
const toast = useToast()
const queryClient = useQueryClient()

const { plugins, enabled } = useHouseholdPlugins(() => props.household.id)
const on = computed(() => new Set(enabled.value.map((p) => p.manifest.id)))
const target = computed(() => ({ id: props.household.id, name: props.household.name, myRole: props.household.myRole }))

function canSwitch(plugin: LoadedPlugin) {
  return (
    !plugin.problem &&
    !!plugin.manifest.enablement &&
    hasRole(props.household.myRole, plugin.manifest.enablement.minRole)
  )
}

async function toggle(plugin: LoadedPlugin, value: boolean) {
  try {
    const state = await pluginsApi.setEnabled(plugin.manifest, props.household.id, value)
    queryClient.setQueryData(pluginKeys.enablement(plugin.manifest.id, props.household.id), state)
    toast.add({
      severity: 'success',
      summary: t(value ? 'plugins.switchedOn' : 'plugins.switchedOff', { name: localized(plugin.manifest.name) }),
      life: 3000,
    })
  } catch (error) {
    toast.add({ severity: 'error', summary: errorMessage(toProblem(error)), life: 6000 })
  }
}
</script>

<template>
  <Card v-if="plugins?.length">
    <template #title>{{ t('plugins.title') }}</template>
    <template #content>
      <ul class="flex flex-col divide-y divide-surface-200 dark:divide-surface-700">
        <li v-for="plugin in plugins" :key="plugin.manifest.id" class="flex flex-col gap-3 py-3 first:pt-0 last:pb-0">
          <div class="flex items-center gap-3">
            <i :class="['pi', plugin.manifest.icon, 'text-xl text-primary']" aria-hidden="true" />
            <div class="min-w-0 flex-1">
              <div class="font-medium">{{ localized(plugin.manifest.name) }}</div>
              <div v-if="plugin.manifest.description" class="text-sm text-muted-color">
                {{ localized(plugin.manifest.description) }}
              </div>
            </div>
            <Tag v-if="plugin.problem" :value="t(`plugins.problem.${plugin.problem}`)" severity="warn" />
            <ToggleSwitch
              v-else-if="plugin.manifest.enablement"
              :model-value="on.has(plugin.manifest.id)"
              :disabled="!canSwitch(plugin)"
              :aria-label="localized(plugin.manifest.name)"
              @update:model-value="(value: boolean) => toggle(plugin, value)"
            />
          </div>
          <PluginOutlet
            v-if="on.has(plugin.manifest.id) && plugin.manifest.ui?.settings && plugin.module?.settings"
            :plugin="plugin"
            :component="plugin.module.settings"
            :household="target"
          />
        </li>
      </ul>
      <p
        v-if="plugins.some((p) => p.manifest.enablement && !canSwitch(p) && !p.problem)"
        class="mt-3 text-sm text-muted-color"
      >
        {{ t('plugins.whoSwitches') }}
      </p>
    </template>
  </Card>
</template>
