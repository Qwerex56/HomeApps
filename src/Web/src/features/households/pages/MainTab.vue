<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import Message from 'primevue/message'
import Skeleton from 'primevue/skeleton'
import { useHouseholdBoard } from '@/features/widgets/board'
import WidgetGrid from '@/features/widgets/WidgetGrid.vue'
import MembersTab from './MembersTab.vue'
import MemberNames from './MemberNames.vue'
import type { Household, HouseholdPermissions } from '../api'

// "Główna" (UI 2.4.1): the first rows of the household's widgets (3×2, 5×3 or 8×5, the household card among them)
// and its people. A guest sees the people by name, avatar and role only (2.9), no actions.
const props = defineProps<{ household: Household; can: HouseholdPermissions }>()
const { t } = useI18n()

const widgetHousehold = computed(() =>
  props.household.myRole
    ? { id: props.household.id, name: props.household.name, myRole: props.household.myRole }
    : null,
)
const { cells, byKey, isPending } = useHouseholdBoard(widgetHousehold, 'Household')
</script>

<template>
  <div class="flex flex-col gap-6">
    <template v-if="widgetHousehold">
      <Skeleton v-if="isPending" height="16rem" />
      <WidgetGrid v-else :household="widgetHousehold" place="Household" :cells="cells" :by-key="byKey" fixed />
    </template>
    <MembersTab v-if="can.seeMembers" :household="household" :can="can" />
    <MemberNames v-else-if="can.member" :household-id="household.id" />
    <Message v-else severity="info" :closable="false">{{ t('households.settings.guestInfo') }}</Message>
  </div>
</template>
