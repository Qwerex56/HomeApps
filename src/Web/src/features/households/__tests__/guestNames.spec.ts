import { afterEach, describe, expect, it, vi } from 'vitest'
import { http } from '@/api/http'
import { householdsApi } from '../api'

afterEach(() => vi.restoreAllMocks())

describe('households api, release 3.0', () => {
  it('reads the names (guest view) and sets the time zone on the /v1 routes', async () => {
    const get = vi.spyOn(http, 'get').mockResolvedValue({ data: [] })
    const put = vi.spyOn(http, 'put').mockResolvedValue({ data: null })

    await householdsApi.names('h1')
    await householdsApi.setTimeZone('h1', 'Europe/London')

    expect(get).toHaveBeenCalledWith('/households/v1/h1/members/names')
    expect(put).toHaveBeenCalledWith('/households/v1/h1/timezone', { timeZone: 'Europe/London' })
  })
})
