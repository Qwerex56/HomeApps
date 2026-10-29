import { QueryClient } from '@tanstack/vue-query'
import { toProblem } from './problem'

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      // A 4xx answer will not change by asking again; network errors and 5xx get two more tries.
      retry: (failureCount, error) => {
        const { status } = toProblem(error)
        return (status === 0 || status >= 500) && failureCount < 2
      },
    },
  },
})
