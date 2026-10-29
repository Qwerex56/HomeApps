import { describe, expect, it, vi } from 'vitest'
import { defineComponent, h, nextTick } from 'vue'
import { flushPromises, mount } from '@vue/test-utils'
import { createPinia } from 'pinia'
import PrimeVue from 'primevue/config'
import ToastService from 'primevue/toastservice'
import { useHost, type PluginManifest } from '@homeapps/plugin-sdk'
import { i18n } from '@/shared/i18n'
import PluginOutlet from '../PluginOutlet.vue'
import type { LoadedPlugin } from '../registry'

const manifest = {
  id: 'todo',
  endpoints: { api: '/plugins/todo/api' },
  permissions: [{ id: 'tasks.write', minRole: 'Member', description: null }],
} as unknown as PluginManifest

const plugin: LoadedPlugin = { manifest, module: { id: 'todo' }, problem: null }

i18n.global.mergeLocaleMessage('pl', { plugins: { todo: { title: 'Zadania w {name}' } } } as never)

const Page = defineComponent({
  props: { extra: String },
  setup(props) {
    const host = useHost()
    return () =>
      h(
        'p',
        `${host.t('title', { name: host.household.value?.name })} ${host.apiBase} ${host.can('tasks.write')} ${props.extra}`,
      )
  },
})

function render(component: unknown, role: 'Guest' | 'Member' = 'Member') {
  return mount(PluginOutlet, {
    props: {
      plugin,
      component: component as never,
      household: { id: 'h1', name: 'Dom', myRole: role },
      componentProps: { extra: 'x' },
    },
    global: { plugins: [createPinia(), i18n, PrimeVue, ToastService] },
  })
}

describe('PluginOutlet', () => {
  it('gives the plugin its host: messages, API base and permissions for the role', () => {
    expect(render(Page).text()).toBe('Zadania w Dom /plugins/todo/api true x')
    expect(render(Page, 'Guest').text()).toContain('false')
  })

  it('loads a lazy page', async () => {
    const wrapper = render(async () => ({ default: Page }))
    await flushPromises()
    await nextTick()

    expect(wrapper.text()).toContain('Zadania w Dom')
  })

  it('contains a plugin that throws', async () => {
    vi.spyOn(console, 'error').mockImplementation(() => undefined)
    vi.spyOn(console, 'warn').mockImplementation(() => undefined)
    const Broken = defineComponent({
      setup() {
        throw new Error('boom')
      },
    })

    const wrapper = render(Broken)
    await nextTick()

    expect(wrapper.text()).toBe(i18n.global.t('plugins.failed'))
  })
})
