import { ref } from 'vue'
import { errorMessage } from '@/api/errorMessage'
import { toProblem, type Problem } from '@/api/problem'

/**
 * Runs a form's request and keeps what the page shows: busy, the message for the whole form and the messages per field.
 * `onProblem` may handle a problem itself (a ticket, a redirect) by returning true.
 * `fieldPrefix` drops the name of a wrapping request object from the field names ("profile.displayName" -> "displayName").
 */
export function useSubmit(fieldPrefix = '') {
  const busy = ref(false)
  const formError = ref<string | null>(null)
  const fieldErrors = ref<Record<string, string[]>>({})

  async function submit(action: () => Promise<unknown>, onProblem?: (problem: Problem) => boolean | void) {
    busy.value = true
    formError.value = null
    fieldErrors.value = {}
    try {
      await action()
      return true
    } catch (error) {
      const problem = toProblem(error)
      if (!onProblem?.(problem)) {
        fieldErrors.value = Object.fromEntries(
          Object.entries(problem.fieldErrors).map(([field, messages]) => [
            field.startsWith(fieldPrefix) ? field.slice(fieldPrefix.length) : field,
            messages,
          ]),
        )
        if (Object.keys(problem.fieldErrors).length === 0) {
          formError.value = errorMessage(problem)
        }
      }
      return false
    } finally {
      busy.value = false
    }
  }

  return { busy, formError, fieldErrors, submit }
}
