import { afterEach, describe, expect, it, vi } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'
import { QueryClient, VueQueryPlugin } from '@tanstack/vue-query'
import PrimeVue from 'primevue/config'
import ToastService from 'primevue/toastservice'
import ConfirmationService from 'primevue/confirmationservice'
import { i18n } from '@/shared/i18n'
import { householdPermissions, type Household } from '@/features/households/api'
import HouseholdDevicesTab from '../pages/HouseholdDevicesTab.vue'
import { devicesApi, type Device } from '../api'

const salon: Device = {
  id: 'd1',
  name: 'salon',
  kind: 'Satellite',
  householdId: 'h1',
  paired: true,
  online: true,
  removed: false,
  createdAt: '2026-11-18T10:00:00Z',
  lastSeenAt: '2026-11-18T10:00:00Z',
  actions: [{ name: 'lights', description: 'Światło w salonie', readOnly: false, confirm: true, autoApprove: false }],
}

function render(role: 'Member' | 'Admin') {
  const household = { id: 'h1', name: 'Dom', myRole: role } as Household
  return mount(HouseholdDevicesTab, {
    props: { household, can: householdPermissions(role) },
    global: {
      plugins: [
        i18n,
        PrimeVue,
        ToastService,
        ConfirmationService,
        [VueQueryPlugin, { queryClient: new QueryClient({ defaultOptions: { queries: { retry: false } } }) }],
      ],
    },
    attachTo: document.body,
  })
}

afterEach(() => {
  vi.restoreAllMocks()
  document.body.innerHTML = ''
})

describe('HouseholdDevicesTab', () => {
  it('shows a member the satellites and their actions without anything to change', async () => {
    vi.spyOn(devicesApi, 'household').mockResolvedValue([salon])
    const wrapper = render('Member')
    await flushPromises()

    expect(wrapper.text()).toContain('salon')
    expect(wrapper.text()).toContain('Połączone')
    expect(wrapper.text()).toContain('Światło w salonie')
    expect(wrapper.text()).not.toContain('Dodaj satelitę')
    expect(wrapper.text()).not.toContain('Wyłącz')
  })

  it('lets an admin add a satellite and shows its pairing code once', async () => {
    vi.spyOn(devicesApi, 'household').mockResolvedValue([])
    const add = vi.spyOn(devicesApi, 'addSatellite').mockResolvedValue({
      deviceId: 'd2',
      name: 'kuchnia',
      code: 'abcdefgh',
      expiresAt: '2026-11-18T10:10:00Z',
    })
    const wrapper = render('Admin')
    await flushPromises()

    await wrapper.find('#satelliteName').setValue(' kuchnia ')
    await wrapper.find('form').trigger('submit')
    await flushPromises()

    expect(add).toHaveBeenCalledWith('h1', 'kuchnia')
    expect(document.querySelector('[data-testid=pairing-code]')?.textContent).toContain('ABCD-EFGH')
    wrapper.unmount()
  })
})
