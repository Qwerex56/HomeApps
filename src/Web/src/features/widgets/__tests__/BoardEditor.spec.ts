import { describe, expect, it, vi } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'
import { QueryClient, VueQueryPlugin } from '@tanstack/vue-query'
import PrimeVue from 'primevue/config'
import ToastService from 'primevue/toastservice'
import { i18n } from '@/shared/i18n'
import BoardEditor from '../BoardEditor.vue'
import { GridItem } from 'grid-layout-plus'
import { boardKeys, boardsApi, type BoardWidget } from '../board'

vi.mock('grid-layout-plus', async () => {
  const { defineComponent, h } = await import('vue')
  return {
    GridLayout: defineComponent({ setup: (_, { slots }) => () => h('div', { 'data-testid': 'grid' }, slots.default?.()) }),
    GridItem: defineComponent({ setup: (_, { slots }) => () => h('div', slots.default?.()) }),
  }
})
vi.mock('../useGridWidth', async () => {
  const { ref } = await import('vue')
  return { GAP: 12, rowHeightFor: () => 120, useGridWidth: () => ({ width: ref(900) }) }
})
vi.mock('@/features/homemix/WidgetSettingsDialog.vue', () => ({ default: { render: () => null } }))

const widget = (key: string, title: string): BoardWidget =>
  ({ key, kind: 'plugin', title, icon: 'pi pi-box', widths: [1, 2, 3, 4], w: 2, fixedWidth: false }) as unknown as BoardWidget

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

  function mountEditor() {
    const byKey = new Map([['a', widget('a', 'Alfa')], ['b', widget('b', 'Beta')]])
    const cells = [
      { key: 'a', x: 0, y: 0, w: 2, h: 1, fit: 'Center' as const },
      { key: 'b', x: 2, y: 0, w: 2, h: 1, fit: 'Center' as const },
    ]
    return mount(BoardEditor, {
      attachTo: document.body,
      props: { household: { id: 'h1', name: 'Dom', myRole: 'Owner' }, place: 'Household', cells, byKey },
      global: { plugins: [i18n, PrimeVue, ToastService, [VueQueryPlugin, { queryClient: new QueryClient() }]] },
    })
  }

  it('puts a tile dragged from the drawer back on the grid with a drop', async () => {
    const wrapper = mountEditor()
    await flushPromises()
    await wrapper.findAll('button[aria-label="Zdejmij z siatki"]')[0]!.trigger('click')
    await wrapper.findAll('button').find((b) => b.text().includes('Paleta widżetów'))!.trigger('click')
    await flushPromises()
    expect(wrapper.findAllComponents(GridItem)).toHaveLength(1)

    const store: Record<string, string> = {}
    const dataTransfer = { setData: (k: string, v: string) => (store[k] = v), getData: (k: string) => store[k] }
    const item = document.body.querySelector('[data-testid="palette-item"]') as HTMLElement
    item.dispatchEvent(Object.assign(new Event('dragstart', { bubbles: true }), { dataTransfer }))
    expect(store['text/plain']).toBe('a')
    const grid = wrapper.find('[data-testid="grid"]').element.parentElement!
    grid.dispatchEvent(Object.assign(new Event('drop', { bubbles: true, cancelable: true }), { dataTransfer }))
    await flushPromises()

    expect(document.body.querySelectorAll('[data-testid="palette-item"]')).toHaveLength(0)
    expect(wrapper.findAllComponents(GridItem)).toHaveLength(2)
    wrapper.unmount()
  })

  it('snaps the width of a resized plugin tile to its allowed widths', async () => {
    const wrapper = mountEditor()
    await flushPromises()
    wrapper.findAllComponents(GridItem)[0]!.vm.$emit('resized', 'a', 1, 4)
    await flushPromises()

    expect(wrapper.findAllComponents(GridItem)[0]!.attributes('w')).toBe('4')
    wrapper.unmount()
  })

  describe('saving', () => {
    const board = { place: 'Household', items: [], updatedAt: '2026-11-26T10:00:00Z' }

    function mountWith(queryClient: QueryClient) {
      const byKey = new Map([['a', widget('a', 'Alfa')], ['b', widget('b', 'Beta')]])
      const cells = [
        { key: 'a', x: 0, y: 0, w: 2, h: 1, fit: 'Center' as const },
        { key: 'b', x: 2, y: 0, w: 2, h: 1, fit: 'Center' as const },
      ]
      return mount(BoardEditor, {
        attachTo: document.body,
        props: {
          household: { id: 'h1', name: 'Dom', myRole: 'Owner' },
          place: 'Household',
          cells,
          byKey,
          updatedAt: '2026-11-26T09:00:00Z',
        },
        global: { plugins: [i18n, PrimeVue, ToastService, [VueQueryPlugin, { queryClient }]] },
      })
    }

    const button = (wrapper: ReturnType<typeof mountWith>, label: string) =>
      wrapper.findAll('button').find((b) => b.text().includes(label))!

    it('sends the loaded date, puts the saved board in the cache and closes', async () => {
      const save = vi.spyOn(boardsApi, 'save').mockResolvedValue(board as never)
      const queryClient = new QueryClient()
      const wrapper = mountWith(queryClient)
      await flushPromises()

      await button(wrapper, 'Zapisz').trigger('click')
      await flushPromises()

      expect(save).toHaveBeenCalledWith('h1', 'Household', expect.any(Array), '2026-11-26T09:00:00Z', false)
      expect(queryClient.getQueryData(boardKeys.one('h1', 'Household'))).toEqual(board)
      expect(wrapper.emitted('done')).toHaveLength(1)
      wrapper.unmount()
    })

    it('"Ułóż od nowa" saves the default layout, not an empty one', async () => {
      const save = vi.spyOn(boardsApi, 'save').mockResolvedValue(board as never)
      const wrapper = mountWith(new QueryClient())
      await flushPromises()

      await button(wrapper, 'Ułóż od nowa').trigger('click')
      await flushPromises()

      expect(save.mock.calls[save.mock.calls.length - 1]![2]).toHaveLength(2)
      wrapper.unmount()
    })

    it('stays open on a 409, asks, and overwrites with force', async () => {
      const conflict = Object.assign(new Error('conflict'), {
        isAxiosError: true,
        response: { status: 409, headers: {}, data: { status: 409, code: 'homemix.board_conflict' } },
      })
      const save = vi.spyOn(boardsApi, 'save').mockRejectedValueOnce(conflict).mockResolvedValue(board as never)
      const wrapper = mountWith(new QueryClient())
      await flushPromises()

      await button(wrapper, 'Zapisz').trigger('click')
      await flushPromises()

      expect(wrapper.emitted('done')).toBeUndefined()
      expect(document.body.textContent).toContain('Układ zmienił się w międzyczasie')

      const overwrite = [...document.body.querySelectorAll('button')].find((b) => b.textContent?.includes('Nadpisz mimo to'))!
      overwrite.click()
      await flushPromises()

      expect(save.mock.calls[save.mock.calls.length - 1]![4]).toBe(true)
      expect(wrapper.emitted('done')).toHaveLength(1)
      wrapper.unmount()
    })

    it('treats a concurrency.conflict 409 like a board conflict', async () => {
      const conflict = Object.assign(new Error('conflict'), {
        isAxiosError: true,
        response: { status: 409, headers: {}, data: { status: 409, code: 'concurrency.conflict' } },
      })
      vi.spyOn(boardsApi, 'save').mockRejectedValueOnce(conflict).mockResolvedValue(board as never)
      const wrapper = mountWith(new QueryClient())
      await flushPromises()

      await button(wrapper, 'Zapisz').trigger('click')
      await flushPromises()

      expect(document.body.textContent).toContain('Układ zmienił się w międzyczasie')
      wrapper.unmount()
    })

    it('overwrites with the items that conflicted', async () => {
      const conflict = Object.assign(new Error('conflict'), {
        isAxiosError: true,
        response: { status: 409, headers: {}, data: { status: 409, code: 'homemix.board_conflict' } },
      })
      const save = vi.spyOn(boardsApi, 'save').mockRejectedValueOnce(conflict).mockResolvedValue(board as never)
      const wrapper = mountWith(new QueryClient())
      await flushPromises()

      await button(wrapper, 'Zapisz').trigger('click')
      await flushPromises()
      const first = save.mock.calls[0]![2]
      const overwrite = [...document.body.querySelectorAll('button')].find((b) => b.textContent?.includes('Nadpisz mimo to'))!
      overwrite.click()
      await flushPromises()

      expect(save.mock.calls[save.mock.calls.length - 1]![2]).toEqual(first)
      wrapper.unmount()
    })

    it('stays open with a message when the save fails', async () => {
      vi.spyOn(boardsApi, 'save').mockRejectedValue(new Error('boom'))
      const wrapper = mountWith(new QueryClient())
      await flushPromises()

      await button(wrapper, 'Zapisz').trigger('click')
      await flushPromises()

      expect(wrapper.emitted('done')).toBeUndefined()
      expect(wrapper.text()).toContain('Nie udało się zapisać układu')
      expect(wrapper.findAllComponents(GridItem)).toHaveLength(2)
      wrapper.unmount()
    })
  })
})
