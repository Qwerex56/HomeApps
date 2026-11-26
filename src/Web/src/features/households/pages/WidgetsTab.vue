<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import Button from 'primevue/button'
import SelectButton from 'primevue/selectbutton'
import Skeleton from 'primevue/skeleton'
import { useHouseholdBoard, type BoardPlace } from '@/features/widgets/board'
import WidgetGrid from '@/features/widgets/WidgetGrid.vue'
import BoardEditor from '@/features/widgets/BoardEditor.vue'
import type { Household, HouseholdPermissions } from '../api'

// "Widżety" (UI 2.4.1): all of the household's widgets, without the row limit of "Główna", for its page or its wall
// tablet. Admins and up arrange them (W1).
const props = defineProps<{ household: Household; can: HouseholdPermissions }>()
const { t } = useI18n()

const place = ref<BoardPlace>('Household')
const places = computed(() => [
  { value: 'Household', label: t('widgets.place.Household') },
  { value: 'Wall', label: t('widgets.place.Wall') },
])
const widgetHousehold = computed(() =>
  props.household.myRole
    ? { id: props.household.id, name: props.household.name, myRole: props.household.myRole }
    : null,
)
const canArrange = computed(() => props.household.myRole === 'Admin' || props.household.myRole === 'Owner')
const editing = ref(false)
watch(place, () => (editing.value = false))

const page = useHouseholdBoard(widgetHousehold, 'Household')
const wall = useHouseholdBoard(widgetHousehold, 'Wall')
const board = computed(() => (place.value === 'Wall' ? wall : page))
</script>

<template>
  <div v-if="widgetHousehold" class="flex flex-col gap-4">
    <div class="flex flex-wrap items-center justify-between gap-3">
      <SelectButton
        v-model="place"
        :options="places"
        option-label="label"
        option-value="value"
        :allow-empty="false"
        :aria-label="t('widgets.placeLabel')"
      />
      <Button
        v-if="canArrange && !editing"
        :label="t('widgets.arrange')"
        icon="pi pi-th-large"
        outlined
        @click="editing = true"
      />
    </div>
    <p class="text-sm text-muted-color">{{ t(`widgets.hint.${place}`) }}</p>
    <Skeleton v-if="board.isPending.value" height="12rem" />
    <BoardEditor
      v-else-if="editing"
      :key="place"
      :household="widgetHousehold"
      :place="place"
      :cells="board.cells.value"
      :by-key="board.byKey.value"
      :updated-at="board.board.data.value?.updatedAt"
      @done="editing = false"
    />
    <WidgetGrid
      v-else
      :household="widgetHousehold"
      :place="place"
      :cells="board.cells.value"
      :by-key="board.byKey.value"
    />
  </div>
</template>
