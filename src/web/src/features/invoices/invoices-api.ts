import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '@/lib/api-client'
import { budgetKeys } from '@/features/budgets/budgets-api'

export type InvoiceStatus = 'OnHold' | 'Matched' | 'Approved' | 'Rejected' | 'Paid'

export const invoiceStatusLabels: Record<InvoiceStatus, string> = {
  OnHold: 'On hold',
  Matched: 'Matched',
  Approved: 'Approved',
  Rejected: 'Rejected',
  Paid: 'Paid',
}

/** VAT in Nigeria, used to suggest the VAT amount (the supplier's invoice is what counts). */
export const VAT_RATE = 0.075
/** How far above the ordered price an invoice may be and still match (mirrors SupplierInvoice.PriceTolerance). */
export const PRICE_TOLERANCE = 0.02

// Mirrors InvoiceSummaryDto
export type InvoiceSummary = {
  id: string
  number: string
  status: InvoiceStatus
  supplierId: string
  supplierName: string
  supplierInvoiceNumber: string
  purchaseOrderId: string
  purchaseOrderNumber: string
  invoiceDate: string
  dueDate: string
  total: number
  exceptions: number
  recordedAt: string
}

export type InvoiceLine = {
  lineNo: number
  purchaseOrderLineId: string
  description: string
  uomCode: string
  quantity: number
  unitPrice: number
  lineTotal: number
  orderedUnitPrice: number
  received: number | null
  invoicedBefore: number | null
  exception: string | null
}

// Mirrors InvoiceDetailDto
export type InvoiceDetail = Omit<InvoiceSummary, 'exceptions' | 'total'> & {
  subtotal: number
  vatAmount: number
  total: number
  currencyCode: string
  recordedByName: string
  matchedAt: string | null
  decisionReason: string | null
  decidedByName: string | null
  decidedAt: string | null
  paymentReference: string | null
  paidOn: string | null
  lines: InvoiceLine[]
}

export type InvoiceableOrder = {
  id: string
  number: string
  status: string
  supplierId: string
  supplierName: string
  paymentTermsDays: number
  total: number
  invoiced: number
  lines: { id: string; lineNo: number; itemCode: string | null; description: string; uomCode: string; ordered: number; received: number; invoiced: number; unitPrice: number }[]
}

export const invoiceKeys = {
  all: ['invoices'] as const,
  list: (poId?: string) => ['invoices', 'list', poId ?? 'all'] as const,
  detail: (id: string) => ['invoices', 'detail', id] as const,
  invoiceable: ['invoices', 'invoiceable'] as const,
}

export const useInvoices = (purchaseOrderId?: string, enabled = true) =>
  useQuery({
    queryKey: invoiceKeys.list(purchaseOrderId),
    queryFn: ({ signal }) => api<InvoiceSummary[]>(`/api/finance/invoices${purchaseOrderId ? `?purchaseOrderId=${purchaseOrderId}` : ''}`, { signal }),
    enabled,
  })

export const useInvoice = (id: string | undefined) =>
  useQuery({
    queryKey: invoiceKeys.detail(id ?? ''),
    queryFn: ({ signal }) => api<InvoiceDetail>(`/api/finance/invoices/${id}`, { signal }),
    enabled: !!id,
  })

export const useInvoiceableOrders = () =>
  useQuery({ queryKey: invoiceKeys.invoiceable, queryFn: ({ signal }) => api<InvoiceableOrder[]>('/api/finance/invoices/invoiceable-orders', { signal }) })

/** Invoices move budget from committed to spent, so budgets refresh too. */
function useInvoiceMutation<TVariables, TResult = void>(fn: (v: TVariables) => Promise<TResult>) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: fn,
    onSuccess: () =>
      Promise.all([
        queryClient.invalidateQueries({ queryKey: invoiceKeys.all }),
        queryClient.invalidateQueries({ queryKey: budgetKeys.all }),
      ]),
  })
}

export type RecordInvoice = {
  purchaseOrderId: string
  supplierInvoiceNumber: string
  invoiceDate: string
  vatAmount: number
  lines: { purchaseOrderLineId: string; quantity: number; unitPrice: number }[]
}

export const useRecordInvoice = () =>
  useInvoiceMutation((body: RecordInvoice) =>
    api<{ id: string; number: string; status: InvoiceStatus }>('/api/finance/invoices', { method: 'POST', body }))

const base = (id: string) => `/api/finance/invoices/${id}`

export const useRematch = (id: string) =>
  useInvoiceMutation(() => api<{ status: InvoiceStatus }>(`${base(id)}/rematch`, { method: 'POST' }))

export const useInvoiceDecision = (id: string) =>
  useInvoiceMutation(({ decision, reason }: { decision: 'approve' | 'reject'; reason: string }) =>
    api<void>(`${base(id)}/${decision}`, { method: 'POST', body: { reason } }))

export const usePayInvoice = (id: string) =>
  useInvoiceMutation((body: { paymentReference: string; paidOn: string }) => api<void>(`${base(id)}/pay`, { method: 'POST', body }))
