import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '@/lib/api-client'

// Mirrors BudgetSummaryDto
export type BudgetSummary = {
  id: string
  costCentreId: string
  costCentreCode: string
  costCentreName: string
  fiscalYear: number
  currencyCode: string
  amount: number
  committed: number
  consumed: number
  available: number // amount - committed - consumed
  inApproval: number // submitted requisitions not yet approved
  departmentCode: string
}

// Mirrors BudgetCommitmentDto
export type BudgetCommitment = {
  documentType: string
  documentId: string
  documentNumber: string
  amount: number
  status: 'Open' | 'Released' | 'Consumed'
  openedAt: string
  closedAt: string | null
}

export type BudgetDetail = { budget: BudgetSummary; commitments: BudgetCommitment[] }

export const budgetKeys = {
  all: ['budgets'] as const,
  list: (fiscalYear: number) => ['budgets', 'list', fiscalYear] as const,
  detail: (id: string) => ['budgets', 'detail', id] as const,
}

export const useBudgets = (fiscalYear: number, enabled = true) =>
  useQuery({
    queryKey: budgetKeys.list(fiscalYear),
    queryFn: ({ signal }) => api<BudgetSummary[]>(`/api/finance/budgets?fiscalYear=${fiscalYear}`, { signal }),
    enabled,
  })

export const useBudget = (id: string | undefined) =>
  useQuery({
    queryKey: budgetKeys.detail(id ?? ''),
    queryFn: ({ signal }) => api<BudgetDetail>(`/api/finance/budgets/${id}`, { signal }),
    enabled: !!id,
  })

export function useSetBudget() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (request: { costCentreId: string; fiscalYear: number; amount: number }) =>
      api<{ id: string }>('/api/finance/budgets', { method: 'POST', body: request }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: budgetKeys.all }),
  })
}

/** What's genuinely free: available, minus what's already on its way through approval. */
export const freeOf = (b: Pick<BudgetSummary, 'available' | 'inApproval'>) => b.available - b.inApproval

export type BudgetHealth = 'over' | 'tight' | 'ok'

/** Over: nothing free. Tight: under 10% of the budget free. */
export function healthOf(b: Pick<BudgetSummary, 'amount' | 'available' | 'inApproval'>): BudgetHealth {
  const free = freeOf(b)
  if (free < 0) return 'over'
  if (b.amount > 0 && free / b.amount < 0.1) return 'tight'
  return 'ok'
}
