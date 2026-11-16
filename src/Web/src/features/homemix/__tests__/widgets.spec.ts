import { describe, expect, it } from 'vitest'
import type { Installation } from '../api'
import { placedWidgets } from '../widgets'

// Which widgets stand in a place (docs/homemix.md, "Widgets").
function installation(id: string, widgetChoices: Installation['widgetChoices'] = []): Installation {
  return {
    id,
    widgets: [
      { id: 'timer', title: { pl: 'T', en: 'T' }, size: 'Small', sizes: ['Small', 'Medium'], places: ['Household', 'Wall'], entry: null },
      { id: 'stats', title: { pl: 'S', en: 'S' }, size: 'Large', sizes: ['Large'], places: ['Household'], entry: null },
    ],
    widgetChoices,
  } as unknown as Installation
}

describe('placedWidgets', () => {
  it('shows every widget of the place in its own size until someone chooses', () => {
    const placed = placedWidgets([installation('a')], 'Household')

    expect(placed.map((p) => [p.widget.id, p.size])).toEqual([
      ['timer', 'Small'],
      ['stats', 'Large'],
    ])
  })

  it('shows the chosen widget in the chosen size, nothing when off, in order', () => {
    const chosen = installation('a', [{ place: 'Household', widgetId: 'timer', size: 'Medium', order: 2 }])
    const off = installation('b', [{ place: 'Household', widgetId: null, size: null, order: 0 }])
    const first = installation('c', [{ place: 'Household', widgetId: 'stats', size: null, order: 1 }])

    const placed = placedWidgets([chosen, off, first], 'Household')

    expect(placed.map((p) => [p.installation.id, p.widget.id, p.size])).toEqual([
      ['c', 'stats', 'Large'],
      ['a', 'timer', 'Medium'],
    ])
  })
})
