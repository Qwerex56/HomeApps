import { describe, expect, it } from 'vitest'
import { checkDeviceId } from '../deviceId'

describe('checkDeviceId', () => {
  it('lets a UUID through', () => {
    const id = '0b9f1c52-3a4e-4f7a-9d21-5c8e6a7b1d30'
    expect(checkDeviceId(id)).toBe(id)
  })

  it.each(['../../admin/blocks', 'abc?x=1', 'abc#', '', 42, null, undefined, '0b9f1c52-3a4e-4f7a-9d21-5c8e6a7b1d30/../x'])(
    'refuses %j',
    (id) => {
      expect(() => checkDeviceId(id)).toThrow(/Unknown device/)
    },
  )
})
