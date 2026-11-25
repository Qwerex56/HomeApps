import { afterEach, describe, expect, it, vi } from 'vitest'
import { http } from '@/api/http'
import { jarvisApi } from '../api'

afterEach(() => vi.restoreAllMocks())

describe('jarvis conversations api', () => {
  it('lists with the search phrase as q, and without it when empty', async () => {
    const get = vi.spyOn(http, 'get').mockResolvedValue({ data: [] })

    await jarvisApi.conversations('kuchnia')
    await jarvisApi.conversations()

    expect(get).toHaveBeenNthCalledWith(1, '/jarvis/api/v1/conversations', { params: { q: 'kuchnia' } })
    expect(get).toHaveBeenNthCalledWith(2, '/jarvis/api/v1/conversations', { params: {} })
  })

  it('renames with PATCH on the title route', async () => {
    const patch = vi.spyOn(http, 'patch').mockResolvedValue({ data: null })

    await jarvisApi.rename('c1', 'Projekt kuchni')

    expect(patch).toHaveBeenCalledWith('/jarvis/api/v1/conversations/c1/title', { title: 'Projekt kuchni' })
  })
})
