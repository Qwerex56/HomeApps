// @homemix/vue building blocks (homemix-2.md 3.3): ready parts of a widget or page in the HomeApps look, drawn with the
// @homemix/css classes. Each knows the three widget sizes through useWidget() and the size classes on <html>.
// Written as render functions so the package needs no Vue compiler: <HmStat :value="3" label="Zadania" /> as usual.
import { computed, defineComponent, h, onScopeDispose, ref, watch, type PropType, type VNode } from 'vue'
import type { WidgetSize } from '@homemix/sdk'
import { useWidget } from './index.js'

let ids = 0
const nextId = (prefix: string) => `${prefix}-${++ids}`

/** How many rows a list shows in each widget size, unless it is given max. */
export const LIST_ROWS: Record<WidgetSize, number> = { Small: 3, Medium: 5, Large: 10 }

/** 75 seconds -> "01:15", an hour and more -> "1:02:03"; never negative. */
export function formatDuration(ms: number): string {
  const total = Math.max(0, Math.ceil(ms / 1000))
  const hours = Math.floor(total / 3600)
  const minutes = Math.floor((total % 3600) / 60)
  const seconds = total % 60
  const two = (n: number) => String(n).padStart(2, '0')
  return hours ? `${hours}:${two(minutes)}:${two(seconds)}` : `${two(minutes)}:${two(seconds)}`
}

/**
 * A widget's frame: a card with a title; in a widget the whole card opens the add-on (open(path)), by click or Enter.
 * Slots: default (the content), actions (next to the title, clicks there do not open), footer.
 */
export const HmWidget = defineComponent({
  name: 'HmWidget',
  props: {
    title: { type: String, default: '' },
    icon: { type: String, default: '' },
    /** The add-on's page to open; '' is its first page. */
    path: { type: String, default: '' },
    /** false: the card does not open the add-on (e.g. it has its own buttons everywhere). */
    clickable: { type: Boolean, default: true },
  },
  setup(props, { slots }) {
    const widget = useWidget()
    const opens = computed(() => props.clickable && widget.isWidget.value)
    const go = () => void widget.open(props.path || undefined)
    return () =>
      h(
        'section',
        {
          class: ['hm-card', 'hm-stack', { 'hm-clickable': opens.value }],
          role: opens.value ? 'link' : undefined,
          tabindex: opens.value ? 0 : undefined,
          onClick: opens.value ? go : undefined,
          onKeydown: opens.value ? (e: KeyboardEvent) => e.key === 'Enter' && go() : undefined,
        },
        [
          props.title || props.icon || slots.actions
            ? h('header', { class: 'hm-row hm-between' }, [
                h('h2', { class: 'hm-subtitle hm-truncate hm-grow' }, [
                  props.icon ? h('span', { 'aria-hidden': 'true' }, `${props.icon} `) : null,
                  props.title,
                ]),
                slots.actions
                  ? h('div', { class: 'hm-row', onClick: (e: Event) => e.stopPropagation() }, slots.actions())
                  : null,
              ])
            : null,
          slots.default?.(),
          slots.footer ? h('footer', { class: 'hm-muted hm-small' }, slots.footer()) : null,
        ],
      )
  },
})

/** One number with a caption and, optionally, a trend (+ up, - down). */
export const HmStat = defineComponent({
  name: 'HmStat',
  props: {
    value: { type: [String, Number], required: true },
    label: { type: String, default: '' },
    unit: { type: String, default: '' },
    /** The change since before, e.g. 3 or -2; shown with an arrow. */
    trend: { type: Number, default: null },
  },
  setup(props) {
    const { size } = useWidget()
    return () =>
      h('div', { class: 'hm-stack', style: { gap: '0.25rem' } }, [
        h('div', { class: 'hm-row', style: { alignItems: 'baseline', gap: '0.25rem' } }, [
          h('span', { class: 'hm-big' }, String(props.value)),
          props.unit ? h('span', { class: 'hm-muted' }, props.unit) : null,
          props.trend != null && props.trend !== 0 && size.value !== 'Small'
            ? h(
                'span',
                { class: ['hm-small', props.trend > 0 ? 'hm-trend-up' : 'hm-trend-down'] },
                `${props.trend > 0 ? '▲' : '▼'} ${Math.abs(props.trend)}`,
              )
            : null,
        ]),
        props.label ? h('span', { class: 'hm-muted hm-small' }, props.label) : null,
      ])
  },
})

