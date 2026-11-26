import { describe, expect, it, vi } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'
import { QueryClient, VueQueryPlugin } from '@tanstack/vue-query'
import PrimeVue from 'primevue/config'
import { i18n } from '@/shared/i18n'
import { adminApi, type ServerAbout } from '../api'
import ServerAboutPage from '../pages/ServerAboutPage.vue'

const about: ServerAbout = {
  currentVersion: '3.0.0',
  latestVersion: '3.0.0',
  isUpdateAvailable: false,
  releaseUrl: 'https://example.test/release',
  services: [
    { name: 'Auth', version: '3.0.0', appliedMigrations: 8, lastMigration: '20261120_AddDelegations' },
    { name: 'Jarvis', version: null, appliedMigrations: 0, lastMigration: null },
  ],
  plugins: [{ id: 'todo', version: '1.0.0', status: 'unavailable' }],
}

async function render(data: ServerAbout) {
  vi.spyOn(adminApi, 'about').mockResolvedValue(data)
  const wrapper = mount(ServerAboutPage, {
    global: {
      plugins: [
        i18n,
        PrimeVue,
        [VueQueryPlugin, { queryClient: new QueryClient({ defaultOptions: { queries: { retry: false } } }) }],
      ],
    },
  })
  await flushPromises()
  return wrapper.text()
}

describe('ServerAboutPage', () => {
  it('shows the version, the services with migrations and the plugins', async () => {
    const text = await render(about)

    expect(text).toContain('Wersja HomeApps: 3.0.0')
    expect(text).toContain('Masz najnowszą wersję')
    expect(text).toContain('Migracje: 8, ostatnia: 20261120_AddDelegations')
    expect(text).toContain('brak odpowiedzi')
    expect(text).toContain('niedostępny')
  })

  it('shows an unknown update state when the latest release could not be read', async () => {
    const text = await render({ ...about, latestVersion: null, isUpdateAvailable: null, releaseUrl: null })

    expect(text).toContain('Nie udało się sprawdzić najnowszej wersji')
  })

  it('announces an available update', async () => {
    expect(await render({ ...about, latestVersion: '3.1.0', isUpdateAvailable: true })).toContain('Jest nowsza wersja')
  })
})
