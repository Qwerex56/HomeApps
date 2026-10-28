import { describe, expect, it } from 'vitest'
import { permissions } from '../api'

const owner = { id: 'o', role: 'Owner' as const }
const admin = { id: 'a', role: 'Admin' as const }

describe('permissions', () => {
  it('lets the Owner do everything to an admin, erase only once disabled', () => {
    expect(permissions(owner, { id: 'a', role: 'Admin', status: 'Active' })).toEqual({
      manage: true,
      disable: true,
      enable: false,
      resetPassword: true,
      changeRole: true,
      erase: false,
    })
    expect(permissions(owner, { id: 'a', role: 'Admin', status: 'Disabled' }).erase).toBe(true)
  })

  it('lets an admin manage users but never change roles or erase', () => {
    const can = permissions(admin, { id: 'u', role: 'User', status: 'Disabled' })
    expect(can).toMatchObject({ manage: true, enable: true, changeRole: false, erase: false })
  })

  it('protects equal and higher roles and the own account from admin actions', () => {
    expect(permissions(admin, { id: 'b', role: 'Admin', status: 'Active' }).manage).toBe(false)
    expect(permissions(admin, { id: 'o', role: 'Owner', status: 'Active' }).disable).toBe(false)
    expect(permissions(owner, { id: 'o', role: 'Owner', status: 'Active' })).toMatchObject({
      manage: true,
      disable: false,
      changeRole: false,
      erase: false,
    })
  })
})