export interface HmListItem {
  key: string
  title: string
  subtitle?: string
  icon?: string
  /** Text at the end of the row, e.g. a time or a count. */
  trailing?: string
}

/** A list of rows; in a widget only as many as fit its size (LIST_ROWS), then "+N". Slot item: a row of your own. */
export const HmList = defineComponent({
  name: 'HmList',
  props: {
    items: { type: Array as PropType<HmListItem[]>, required: true },
    /** Rows to show; defaults to LIST_ROWS for the widget's size, all on a page. */
    max: { type: Number, default: null },
    empty: { type: String, default: '' },
  },
  emits: ['select'],
  setup(props, { slots, emit }) {
    const widget = useWidget()
    const limit = computed(() => props.max ?? (widget.isWidget.value ? LIST_ROWS[widget.size.value] : Infinity))
    return () => {
      if (!props.items.length) return h(HmEmpty, { text: props.empty })
      const shown = props.items.slice(0, limit.value)
      const rest = props.items.length - shown.length
      return h('ul', { class: 'hm-list' }, [
        ...shown.map((item) =>
          h(
            'li',
            { key: item.key, onClick: () => emit('select', item) },
            slots.item?.({ item }) ?? [
              item.icon ? h('span', { 'aria-hidden': 'true' }, item.icon) : null,
              h('div', { class: 'hm-grow' }, [
                h('div', { class: 'hm-truncate' }, item.title),
                item.subtitle ? h('div', { class: 'hm-muted hm-small hm-truncate' }, item.subtitle) : null,
              ]),
              item.trailing ? h('span', { class: 'hm-muted hm-small' }, item.trailing) : null,
            ],
          ),
        ),
        rest > 0 ? h('li', { key: '+rest', class: 'hm-muted hm-small' }, `+${rest}`) : null,
      ])
    }
  },
})

/** Progress as a bar, or as a ring with the percentage in it (ring). */
export const HmProgress = defineComponent({
  name: 'HmProgress',
  props: {
    value: { type: Number, required: true },
    max: { type: Number, default: 100 },
    label: { type: String, default: '' },
    ring: { type: Boolean, default: false },
  },
  setup(props) {
    const part = computed(() => (props.max > 0 ? Math.min(1, Math.max(0, props.value / props.max)) : 0))
    const percent = computed(() => Math.round(part.value * 100))
    return () => {
      const aria = {
        role: 'progressbar',
        'aria-valuemin': 0,
        'aria-valuemax': props.max,
        'aria-valuenow': props.value,
        'aria-label': props.label || undefined,
      }
      if (props.ring) {
        const c = 2 * Math.PI * 16
        return h('div', { class: 'hm-stack', style: { alignItems: 'center', gap: '0.25rem' } }, [
          h('svg', { class: 'hm-ring', viewBox: '0 0 36 36', ...aria }, [
            h('circle', { cx: 18, cy: 18, r: 16, fill: 'none', stroke: 'var(--hm-surface-alt)', 'stroke-width': 4 }),
            h('circle', {
              cx: 18,
              cy: 18,
              r: 16,
              fill: 'none',
              stroke: 'var(--hm-primary)',
              'stroke-width': 4,
              'stroke-linecap': 'round',
              'stroke-dasharray': `${c * part.value} ${c}`,
              transform: 'rotate(-90 18 18)',
            }),
            h(
              'text',
              {
                x: 18,
                y: 18,
                'text-anchor': 'middle',
                'dominant-baseline': 'central',
                'font-size': 9,
                fill: 'currentColor',
              },
              `${percent.value}%`,
            ),
          ]),
          props.label ? h('span', { class: 'hm-muted hm-small' }, props.label) : null,
        ])
      }
      return h('div', { class: 'hm-stack', style: { gap: '0.25rem' } }, [
        props.label
          ? h('div', { class: 'hm-row hm-between hm-small' }, [
              h('span', props.label),
              h('span', { class: 'hm-muted' }, `${percent.value}%`),
            ])
          : null,
        h('div', { class: 'hm-progress', ...aria }, [h('span', { style: { width: `${percent.value}%` } })]),
      ])
    }
  },
})

