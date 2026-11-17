<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useQueryClient } from '@tanstack/vue-query'
import { GridItem, GridLayout } from 'grid-layout-plus'
import Button from 'primevue/button'
import Message from 'primevue/message'
import { useSubmit } from '@/shared/composables/useSubmit'
import { homemixApi, mixKeys, type Installation } from '@/features/homemix/api'
import WidgetSettingsDialog from '@/features/homemix/WidgetSettingsDialog.vue'
import { boardKeys, boardsApi, type BoardPlace, type BoardWidget, type WidgetHousehold } from './board'
import { COLUMNS, FIXED_ROWS, defaultHeight, firstFree, nearest, sizeOf, type Cell, type Fit } from './layout'
import { GAP, rowHeightFor, useGridWidth } from './useGridWidth'

// The "Widżety" editor (UI 2.4.1): drag a widget to move it, drag its corner to change its width (snapped to the
// sizes it has) and height. The line marks what fits on "Główna" and the wall (8×5). One layout of 8 columns (P4).
const props = defineProps<{
  household: WidgetHousehold
  place: BoardPlace
  cells: Cell[]
  byKey: Map<string, BoardWidget>
}>()
const emit = defineEmits<{ done: [] }>()
const { t } = useI18n()
const queryClient = useQueryClient()
const { busy, formError, submit } = useSubmit()

type Item = { i: string; x: number; y: number; w: number; h: number; fit: Fit }
const layout = ref<Item[]>(props.cells.map(({ key, fit, ...rest }) => ({ i: key, fit, ...rest })))

const root = ref<HTMLElement>()
const { width } = useGridWidth(root)
const rowHeight = computed(() => rowHeightFor(width.value, COLUMNS))
const visibleLine = computed(() => FIXED_ROWS[COLUMNS]! * (rowHeight.value + GAP) + GAP / 2)

// ⚙ can turn an add-on's widget off or pick another one while the editor is open.
watch(
  () => props.byKey,
  (byKey) => {
    const kept = layout.value.filter((item) => byKey.has(item.i))
    for (const [key, w] of byKey) {
      if (kept.some((item) => item.i === key)) continue
      const h = defaultHeight(w.w)
      const at = firstFree(
        kept.map(({ i, ...rest }) => ({ key: i, ...rest })),
        w.w,
        h,
      )
      kept.push({ i: key, ...at, w: w.w, h, fit: 'Center' })
    }
    layout.value = kept
  },
)

function widget(item: Item) {
  return props.byKey.get(item.i)!
}

function snap(i: string | number, _h: number, w: number) {
  const item = layout.value.find((it) => it.i === i)
  if (!item) return
  item.w = nearest(w, widget(item).widths)
  if (item.x + item.w > COLUMNS) item.x = COLUMNS - item.w
}

function toggleFit(item: Item) {
  item.fit = item.fit === 'Fill' ? 'Center' : 'Fill'
}

const settingsFor = ref<Installation | null>(null)
const settingsOpen = computed({ get: () => !!settingsFor.value, set: (v) => !v && (settingsFor.value = null) })

async function save(items: Item[]) {
  const ok = await submit(async () => {
    await boardsApi.save(
      props.household.id,
      props.place,
      items.map(({ i, x, y, w, h, fit }) => ({ key: i, x, y, w, h, fit })),
    )
    // An add-on's width is its own size setting: a new width is a new size.
    for (const item of items) {
      const w = widget(item)
      if (w.kind === 'mix' && item.w !== w.w)
        await homemixApi.chooseWidget(w.installation.id, props.place, w.widget.id, sizeOf[item.w]!)
    }
  })
  await queryClient.invalidateQueries({ queryKey: boardKeys.one(props.household.id, props.place) })
  await queryClient.invalidateQueries({ queryKey: mixKeys.all })
  if (ok) emit('done')
}
</script>

<template>
  <div class="flex flex-col gap-4">
    <Message v-if="formError" severity="error" :closable="false">{{ formError }}</Message>
    <p class="text-sm text-muted-color md:hidden">{{ t('widgets.editor.narrow') }}</p>
    <div class="overflow-x-auto">
      <div ref="root" class="relative min-w-[720px]" :style="{ minHeight: `${visibleLine + 32}px` }">
        <div
          class="pointer-events-none absolute inset-x-0 z-[1] border-t-2 border-dashed border-primary"
          :style="{ top: `${visibleLine}px` }"
        >
          <span class="ml-2 bg-surface-0 px-2 text-xs text-primary dark:bg-surface-900">{{
            t('widgets.editor.visibleLine')
          }}</span>
        </div>
        <GridLayout
          v-if="width > 0"
          v-model:layout="layout"
          :col-num="COLUMNS"
          :row-height="rowHeight"
          :margin="[GAP, GAP]"
          :vertical-compact="false"
          :is-draggable="!busy"
          :is-resizable="!busy"
        >
          <GridItem
            v-for="item in layout"
            :key="item.i"
            :i="item.i"
            :x="item.x"
            :y="item.y"
            :w="item.w"
            :h="item.h"
            :min-w="Math.min(...widget(item).widths)"
            :max-w="Math.max(...widget(item).widths)"
            :max-h="12"
            @resized="snap"
          >
            <div
              class="flex h-full cursor-move flex-col gap-1 overflow-hidden rounded-xl border-2 border-dashed border-surface-300 bg-surface-0 p-3 dark:border-surface-600 dark:bg-surface-900"
            >
              <div class="flex items-center gap-2 text-sm font-semibold">
                <i :class="widget(item).icon" aria-hidden="true" />
                <span class="min-w-0 flex-1 truncate">{{
                  widget(item).kind === 'core' ? t('widgets.householdCard') : widget(item).title
                }}</span>
              </div>
              <div class="text-xs text-muted-color">
                {{ t(`homemix.widgets.size.${sizeOf[item.w]}`) }} · {{ t('widgets.editor.rows', item.h) }}
              </div>
              <div class="mt-auto flex flex-wrap gap-1">
                <Button
                  v-if="place === 'Wall'"
                  size="small"
                  text
                  :icon="item.fit === 'Fill' ? 'pi pi-arrows-alt' : 'pi pi-stop'"
                  :label="t(`widgets.fit.${item.fit}`)"
                  @click="toggleFit(item)"
                />
                <Button
                  v-if="widget(item).kind === 'mix'"
                  size="small"
                  text
                  rounded
                  severity="secondary"
                  icon="pi pi-cog"
                  :aria-label="t('homemix.widgets.settings')"
                  @click="settingsFor = (widget(item) as Extract<BoardWidget, { kind: 'mix' }>).installation"
                />
              </div>
            </div>
          </GridItem>
        </GridLayout>
      </div>
    </div>
    <div class="flex flex-wrap justify-end gap-2">
      <Button :label="t('widgets.editor.reset')" severity="secondary" text :loading="busy" @click="save([])" />
      <Button :label="t('common.cancel')" severity="secondary" text @click="emit('done')" />
      <Button :label="t('common.save')" icon="pi pi-check" :loading="busy" @click="save(layout)" />
    </div>
    <WidgetSettingsDialog
      v-model:visible="settingsOpen"
      :installation="settingsFor"
      :target="{ kind: 'Household', householdId: household.id }"
    />
  </div>
</template>
