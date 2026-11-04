import { useQuery } from '@tanstack/react-query'
import { api } from '@/lib/api-client'

export type RequisitionStatus = 'Draft' | 'PendingApproval' | 'Approved' | 'Cancelled'
export type BudgetStatus = 'NotChecked' | 'WithinBudget' | 'OverBudget'
export type RequisitionScope = 'mine' | 'department' | 'all'

export const statusLabels: Record<RequisitionStatus, string> = {
  Draft: 'Draft',
  PendingApproval: 'Pending approval',
  Approved: 'Approved',
  Cancelled: 'Cancelled',
}

// Mirrors RequisitionSummaryDto
export type RequisitionSummary = {
  id: string
  number: string
  title: string
  status: RequisitionStatus
  returned: boolean
  requesterId: string
  requesterName: string
  costCentreCode: string
  budgetStatus: BudgetStatus
  requiredBy: string // DateOnly, "2027-01-31"
  estimatedTotal: number
  currencyCode: string
  submittedAt: string | null
}

// Mirrors RequisitionLineDto
export type RequisitionLine = {
  lineNo: number
  lineType: 'Goods' | 'Service'
  itemId: string | null
  itemCode: string | null
  description: string
  categoryId: string
  quantity: number
  uomCode: string
  estimatedUnitPrice: number
  lineTotal: number
}

// Mirrors RequisitionDetailDto
export type RequisitionDetail = {
  id: string
  number: string
  title: string
  justification: string
  status: RequisitionStatus
  requesterId: string
  requesterName: string
  costCentreCode: string
  deliverToSiteCode: string
  requiredBy: string
  currencyCode: string
  estimatedTotal: number
  submittedAt: string | null
  cancellationReason: string | null
  rejectionReason: string | null
  approvedAt: string | null
  budgetStatus: BudgetStatus
  budgetAvailableAtSubmit: number | null
  lines: RequisitionLine[]
}

// Mirrors ApprovalStatusDto / ApprovalStepDto in VivaBev.Approvals
export type ApprovalStep = {
  sequence: number
  name: string
  approverId: string
  approverName: string
  decision: 'Pending' | 'Approved' | 'Rejected'
  decidedAt: string | null
  comment: string | null
}

export type ApprovalStatus = {
  requestId: string
  documentNumber: string
  status: 'Pending' | 'Approved' | 'Rejected' | 'Withdrawn'
  amount: number
  steps: ApprovalStep[]
}

export const requisitionKeys = {
  all: ['requisitions'] as const,
  list: (scope: RequisitionScope) => ['requisitions', 'list', scope] as const,
  detail: (id: string) => ['requisitions', 'detail', id] as const,
  approval: (id: string) => ['requisitions', 'approval', id] as const,
}

export function useRequisitions(scope: RequisitionScope) {
  return useQuery({
    queryKey: requisitionKeys.list(scope),
    queryFn: ({ signal }) => api<RequisitionSummary[]>(`/api/requisitions?scope=${scope}`, { signal }),
  })
}

export function useRequisition(id: string | undefined) {
  return useQuery({
    queryKey: requisitionKeys.detail(id ?? ''),
    queryFn: ({ signal }) => api<RequisitionDetail>(`/api/requisitions/${id}`, { signal }),
    enabled: !!id,
  })
}

export function useRequisitionApproval(id: string | undefined) {
  return useQuery({
    queryKey: requisitionKeys.approval(id ?? ''),
    // 204 (never submitted) comes back as undefined; store it as null
    queryFn: async ({ signal }) =>
      (await api<ApprovalStatus | undefined>(`/api/requisitions/${id}/approval`, { signal })) ?? null,
    enabled: !!id,
  })
}

/** The step waiting for a decision, if the request is still open. */
export function currentStep(approval: ApprovalStatus | null | undefined): ApprovalStep | undefined {
  if (approval?.status !== 'Pending') return undefined
  return approval.steps.find((s) => s.decision === 'Pending')
}
