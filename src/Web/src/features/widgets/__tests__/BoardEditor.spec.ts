import { describe, expect, it, vi } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'
import { QueryClient, VueQueryPlugin } from '@tanstack/vue-query'
import PrimeVue from 'primevue/config'
import ToastService from 'primevue/toastservice'
import { i18n } from '@/shared/i18n'
import BoardEditor from '../BoardEditor.vue'
import type { BoardWidget } from '../board'

vi.mock('grid-layout-plus', async () => {
  const { defineComponent, h } = await import('vue')
  return {
    GridLayout: defineComponent({ setup: (_, { slots }) => () => h('div', slots.default?.()) }),
    GridItem: defineComponent({ setup: (_, { slots }) => () => h('div', slots.default?.()) }),
  }
})
vi.mock('../useGridWidth', async () => {
  const { ref } = await import('vue')
  return { GAP: 12, rowHeightFor: () => 120, useGridWidth: () => ({ width: ref(900) }) }
})
vi.mock('@/features/homemix/WidgetSettingsDialog.vue', () => ({ default: { render: () => null } }))

const widget = (key: string, title: string): BoardWidget =>
  ({ key, kind: 'plugin', title, icon: 'pi pi-box', widths: [2], w: 2, fixedWidth: true }) as unknown as BoardWidget

describe('widget palette', () => {
  it('moves a removed tile to the drawer and puts it back with "Dodaj"', async () => {
    const byKey = new Map([['a', widget('a', 'Alfa')], ['b', widget('b', 'Beta')]])
    const cells = [
      { key: 'a', x: 0, y: 0, w: 2, h: 1, fit: 'Center' as const },
      { key: 'b', x: 2, y: 0, w: 2, h: 1, fit: 'Center' as const },
    ]
    const wrapper = mount(BoardEditor, {
      attachTo: document.body,
      props: { household: { id: 'h1', name: 'Dom', myRole: 'Owner' }, place: 'Household', cells, byKey },
      global: {
        plugins: [i18n, PrimeVue, ToastService, [VueQueryPlugin, { queryClient: new QueryClient() }]],
      },
    })
    await flushPromises()

    await wrapper.findAll('button[aria-label="Zdejmij z siatki"]')[0]!.trigger('click')
    await wrapper.findAll('button').find((b) => b.text().includes('Paleta widżetów'))!.trigger('click')
    await flushPromises()

    const items = () => [...document.body.querySelectorAll('[data-testid="palette-item"]')]
    expect(items().map((i) => i.textContent)).toEqual([expect.stringContaining('Alfa')])

    ;(items()[0]!.querySelector('button') as HTMLButtonElement).click()
    await flushPromises()

    expect(items()).toHaveLength(0)
    wrapper.unmount()
  })
})
