import { isAxiosError } from 'axios'

// The two error shapes the services send (application/problem+json, kit ResultHttpMapper):
//  - an AppError: { status, detail, code, errors?: [{ code, message }] } when there are several,
//  - a validation failure: { status, errors: { Field: [messages] } }.
export interface Problem {
  status: number
  /** The first AppError code, e.g. "auth.invalid_credentials". Missing on validation failures and network errors. */
  code?: string
  detail?: string
  /** Every AppError when the service sent more than one. */
  errors: { code: string; message: string }[]
  /** Validation messages per form field, with the field name in camelCase like the request body. */
  fieldErrors: Record<string, string[]>
  /** Seconds from Retry-After (429 rate_limited, 409 reactivation.pending). */
  retryAfter?: number
  /** A sign-in ticket: reactivation (403 account.disabled_by_user) or linking (409 external_login.link_required). */
  ticket?: string
  /** With external_login.link_required: the provider and the email of the existing account. */
  provider?: string
  email?: string
}

interface ProblemBody {
  status?: number
  detail?: string
  code?: string
  ticket?: string
  provider?: string
  email?: string
  errors?: Record<string, string[]> | { code: string; message: string }[]
}

export function toProblem(error: unknown): Problem {
  if (!isAxiosError(error) || !error.response) {
    return { status: 0, code: 'network', errors: [], fieldErrors: {} }
  }

  const { status, headers } = error.response
  const body = (typeof error.response.data === 'object' ? error.response.data : null) as ProblemBody | null
  const retryAfter = Number.parseInt(String(headers['retry-after'] ?? ''), 10)
  const problem: Problem = {
    status,
    code: body?.code,
    detail: body?.detail,
    errors: [],
    fieldErrors: {},
    retryAfter: Number.isNaN(retryAfter) ? undefined : retryAfter,
    ticket: body?.ticket,
    provider: body?.provider,
    email: body?.email,
  }

  if (Array.isArray(body?.errors)) {
    problem.errors = body.errors
  } else if (body?.errors) {
    for (const [field, messages] of Object.entries(body.errors)) {
      problem.fieldErrors[camelCase(field)] = messages
    }
  }

  return problem
}

// "DisplayName" -> "displayName", "Profile.FirstName" -> "profile.firstName".
function camelCase(field: string) {
  return field
    .split('.')
    .map((part) => part.charAt(0).toLowerCase() + part.slice(1))
    .join('.')
}