/** A countdown to until (a time); emits done once at zero. */
export const HmCountdown = defineComponent({
  name: 'HmCountdown',
  props: {
    until: { type: [Date, String, Number] as PropType<Date | string | number>, required: true },
    label: { type: String, default: '' },
  },
  emits: ['done'],
  setup(props, { emit }) {
    const now = ref(Date.now())
    const end = computed(() => new Date(props.until).getTime())
    const left = computed(() => end.value - now.value)
    const timer = setInterval(() => (now.value = Date.now()), 1000)
    onScopeDispose(() => clearInterval(timer))
    let fired = false
    watch(end, () => (fired = false))
    watch(left, (ms) => {
      if (ms <= 0 && !fired) {
        fired = true
        emit('done')
      }
    })
    return () =>
      h('div', { class: 'hm-stack', style: { gap: '0.25rem' } }, [
        h('time', { class: 'hm-big', role: 'timer' }, formatDuration(left.value)),
        props.label ? h('span', { class: 'hm-muted hm-small' }, props.label) : null,
      ])
  },
})

/** Nothing here yet: a short text, an icon and, in the default slot, what to do about it. */
export const HmEmpty = defineComponent({
  name: 'HmEmpty',
  props: {
    text: { type: String, default: '' },
    icon: { type: String, default: '' },
  },
  setup(props, { slots }) {
    return () =>
      h('div', { class: 'hm-empty' }, [
        props.icon ? h('span', { style: { fontSize: '1.5em' }, 'aria-hidden': 'true' }, props.icon) : null,
        props.text ? h('span', props.text) : null,
        slots.default?.(),
      ])
  },
})

/** For the wall tablet: one big value with a title and a line under it, readable from across the room. */
export const HmWallTile = defineComponent({
  name: 'HmWallTile',
  props: {
    title: { type: String, default: '' },
    value: { type: [String, Number], required: true },
    subtitle: { type: String, default: '' },
    icon: { type: String, default: '' },
  },
  setup(props, { slots }) {
    const widget = useWidget()
    return () =>
      h(
        'section',
        {
          class: ['hm-card', 'hm-stack', { 'hm-clickable': widget.isWidget.value }],
          onClick: widget.isWidget.value ? () => void widget.open() : undefined,
        },
        [
          props.title
            ? h('div', { class: 'hm-muted hm-truncate' }, [props.icon ? `${props.icon} ` : '', props.title])
            : null,
          h('div', { class: 'hm-big' }, String(props.value)),
          props.subtitle ? h('div', { class: 'hm-muted' }, props.subtitle) : null,
          slots.default?.(),
        ],
      )
  },
})

export interface HmTab {
  id: string
  label: string
}

/** A page of the add-on: a title, actions, tabs (v-model:tab) and sections in the default slot. */
export const HmPage = defineComponent({
  name: 'HmPage',
  props: {
    title: { type: String, required: true },
    subtitle: { type: String, default: '' },
    tabs: { type: Array as PropType<HmTab[]>, default: () => [] },
    tab: { type: String, default: '' },
  },
  emits: ['update:tab'],
  setup(props, { slots, emit }) {
    return () =>
      h('main', { class: 'hm-stack' }, [
        h('header', { class: 'hm-row hm-between' }, [
          h('div', { class: 'hm-grow' }, [
            h('h1', { class: 'hm-title' }, props.title),
            props.subtitle ? h('p', { class: 'hm-muted', style: { margin: 0 } }, props.subtitle) : null,
          ]),
          slots.actions ? h('div', { class: 'hm-row' }, slots.actions()) : null,
        ]),
        props.tabs.length
          ? h(
              'nav',
              { class: 'hm-tabs', role: 'tablist' },
              props.tabs.map((t) =>
                h(
                  'button',
                  {
                    key: t.id,
                    type: 'button',
                    role: 'tab',
                    'aria-selected': String((props.tab || props.tabs[0]!.id) === t.id),
                    onClick: () => emit('update:tab', t.id),
                  },
                  t.label,
                ),
              ),
            )
          : null,
        slots.default?.(),
      ])
  },
})

