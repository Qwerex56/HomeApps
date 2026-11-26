<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import PluginsSettings from '@/features/plugins/PluginsSettings.vue'
import HomeMixSettings from '@/features/homemix/HomeMixSettings.vue'
import type { Household } from '../api'

// 2.12: "Aplikacje domu", one place for the household's apps: the server's plugins (on or off) and its HomeMix
// add-ons (install, update, remove, widgets). For Owner and Admins only; SettingsTab decides who sees it.
const props = defineProps<{ household: Household }>()
const { t } = useI18n()
</script>

<template>
  <section id="apps" class="flex scroll-mt-20 flex-col gap-4" :aria-label="t('households.settings.apps')">
    <h2 class="text-xl font-semibold">{{ t('households.settings.apps') }}</h2>
    <PluginsSettings :household="props.household" />
    <HomeMixSettings :target="{ kind: 'Household', householdId: props.household.id }" can-manage />
  </section>
</template>
