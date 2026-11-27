import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { QueryClient, VueQueryPlugin } from '@tanstack/vue-query'
import PrimeVue from 'primevue/config'
import ToastService from 'primevue/toastservice'
import ConfirmationService from 'primevue/confirmationservice'
import Select from 'primevue/select'
import Slider from 'primevue/slider'
import ToggleSwitch from 'primevue/toggleswitch'
import { i18n } from '@/shared/i18n'
import { errorMessage } from '@/api/errorMessage'
import { householdPermissions, type Household, type HouseholdRole } from '@/features/households/api'
import HouseholdDevicesTab from '../pages/HouseholdDevicesTab.vue'
import ControlWidget from '../components/ControlWidget.vue'
import ConsentPanel from '@/features/homemix/ConsentPanel.vue'
import WidgetSettingsDialog from '@/features/homemix/WidgetSettingsDialog.vue'
import { homemixApi, type Installation, type Package } from '@/features/homemix/api'
import { devicesApi, EVENTS_PAGE_SIZE, type DescriptorControl, type Device, type HouseholdAssistant } from '../api'
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
  Object.defineProperty(window, 'matchMedia', {
    writable: true,
    value: vi.fn().mockImplementation((query: string) => ({
      matches: false,
      media: query,
      onchange: null,
      addListener: vi.fn(),
      removeListener: vi.fn(),
      addEventListener: vi.fn(),
      removeEventListener: vi.fn(),
      dispatchEvent: vi.fn(),
    })),
  })
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

  it('renders switch, select, button, number and text controls and emits set on change', async () => {
    const switchWrapper = mount(ControlWidget, {
      props: {
        control: { name: 'power', label: 'Zasilanie', description: null, value: { type: 'boolean' }, widget: null },
        value: false,
        disabledReason: null,
        pending: false,
      },
      global: { plugins: [i18n, PrimeVue] },
    })
    await switchWrapper.findComponent(ToggleSwitch).vm.$emit('update:modelValue', true)
    expect(switchWrapper.emitted('set')).toEqual([[true]])

    const selectWrapper = mount(ControlWidget, {
      props: {
        control: {
          name: 'mode',
          label: 'Tryb',
          description: null,
          value: { type: 'string', enum: ['eco', 'turbo'] },
          widget: 'select',
        },
        value: 'eco',
        disabledReason: null,
        pending: false,
      },
      global: { plugins: [i18n, PrimeVue] },
    })
    await selectWrapper.findComponent(Select).vm.$emit('update:modelValue', 'turbo')
    expect(selectWrapper.emitted('set')).toEqual([['turbo']])

    const buttonWrapper = mount(ControlWidget, {
      props: {
        control: { name: 'ping', label: 'Ping', description: null, value: { const: 'go' }, widget: 'button' },
        value: null,
        disabledReason: null,
        pending: false,
      },
      global: { plugins: [i18n, PrimeVue] },
    })
    await buttonWrapper.find('button').trigger('click')
    expect(buttonWrapper.emitted('set')).toEqual([['go']])
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

  it('shows empty state CTA for Admin, error state on failure, and disabled assistant banner', async () => {
    vi.spyOn(devicesApi, 'assistant').mockResolvedValue({ ...defaultAssistant, enabled: false })
    vi.spyOn(devicesApi, 'household').mockResolvedValue([])
    const wrapper = render('Admin')
    await flushPromises()

    expect(wrapper.find('[data-testid=assistant-disabled-banner]').text()).toContain('JARVIS domu jest wyłączony')
    expect(wrapper.text()).toContain('W tym domu nie ma jeszcze urządzeń.')
    expect(wrapper.find('[data-testid=empty-add-cta]').exists()).toBe(true)
    wrapper.unmount()

    vi.spyOn(devicesApi, 'household').mockRejectedValue({
      isAxiosError: true,
      response: { status: 503, headers: {}, data: { status: 503, code: 'devices.members_unavailable' } },
    })
    const errWrapper = render('Member')
    await flushPromises()
    expect(errWrapper.text()).toContain('Nie udało się sprawdzić uprawnień w domu.')
    errWrapper.unmount()
  })

  it('lets Admin update household assistant settings and paginate device events with Starsze', async () => {
    vi.spyOn(devicesApi, 'household').mockResolvedValue([chair])
    const updateAssistant = vi.spyOn(devicesApi, 'updateAssistant').mockResolvedValue(defaultAssistant)
    const firstPage = Array.from({ length: EVENTS_PAGE_SIZE }, (_, idx) => ({
      id: `e-${idx}`,
      name: 'button',
      data: { idx },
      occurredAt: '2026-11-18T10:05:00Z',
      isLocation: idx === 0,
    }))
    const eventsSpy = vi
      .spyOn(devicesApi, 'events')
      .mockResolvedValueOnce(firstPage)
      .mockResolvedValueOnce([
        { id: 'e-older', name: 'button', data: { text: 'Starszy wpis' }, occurredAt: '2026-11-18T09:00:00Z', isLocation: false },
      ])

    const wrapper = render('Admin')
    await flushPromises()

    await wrapper.find('#assistantName').setValue('Jarvis Domowy')
    await wrapper.find('#assistant-source-HomeMix').setValue(false)
    await wrapper.find('form').trigger('submit')
    await flushPromises()

    expect(updateAssistant).toHaveBeenCalledWith(
      'h1',
      expect.objectContaining({
        name: 'Jarvis Domowy',
        toolSources: 'Plugins, Devices',
      }),
    )

    await wrapper.find('[data-testid=toggle-events]').trigger('click')
    await flushPromises()
    expect(eventsSpy).toHaveBeenCalledWith('d1', 0, EVENTS_PAGE_SIZE)
    expect(wrapper.find('[data-testid=events-older]').exists()).toBe(true)

    await wrapper.find('[data-testid=events-older]').trigger('click')
    await flushPromises()
    expect(eventsSpy).toHaveBeenCalledWith('d1', EVENTS_PAGE_SIZE, EVENTS_PAGE_SIZE)
    expect(wrapper.find('[data-testid=device-events-panel]').text()).toContain('Starszy wpis')
    expect(wrapper.find('[data-testid=events-older]').exists()).toBe(false)
    wrapper.unmount()
  })

  it('appends server detail to devices.invalid_frame error message', () => {
    const msg = errorMessage({
      status: 400,
      code: 'devices.invalid_frame',
      detail: 'level must be <= 3',
      errors: [],
      fieldErrors: {},
    })
    expect(msg).toBe('Wartość nie pasuje do opisu urządzenia. (level must be <= 3)')
  })
})

