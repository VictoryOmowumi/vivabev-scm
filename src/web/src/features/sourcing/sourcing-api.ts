import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '@/lib/api-client'
import { requisitionKeys } from '@/features/requisitions/requisitions-api'

export type SourcingEventStatus = 'Draft' | 'Published' | 'ResponsesClosed' | 'Awarded' | 'Cancelled'

export const eventStatusLabels: Record<SourcingEventStatus, string> = {
  Draft: 'Draft',
  Published: 'Open for quotes',
  ResponsesClosed: 'Responses closed',
  Awarded: 'Awarded',
  Cancelled: 'Cancelled',
}

// Mirrors SourcingEventSummaryDto
export type SourcingEventSummary = {
  id: string
  number: string
  title: string
  status: SourcingEventStatus
  buyerName: string
  responseDeadline: string
  lineCount: number
  requisitionNumbers: string[]
  invited: number
  quoted: number
  estimatedTotal: number
  lowestQuote: number | null
  publishedAt: string | null
}

export type SourcingEventLine = {
  id: string
  lineNo: number
  requisitionId: string
  requisitionNumber: string
  itemId: string | null
  itemCode: string | null
  description: string
  categoryId: string
  quantity: number
  uomCode: string
  estimatedUnitPrice: number
  estimatedTotal: number
}

export type Invitation = { supplierId: string; supplierName: string; status: 'Invited' | 'Declined' | 'Responded'; invitedAt: string | null }

export type QuotationLine = { sourcingEventLineId: string; unitPrice: number; quantity: number; lineTotal: number; notes: string | null }

export type Quotation = {
  id: string
  supplierId: string
  supplierName: string
  supplierReference: string | null
  receivedAt: string
  validUntil: string
  leadTimeDays: number
  paymentTermsDays: number
  total: number
  status: 'Received' | 'Withdrawn'
  withdrawalReason: string | null
  lines: QuotationLine[]
}

// Mirrors SourcingEventDetailDto
export type SourcingEventDetail = {
  id: string
  number: string
  title: string
  status: SourcingEventStatus
  buyerId: string
  buyerName: string
  responseDeadline: string
  currencyCode: string
  publishedAt: string | null
  responsesClosedAt: string | null
  cancellationReason: string | null
  estimatedTotal: number
  needsCompetition: boolean
  minimumSuppliers: number
  competitiveThreshold: number
  lines: SourcingEventLine[]
  invitations: Invitation[]
  quotations: Quotation[]
}

export type SourceableRequisition = {
  id: string
  number: string
  title: string
  requesterName: string
  costCentreCode: string
  requiredBy: string
  estimatedTotal: number
  lineCount: number
  approvedAt: string | null
}

export type EligibleSupplier = {
  id: string
  code: string
  name: string
  paymentTermsDays: number
  linesCovered: number
  totalLines: number
  invited: boolean
}

export const sourcingKeys = {
  all: ['sourcing'] as const,
  list: ['sourcing', 'events'] as const,
  detail: (id: string) => ['sourcing', 'event', id] as const,
  eligible: (id: string) => ['sourcing', 'eligible', id] as const,
  sourceable: ['sourcing', 'sourceable'] as const,
}

export const useSourcingEvents = () =>
  useQuery({ queryKey: sourcingKeys.list, queryFn: ({ signal }) => api<SourcingEventSummary[]>('/api/sourcing/events', { signal }) })

export const useSourcingEvent = (id: string | undefined) =>
  useQuery({
    queryKey: sourcingKeys.detail(id ?? ''),
    queryFn: ({ signal }) => api<SourcingEventDetail>(`/api/sourcing/events/${id}`, { signal }),
    enabled: !!id,
  })

export const useSourceableRequisitions = (enabled = true) =>
  useQuery({
    queryKey: sourcingKeys.sourceable,
    queryFn: ({ signal }) => api<SourceableRequisition[]>('/api/sourcing/sourceable-requisitions', { signal }),
    enabled,
  })

export const useEligibleSuppliers = (id: string, enabled: boolean) =>
  useQuery({
    queryKey: sourcingKeys.eligible(id),
    queryFn: ({ signal }) => api<EligibleSupplier[]>(`/api/sourcing/events/${id}/eligible-suppliers`, { signal }),
    enabled,
  })

/** Every RFQ change can move requisitions in or out of sourcing, so both caches refresh. */
function useSourcingMutation<TVariables, TResult = void>(fn: (v: TVariables) => Promise<TResult>) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: fn,
    onSuccess: () =>
      Promise.all([
        queryClient.invalidateQueries({ queryKey: sourcingKeys.all }),
        queryClient.invalidateQueries({ queryKey: requisitionKeys.all }),
      ]),
  })
}

const base = (id: string) => `/api/sourcing/events/${id}`

export const useCreateEvent = () =>
  useSourcingMutation((body: { title: string; responseDeadline: string; requisitionIds: string[] }) =>
    api<{ id: string; number: string }>('/api/sourcing/events', { method: 'POST', body }))

export const useAddRequisition = (id: string) =>
  useSourcingMutation((requisitionId: string) => api<void>(`${base(id)}/requisitions`, { method: 'POST', body: { requisitionId } }))

export const useRemoveRequisition = (id: string) =>
  useSourcingMutation((requisitionId: string) => api<void>(`${base(id)}/requisitions/${requisitionId}`, { method: 'DELETE' }))

export const useInvite = (id: string) =>
  useSourcingMutation((supplierId: string) => api<void>(`${base(id)}/invitations`, { method: 'POST', body: { supplierId } }))

export const useUninvite = (id: string) =>
  useSourcingMutation((supplierId: string) => api<void>(`${base(id)}/invitations/${supplierId}`, { method: 'DELETE' }))

export const useRecordDecline = (id: string) =>
  useSourcingMutation((supplierId: string) => api<void>(`${base(id)}/invitations/${supplierId}/decline`, { method: 'POST' }))

export const useChangeDeadline = (id: string) =>
  useSourcingMutation((responseDeadline: string) => api<void>(`${base(id)}/deadline`, { method: 'PUT', body: { responseDeadline } }))

export const usePublish = (id: string) => useSourcingMutation(() => api<void>(`${base(id)}/publish`, { method: 'POST' }))

export const useCloseResponses = (id: string) => useSourcingMutation(() => api<void>(`${base(id)}/close`, { method: 'POST' }))

export const useCancelEvent = (id: string) =>
  useSourcingMutation((reason: string) => api<void>(`${base(id)}/cancel`, { method: 'POST', body: { reason } }))

export type RecordQuotation = {
  supplierId: string
  supplierReference: string | null
  validUntil: string
  leadTimeDays: number
  paymentTermsDays: number
  lines: { sourcingEventLineId: string; unitPrice: number; quantity: number; notes: string | null }[]
}

export const useRecordQuotation = (id: string) =>
  useSourcingMutation((body: RecordQuotation) => api<{ id: string }>(`${base(id)}/quotations`, { method: 'POST', body }))

export const useWithdrawQuotation = (id: string) =>
  useSourcingMutation(({ quotationId, reason }: { quotationId: string; reason: string }) =>
    api<void>(`${base(id)}/quotations/${quotationId}/withdraw`, { method: 'POST', body: { reason } }))
