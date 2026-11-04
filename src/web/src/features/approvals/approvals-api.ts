import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '@/lib/api-client'
import { requisitionKeys } from '@/features/requisitions/requisitions-api'

// Mirrors ApprovalTaskDto in VivaBev.Approvals
export type ApprovalTask = {
  requestId: string
  documentType: string
  documentId: string
  documentNumber: string
  title: string
  amount: number
  requestedByName: string
  requestedAt: string
  stepName: string
  stepNumber: number
  totalSteps: number
  overBudget: boolean
}

// Mirrors DecidedTaskDto
export type DecidedTask = {
  requestId: string
  documentType: string
  documentId: string
  documentNumber: string
  title: string
  amount: number
  requestedByName: string
  myDecision: 'Approved' | 'Rejected'
  decidedAt: string
  myComment: string | null
  requestStatus: 'Pending' | 'Approved' | 'Rejected' | 'Withdrawn'
  overBudget: boolean
}

export const approvalKeys = {
  all: ['approvals'] as const,
  inbox: ['approvals', 'inbox'] as const,
  decided: ['approvals', 'decided'] as const,
}

export function useApprovalInbox() {
  return useQuery({
    queryKey: approvalKeys.inbox,
    queryFn: ({ signal }) => api<ApprovalTask[]>('/api/approvals/inbox', { signal }),
  })
}

export function useDecidedApprovals() {
  return useQuery({
    queryKey: approvalKeys.decided,
    queryFn: ({ signal }) => api<DecidedTask[]>('/api/approvals/decided?days=30', { signal }),
  })
}

export type Decision = 'approve' | 'reject'

export function useDecide() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ requestId, decision, comment }: { requestId: string; decision: Decision; comment: string }) =>
      api<void>(`/api/approvals/${requestId}/${decision}`, {
        method: 'POST',
        body: { comment: comment.trim() || null },
      }),
    // Everything that shows approvals or requisitions may now be out of date
    onSuccess: () =>
      Promise.all([
        queryClient.invalidateQueries({ queryKey: approvalKeys.all }),
        queryClient.invalidateQueries({ queryKey: requisitionKeys.all }),
      ]),
  })
}
