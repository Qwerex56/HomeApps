import { describe, expect, it } from 'vitest'
import { placedWidgets } from '../widgets'
import type { Installation } from '../api'

describe('add-ons that declare no widgets', () => {
  it('leave nothing on the board or in the palette', () => {
    const none = { widgets: [], widgetChoices: [] } as unknown as Installation

    expect(placedWidgets([none], 'Household')).toEqual([])
  })
})
