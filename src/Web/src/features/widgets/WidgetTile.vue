<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter, type RouteLocationRaw } from 'vue-router'
import PluginOutlet from '@/features/plugins/PluginOutlet.vue'
import MixFrame from '@/features/homemix/MixFrame.vue'
import HouseholdCard from './HouseholdCard.vue'
import type { BoardPlace, BoardWidget, WidgetHousehold } from './board'

// One widget in its cell: the household card, a plugin's component or an add-on's sandboxed frame, with a small
// header (icon, title, "Przejdź do aplikacji") for the last two. It fills the cell; the cell decides the size.
const props = defineProps<{ widget: BoardWidget; household: WidgetHousehold; place: BoardPlace; dark?: boolean }>()
const { t } = useI18n()
const router = useRouter()

const appRoute = computed<RouteLocationRaw | null>(() => {
  if (props.widget.kind !== 'mix' || !props.widget.installation.pages.length) return null
  const addon = props.widget.installation.addonId
  const path = props.widget.installation.pages[0]?.path ?? ''
  return props.place === 'Wall'
    ? { name: 'wall-mix', params: { addon, path } }
    : { name: 'household-mix', params: { id: props.household.id, addon, path } }
})
</script>

<template>
  <section
    class="flex h-full min-h-0 flex-col overflow-hidden"
    :class="
      dark ? 'rounded-3xl bg-surface-900 p-4' : 'rounded-xl border border-surface bg-surface-0 p-3 dark:bg-surface-900'
    "
  >
    <HouseholdCard v-if="widget.kind === 'core'" :household="household" :dark="dark" />
    <template v-else>
      <header class="mb-2 flex shrink-0 items-center gap-2" :class="dark ? 'text-xl' : 'text-sm'">
        <i :class="widget.icon" aria-hidden="true" />
        <h2 class="min-w-0 flex-1 truncate font-semibold">{{ widget.title }}</h2>
        <button
          v-if="appRoute"
          type="button"
          class="rounded-full p-1 text-muted-color hover:bg-emphasis hover:text-color"
          :aria-label="t('homemix.widgets.goTo')"
          :title="t('homemix.widgets.goTo')"
          @click="router.push(appRoute)"
        >
          <i class="pi pi-arrow-up-right" aria-hidden="true" />
        </button>
      </header>
      <div class="min-h-0 flex-1 overflow-auto">
        <MixFrame
          v-if="widget.kind === 'mix'"
          :installation="widget.installation"
          :household="household"
          page=""
          :file="widget.widget.entry"
          :widget="{ id: widget.widget.id, place, size: widget.size }"
          fill
        />
        <PluginOutlet
          v-else
          :plugin="widget.plugin"
          :component="widget.plugin.module!.widgets![widget.widgetId]!"
          :household="household"
          :component-props="{ householdId: household.id, place }"
        />
      </div>
    </template>
  </section>
</template>
