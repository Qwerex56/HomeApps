<script setup lang="ts">
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter, type RouteLocationRaw } from 'vue-router'
import Button from 'primevue/button'
import type { HouseholdRole, WidgetPlace } from '@homemix/sdk'
import { localized } from '@/features/plugins/registry'
import { useInstallations, type Installation, type MixTarget } from './api'
import { placedWidgets } from './widgets'
import MixFrame from './MixFrame.vue'
import WidgetSettingsDialog from './WidgetSettingsDialog.vue'

// The widgets of the add-ons installed here that stand in this place (docs/homemix.md, "Widgets"). Each has a header
// with the add-on's name, "Przejdź do aplikacji" and, for whoever manages the add-on, ⚙ to pick the widget and size.
const props = defineProps<{
  household: { id: string; name: string; myRole: HouseholdRole } | null
  place: WidgetPlace
  dark?: boolean
}>()
const { t } = useI18n()
const router = useRouter()

const target = computed<MixTarget>(() =>
  props.household ? { kind: 'Household', householdId: props.household.id } : { kind: 'User' },
)
const { data: installations } = useInstallations(target)
const widgets = computed(() => placedWidgets(installations.value ?? [], props.place))
// Admins and up choose a household's widgets (W1); my own add-ons are mine.
const canManage = computed(
  () => !props.household || props.household.myRole === 'Admin' || props.household.myRole === 'Owner',
)

const span: Record<string, string> = { Small: '', Medium: 'md:col-span-2', Large: 'md:col-span-3' }
const height: Record<string, number> = { Small: 160, Medium: 220, Large: 320 }

function appRoute(installation: Installation): RouteLocationRaw {
  const addon = installation.addonId
  const path = installation.pages[0]?.path ?? ''
  if (props.place === 'Wall') return { name: 'wall-mix', params: { addon, path } }
  return props.household
    ? { name: 'household-mix', params: { id: props.household.id, addon, path } }
    : { name: 'my-mix', params: { addon, path } }
}

const settingsFor = ref<Installation | null>(null)
const settingsOpen = computed({ get: () => !!settingsFor.value, set: (v) => !v && (settingsFor.value = null) })
</script>

<template>
  <div v-if="widgets.length" class="grid gap-4 md:grid-cols-3">
    <section
      v-for="{ installation, widget, size } in widgets"
      :key="`${installation.id}/${installation.version}/${widget.id}/${size}`"
      :class="[span[size], dark ? 'rounded-3xl bg-surface-900 p-8' : 'rounded-xl border border-surface p-4']"
    >
      <header class="mb-3 flex items-center gap-2">
        <!-- ⚙ in the top-left corner, as asked for HomeMix 2.0. -->
        <Button
          v-if="canManage"
          icon="pi pi-cog"
          size="small"
          text
          rounded
          severity="secondary"
          :aria-label="t('homemix.widgets.settings')"
          @click="settingsFor = installation"
        />
        <h2 class="flex min-w-0 flex-1 items-center gap-2 font-semibold" :class="dark ? 'text-2xl' : 'text-base'">
          <i :class="['pi', installation.icon]" aria-hidden="true" />
          <RouterLink v-if="installation.pages.length" :to="appRoute(installation)" class="truncate hover:underline">
            {{ localized(widget.title) }}
          </RouterLink>
          <span v-else class="truncate">{{ localized(widget.title) }}</span>
        </h2>
        <Button
          v-if="installation.pages.length"
          :label="t('homemix.widgets.goTo')"
          icon="pi pi-arrow-up-right"
          icon-pos="right"
          size="small"
          text
          @click="router.push(appRoute(installation))"
        />
      </header>
      <MixFrame
        :installation="installation"
        :household="household"
        page=""
        :file="widget.entry"
        :widget="{ id: widget.id, place, size }"
        :height="height[size]"
      />
    </section>
  </div>
  <WidgetSettingsDialog v-model:visible="settingsOpen" :installation="settingsFor" :target="target" />
</template>
