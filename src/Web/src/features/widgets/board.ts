import { computed, toValue, type MaybeRefOrGetter } from 'vue'
import { useQuery } from '@tanstack/vue-query'
import type { HouseholdRole, WidgetSize } from '@home-apps/sdk'
import { http } from '@/api/http'
import type { components } from '@/api/types/homemix'
import { localized, useHouseholdPlugins, type LoadedPlugin } from '@/features/plugins/registry'
import { useInstallations, type AddonWidget, type Installation } from '@/features/homemix/api'
import { placedWidgets } from '@/features/homemix/widgets'
import { arrange, widthOf, type Available, type Cell, type Fit } from './layout'

export type BoardPlace = 'Household' | 'Wall'
export type Board = components['schemas']['BoardView']

export const boardKeys = {
  one: (householdId: string, place: BoardPlace) => ['homemix', 'boards', householdId, place] as const,
}

export const boardsApi = {
  async get(householdId: string, place: BoardPlace) {
    return (await http.get<Board>(`/homemix/api/v1/households/${householdId}/boards/${place}`)).data
  },
  async save(householdId: string, place: BoardPlace, items: Cell[]) {
    return (await http.put<Board>(`/homemix/api/v1/households/${householdId}/boards/${place}`, { items })).data
  },
}

export const HOUSEHOLD_CARD = 'core:household'

/** One widget that can stand on a household's board: the household card, an add-on's widget or a plugin's. */
export type BoardWidget = Available & { title: string; icon: string } & (
    | { kind: 'core' }
    | { kind: 'mix'; installation: Installation; widget: AddonWidget; size: WidgetSize }
    | { kind: 'plugin'; plugin: LoadedPlugin; widgetId: string; size: WidgetSize }
  )

export interface WidgetHousehold {
  id: string
  name: string
  myRole: HouseholdRole
}

/**
 * Everything on a household's board in one place, already arranged on the 8 columns: the saved layout fitted to the
 * widgets that exist now (layout.ts). `cells` keeps the board's order; `widgets` finds each cell's widget.
 */
export function useHouseholdBoard(household: MaybeRefOrGetter<WidgetHousehold | null>, place: BoardPlace) {
  const id = computed(() => toValue(household)?.id ?? '')
  const board = useQuery({
    queryKey: computed(() => boardKeys.one(id.value, place)),
    queryFn: () => boardsApi.get(id.value, place),
    enabled: computed(() => !!id.value),
  })
  const { data: installations } = useInstallations(() => ({ kind: 'Household', householdId: id.value }))
  const { enabled: plugins } = useHouseholdPlugins(() => id.value || null)

  const widgets = computed<BoardWidget[]>(() => {
    const core: BoardWidget = {
      key: HOUSEHOLD_CARD,
      kind: 'core',
      title: toValue(household)?.name ?? '',
      icon: 'pi pi-home',
      widths: [1, 2, 3],
      w: 3,
      fixedWidth: false,
    }
    const mixes = placedWidgets(installations.value ?? [], place).map(
      ({ installation, widget, size }): BoardWidget => ({
        key: `mix:${installation.id}/${widget.id}`,
        kind: 'mix',
        installation,
        widget,
        size,
        title: localized(widget.title),
        icon: `pi ${installation.icon}`,
        widths: widget.sizes.map((s) => widthOf[s as WidgetSize]),
        w: widthOf[size],
        fixedWidth: true,
      }),
    )
    const fromPlugins = plugins.value.flatMap((plugin) =>
      (plugin.manifest.ui?.widgets ?? [])
        .filter((w) => w.places.includes(place) && plugin.module?.widgets?.[w.id])
        .map((w): BoardWidget => ({
          key: `plugin:${plugin.manifest.id}/${w.id}`,
          kind: 'plugin',
          plugin,
          widgetId: w.id,
          size: w.size,
          title: localized(w.title),
          icon: `pi ${plugin.manifest.icon}`,
          widths: [widthOf[w.size]],
          w: widthOf[w.size],
          fixedWidth: true,
        })),
    )
    return [core, ...fromPlugins, ...mixes]
  })

  const cells = computed(() =>
    arrange(
      (board.data.value?.items ?? []).map((i) => ({
        key: i.key,
        x: Number(i.x),
        y: Number(i.y),
        w: Number(i.w),
        h: Number(i.h),
        fit: i.fit as Fit,
      })),
      widgets.value,
    ),
  )
  const byKey = computed(() => new Map(widgets.value.map((w) => [w.key, w])))

  return { board, widgets, byKey, cells, isPending: board.isPending }
}
