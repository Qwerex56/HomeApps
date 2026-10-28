import { describe, expect, it } from 'vitest'
import { formatCode, householdPermissions } from '../api'

describe('householdPermissions', () => {
  it('lets a guest only leave', () => {
    const can = householdPermissions('Guest')
    expect(can).toMatchObject({ member: true, seeMembers: false, invite: false, edit: false, leave: true })
  })

  it('lets a member invite but not manage anyone', () => {
    const can = householdPermissions('Member')
    expect(can).toMatchObject({ seeMembers: true, invite: true, edit: false, guestLink: false })
    expect(can.manage('Guest')).toBe(false)
  })

  it('lets an admin manage only roles below', () => {
    const can = householdPermissions('Admin')
    expect(can.manage('Member')).toBe(true)
    expect(can.manage('Admin')).toBe(false)
    expect(can).toMatchObject({ guestLink: true, changeRoles: false, archive: false })
  })

  it('keeps the owner in until they hand over', () => {
    expect(householdPermissions('Owner')).toMatchObject({ changeRoles: true, transfer: true, leave: false })
  })

  it('lets a server admin who is not a member look at the members', () => {
    expect(householdPermissions(null, true)).toMatchObject({ seeMembers: true, member: false, invite: false })
  })
})

describe('formatCode', () => {
  it('shows codes as ABCDE-FGHJK whatever was typed', () => {
    expect(formatCode('abcdefghjk')).toBe('ABCDE-FGHJK')
    expect(formatCode('ABCDE-FGHJK')).toBe('ABCDE-FGHJK')
  })
})
