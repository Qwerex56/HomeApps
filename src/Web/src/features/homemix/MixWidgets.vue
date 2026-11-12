<script setup lang="ts">
import { computed } from 'vue'
import type { HouseholdRole, WidgetPlace } from '@homemix/sdk'
import { localized } from '@/features/plugins/registry'
import { useInstallations, type MixTarget } from './api'
import MixFrame from './MixFrame.vue'

// The widgets of the add-ons installed here that may stand in this place (homemix.json widgets[].places).
const props = defineProps<{
  household: { id: string; name: string; myRole: HouseholdRole } | null
  place: WidgetPlace
  dark?: boolean
}>()

const target = computed<MixTarget>(() =>
  props.household ? { kind: 'Household', householdId: props.household.id } : { kind: 'User' },
)
const { data: installations } = useInstallations(target)
const widgets = computed(() =>
  (installations.value ?? []).flatMap((installation) =>
    installation.widgets.filter((w) => w.places.includes(props.place)).map((widget) => ({ installation, widget })),
  ),
)
const span: Record<string, string> = { Small: '', Medium: 'md:col-span-2', Large: 'md:col-span-3' }
const height: Record<string, number> = { Small: 160, Medium: 220, Large: 320 }
</script>

<template>
  <div v-if="widgets.length" class="grid gap-4 md:grid-cols-3">
    <section
      v-for="{ installation, widget } in widgets"
      :key="`${installation.id}/${widget.id}`"
      :class="[span[widget.size], dark ? 'rounded-3xl bg-surface-900 p-8' : 'rounded-xl border border-surface p-4']"
    >
      <h2 class="mb-3 flex items-center gap-2 font-semibold" :class="dark ? 'text-2xl' : 'text-base'">
        <i :class="['pi', installation.icon]" aria-hidden="true" />{{ localized(widget.title) }}
      </h2>
      <MixFrame
        :installation="installation"
        :household="household"
        page=""
        :file="widget.entry"
        :widget="{ id: widget.id, place }"
        :height="height[widget.size]"
      />
    </section>
  </div>
</template>
