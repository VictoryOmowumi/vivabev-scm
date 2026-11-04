import { api, ApiError } from '@/lib/api-client'

// Mirrors CurrentUserDto in VivaBev.Identity.Contracts (ASP.NET Core sends camelCase JSON)
export type CurrentUser = {
  id: string
  email: string
  displayName: string
  departmentCode: string
  siteCode: string
  roles: string[]
  permissions: string[]
}

export type LoginRequest = { email: string; password: string }

export const authApi = {
  /** The signed-in user, or null when there is no valid cookie. */
  async me(signal?: AbortSignal): Promise<CurrentUser | null> {
    try {
      return await api<CurrentUser>('/api/auth/me', { signal })
    } catch (error) {
      if (error instanceof ApiError && error.status === 401) return null
      throw error
    }
  },
  login: (request: LoginRequest) => api<void>('/api/auth/login', { method: 'POST', body: request }),
  logout: () => api<void>('/api/auth/logout', { method: 'POST' }),
}
