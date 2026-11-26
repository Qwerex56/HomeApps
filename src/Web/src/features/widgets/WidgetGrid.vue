<script setup lang="ts">
import { computed, ref } from 'vue'
import type { BoardPlace, BoardWidget, WidgetHousehold } from './board'
import { clip, FIXED_ROWS, flow, type Cell } from './layout'
import { GAP, useGridWidth } from './useGridWidth'
import WidgetTile from './WidgetTile.vue'

// A household's widgets on the grid (UI 2.4.1). The 8-column layout flows into 5 or 3 columns on narrower screens.
// `fixed` shows only the first rows (3×2, 5×3, 8×5) as on "Główna" and the wall; `fill` stretches those rows over
// the parent's height (the wall), where `fit` decides whether a widget keeps its square proportions in the middle
// of its cell or fills it.
const props = defineProps<{
  household: WidgetHousehold
  place: BoardPlace
  cells: Cell[]
  byKey: Map<string, BoardWidget>
  fixed?: boolean
  fill?: boolean
  dark?: boolean
  /** The columns, when the caller knows better than the container's width (the wall uses the screen). */
  columns?: number
}>()

const root = ref<HTMLElement>()
const { width, columnsFor, rowHeight } = useGridWidth(root)
const cols = computed(() => props.columns ?? columnsFor(width.value))
const rows = computed(() => FIXED_ROWS[cols.value] ?? 5)
const shown = computed(() => {
  const flowed = flow(props.cells, cols.value)
  return props.fixed ? clip(flowed, rows.value) : flowed
})

const gridStyle = computed(() => ({
  gap: `${GAP}px`,
  gridTemplateColumns: `repeat(${cols.value}, minmax(0, 1fr))`,
  ...(props.fill
    ? { gridTemplateRows: `repeat(${rows.value}, minmax(0, 1fr))`, height: '100%' }
    : { gridAutoRows: `${rowHeight(cols.value)}px` }),
}))

function cellStyle(cell: Cell) {
  return { gridColumn: `${cell.x + 1} / span ${cell.w}`, gridRow: `${cell.y + 1} / span ${cell.h}` }
}

/** In a stretched cell a centred widget is the biggest box of its own proportions (square fields) that fits. */
function boxStyle(cell: Cell) {
  if (!props.fill || cell.fit === 'Fill') return { width: '100%', height: '100%' }
  return {
    width: `min(100cqw, calc(100cqh * ${cell.w} / ${cell.h}))`,
    height: `min(100cqh, calc(100cqw * ${cell.h} / ${cell.w}))`,
  }
}
</script>

<template>
  <div
    ref="root"
    class="w-full"
    :class="{ 'h-full': fill, 'mx-auto max-w-3xl': cells.length === 1 }"
    :data-single="cells.length === 1 || undefined"
  >
    <div v-if="width > 0" class="grid" :style="gridStyle">
      <div
        v-for="cell in shown"
        :key="cell.key"
        class="flex min-h-0 min-w-0 items-center justify-center"
        :style="[cellStyle(cell), fill ? { containerType: 'size' } : {}]"
      >
        <div :style="boxStyle(cell)">
          <WidgetTile :widget="byKey.get(cell.key)!" :household="household" :place="place" :dark="dark" />
        </div>
      </div>
    </div>
  </div>
</template>
