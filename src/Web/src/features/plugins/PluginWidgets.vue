<script setup lang="ts">
import { computed } from 'vue'
import type { PluginHousehold, WidgetPlace } from '@homeapps/plugin-sdk'
import PluginOutlet from './PluginOutlet.vue'
import { localized, useHouseholdPlugins } from './registry'

// The widgets of every plugin that is on in the household and may stand here (manifest.ui.widgets[].places).
const props = defineProps<{ household: PluginHousehold; place: WidgetPlace; dark?: boolean }>()

const { enabled } = useHouseholdPlugins(() => props.household.id)
const widgets = computed(() =>
  enabled.value.flatMap((plugin) =>
    (plugin.manifest.ui?.widgets ?? [])
      .filter((w) => w.places.includes(props.place) && plugin.module?.widgets?.[w.id])
      .map((widget) => ({ plugin, widget, component: plugin.module!.widgets![widget.id]! })),
  ),
)
const span = { Small: '', Medium: 'md:col-span-2', Large: 'md:col-span-3' }

defineExpose({ count: computed(() => widgets.value.length) })
</script>

<template>
  <div v-if="widgets.length" class="grid gap-4 md:grid-cols-3">
    <section
      v-for="{ plugin, widget, component } in widgets"
      :key="`${plugin.manifest.id}/${widget.id}`"
      :class="[span[widget.size], dark ? 'rounded-3xl bg-surface-900 p-8' : 'rounded-xl border border-surface p-4']"
    >
      <h2 class="mb-3 flex items-center gap-2 font-semibold" :class="dark ? 'text-2xl' : 'text-base'">
        <i :class="['pi', plugin.manifest.icon]" aria-hidden="true" />{{ localized(widget.title) }}
      </h2>
      <PluginOutlet
        :plugin="plugin"
        :component="component"
        :household="household"
        :component-props="{ householdId: household.id, place }"
      />
    </section>
  </div>
</template>