function field(id: string, label: string, error: string, control: VNode) {
  return h('div', { class: 'hm-field' }, [
    label ? h('label', { class: 'hm-label', for: id }, label) : null,
    control,
    error ? h('span', { class: 'hm-error', id: `${id}-error` }, error) : null,
  ])
}

/** A text field with a label and an error under it (v-model). type: text, number, email, date, time... */
export const HmInput = defineComponent({
  name: 'HmInput',
  props: {
    modelValue: { type: [String, Number], default: '' },
    label: { type: String, default: '' },
    type: { type: String, default: 'text' },
    placeholder: { type: String, default: '' },
    error: { type: String, default: '' },
    required: { type: Boolean, default: false },
  },
  emits: ['update:modelValue'],
  setup(props, { emit }) {
    const id = nextId('hm-input')
    return () =>
      field(
        id,
        props.label,
        props.error,
        h('input', {
          id,
          class: 'hm-input',
          type: props.type,
          value: props.modelValue,
          placeholder: props.placeholder || undefined,
          required: props.required,
          'aria-invalid': props.error ? 'true' : undefined,
          'aria-describedby': props.error ? `${id}-error` : undefined,
          onInput: (e: Event) => {
            const value = (e.target as HTMLInputElement).value
            emit('update:modelValue', props.type === 'number' && value !== '' ? Number(value) : value)
          },
        }),
      )
  },
})

/** A date (YYYY-MM-DD) with the browser's own picker. */
export const HmDate = defineComponent({
  name: 'HmDate',
  props: {
    modelValue: { type: String, default: '' },
    label: { type: String, default: '' },
    error: { type: String, default: '' },
  },
  emits: ['update:modelValue'],
  setup(props, { emit }) {
    return () =>
      h(HmInput, {
        type: 'date',
        label: props.label,
        error: props.error,
        modelValue: props.modelValue,
        'onUpdate:modelValue': (v: string) => emit('update:modelValue', v),
      })
  },
})

export interface HmOption {
  value: string
  label: string
}

/** A choice from options (v-model). */
export const HmSelect = defineComponent({
  name: 'HmSelect',
  props: {
    modelValue: { type: String, default: '' },
    options: { type: Array as PropType<HmOption[]>, required: true },
    label: { type: String, default: '' },
    error: { type: String, default: '' },
  },
  emits: ['update:modelValue'],
  setup(props, { emit }) {
    const id = nextId('hm-select')
    return () =>
      field(
        id,
        props.label,
        props.error,
        h(
          'select',
          {
            id,
            class: 'hm-select',
            value: props.modelValue,
            onChange: (e: Event) => emit('update:modelValue', (e.target as HTMLSelectElement).value),
          },
          props.options.map((o) =>
            h('option', { key: o.value, value: o.value, selected: o.value === props.modelValue }, o.label),
          ),
        ),
      )
  },
})

/** On or off (v-model boolean), with its label next to it. */
export const HmSwitch = defineComponent({
  name: 'HmSwitch',
  props: {
    modelValue: { type: Boolean, default: false },
    label: { type: String, default: '' },
  },
  emits: ['update:modelValue'],
  setup(props, { emit }) {
    return () =>
      h('label', { class: 'hm-switch' }, [
        h('input', {
          type: 'checkbox',
          role: 'switch',
          checked: props.modelValue,
          onChange: (e: Event) => emit('update:modelValue', (e.target as HTMLInputElement).checked),
        }),
        h('span', { 'aria-hidden': 'true' }),
        props.label,
      ])
  },
})
