import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { AxiosResponse } from 'axios'
import { hasPermission, type PluginManifest, type PluginModule } from '@home-apps/plugin-sdk'
import { http } from '@/api/http'
import { i18n } from '@/shared/i18n'
import { APP_VERSION, loadPlugins, setPluginImporter, versionAtLeast } from '../registry'

function manifest(id: string, changes: Partial<PluginManifest> = {}): PluginManifest {
  return {
    manifestVersion: 1,
    id,
    version: '1.0.0',
    requires: { homeApps: '2.1.0' },
    name: { pl: 'Zadania', en: 'Tasks' },
    description: null,
    icon: 'pi-check-square',
    scopes: ['HouseholdUser'],
    enablement: {
      scope: 'Household',
      minRole: 'Admin',
      path: `/plugins/${id}/api/households/{householdId}/enablement`,
    },
    permissions: [
      { id: 'tasks.read', minRole: 'Guest', description: null },
      { id: 'tasks.write', minRole: 'Member', description: null },
    ],
    endpoints: { api: `/plugins/${id}/api`, openApi: '', mcp: null, health: '' },
    ui: {
      entry: `/plugins/${id}/ui/entry.js`,
      style: `/plugins/${id}/ui/entry.css`,
      routes: [],
      widgets: [],
      settings: false,
    },
    display: [],
    mcp: null,
    events: { publishes: [], consumes: [] },
    ...changes,
  }
}

function givenPlugins(ids: string[], manifests: Record<string, PluginManifest>) {
  vi.stubGlobal(
    'fetch',
    vi.fn(async () => new Response(JSON.stringify(ids), { status: 200 })),
  )
  vi.spyOn(http, 'get').mockImplementation(async (url: string) => {
    const id = url.split('/')[2]!
    if (!manifests[id]) throw new Error('not running')
    return { data: manifests[id] } as AxiosResponse
  })
}

beforeEach(() => {
  vi.restoreAllMocks()
  vi.unstubAllGlobals()
  document.head.innerHTML = ''
})

describe('versionAtLeast', () => {
  it('compares each part as a number', () => {
    expect(versionAtLeast('2.10.0', '2.9.1')).toBe(true)
    expect(versionAtLeast('2.1.0', '2.1.0')).toBe(true)
    expect(versionAtLeast('2.0.9', '2.1.0')).toBe(false)
    expect(versionAtLeast('3.0.0-rc.1', '3.0.0')).toBe(true)
  })

  it('knows the app version', () => expect(APP_VERSION).toMatch(/^\d+\.\d+\.\d+/))
})

describe('loadPlugins', () => {
  it('loads the module, its stylesheet and its messages', async () => {
    const module: PluginModule = { id: 'todo', messages: { pl: { hello: 'Cześć {name}' } } }
    setPluginImporter(async () => ({ default: module }))
    givenPlugins(['todo'], { todo: manifest('todo') })

    const [plugin] = await loadPlugins()

    expect(plugin).toMatchObject({ module, problem: null })
    expect(document.head.querySelector('link')?.getAttribute('href')).toBe('/plugins/todo/ui/entry.css')
    expect(i18n.global.t('plugins.todo.hello', { name: 'Ala' })).toBe('Cześć Ala')
  })

  it('keeps a listed plugin that is not running as unavailable', async () => {
    setPluginImporter(async () => ({ default: { id: 'todo' } }))
    givenPlugins(['todo', 'weather'], { todo: manifest('todo') })

    const plugins = await loadPlugins()

    expect(plugins.map((p) => p.manifest.id)).toEqual(['todo', 'weather'])
    expect(plugins[1]).toMatchObject({ module: null, problem: 'unavailable' })
  })

  it('answers a 502 from the gateway with an unavailable stub named by its id, asking once', async () => {
    setPluginImporter(async () => ({ default: { id: 'weather' } }))
    vi.stubGlobal('fetch', vi.fn(async () => new Response(JSON.stringify(['weather']), { status: 200 })))
    const get = vi.spyOn(http, 'get').mockRejectedValue(Object.assign(new Error('Bad Gateway'), { response: { status: 502 } }))

    const [plugin] = await loadPlugins()

    expect(plugin).toMatchObject({
      manifest: { id: 'weather', name: { pl: 'weather', en: 'weather' }, ui: null },
      module: null,
      problem: 'unavailable',
    })
    expect(get).toHaveBeenCalledTimes(1)
  })

  it('marks a plugin for a newer HomeApps or an unknown manifest as incompatible and never imports it', async () => {
    const importer = vi.fn()
    setPluginImporter(importer)
    givenPlugins(['new', 'odd'], {
      new: manifest('new', { requires: { homeApps: '99.0.0' } }),
      odd: manifest('odd', { manifestVersion: 2 }),
    })

    const plugins = await loadPlugins()

    expect(plugins.map((p) => p.problem)).toEqual(['incompatible', 'incompatible'])
    expect(importer).not.toHaveBeenCalled()
  })

  it('keeps a plugin whose module fails as unavailable', async () => {
    vi.spyOn(console, 'error').mockImplementation(() => undefined)
    setPluginImporter(async () => {
      throw new Error('404')
    })
    givenPlugins(['todo'], { todo: manifest('todo') })

    expect((await loadPlugins())[0]).toMatchObject({ module: null, problem: 'unavailable' })
  })

  it('works without a plugin list', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn(async () => new Response('', { status: 404 })),
    )

    expect(await loadPlugins()).toEqual([])
  })
})

describe('hasPermission', () => {
  it('follows the manifest and never grants an undeclared permission', () => {
    const todo = manifest('todo')
    expect(hasPermission(todo, 'Guest', 'tasks.read')).toBe(true)
    expect(hasPermission(todo, 'Guest', 'tasks.write')).toBe(false)
    expect(hasPermission(todo, 'Owner', 'tasks.delete')).toBe(false)
    expect(hasPermission(todo, null, 'tasks.read')).toBe(false)
  })
})
