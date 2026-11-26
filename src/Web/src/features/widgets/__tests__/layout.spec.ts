import { describe, expect, it } from 'vitest'
import { arrange, clip, columnsFor, firstFree, flow, nearest, sizeFor, widthsFor, widthOf, type Available, type Cell } from '../layout'

const widget = (key: string, w: number, widths = [w], fixedWidth = false): Available => ({ key, w, widths, fixedWidth })
const cell = (key: string, x: number, y: number, w = 1, h = 1): Cell => ({ key, x, y, w, h, fit: 'Center' })

describe('widget layout', () => {
  it('places widgets nobody arranged in the first free places, the household card first', () => {
    const cells = arrange([], [widget('core:household', 3, [1, 2, 3]), widget('a', 2), widget('b', 1), widget('c', 3)])

    expect(cells.map(({ key, x, y, w, h }) => [key, x, y, w, h])).toEqual([
      ['core:household', 0, 0, 3, 1],
      ['a', 3, 0, 2, 1],
      ['b', 5, 0, 1, 1],
      ['c', 0, 1, 3, 1],
    ])
  })

  it('keeps the saved places, drops gone widgets and moves one down when its add-on grew', () => {
    const saved = [cell('a', 0, 0, 1), cell('b', 1, 0, 1), cell('gone', 4, 0)]

    const cells = arrange(saved, [widget('a', 4, [3, 4, 5], true), widget('b', 1)])

    expect(cells).toEqual([cell('a', 0, 0, 4), cell('b', 1, 1, 1)])
  })

  it('snaps a free width to the sizes the widget has', () => {
    expect(nearest(3, [1, 2])).toBe(2)
    expect(nearest(9, [])).toBe(8)
    expect(nearest(2, [1, 3])).toBe(1)
    expect(arrange([cell('core:household', 6, 0, 3)], [widget('core:household', 3, [1, 2, 3]), widget('z', 1)])[0]).toMatchObject({
      x: 5,
      w: 3,
    })
  })

  it('flows into fewer columns in reading order and clips to the visible rows', () => {
    const cells = [cell('a', 0, 0, 3, 2), cell('b', 3, 0, 2), cell('c', 5, 0, 3)]

    const three = flow(cells, 3)

    expect(three.map(({ key, x, y, w }) => [key, x, y, w])).toEqual([
      ['a', 0, 0, 3],
      ['b', 0, 2, 2],
      ['c', 0, 3, 3],
    ])
    expect(clip(three, 2).map((c) => c.key)).toEqual(['a'])
    expect(firstFree(three, 1, 1, 3)).toEqual({ x: 2, y: 2 })
  })

  it('picks the columns from the width', () => {
    expect([columnsFor(390), columnsFor(700), columnsFor(888)]).toEqual([3, 5, 8])
  })

  it('maps a width to an add-on size: Small 1-2, Medium 3-5, Large 6-8', () => {
    expect([1, 2, 3, 5, 6, 8].map(sizeFor)).toEqual(['Small', 'Small', 'Medium', 'Medium', 'Large', 'Large'])
    expect(widthsFor(['Medium'])).toEqual([3, 4, 5])
    expect(sizeFor(widthOf.Medium)).toBe('Medium')
  })

  it('keeps a saved width inside the add-on size and falls back to the size when it left it', () => {
    const a = widget('a', 4, [3, 4, 5], true)

    expect(arrange([cell('a', 0, 0, 5)], [a, widget('b', 1)])[0]).toMatchObject({ w: 5 })
    expect(arrange([cell('a', 0, 0, 7)], [a, widget('b', 1)])[0]).toMatchObject({ w: 4 })
  })

  it('shows a single widget as Medium (4x3) in the middle', () => {
    const [only, ...rest] = arrange([cell('a', 0, 5, 1)], [widget('a', 2, [1, 2, 3, 4])])

    expect(rest).toEqual([])
    expect(only).toMatchObject({ key: 'a', x: 2, y: 0, w: 4, h: 3 })
  })

  it('does not put widgets back on an arranged board, even an empty one', () => {
    const widgets = [widget('a', 2), widget('b', 2)]

    expect(arrange([], widgets, true)).toEqual([])
    expect(arrange([cell('a', 0, 0, 2)], widgets, true).map((c) => c.key)).toEqual(['a'])
  })

  it('keeps the saved place and size of a single widget on an arranged board', () => {
    const [only] = arrange([cell('a', 1, 2, 5, 2)], [widget('a', 2, [1, 2, 3, 4, 5, 6, 7, 8])], true)

    expect(only).toMatchObject({ key: 'a', x: 1, y: 2, w: 5, h: 2 })
  })

  it('lets a free-width add-on widget take any width of its widths', () => {
    const a = widget('a', 4, [1, 2, 3, 4, 5, 6, 7, 8])

    expect(arrange([cell('a', 0, 0, 5), cell('b', 0, 1, 1)], [a, widget('b', 1)], true)[0]).toMatchObject({ w: 5 })
    expect(arrange([cell('a', 0, 0, 1), cell('b', 0, 1, 1)], [a, widget('b', 1)], true)[0]).toMatchObject({ w: 1 })
  })

  it('arranging a saved layout again changes nothing, so a refetch after a save cannot move a tile back', () => {
    const widgets = [widget('a', 4, [1, 2, 3, 4, 5, 6, 7, 8]), widget('b', 1)]
    const saved = [cell('a', 0, 0, 5, 1), cell('b', 0, 1, 1, 1)]

    const once = arrange(saved, widgets, true)

    expect(once[0]).toMatchObject({ key: 'a', w: 5 })
    expect(arrange(once, widgets, true)).toEqual(once)
  })
})
