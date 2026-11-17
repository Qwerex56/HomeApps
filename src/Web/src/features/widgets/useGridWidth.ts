import { onBeforeUnmount, onMounted, ref, type Ref } from 'vue'
import { columnsFor } from './layout'

export const GAP = 12

/**
 * The width of a grid's container and what follows from it: the number of columns (3, 5 or 8) and the height of a
 * row, which makes a field square, between 120 and 200 px.
 */
export function useGridWidth(element: Ref<HTMLElement | undefined>) {
  const width = ref(0)
  let observer: ResizeObserver | undefined
  onMounted(() => {
    observer = new ResizeObserver(([entry]) => (width.value = entry?.contentRect.width ?? 0))
    if (element.value) observer.observe(element.value)
  })
  onBeforeUnmount(() => observer?.disconnect())

  return { width, columnsFor, rowHeight: (cols: number) => rowHeightFor(width.value, cols) }
}

export function rowHeightFor(width: number, cols: number) {
  const column = (width - GAP * (cols - 1)) / cols
  return Math.round(Math.min(200, Math.max(120, column)))
}
