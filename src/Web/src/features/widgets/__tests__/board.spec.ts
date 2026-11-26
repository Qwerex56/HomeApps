import { afterEach, describe, expect, it, vi } from 'vitest'
import { defineComponent, h } from 'vue'
import { flushPromises, mount } from '@vue/test-utils'
import { QueryClient, VueQueryPlugin } from '@tanstack/vue-query'
import { boardKeys, boardsApi, useHouseholdBoard, type Board } from '../board'
import type { Cell } from '../layout'

const state = vi.hoisted(() => ({ installations: [] as unknown[] }))

vi.mock('@/features/homemix/api', async () => {
  const { ref } = await import('vue')
  return { useInstallations: () => ({ data: ref(state.installations) }) }
})
vi.mock('@/features/plugins/registry', () => ({
  localized: (text: { pl?: string } | null | undefined) => text?.pl ?? '',
  useHouseholdPlugins: () => ({ enabled: { value: [] } }),
}))

const household = { id: 'h1', name: 'Dom', myRole: 'Owner' as const }

/** A household add-on with one widget; `sizes` are the sizes the widget declares (none: any width). */
const addon = (sizes: string[] = []) => ({
  id: 'i1',
  icon: 'pi-cloud',
  targetKind: 'Household',
  widgetChoices: [],
  widgets: [{ id: 'w', title: { pl: 'Pogoda', en: 'Weather' }, places: ['Household'], size: 'Medium', sizes }],
})
const WIDGET = 'mix:i1/w'

function board(items: Board['items'], updatedAt: string | null): Board {
  return { place: 'Household', items, updatedAt } as unknown as Board
}

const cell = (key: string, x: number, y: number, w: number, h: number): Cell => ({ key, x, y, w, h, fit: 'Center' })

/** Mounts the composable the way the pages do, with the board as the server has it. */
async function useBoard(queryClient: QueryClient) {
  let api!: ReturnType<typeof useHouseholdBoard>
  mount(
    defineComponent({
      setup() {
        api = useHouseholdBoard(() => household, 'Household')
        return () => h('div')
      },
    }),
    { global: { plugins: [[VueQueryPlugin, { queryClient }]] } },
  )
  await flushPromises()
  return api
}

afterEach(() => {
  vi.restoreAllMocks()
  state.installations.length = 0
})

describe('board of a household (free widths, arranged boards)', () => {
  it('lets an add-on widget take any width between its narrowest and widest declared size', async () => {
    state.installations.push(addon(['Small', 'Large']))
    vi.spyOn(boardsApi, 'get').mockResolvedValue(board([], null))

    const api = await useBoard(new QueryClient())

    expect(api.widgets.value.find((w) => w.key === WIDGET)!.widths).toEqual([1, 2, 3, 4, 5, 6, 7, 8])
  })

  it('offers only the widths of the declared size when it declares one', async () => {
    state.installations.push(addon(['Medium']))
    vi.spyOn(boardsApi, 'get').mockResolvedValue(board([], null))

    const api = await useBoard(new QueryClient())

    expect(api.widgets.value.find((w) => w.key === WIDGET)!.widths).toEqual([3, 4, 5])
  })

  it('falls back to widths 1 to 8 when the add-on declares no size', async () => {
    state.installations.push(addon([]))
    vi.spyOn(boardsApi, 'get').mockResolvedValue(board([], null))

    const api = await useBoard(new QueryClient())

    expect(api.widgets.value.find((w) => w.key === WIDGET)!.widths).toEqual([1, 2, 3, 4, 5, 6, 7, 8])
  })

  it('arranges an unconfigured board: the household card first, the add-on next to it', async () => {
    state.installations.push(addon(['Small', 'Large']))
    vi.spyOn(boardsApi, 'get').mockResolvedValue(board([], null))

    const api = await useBoard(new QueryClient())

    expect(api.cells.value.map(({ key, x, y, w, h }) => [key, x, y, w, h])).toEqual([
      ['core:household', 0, 0, 4, 1],
      [WIDGET, 4, 0, 4, 1],
    ])
  })

  it('keeps an arranged board empty when every widget was taken off it', async () => {
    state.installations.push(addon(['Small', 'Large']))
    vi.spyOn(boardsApi, 'get').mockResolvedValue(board([], '2026-11-26T09:00:00Z'))

    const api = await useBoard(new QueryClient())

    expect(api.cells.value).toEqual([])
  })

  it('keeps the saved place and width of the only widget on an arranged board (not forced to the 4x3 middle)', async () => {
    vi.spyOn(boardsApi, 'get').mockResolvedValue(
      board([{ key: 'core:household', x: 1, y: 2, w: 5, h: 2, fit: 'Center' } as never], '2026-11-26T09:00:00Z'),
    )

    const api = await useBoard(new QueryClient())

    expect(api.cells.value).toEqual([cell('core:household', 1, 2, 5, 2)])
  })

  it('shows the only widget of an unconfigured board as the 4x3 default in the middle', async () => {
    vi.spyOn(boardsApi, 'get').mockResolvedValue(board([], null))

    const api = await useBoard(new QueryClient())

    expect(api.cells.value).toEqual([cell('core:household', 2, 0, 4, 3)])
  })

  it('shows a width changed by one column as saved, and a taken-off widget stays off, after the board is refetched', async () => {
    state.installations.push(addon(['Small', 'Large']))
    const saved = board([cell(WIDGET, 0, 0, 4, 1) as never, cell('core:household', 4, 0, 4, 1) as never], '2026-11-26T09:00:00Z')
    const get = vi.spyOn(boardsApi, 'get').mockResolvedValue(saved)
    const save = vi.spyOn(boardsApi, 'save')
    const queryClient = new QueryClient()
    const api = await useBoard(queryClient)
    const before = api.cells.value

    // The editor: widen the add-on from 4 to 5 columns, take the household card off, save.
    const after = board([cell(WIDGET, 0, 0, 5, 1) as never], '2026-11-26T10:00:00Z')
    save.mockResolvedValue(after)
    const returned = await boardsApi.save('h1', 'Household', [cell(WIDGET, 0, 0, 5, 1)], saved.updatedAt, false)
    queryClient.setQueryData(boardKeys.one('h1', 'Household'), returned)
    await flushPromises()
    const justSaved = api.cells.value

    // The realtime board.changed: the same board comes back from the server.
    get.mockResolvedValue(after)
    await queryClient.invalidateQueries({ queryKey: boardKeys.one('h1', 'Household') })
    await flushPromises()

    expect(before.find((c) => c.key === WIDGET)).toMatchObject({ x: 0, y: 0, w: 4 })
    expect(justSaved).toEqual([cell(WIDGET, 0, 0, 5, 1)])
    expect(api.cells.value).toEqual(justSaved)
    expect(api.cells.value.some((c) => c.key === 'core:household')).toBe(false)
  })
})
