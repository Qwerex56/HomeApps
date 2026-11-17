<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import Message from 'primevue/message'
import PluginWidgets from '@/features/plugins/PluginWidgets.vue'
import MixWidgets from '@/features/homemix/MixWidgets.vue'
import MembersTab from './MembersTab.vue'
import type { Household, HouseholdPermissions } from '../api'

// "Główna" (2.4.1, U3): the household's widgets and its people. A guest sees no members (D2), only the widgets.
const props = defineProps<{ household: Household; can: HouseholdPermissions }>()
const { t } = useI18n()

const widgetHousehold = computed(() =>
  props.household.myRole
    ? { id: props.household.id, name: props.household.name, myRole: props.household.myRole }
    : null,
)
</script>

<template>
  <div class="flex flex-col gap-6">
    <PluginWidgets v-if="widgetHousehold" :household="widgetHousehold" place="Household" />
    <MixWidgets v-if="widgetHousehold" :household="widgetHousehold" place="Household" />
    <MembersTab v-if="can.seeMembers" :household="household" :can="can" />
    <Message v-else severity="info" :closable="false">{{ t('households.settings.guestInfo') }}</Message>
  </div>
</template>