describe('HomeMix consent and widget device picker (2.17)', () => {
  it('describes devices and devices.control capabilities in Polish on the consent panel', () => {
    const pkg = {
      id: 'thermostat',
      version: '1.0.0',
      sha256: 'abc',
      name: { pl: 'Termostat', en: 'Thermostat' },
      description: { pl: 'Sterowanie ogrzewaniem', en: 'Heating' },
      icon: 'pi-sliders-h',
      targets: ['Household'],
      capabilities: ['devices', 'devices.control'],
      network: [],
      jarvis: true,
      widgets: [],
      repository: null,
      size: 1024,
    } as unknown as Package
    const wrapper = mount(ConsentPanel, {
      props: { pkg },
      global: { plugins: [i18n, PrimeVue] },
    })

    expect(wrapper.text()).toContain('widzieć pasujące urządzenia tego domu i ich stan')
    expect(wrapper.text()).toContain('sterować pasującymi urządzeniami tego domu (w granicach Twoich uprawnień)')
  })

  it('loads matching devices for a widget in WidgetSettingsDialog and saves the picked device', async () => {
    const listDevices = vi.spyOn(homemixApi.devices, 'list').mockResolvedValue({
      needsPick: true,
      selected: null,
      devices: [
        { id: 'd1', name: 'Fotel taty', slug: 'fotel-taty' } as never,
        { id: 'd2', name: 'Fotel mamy', slug: 'fotel-mamy' } as never,
      ],
    })
    const chooseDevice = vi
      .spyOn(homemixApi.devices, 'chooseWidgetDevice')
      .mockResolvedValue({} as unknown as Installation)
    vi.spyOn(homemixApi, 'resetWidget').mockResolvedValue({} as unknown as Installation)

    const installation = {
      id: 'inst-1',
      addonId: 'thermostat',
      version: '1.0.0',
      name: { pl: 'Termostat', en: 'Thermostat' },
      targetKind: 'Household',
      capabilities: ['devices'],
      widgets: [
        {
          id: 'chair-widget',
          title: { pl: 'Fotel', en: 'Chair' },
          size: 'Small',
          sizes: ['Small'],
          places: ['Household'],
          devices: [{ role: 'seat_heater', pick: 'manual' }],
        },
      ],
      widgetChoices: [],
      devicePicks: [],
    } as unknown as Installation

    const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } })
    const wrapper = mount(WidgetSettingsDialog, {
      props: {
        visible: false,
        installation,
        target: { kind: 'Household', householdId: 'h1' },
      },
      global: {
        plugins: [i18n, PrimeVue, [VueQueryPlugin, { queryClient }]],
        stubs: {
          Dialog: {
            props: ['visible', 'header'],
            template: '<div v-if="visible" data-testid="widget-dialog"><slot /></div>',
          },
        },
      },
    })

    await wrapper.setProps({ visible: true })
    await flushPromises()

    expect(listDevices).toHaveBeenCalledWith('inst-1', { widget: 'chair-widget', state: true })
    const fieldset = wrapper.find('[data-testid=widget-device-chair-widget]')
    expect(fieldset.exists()).toBe(true)
    expect(fieldset.text()).toContain('Ten widżet wymaga ręcznego wyboru urządzenia przez administratora domu.')

    await fieldset.findComponent(Select).vm.$emit('update:modelValue', 'd2')
    await wrapper.find('form').trigger('submit')
    await flushPromises()

    expect(chooseDevice).toHaveBeenCalledWith('inst-1', 'chair-widget', 'd2')
  })
})



