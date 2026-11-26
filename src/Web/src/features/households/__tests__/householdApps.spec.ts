import { describe, expect, it, vi } from 'vitest'
import { defineComponent, h } from 'vue'
import { mount } from '@vue/test-utils'
import { i18n } from '@/shared/i18n'
import { householdPermissions, type Household } from '../api'
import HouseholdAppsSection from '../pages/HouseholdAppsSection.vue'

vi.stubGlobal('matchMedia', () => ({ matches: false, addEventListener: () => undefined, removeEventListener: () => undefined }))

// 2.12: one "Aplikacje domu" section that holds the plugins list and the HomeMix add-ons.
const PluginsStub = defineComponent({
  props: { household: { type: Object, required: true } },
  setup: (props) => () => h('div', { 'data-testid': 'plugins', 'data-household': (props.household as Household).id }),
})
const HomeMixStub = defineComponent({
  props: { target: { type: Object, required: true }, canManage: { type: Boolean, default: false } },
  setup: (props) => () =>
    h('div', { 'data-testid': 'homemix', 'data-household': (props.target as { householdId: string }).householdId }),
})

const household = { id: 'h1', name: 'Dom', myRole: 'Admin', guestLink: null } as unknown as Household

function mountSection() {
  return mount(HouseholdAppsSection, {
    props: { household },
    global: {
      plugins: [i18n],
      stubs: { PluginsSettings: PluginsStub, HomeMixSettings: HomeMixStub },
    },
  })
}

describe('Aplikacje domu (2.12)', () => {
  it('shows the plugins and the HomeMix add-ons in one section for this household', () => {
    const wrapper = mountSection()

    expect(wrapper.attributes('aria-label')).toBe(i18n.global.t('households.settings.apps'))
    expect(wrapper.find('[data-testid="plugins"]').attributes('data-household')).toBe('h1')
    expect(wrapper.find('[data-testid="homemix"]').attributes('data-household')).toBe('h1')
  })

  it('is offered to Owner and Admin only (the edit gate in SettingsTab)', () => {
    expect(householdPermissions('Owner').edit).toBe(true)
    expect(householdPermissions('Admin').edit).toBe(true)
    expect(householdPermissions('Member').edit).toBe(false)
    expect(householdPermissions('Guest').edit).toBe(false)
  })
})
