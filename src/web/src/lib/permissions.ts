import type { CurrentUser } from '@/features/auth/auth-api'

// Mirrors VivaBev.SharedKernel.Security.Permissions. The API is the real gate;
// the app only uses these to decide what to show.
export const Permission = {
  RequisitionsCreate: 'requisitions.create',
  RequisitionsViewDepartment: 'requisitions.view-department',
  RequisitionsViewAll: 'requisitions.view-all',
} as const

export function can(user: CurrentUser | null | undefined, permission: string): boolean {
  return user?.permissions.includes(permission) ?? false
}