describe('Sleepy device controls (downlink)', () => {
  const sleepyChair: Device = {
    ...chair,
    online: false,
    descriptor: { ...chair.descriptor!, sleepy: true },
    state: { ...chair.state!, online: false },
  }

  it('is not locked by sleepy or offline state and sets without the 20s false timeout', async () => {
    vi.useFakeTimers({ shouldAdvanceTime: true })
    vi.spyOn(devicesApi, 'household').mockResolvedValue([sleepyChair])
    const setControl = vi.spyOn(devicesApi, 'setControl').mockResolvedValue(undefined)
    const wrapper = render('Member')
    await flushPromises()

    expect(wrapper.text()).not.toContain('sterowanie niedostępne')
    expect(wrapper.find('[data-testid=control-disabled-reason]').exists()).toBe(false)

    await wrapper.find('[data-testid=control-lampka]').findComponent(ToggleSwitch).vm.$emit('update:modelValue', true)
    await flushPromises()
    expect(setControl).toHaveBeenCalledWith('d1', 'lampka', true)

    vi.advanceTimersByTime(25_000)
    await flushPromises()
    expect(document.body.textContent).not.toContain('Urządzenie nie odpowiedziało na czas.')
    vi.useRealTimers()
    wrapper.unmount()
  })

  it('shows "Czeka na urządzenie" with aria-busy until a state frame reports the value', async () => {
    const waiting: Device = {
      ...sleepyChair,
      state: { ...sleepyChair.state!, pendingSettings: { lampka: true } },
    }
    vi.spyOn(devicesApi, 'household').mockResolvedValue([waiting])
    const wrapper = render('Member')
    await flushPromises()

    const control = () => wrapper.find('[data-testid=control-lampka]')
    expect(control().find('[data-testid=control-awaiting-device]').text()).toBe('Czeka na urządzenie')
    expect(control().attributes('aria-busy')).toBe('true')
    expect(control().findComponent(ToggleSwitch).props('disabled')).toBe(true)

    activeStreamOptions?.onEvent({
      type: 'state',
      data: { deviceId: 'd1', values: { lampka: true }, at: '2026-11-27T10:00:00Z' },
    })
    await flushPromises()

    expect(control().find('[data-testid=control-awaiting-device]').exists()).toBe(false)
    expect(control().attributes('aria-busy')).toBeUndefined()
    wrapper.unmount()
  })
})

describe('Silent sleepy device', () => {
  const silentState = { ...chair.state!, silent: true, online: false }

  it('stays controllable (the setting waits for it) while a silent awake device stays locked', async () => {
    const sleepy: Device = { ...chair, online: false, descriptor: { ...chair.descriptor!, sleepy: true }, state: silentState }
    vi.spyOn(devicesApi, 'household').mockResolvedValue([sleepy])
    const wrapper = render('Member')
    await flushPromises()
    expect(wrapper.find('[data-testid=control-disabled-reason]').exists()).toBe(false)
    wrapper.unmount()

    vi.spyOn(devicesApi, 'household').mockResolvedValue([{ ...chair, state: silentState }])
    const awake = render('Member')
    await flushPromises()
    expect(awake.find('[data-testid=control-disabled-reason]').text()).toContain('zaginęło')
    awake.unmount()
  })
})
