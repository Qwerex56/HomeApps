import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { PluginManifest } from '@homeapps/plugin-sdk'
import type { LoadedPlugin } from '@/features/plugins/registry'
import { checkHealth, describeServerPlugins } from '../serverPlugins'

function plugin(id: string, changes: Partial<LoadedPlugin> = {}, manifest: Partial<PluginManifest> = {}): LoadedPlugin {
  return {
    manifest: {
      manifestVersion: 1,
      id,
      version: '1.2.0',
      requires: { homeApps: '2.1.0' },
      name: { pl: id, en: id },
      description: null,
      icon: 'pi-box',
      scopes: [],
      enablement: null,
      permissions: [],
      endpoints: { api: `/plugins/${id}/api`, openApi: '', mcp: null, health: `/plugins/${id}/health/ready` },
      ui: { entry: '', style: '', routes: [], widgets: [], settings: false },
      display: [],
      mcp: null,
      events: { publishes: [], consumes: [] },
      ...manifest,
    },
    module: { id } as LoadedPlugin['module'],
    problem: null,
    ...changes,
  }
}

function healthAnswers(answers: Record<string, number | 'down'>) {
  vi.stubGlobal(
    'fetch',
    vi.fn(async (url: string) => {
      const answer = answers[url]
      if (answer === undefined || answer === 'down') throw new TypeError('Failed to fetch')
      return new Response('{}', { status: answer })
    }),
  )
}

beforeEach(() => {
  vi.unstubAllGlobals()
})

describe('checkHealth', () => {
  it('is healthy on 2xx, unhealthy on an error or no answer, unknown without a path', async () => {
    healthAnswers({ '/a': 200, '/b': 503, '/c': 'down' })
    expect(await checkHealth('/a')).toBe('healthy')
    expect(await checkHealth('/b')).toBe('unhealthy')
    expect(await checkHealth('/c')).toBe('unhealthy')
    expect(await checkHealth('')).toBe('unknown')
  })
})

describe('describeServerPlugins', () => {
  it('keeps every listed id in order, the ones that are not running included', async () => {
    healthAnswers({ '/plugins/todo/health/ready': 200, '/plugins/chat/health/ready': 503 })
    const result = await describeServerPlugins(
      ['todo', 'weather', 'chat'],
      [plugin('chat', { problem: 'unavailable', module: null }), plugin('todo')],
    )

    expect(result.map((p) => [p.id, p.health, p.ui])).toEqual([
      ['todo', 'healthy', 'loaded'],
      ['weather', 'unhealthy', 'notRunning'],
      ['chat', 'unhealthy', 'unavailable'],
    ])
    expect(result[1]!.manifest).toBeNull()
  })

  it('tells a plugin without UI and one that needs a newer HomeApps', async () => {
    healthAnswers({})
    const result = await describeServerPlugins(
      ['news', 'future'],
      [
        plugin('news', { module: null }, { ui: null, endpoints: { api: '', openApi: '', mcp: null, health: '' } }),
        plugin('future', { problem: 'incompatible', module: null }, { requires: { homeApps: '99.0.0' } }),
      ],
    )

    expect(result.map((p) => [p.id, p.health, p.ui, p.compatible])).toEqual([
      ['news', 'unknown', 'none', true],
      ['future', 'unhealthy', 'incompatible', false],
    ])
  })
})
