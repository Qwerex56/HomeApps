import { describe, expect, it } from 'vitest'
import { i18n, polishPlural } from '../index'

describe('polishPlural', () => {
  it.each([
    [0, 2],
    [1, 0],
    [2, 1],
    [4, 1],
    [5, 2],
    [12, 2],
    [14, 2],
    [22, 1],
    [25, 2],
    [112, 2],
    [123, 1],
  ])('%i -> form %i', (n, form) => expect(polishPlural(n)).toBe(form))

  it('is used by t()', () => {
    const t = i18n.global.t
    expect(t('households.memberCount', 1)).toBe('1 osoba')
    expect(t('households.memberCount', 3)).toBe('3 osoby')
    expect(t('households.memberCount', 13)).toBe('13 osób')
  })
})
