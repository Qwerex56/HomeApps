import { describe, expect, it } from 'vitest'
import { AxiosError, AxiosHeaders, type AxiosResponse } from 'axios'
import { toProblem } from '../problem'

function failed(status: number, data: unknown, headers: Record<string, string> = {}) {
  const response = { status, data, headers, config: { headers: new AxiosHeaders() } } as AxiosResponse
  return new AxiosError('failed', undefined, undefined, undefined, response)
}

describe('toProblem', () => {
  it('reads the code of an AppError', () => {
    const problem = toProblem(failed(401, { status: 401, code: 'auth.invalid_credentials', detail: 'Wrong.' }))

    expect(problem).toMatchObject({ status: 401, code: 'auth.invalid_credentials', detail: 'Wrong.', errors: [] })
  })

  it('keeps every AppError when there are several', () => {
    const errors = [
      { code: 'profile.name_required', message: 'a' },
      { code: 'profile.too_young', message: 'b' },
    ]

    expect(toProblem(failed(400, { code: errors[0]!.code, errors })).errors).toEqual(errors)
  })

  it('maps validation failures to camelCase fields', () => {
    const problem = toProblem(failed(400, { errors: { DisplayName: ['empty'], 'Profile.FirstName': ['long'] } }))

    expect(problem.fieldErrors).toEqual({ displayName: ['empty'], 'profile.firstName': ['long'] })
    expect(problem.code).toBeUndefined()
  })

  it('reads Retry-After', () => {
    expect(toProblem(failed(429, { code: 'rate_limited' }, { 'retry-after': '42' })).retryAfter).toBe(42)
  })

  it('treats a missing response as a network error', () => {
    expect(toProblem(new AxiosError('offline'))).toMatchObject({ status: 0, code: 'network' })
  })
})
