import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'
import { h } from 'vue'
import type { HomeMixContext, WidgetSize } from '@homemix/sdk'

// @homemix/vue's building blocks (homemix-2.md 3.3), over a fake bridge: the context says where the add-on stands.
const bridge = vi.hoisted(() => ({
  widget: null as { id: string; place: 'Household' | 'Wall' | 'Account'; size: 'Small' | 'Medium' | 'Large' } | null,
  open: vi.fn(async () => {}),
}))

vi.mock('@homemix/sdk', async (original) => {
  const actual = await original<typeof import('@homemix/sdk')>()
  return {
    ...actual,
    autoResize: () => () => {},
    connect: () => ({
      context: async () =>
        ({ widget: bridge.widget, theme: { dark: false, vars: {} }, permissions: [] }) as unknown as HomeMixContext,
      open: bridge.open,
      on: () => () => {},
    }),
  }
})

async function blocks(widget: typeof bridge.widget) {
  bridge.widget = widget
  vi.resetModules()
  const vue = await import('../../../../homemix/vue/src/index')
  vue.useHomeMix()
  await flushPromises()
  return vue
}

const inWidget = (size: WidgetSize, place: 'Household' | 'Wall' = 'Household') => ({ id: 'w', place, size })

describe('layout', () => {
  afterEach(() => document.documentElement.removeAttribute('class'))

  it('marks a page, a widget of a size and the wall tablet on <html>', async () => {
    await blocks(null)
    expect(document.documentElement.classList.contains('hm-page')).toBe(true)

    await blocks(inWidget('Small', 'Wall'))
    expect([...document.documentElement.classList]).toEqual(
      expect.arrayContaining(['hm-widget', 'hm-size-small', 'hm-wall']),
    )
    expect(document.documentElement.classList.contains('hm-page')).toBe(false)
  })
})

describe('HmWidget', () => {
  beforeEach(() => bridge.open.mockClear())

  it('opens the add-on when a widget is clicked', async () => {
    const { HmWidget } = await blocks(inWidget('Medium'))
    const card = mount(HmWidget, {
      props: { title: 'Zakupy', path: 'list' },
      slots: { default: () => h('p', 'treść') },
    })

    await card.trigger('click')

    expect(card.text()).toContain('Zakupy')
    expect(bridge.open).toHaveBeenCalledWith('list')
    expect(card.attributes('role')).toBe('link')
  })

  it('does not open anything on a page', async () => {
    const { HmWidget } = await blocks(null)
    const card = mount(HmWidget, { props: { title: 'Zakupy' } })

    await card.trigger('click')

    expect(bridge.open).not.toHaveBeenCalled()
  })
})

describe('HmList', () => {
  const items = Array.from({ length: 8 }, (_, i) => ({ key: String(i), title: `Rzecz ${i}` }))

  it('shows as many rows as the widget size fits, then +N', async () => {
    const { HmList } = await blocks(inWidget('Small'))
    const list = mount(HmList, { props: { items } })

    expect(list.findAll('li').map((li) => li.text())).toEqual(['Rzecz 0', 'Rzecz 1', 'Rzecz 2', '+5'])
  })

  it('shows every row on a page and the empty text without rows', async () => {
    const { HmList } = await blocks(null)

    expect(mount(HmList, { props: { items } }).findAll('li')).toHaveLength(8)
    expect(mount(HmList, { props: { items: [], empty: 'Pusto' } }).text()).toBe('Pusto')
  })
})

describe('HmStat and HmProgress', () => {
  it('shows a value with its trend, but no trend in a small widget', async () => {
    let { HmStat } = await blocks(inWidget('Medium'))
    expect(mount(HmStat, { props: { value: 12, label: 'Zadania', trend: -2 } }).text()).toBe('12▼ 2Zadania')
    ;({ HmStat } = await blocks(inWidget('Small')))
    expect(mount(HmStat, { props: { value: 12, label: 'Zadania', trend: -2 } }).text()).toBe('12Zadania')
  })

  it('caps progress at 100 percent', async () => {
    const { HmProgress } = await blocks(null)
    const bar = mount(HmProgress, { props: { value: 150, max: 100, label: 'Postęp' } })

    expect(bar.text()).toContain('100%')
    expect(bar.find('[role="progressbar"]').attributes('aria-valuenow')).toBe('150')
  })
})

describe('formatDuration and HmCountdown', () => {
  it('formats minutes and hours', async () => {
    const { formatDuration } = await blocks(null)

    expect(formatDuration(75_000)).toBe('01:15')
    expect(formatDuration(3_723_000)).toBe('1:02:03')
    expect(formatDuration(-5)).toBe('00:00')
  })

  it('counts down and says done once', async () => {
    vi.useFakeTimers()
    try {
      const { HmCountdown } = await blocks(null)
      const timer = mount(HmCountdown, { props: { until: Date.now() + 2_000 } })
      expect(timer.text()).toBe('00:02')

      await vi.advanceTimersByTimeAsync(3_000)

      expect(timer.text()).toBe('00:00')
      expect(timer.emitted('done')).toHaveLength(1)
    } finally {
      vi.useRealTimers()
    }
  })
})

describe('form fields', () => {
  it('binds an input, a select and a switch with v-model', async () => {
    const { HmInput, HmSelect, HmSwitch } = await blocks(null)
    const input = mount(HmInput, { props: { label: 'Ile', type: 'number', modelValue: 1, error: 'Za mało' } })
    const select = mount(HmSelect, {
      props: {
        modelValue: 'a',
        options: [
          { value: 'a', label: 'A' },
          { value: 'b', label: 'B' },
        ],
      },
    })
    const toggle = mount(HmSwitch, { props: { modelValue: false, label: 'Włącz' } })

    await input.find('input').setValue('3')
    await select.find('select').setValue('b')
    await toggle.find('input').setValue(true)

    expect(input.emitted('update:modelValue')?.[0]).toEqual([3])
    expect(input.find('label').attributes('for')).toBe(input.find('input').attributes('id'))
    expect(input.find('input').attributes('aria-invalid')).toBe('true')
    expect(select.emitted('update:modelValue')?.[0]).toEqual(['b'])
    expect(toggle.emitted('update:modelValue')?.[0]).toEqual([true])
  })
})
