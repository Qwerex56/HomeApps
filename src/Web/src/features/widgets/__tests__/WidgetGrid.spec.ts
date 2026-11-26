import { describe, expect, it, vi } from 'vitest'
import { ref } from 'vue'
import { mount } from '@vue/test-utils'
import WidgetGrid from '../WidgetGrid.vue'
import type { BoardWidget } from '../board'

vi.mock('../useGridWidth', () => ({
  GAP: 12,
  useGridWidth: () => ({ width: ref(360), columnsFor: () => 3, rowHeight: () => 100 }),
}))

describe('WidgetGrid on a phone', () => {
  it('keeps a single widget visible in the fixed 3-column board', () => {
    const byKey = new Map([['a', { key: 'a', title: 'Alfa', kind: 'plugin' } as unknown as BoardWidget]])
    const wrapper = mount(WidgetGrid, {
      props: { household: { id: 'h1', name: 'Dom', myRole: 'Owner' }, place: 'Household', cells: [{ key: 'a', x: 0, y: 0, w: 4, h: 3, fit: 'Center' }], byKey, fixed: true, columns: 3 },
      global: { stubs: { WidgetTile: { template: '<div data-testid="tile" />' } } },
    })

    expect(wrapper.findAll('[data-testid="tile"]')).toHaveLength(1)
  })
})
