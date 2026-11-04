import { useQuery } from '@tanstack/react-query'
import { api } from '@/lib/api-client'

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

export function useApprovalInbox() {
  return useQuery({
    queryKey: ['approvals', 'inbox'],
    queryFn: ({ signal }) => api<ApprovalTask[]>('/api/approvals/inbox', { signal }),
  })
}
