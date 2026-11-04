import { QueryCache, QueryClient } from '@tanstack/react-query'
import { currentUserKey } from '@/features/auth/use-auth'
import { ApiError } from '@/lib/api-client'

export const queryClient: QueryClient = new QueryClient({
  queryCache: new QueryCache({
    // Any 401 means the cookie expired: mark the user signed out
    onError: (error) => {
      if (error instanceof ApiError && error.status === 401) {
        queryClient.setQueryData(currentUserKey, null)
      }
    },
  }),
  defaultOptions: {
    queries: {
      // Retry network blips and 5xx twice; a 4xx will not fix itself
      retry: (failureCount, error) =>
        !(error instanceof ApiError && error.status < 500) && failureCount < 2,
      refetchOnWindowFocus: false,
    },
  },
})
