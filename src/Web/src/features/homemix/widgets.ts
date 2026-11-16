import type { WidgetPlace, WidgetSize } from '@homemix/sdk'
import type { AddonWidget, Installation } from './api'

export interface PlacedWidget {
  installation: Installation
  widget: AddonWidget
  size: WidgetSize
  order: number
}

/** The places a widget of this installation may be set for: a household's home page and wall, or my own page. */
export function placesOf(installation: Installation): WidgetPlace[] {
  return installation.targetKind === 'Household' ? ['Household', 'Wall'] : ['Account']
}

/**
 * The widgets that stand in a place (docs/homemix.md, "Widgets"). With no choice for the place, every widget that lists
 * it, in its default size; with one, that widget in the chosen size, or nothing when it was turned off.
 */
export function placedWidgets(installations: Installation[], place: WidgetPlace): PlacedWidget[] {
  return installations
    .flatMap((installation) => {
      const choice = installation.widgetChoices.find((c) => c.place === place)
      if (!choice)
        return installation.widgets
          .filter((w) => w.places.includes(place))
          .map((widget) => ({ installation, widget, size: widget.size as WidgetSize, order: 0 }))
      const widget = installation.widgets.find((w) => w.id === choice.widgetId)
      return widget
        ? [{ installation, widget, size: (choice.size ?? widget.size) as WidgetSize, order: Number(choice.order) }]
        : []
    })
    .map((placed, index) => ({ placed, index }))
    .sort((a, b) => a.placed.order - b.placed.order || a.index - b.index)
    .map(({ placed }) => placed)
}
