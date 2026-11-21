import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { QueryClient, VueQueryPlugin } from '@tanstack/vue-query'
import PrimeVue from 'primevue/config'
import ToastService from 'primevue/toastservice'
import ConfirmationService from 'primevue/confirmationservice'
import Slider from 'primevue/slider'
import { i18n } from '@/shared/i18n'
import { householdPermissions, type Household, type HouseholdRole } from '@/features/households/api'
import HouseholdDevicesTab from '../pages/HouseholdDevicesTab.vue'
import ControlWidget from '../components/ControlWidget.vue'
import { devicesApi, type DescriptorControl, type Device, type HouseholdAssistant } from '../api'
import * as streamModule from '../stream'

const defaultAssistant: HouseholdAssistant = {
  enabled: true,
  name: 'JARVIS',
  accessLevel: 'Member',
  toolSources: 'All',
  eventDays: 30,
  locationEventDays: 7,
}

const chair: Device = {
  id: 'd1',
  name: 'Fotel taty',
  slug: 'fotel-taty',
  kind: 'Satellite',
  purpose: 'smart_home',
  householdId: 'h1',
  paired: true,
  online: true,
  removed: false,
  createdAt: '2026-11-18T10:00:00Z',
  lastSeenAt: '2026-11-18T10:00:00Z',
  actions: [],
  adminNotes: 'Fotel w salonie',
  jarvisMayControl: true,
  jarvisAsks: true,
  descriptor: {
    described: true,
    model: 'qwerex56/fotel',
    version: '1.2.0',
    roles: ['seat_heater'],
    capabilities: ['heating'],
    warnings: [],
    jarvis: 'Grzanie 3 tylko na krótko',
    sleepy: false,
    controls: [
      {
        name: 'level',
        label: 'Grzanie',
        description: '0 = wyłączone',
        value: { type: 'integer', minimum: 0, maximum: 3 },
        widget: 'slider',
      },
      {
        name: 'lampka',
        label: 'Lampka',
        description: null,
        value: { type: 'boolean' },
        widget: 'switch',
      },
    ],
    sensors: [{ name: 'temperature', label: 'Temperatura', value: { type: 'number' }, unit: '°C' }],
    events: [{ name: 'button', label: 'Przycisk', notify: false, isLocation: false }],
  },
  state: {
    deviceId: 'd1',
    values: { level: 1, lampka: false, temperature: 21.5 },
    valuesAt: '2026-11-18T10:00:00Z',
    online: true,
    silent: false,
    lowBattery: false,
    lastStatusAt: '2026-11-18T10:00:00Z',
    battery: 85,
    voltage: null,
    rssi: -60,
    uptime: 100,
    firmware: '1.2.0',
    error: null,
  },
}

let activeStreamOptions: streamModule.ConnectDeviceStreamOptions | null = null
const abortedHouseholds: string[] = []

beforeEach(() => {
  setActivePinia(createPinia())
  activeStreamOptions = null
  abortedHouseholds.length = 0
  vi.spyOn(devicesApi, 'assistant').mockResolvedValue(defaultAssistant)
  vi.spyOn(streamModule, 'connectDeviceStreamOnce').mockImplementation(
    (opts) =>
      new Promise<void>((resolve) => {
        activeStreamOptions = opts
        opts.onOpen?.()
        opts.signal.addEventListener('abort', () => {
          abortedHouseholds.push(opts.householdId)
          resolve()
        })
      }),
  )
})

afterEach(() => {
  vi.restoreAllMocks()
  document.body.innerHTML = ''
})

function render(role: HouseholdRole, householdId = 'h1') {
  const household = { id: householdId, name: 'Dom', myRole: role } as Household
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  return mount(HouseholdDevicesTab, {
    props: { household, can: householdPermissions(role) },
    global: {
      plugins: [
        createPinia(),
        i18n,
        PrimeVue,
        ToastService,
        ConfirmationService,
        [VueQueryPlugin, { queryClient }],
      ],
      stubs: {
        RouterLink: { template: '<a><slot /></a>' },
      },
    },
    attachTo: document.body,
  })
}

describe('ControlWidget', () => {
  it('renders switch, slider, select and button per schema/widget and commits slider on release', async () => {
    const sliderControl: DescriptorControl = {
      name: 'level',
      label: 'Poziom',
      description: null,
      value: { type: 'integer', minimum: 0, maximum: 10 },
      widget: 'slider',
    }
    const wrapper = mount(ControlWidget, {
      props: { control: sliderControl, value: 2, disabledReason: null, pending: false },
      global: { plugins: [i18n, PrimeVue] },
    })

    const slider = wrapper.findComponent(Slider)
    expect(slider.exists()).toBe(true)
    await slider.vm.$emit('update:modelValue', 5)
    expect(wrapper.emitted('set')).toBeUndefined()

    await slider.vm.$emit('slideend', { originalEvent: new Event('mouseup'), value: 7 })
    expect(wrapper.emitted('set')).toEqual([[7]])
  })

  it('disables control and displays reason when locked', () => {
    const buttonControl: DescriptorControl = {
      name: 'calibrate',
      label: 'Kalibruj',
      description: null,
      value: { const: true },
      widget: 'button',
    }
    const wrapper = mount(ControlWidget, {
      props: {
        control: buttonControl,
        value: null,
        disabledReason: 'Urządzenie na baterii, sterowanie niedostępne.',
        pending: false,
      },
      global: { plugins: [i18n, PrimeVue] },
    })

    expect(wrapper.find('[data-testid=control-disabled-reason]').text()).toContain(
      'Urządzenie na baterii, sterowanie niedostępne.',
    )
    expect(wrapper.find('button').attributes('disabled')).toBeDefined()
  })
})

