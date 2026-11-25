import { afterEach, describe, expect, it, vi } from 'vitest'
import { http } from '@/api/http'
import { notificationsApi } from '../api'

afterEach(() => vi.restoreAllMocks())

describe('notification center api', () => {
  it('reads the list with a limit, marks one and all as read on the /v1 routes', async () => {
    const get = vi.spyOn(http, 'get').mockResolvedValue({ data: { unreadCount: 1, items: [] } })
    const post = vi.spyOn(http, 'post').mockResolvedValue({ data: null })

    expect((await notificationsApi.inApp(20)).unreadCount).toBe(1)
    await notificationsApi.markRead('abc')
    await notificationsApi.markAllRead()

    expect(get).toHaveBeenCalledWith('/notifications/v1/in-app', { params: { limit: 20 } })
    expect(post).toHaveBeenCalledWith('/notifications/v1/in-app/abc/read')
    expect(post).toHaveBeenCalledWith('/notifications/v1/in-app/read-all')
  })
})
