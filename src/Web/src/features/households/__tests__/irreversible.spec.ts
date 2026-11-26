import { afterEach, describe, expect, it, vi } from 'vitest'
import { defineComponent, h } from 'vue'
import { flushPromises, mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { QueryClient, VueQueryPlugin } from '@tanstack/vue-query'
import PrimeVue from 'primevue/config'
import ToastService from 'primevue/toastservice'
import { i18n } from '@/shared/i18n'
import { householdPermissions, householdsApi, type Household } from '../api'
import MembersTab from '../pages/MembersTab.vue'
import InvitesTab from '../pages/InvitesTab.vue'

const required = vi.fn()
vi.stubGlobal('matchMedia', () => ({ matches: false, addEventListener: () => undefined, removeEventListener: () => undefined }))
vi.mock('primevue/useconfirm', () => ({ useConfirm: () => ({ require: required }) }))

// A stand-in for the popup menu: its items as buttons.
const MenuStub = defineComponent({
  props: { model: { type: Array, default: () => [] } },
  setup(props, { expose }) {
    expose({ toggle: () => undefined })
    return () =>
      h(
        'div',
        (props.model as { label?: string; command?: () => void }[]).map((item) =>
          item.label ? h('button', { 'data-item': item.label, onClick: item.command }, item.label) : null,
        ),
      )
  },
})

const household = { id: 'h1', name: 'Dom', myRole: 'Owner', guestLink: null } as unknown as Household
const member = (userId: string, name: string, role: string) => ({
  userId, name, nickname: null, displayName: name, avatarUrl: null, role, isActive: true,
  joinedAt: '2026-01-01T00:00:00Z', invitedBy: null, invitedByName: null, accessUntil: null,
})

function mountTab(component: typeof MembersTab | typeof InvitesTab) {
  setActivePinia(createPinia())
  return mount(component, {
    props: { household, can: householdPermissions('Owner') },
    global: {
      plugins: [
        i18n,
        PrimeVue,
        ToastService,
        [VueQueryPlugin, { queryClient: new QueryClient({ defaultOptions: { queries: { retry: false } } }) }],
      ],
      stubs: { Menu: MenuStub },
      directives: { tooltip: {} },
    },
  })
}

afterEach(() => {
  vi.restoreAllMocks()
  required.mockReset()
})

describe('irreversible household actions', () => {
  it('asks before transferring ownership and transfers only after accepting', async () => {
    vi.spyOn(householdsApi, 'members').mockResolvedValue([member('u1', 'Ola', 'Member')] as never)
    const transfer = vi.spyOn(householdsApi, 'transfer').mockResolvedValue(undefined as never)
    const wrapper = mountTab(MembersTab)
    await flushPromises()

    await wrapper.find('button[aria-haspopup]').trigger('click')
    await wrapper.find('[data-item="Przekaż dom"]').trigger('click')

    expect(transfer).not.toHaveBeenCalled()
    expect(required).toHaveBeenCalledTimes(1)
    await required.mock.calls[0]![0].accept()
    expect(transfer).toHaveBeenCalledWith('h1', 'u1')
  })

  it('changes a member to Guest through the role action', async () => {
    vi.spyOn(householdsApi, 'members').mockResolvedValue([member('u1', 'Ola', 'Member')] as never)
    const setRole = vi.spyOn(householdsApi, 'setRole').mockResolvedValue(undefined as never)
    const wrapper = mountTab(MembersTab)
    await flushPromises()

    await wrapper.find('button[aria-haspopup]').trigger('click')
    await wrapper.find('[data-item="Zmień na: Gość"]').trigger('click')
    await flushPromises()

    expect(setRole).toHaveBeenCalledWith('h1', 'u1', 'Guest')
  })

  it('cancels an invite by its revoke button', async () => {
    vi.spyOn(householdsApi, 'invites').mockResolvedValue([
      { id: 'i1', code: 'ABCDE12345', role: 'Member', createdAt: '2026-01-01T00:00:00Z', expiresAt: '2099-01-01T00:00:00Z', guestAccessUntil: null, uses: 0, createdBy: null },
    ] as never)
    const revoke = vi.spyOn(householdsApi, 'revokeInvite').mockResolvedValue(undefined as never)
    const wrapper = mountTab(InvitesTab)
    await flushPromises()

    await wrapper.find('button[aria-label="Odwołaj"]').trigger('click')
    await flushPromises()

    expect(revoke).toHaveBeenCalledWith('h1', 'i1')
  })
})