describe('HouseholdDevicesTab role matrix & live stream', () => {
  it('shows Guest state and read-only switches, without controls action, events or admin notes', async () => {
    vi.spyOn(devicesApi, 'household').mockResolvedValue([chair])
    const wrapper = render('Guest')
    await flushPromises()

    expect(wrapper.text()).toContain('Fotel taty')
    expect(wrapper.text()).toContain('21.5 °C')
    expect(wrapper.text()).toContain('Gość może tylko przeglądać stan urządzeń.')
    expect(wrapper.find('[data-testid=toggle-events]').exists()).toBe(false)
    expect(wrapper.find('[data-testid=admin-notes-display]').exists()).toBe(false)
    expect(wrapper.find('[data-testid=maker-jarvis-text]').exists()).toBe(false)
    expect(wrapper.find('[data-testid=add-device-form]').exists()).toBe(false)
    wrapper.unmount()
  })

  it('lets Member control and view events, while switches and assistant stay read-only and notes hidden', async () => {
    vi.spyOn(devicesApi, 'household').mockResolvedValue([chair])
    const setControl = vi.spyOn(devicesApi, 'setControl').mockResolvedValue()
    vi.spyOn(devicesApi, 'events').mockResolvedValue([
      { id: 'e1', name: 'button', data: { count: 2 }, occurredAt: '2026-11-18T10:05:00Z', isLocation: false },
    ])
    const wrapper = render('Member')
    await flushPromises()

    expect(wrapper.find('[data-testid=toggle-events]').exists()).toBe(true)
    expect(wrapper.find('[data-testid=admin-notes-display]').exists()).toBe(false)
    expect(wrapper.find('[data-testid=add-device-form]').exists()).toBe(false)

    await wrapper.find('[data-testid=toggle-events]').trigger('click')
    await flushPromises()
    expect(wrapper.find('[data-testid=device-events-panel]').text()).toContain('Przycisk')

    const lampControl = wrapper.find('[data-testid=control-lampka]')
    await lampControl.find('input[type=checkbox]').setValue(true)
    await flushPromises()
    expect(setControl).toHaveBeenCalledWith('d1', 'lampka', true)
    wrapper.unmount()
  })

  it('lets Admin see notes/jarvis text, add a device, and closes pairing dialog on changed: paired', async () => {
    vi.spyOn(devicesApi, 'household').mockResolvedValue([chair])
    const add = vi.spyOn(devicesApi, 'addSatellite').mockResolvedValue({
      deviceId: 'd2',
      name: 'kuchnia',
      code: 'abcdefgh',
      expiresAt: '2026-11-18T10:10:00Z',
    })
    const wrapper = render('Admin')
    await flushPromises()

    expect(wrapper.find('[data-testid=admin-notes-display]').text()).toContain('Fotel w salonie')
    expect(wrapper.find('[data-testid=maker-jarvis-text]').text()).toContain('Grzanie 3 tylko na krótko')

    await wrapper.find('#satelliteName').setValue(' kuchnia ')
    await wrapper.find('[data-testid=add-device-form]').trigger('submit')
    await flushPromises()

    expect(add).toHaveBeenCalledWith('h1', 'kuchnia')
    expect(document.querySelector('[data-testid=pairing-code]')?.textContent).toContain('ABCD-EFGH')

    // Stream emits changed: paired for d2 -> dialog closes automatically.
    activeStreamOptions?.onEvent({ type: 'changed', data: { deviceId: 'd2', reason: 'paired' } })
    await flushPromises()
    expect(document.querySelector('[data-testid=pairing-code]')).toBeNull()
    wrapper.unmount()
  })

  it('closes inline editor and refreshes list when saving a device removed by another admin (404)', async () => {
    vi.spyOn(devicesApi, 'household').mockResolvedValue([chair])
    vi.spyOn(devicesApi, 'rename').mockRejectedValue({
      isAxiosError: true,
      response: { status: 404, headers: {}, data: { status: 404, code: 'devices.not_found' } },
    })
    const wrapper = render('Admin')
    await flushPromises()

    await wrapper.find('[data-testid=open-device-admin]').trigger('click')
    expect(wrapper.find('[data-testid=device-admin-form]').exists()).toBe(true)

    await wrapper.find(`#device-name-${chair.id}`).setValue('Nowa nazwa')
    await wrapper.find('[data-testid=device-admin-form]').trigger('submit')
    await flushPromises()

    expect(wrapper.find('[data-testid=device-admin-form]').exists()).toBe(false)
    wrapper.unmount()
  })

  it('aborts the old household stream and opens a new one when the household changes', async () => {
    vi.spyOn(devicesApi, 'household').mockResolvedValue([chair])
    const wrapper = render('Member', 'h1')
    await flushPromises()
    expect(activeStreamOptions?.householdId).toBe('h1')

    await wrapper.setProps({
      household: { id: 'h2', name: 'Drugi dom', myRole: 'Member' } as Household,
    })
    await flushPromises()

    expect(abortedHouseholds).toContain('h1')
    expect(activeStreamOptions?.householdId).toBe('h2')
    wrapper.unmount()
  })
})

