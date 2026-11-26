import { afterEach, describe, expect, it, vi } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { QueryClient, VueQueryPlugin } from '@tanstack/vue-query'
import PrimeVue from 'primevue/config'
import ToastService from 'primevue/toastservice'
import { i18n } from '@/shared/i18n'
import { http } from '@/api/http'
import { useSessionStore } from '@/features/auth/session'
import { accountApi } from '../api'
import ProfilePage from '../pages/ProfilePage.vue'

vi.stubGlobal('ResizeObserver', class { observe() {} unobserve() {} disconnect() {} })
afterEach(() => vi.restoreAllMocks())

vi.mock('vue-router', () => ({ useRouter: () => ({ replace: vi.fn(), push: vi.fn() }) }))

describe('disabling my account', () => {
  it('opens a confirmation first and disables only when confirmed in the dialog', async () => {
    setActivePinia(createPinia())
    vi.spyOn(http, 'get').mockResolvedValue({ data: { profile: {}, status: 'Active' } })
    useSessionStore()
    const disable = vi.spyOn(accountApi, 'disable').mockResolvedValue(undefined as never)
    const wrapper = mount(ProfilePage, {
      attachTo: document.body,
      global: {
        plugins: [i18n, PrimeVue, ToastService, [VueQueryPlugin, { queryClient: new QueryClient() }]],
        stubs: { AvatarUpload: true, ProfileForm: true },
      },
    })
    await flushPromises()

    const open = wrapper.findAll('button').find((b) => b.text() === 'Wyłącz konto')
    await open!.trigger('click')
    await flushPromises()
    expect(disable).not.toHaveBeenCalled()

    const confirmBtn = [...document.body.querySelectorAll('button')].filter((b) => b.textContent?.trim() === 'Wyłącz konto').pop()
    confirmBtn!.click()
    await flushPromises()

    expect(disable).toHaveBeenCalledWith(null)
    wrapper.unmount()
  })
})
