import { afterEach, describe, expect, it, vi } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'
import PrimeVue from 'primevue/config'
import ToastService from 'primevue/toastservice'
import { i18n } from '@/shared/i18n'
import { jarvisApi, type Action } from '../api'
import ActionCard from '../ActionCard.vue'

afterEach(() => vi.restoreAllMocks())

const waiting = {
  id: 'a1',
  tool: 'devices.unlock',
  description: 'Odblokuj drzwi',
  status: 'NeedsConfirmation',
  confirmBy: new Date(Date.now() + 600_000).toISOString(),
} as Action

function render(actions: Action[]) {
  return mount(ActionCard, { props: { actions }, global: { plugins: [i18n, PrimeVue, ToastService] } })
}

describe('JARVIS action confirmation', () => {
  it('does nothing until the user confirms, then sends the approval', async () => {
    const decide = vi.spyOn(jarvisApi, 'decide').mockResolvedValue({ ...waiting, status: 'Done' })
    const wrapper = render([waiting])

    expect(decide).not.toHaveBeenCalled()
    await wrapper.findAll('button')[0]!.trigger('click')
    await flushPromises()

    expect(decide).toHaveBeenCalledWith('a1', true)
    expect(wrapper.emitted('decided')).toHaveLength(1)
  })

  it('sends a refusal from the cancel button and shows no buttons once expired', async () => {
    const decide = vi.spyOn(jarvisApi, 'decide').mockResolvedValue({ ...waiting, status: 'Denied' })
    const wrapper = render([waiting])
    await wrapper.findAll('button')[1]!.trigger('click')
    await flushPromises()
    expect(decide).toHaveBeenCalledWith('a1', false)

    const expired = render([{ ...waiting, confirmBy: new Date(Date.now() - 1000).toISOString() }])
    expect(expired.findAll('button')).toHaveLength(0)
  })
})
