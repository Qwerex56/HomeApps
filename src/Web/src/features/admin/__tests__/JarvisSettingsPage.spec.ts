import { describe, expect, it, vi } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'
import { QueryClient, VueQueryPlugin } from '@tanstack/vue-query'
import PrimeVue from 'primevue/config'
import ToastService from 'primevue/toastservice'
import ConfirmationService from 'primevue/confirmationservice'
import { i18n } from '@/shared/i18n'
import { llmSettingsApi } from '../llmSettings'
import JarvisSettingsPage from '../pages/JarvisSettingsPage.vue'

const settings = { baseUrl: 'https://models.test/v1', model: 'gpt-test', apiKeySet: true, timeoutSeconds: 20 }

async function render() {
  vi.spyOn(llmSettingsApi, 'get').mockResolvedValue(settings)
  const wrapper = mount(JarvisSettingsPage, {
    global: {
      plugins: [
        i18n,
        PrimeVue,
        ToastService,
        ConfirmationService,
        [VueQueryPlugin, { queryClient: new QueryClient({ defaultOptions: { queries: { retry: false } } }) }],
      ],
    },
  })
  await flushPromises()
  return wrapper
}

describe('JarvisSettingsPage', () => {
  it('fills the form from the server and never shows a key, only that one is stored', async () => {
    const wrapper = await render()

    const inputs = wrapper.findAll('input').map((i) => i.element.value)
    expect(inputs).toContain('https://models.test/v1')
    expect(inputs).toContain('gpt-test')
    expect(wrapper.find('input[type="password"]').element.getAttribute('placeholder')).toContain('Klucz jest zapisany')
  })

  it('saves without a key when the field is empty, so the stored key stays', async () => {
    const wrapper = await render()
    const save = vi.spyOn(llmSettingsApi, 'save').mockResolvedValue(settings)

    await wrapper.find('form').trigger('submit')
    await flushPromises()

    expect(save).toHaveBeenCalledWith({ baseUrl: 'https://models.test/v1', model: 'gpt-test', timeoutSeconds: 20 })
  })

  it('tests the connection and shows the answer', async () => {
    const wrapper = await render()
    vi.spyOn(llmSettingsApi, 'test').mockResolvedValue({ ok: false, error: 'HTTP 401' })

    await wrapper.findAll('button').find((b) => b.text() === 'Testuj połączenie')!.trigger('click')
    await flushPromises()

    expect(wrapper.text()).toContain('Model nie odpowiada: HTTP 401')
  })
})
