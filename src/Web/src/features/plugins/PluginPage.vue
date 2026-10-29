<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import Message from 'primevue/message'
import Skeleton from 'primevue/skeleton'
import type { Household, HouseholdPermissions } from '@/features/households/api'
import PluginOutlet from './PluginOutlet.vue'
import { useHouseholdPlugins } from './registry'

// /homes/:id/apps/:plugin/:path — a plugin's page inside a household (docs/plugins.md, "UI").
const props = defineProps<{ household: Household; can: HouseholdPermissions; plugin: string; path?: string }>()
const { t } = useI18n()

const { plugins, enabled, isPending } = useHouseholdPlugins(() => props.household.id)
const known = computed(() => plugins.value?.find((p) => p.manifest.id === props.plugin))
const active = computed(() => enabled.value.find((p) => p.manifest.id === props.plugin))
const page = computed(() => active.value?.module?.routes?.[props.path ?? ''])
const household = computed(() => ({
  id: props.household.id,
  name: props.household.name,
  myRole: props.household.myRole,
}))
</script>

<template>
  <Skeleton v-if="isPending" height="10rem" />
  <PluginOutlet
    v-else-if="active && page"
    :key="`${plugin}/${path ?? ''}`"
    :plugin="active"
    :component="page"
    :household="household"
  />
  <Message v-else-if="known && !active" severity="info" :closable="false">{{ t('plugins.off') }}</Message>
  <Message v-else severity="warn" :closable="false">{{ t('plugins.notFound') }}</Message>
</template>
