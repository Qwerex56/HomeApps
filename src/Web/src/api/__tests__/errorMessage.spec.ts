import { describe, expect, it } from 'vitest'
import { errorMessage } from '../errorMessage'

const base = { status: 400, errors: [], fieldErrors: {} }

describe('errorMessage', () => {
  it('translates a known code', () => {
    expect(errorMessage({ ...base, code: 'auth.invalid_credentials' })).toBe('Nieprawidłowy email lub hasło.')
  })

  it("falls back to the service's detail for an unknown code", () => {
    expect(errorMessage({ ...base, code: 'something.new', detail: 'From the service.' })).toBe('From the service.')
  })

  it('puts Retry-After into the rate limit message', () => {
    expect(errorMessage({ ...base, status: 429, code: 'rate_limited', retryAfter: 30 })).toContain('30 s')
  })
})
