import { describe, expect, it } from 'vitest'
import { arrange, clip, columnsFor, firstFree, flow, nearest, type Available, type Cell } from '../layout'

const widget = (key: string, w: number, widths = [w], fixedWidth = false): Available => ({ key, w, widths, fixedWidth })
const cell = (key: string, x: number, y: number, w = 1, h = 1): Cell => ({ key, x, y, w, h, fit: 'Center' })

describe('widget layout', () => {
  it('places widgets nobody arranged in the first free places, the household card first', () => {
    const cells = arrange([], [widget('core:household', 3, [1, 2, 3]), widget('a', 2), widget('b', 1), widget('c', 3)])

    expect(cells.map(({ key, x, y, w, h }) => [key, x, y, w, h])).toEqual([
      ['core:household', 0, 0, 3, 2],
      ['a', 3, 0, 2, 1],
      ['b', 5, 0, 1, 1],
      ['c', 3, 1, 3, 2],
    ])
  })

  it('keeps the saved places, drops gone widgets and moves one down when its add-on grew', () => {
    const saved = [cell('a', 0, 0, 1), cell('b', 1, 0, 1), cell('gone', 4, 0)]

    const cells = arrange(saved, [widget('a', 2, [1, 2], true), widget('b', 1)])

    expect(cells).toEqual([cell('a', 0, 0, 2), cell('b', 1, 1, 1)])
  })

  it('snaps a free width to the sizes the widget has', () => {
    expect(nearest(3, [1, 2])).toBe(2)
    expect(nearest(2, [1, 3])).toBe(1)
    expect(arrange([cell('core:household', 6, 0, 3)], [widget('core:household', 3, [1, 2, 3])])[0]).toMatchObject({
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
})
