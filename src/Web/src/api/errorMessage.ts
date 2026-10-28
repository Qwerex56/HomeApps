import { i18n } from '@/shared/i18n'
import type { Problem } from './problem'

/** The text to show for a problem: the translated code, else the service's own detail, else a generic message. */
export function errorMessage(problem: Problem): string {
  const { t, te } = i18n.global
  if (problem.code === 'rate_limited') {
    return t('errors.rate_limited', { seconds: problem.retryAfter ?? 60 })
  }
  if (problem.code && te(`errors.${problem.code}`)) {
    return t(`errors.${problem.code}`)
  }
  return problem.detail ?? t('errors.unknown')
}
