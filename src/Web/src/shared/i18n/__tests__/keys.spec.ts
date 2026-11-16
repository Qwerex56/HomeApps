import { describe, expect, it } from 'vitest'
import { i18n } from '../index'

// Keys with a dot in their name (HomeMix capabilities such as storage.household) are read with brackets.
describe('dotted keys', () => {
  it('finds a capability by its full name', () => {
    expect(i18n.global.t('homemix.capability["storage.household"]')).toBe('zapisywać wspólne dane w tym miejscu')
  })
})
