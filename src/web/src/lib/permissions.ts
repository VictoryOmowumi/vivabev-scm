import type { CurrentUser } from '@/features/auth/auth-api'

// Mirrors VivaBev.SharedKernel.Security.Permissions. The API is the real gate;
// the app only uses these to decide what to show.
export const Permission = {
  RequisitionsCreate: 'requisitions.create',
  RequisitionsViewDepartment: 'requisitions.view-department',
  RequisitionsViewAll: 'requisitions.view-all',
  BudgetsView: 'finance.budgets.view',
  BudgetsViewDepartment: 'finance.budgets.view-department',
  BudgetsManage: 'finance.budgets.manage',
} as const

/** Any budget access at all: Finance, the MD, or a department manager for their own department. */
export const canSeeBudgets = (user: CurrentUser | null | undefined) =>
  can(user, Permission.BudgetsView) || can(user, Permission.BudgetsViewDepartment)

export function can(user: CurrentUser | null | undefined, permission: string): boolean {
  return user?.permissions.includes(permission) ?? false
}
