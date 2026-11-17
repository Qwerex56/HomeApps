/**
 * The widget grid of UI 2.4.1 (plans/features/ui-2.4.1.md): one layout of 8 columns per household and place, saved
 * by HomeMix. Narrower screens flow it into 5 or 3 columns; "Główna" and the wall tablet show only its first rows
 * (3×2, 5×3, 8×5). Width comes from the widget's size (Small 1, Medium 2, Large 3); height is set in the editor.
 */
import type { WidgetSize } from '@home-apps/sdk'

export const COLUMNS = 8
export type Fit = 'Center' | 'Fill'

export interface Cell {
  key: string
  x: number
  y: number
  w: number
  h: number
  fit: Fit
}

/** The visible grid of "Główna" and the wall tablet for a number of columns. */
export const FIXED_ROWS: Record<number, number> = { 3: 2, 5: 3, 8: 5 }

export const widthOf: Record<WidgetSize, number> = { Small: 1, Medium: 2, Large: 3 }
export const sizeOf: Record<number, WidgetSize> = { 1: 'Small', 2: 'Medium', 3: 'Large' }

/** Default height in rows: one, two for a large widget (P2). */
export function defaultHeight(w: number) {
  return w >= 3 ? 2 : 1
}

/** The columns for a container this wide: 3 on a phone, 5 on a tablet or a narrow window, 8 above. */
export function columnsFor(width: number) {
  return width < 560 ? 3 : width < 840 ? 5 : 8
}

function overlaps(a: Omit<Cell, 'fit' | 'key'>, b: Omit<Cell, 'fit' | 'key'>) {
  return a.x < b.x + b.w && b.x < a.x + a.w && a.y < b.y + b.h && b.y < a.y + a.h
}

/** The first free place for a w×h widget, row by row, left to right. */
export function firstFree(taken: Cell[], w: number, h: number, cols = COLUMNS) {
  for (let y = 0; ; y++) {
    for (let x = 0; x + w <= cols; x++) {
      if (!taken.some((c) => overlaps(c, { x, y, w, h }))) return { x, y }
    }
  }
}

export interface Available {
  key: string
  /** The widths this widget comes in. */
  widths: number[]
  /** Its width when the layout does not say (or must not say: an add-on's size is its own setting). */
  w: number
  /** Whether the width is fixed by the widget's own size choice (HomeMix add-ons). */
  fixedWidth: boolean
}

/**
 * The saved layout fitted to the widgets that exist now: gone ones are dropped, sizes follow the add-ons' settings,
 * anything that would now overlap moves down, and widgets not placed yet go to the first free places after.
 */
export function arrange(saved: readonly Cell[], available: readonly Available[]): Cell[] {
  const byKey = new Map(available.map((a) => [a.key, a]))
  const placed: Cell[] = []
  const ordered = [...saved].sort((a, b) => a.y - b.y || a.x - b.x)
  for (const cell of ordered) {
    const widget = byKey.get(cell.key)
    if (!widget || placed.some((p) => p.key === cell.key)) continue
    const w = widget.fixedWidth ? widget.w : nearest(cell.w, widget.widths)
    const x = Math.min(Math.max(cell.x, 0), COLUMNS - w)
    const h = Math.max(1, cell.h)
    let y = Math.max(0, cell.y)
    while (placed.some((p) => overlaps(p, { x, y, w, h }))) y++
    placed.push({ key: cell.key, x, y, w, h, fit: cell.fit === 'Fill' ? 'Fill' : 'Center' })
  }
  for (const widget of available) {
    if (placed.some((p) => p.key === widget.key)) continue
    const h = defaultHeight(widget.w)
    placed.push({ key: widget.key, ...firstFree(placed, widget.w, h), w: widget.w, h, fit: 'Center' })
  }
  return placed
}

/** The width closest to `w` that the widget has, the smaller one on a tie. */
export function nearest(w: number, widths: readonly number[]) {
  if (!widths.length) return Math.min(Math.max(w, 1), 3)
  return [...widths].sort((a, b) => Math.abs(a - w) - Math.abs(b - w) || a - b)[0]!
}

/** The 8-column layout flowed into fewer columns, in reading order. */
export function flow(cells: readonly Cell[], cols: number): Cell[] {
  if (cols >= COLUMNS) return [...cells]
  const placed: Cell[] = []
  for (const cell of [...cells].sort((a, b) => a.y - b.y || a.x - b.x)) {
    const w = Math.min(cell.w, cols)
    placed.push({ ...cell, ...firstFree(placed, w, cell.h, cols), w })
  }
  return placed
}

/** Only the widgets that fit wholly in the first `rows` rows. */
export function clip(cells: readonly Cell[], rows: number): Cell[] {
  return cells.filter((c) => c.y + c.h <= rows)
}
