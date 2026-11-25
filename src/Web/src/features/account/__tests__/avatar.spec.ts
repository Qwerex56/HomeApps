import { afterEach, describe, expect, it, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import { http } from '@/api/http'
import UserAvatar from '@/shared/components/UserAvatar.vue'
import { accountApi } from '../api'

afterEach(() => vi.restoreAllMocks())

describe('avatar', () => {
  it('uploads the file as multipart field "file" and returns the address', async () => {
    const post = vi.spyOn(http, 'post').mockResolvedValue({ data: { avatarUrl: '/users/v1/x/avatar?v=1' } })
    const file = new File(['x'], 'a.png', { type: 'image/png' })

    expect(await accountApi.uploadAvatar(file)).toBe('/users/v1/x/avatar?v=1')

    const form = post.mock.calls[0]![1] as FormData
    expect(post.mock.calls[0]![0]).toBe('/users/v1/me/avatar')
    expect(form.get('file')).toBeInstanceOf(File)
  })

  it('shows the initial without an address', () => {
    const wrapper = mount(UserAvatar, { props: { src: null, name: 'ola' } })

    expect(wrapper.get('[data-testid="avatar-fallback"]').text()).toBe('O')
  })
})
