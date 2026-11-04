import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { authApi, type LoginRequest } from './auth-api'
import { clearSessionExpired } from './session'

export const currentUserKey = ['auth', 'me'] as const

export function useCurrentUser() {
  return useQuery({
    queryKey: currentUserKey,
    queryFn: ({ signal }) => authApi.me(signal),
    staleTime: 5 * 60_000,
  })
}

export function useLogin() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (request: LoginRequest) => authApi.login(request),
    // Returning the promise makes mutateAsync wait until "me" has reloaded
    onSuccess: () => {
      clearSessionExpired()
      return queryClient.invalidateQueries({ queryKey: currentUserKey })
    },
  })
}

export function useLogout() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: authApi.logout,
    onSettled: () => {
      // Mark them signed out (<RequireAuth> sees null and sends them to /login),
      // then forget everything else cached for this user.
      queryClient.setQueryData(currentUserKey, null)
      queryClient.removeQueries({ predicate: (query) => query.queryKey[0] !== 'auth' })
    },
  })
}
