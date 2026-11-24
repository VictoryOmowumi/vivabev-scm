import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '@/lib/api-client'
import { requisitionKeys } from '@/features/requisitions/requisitions-api'
import { sourcingKeys } from '@/features/sourcing/sourcing-api'

export type PurchaseOrderStatus = 'Draft' | 'Issued' | 'Cancelled'

export const orderStatusLabels: Record<PurchaseOrderStatus, string> = {
  Draft: 'Draft',
  Issued: 'Issued',
  Cancelled: 'Cancelled',
}

// Mirrors PurchaseOrderSummaryDto
export type PurchaseOrderSummary = {
  id: string
  number: string
  status: PurchaseOrderStatus
  supplierId: string
  supplierName: string
  sourcingEventId: string
  sourcingEventNumber: string
  total: number
  expectedDelivery: string // DateOnly
  issuedAt: string | null
  lineCount: number
  requisitionNumbers: string[]
}

export type PurchaseOrderLine = {
  id: string
  lineNo: number
  requisitionId: string
  requisitionNumber: string
  costCentreCode: string
  itemCode: string | null
  description: string
  quantity: number
  uomCode: string
  unitPrice: number
  lineTotal: number
}

// Mirrors PurchaseOrderDetailDto
export type PurchaseOrderDetail = {
  id: string
  number: string
  status: PurchaseOrderStatus
  supplier: {
    id: string
    code: string
    name: string
    legalName: string
    taxId: string
    address: string | null
    contactName: string | null
    contactEmail: string | null
    contactPhone: string | null
  }
  sourcingEventId: string
  sourcingEventNumber: string
  supplierReference: string | null
  buyerName: string
  paymentTermsDays: number
  deliverTo: { id: string; code: string; name: string; city: string; state: string }
  expectedDelivery: string
  notes: string | null
  currencyCode: string
  issuedAt: string | null
  cancellationReason: string | null
  total: number
  lines: PurchaseOrderLine[]
}

export const orderKeys = {
  all: ['purchase-orders'] as const,
  list: ['purchase-orders', 'list'] as const,
  detail: (id: string) => ['purchase-orders', 'detail', id] as const,
}

export const usePurchaseOrders = () =>
  useQuery({ queryKey: orderKeys.list, queryFn: ({ signal }) => api<PurchaseOrderSummary[]>('/api/purchase-orders', { signal }) })

export const usePurchaseOrder = (id: string | undefined) =>
  useQuery({
    queryKey: orderKeys.detail(id ?? ''),
    queryFn: ({ signal }) => api<PurchaseOrderDetail>(`/api/purchase-orders/${id}`, { signal }),
    enabled: !!id,
  })

/** Orders touch RFQs (their PO link) and requisitions (Ordered), so all three caches refresh. */
function useOrderMutation<TVariables, TResult = void>(fn: (v: TVariables) => Promise<TResult>) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: fn,
    onSuccess: () =>
      Promise.all([
        queryClient.invalidateQueries({ queryKey: orderKeys.all }),
        queryClient.invalidateQueries({ queryKey: sourcingKeys.all }),
        queryClient.invalidateQueries({ queryKey: requisitionKeys.all }),
      ]),
  })
}

const base = (id: string) => `/api/purchase-orders/${id}`

export const useCreateOrder = () =>
  useOrderMutation((sourcingEventId: string) =>
    api<{ id: string; number: string }>('/api/purchase-orders', { method: 'POST', body: { sourcingEventId } }))

export const useUpdateDelivery = (id: string) =>
  useOrderMutation((body: { deliverToSiteId: string; expectedDelivery: string; notes: string | null }) =>
    api<void>(`${base(id)}/delivery`, { method: 'PUT', body }))

export const useIssueOrder = (id: string) => useOrderMutation(() => api<void>(`${base(id)}/issue`, { method: 'POST' }))

export const useCancelOrder = (id: string) =>
  useOrderMutation((reason: string) => api<void>(`${base(id)}/cancel`, { method: 'POST', body: { reason } }))
