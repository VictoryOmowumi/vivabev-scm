import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '@/lib/api-client'
import { orderKeys } from '@/features/purchase-orders/purchase-orders-api'

// Mirrors ReceivableOrderSummaryDto
export type ReceivableOrder = {
  id: string
  number: string
  status: 'Issued' | 'PartiallyReceived'
  supplierId: string
  supplierName: string
  siteId: string
  siteCode: string
  siteName: string
  expectedDelivery: string
  linesOutstanding: number
  lineCount: number
  total: number
}

export type ReceivableLine = {
  id: string
  lineNo: number
  itemCode: string | null
  description: string
  uomCode: string
  ordered: number
  received: number
  outstanding: number
  stocked: boolean
}

export type ReceiptSummary = {
  id: string
  number: string
  purchaseOrderId: string
  purchaseOrderNumber: string
  supplierName: string
  siteCode: string
  deliveryNoteNumber: string | null
  receivedByName: string
  receivedAt: string
  lineCount: number
  acceptedValue: number
  hasRejections: boolean
}

// Mirrors ReceivableOrderDetailDto
export type ReceivableOrderDetail = {
  id: string
  number: string
  status: string
  supplierId: string
  supplierName: string
  siteId: string
  siteCode: string
  siteName: string
  expectedDelivery: string
  lines: ReceivableLine[]
  receipts: ReceiptSummary[]
}

// Mirrors GoodsReceiptDetailDto
export type ReceiptDetail = {
  id: string
  number: string
  purchaseOrderId: string
  purchaseOrderNumber: string
  supplierId: string
  supplierName: string
  siteCode: string
  siteName: string
  deliveryNoteNumber: string | null
  notes: string | null
  receivedByName: string
  receivedAt: string
  acceptedValue: number
  lines: {
    lineNo: number
    itemCode: string | null
    description: string
    uomCode: string
    quantityAccepted: number
    quantityRejected: number
    rejectionReason: string | null
    unitPrice: number
    acceptedValue: number
  }[]
}

export type StockBalance = {
  siteId: string
  siteCode: string
  itemId: string
  itemCode: string
  description: string
  uomCode: string
  onHand: number
  averageCost: number
  value: number
  lastMovementAt: string
}

export const warehouseKeys = {
  all: ['inventory'] as const,
  due: ['inventory', 'due'] as const,
  order: (id: string) => ['inventory', 'order', id] as const,
  receipts: (poId?: string) => ['inventory', 'receipts', poId ?? 'all'] as const,
  receipt: (id: string) => ['inventory', 'receipt', id] as const,
  stock: ['inventory', 'stock'] as const,
}

export const useDueIn = (enabled = true) =>
  useQuery({ queryKey: warehouseKeys.due, queryFn: ({ signal }) => api<ReceivableOrder[]>('/api/inventory/receivable-orders', { signal }), enabled })

export const useReceivableOrder = (id: string | undefined) =>
  useQuery({
    queryKey: warehouseKeys.order(id ?? ''),
    queryFn: ({ signal }) => api<ReceivableOrderDetail>(`/api/inventory/receivable-orders/${id}`, { signal }),
    enabled: !!id,
  })

export const useReceipts = (purchaseOrderId?: string, enabled = true) =>
  useQuery({
    queryKey: warehouseKeys.receipts(purchaseOrderId),
    queryFn: ({ signal }) =>
      api<ReceiptSummary[]>(`/api/inventory/receipts${purchaseOrderId ? `?purchaseOrderId=${purchaseOrderId}` : ''}`, { signal }),
    enabled,
  })

export const useReceipt = (id: string | undefined) =>
  useQuery({
    queryKey: warehouseKeys.receipt(id ?? ''),
    queryFn: ({ signal }) => api<ReceiptDetail>(`/api/inventory/receipts/${id}`, { signal }),
    enabled: !!id,
  })

export const useStock = (enabled = true) =>
  useQuery({ queryKey: warehouseKeys.stock, queryFn: ({ signal }) => api<StockBalance[]>('/api/inventory/stock', { signal }), enabled })

export type ReceiveInput = {
  purchaseOrderId: string
  deliveryNoteNumber: string | null
  notes: string | null
  lines: { purchaseOrderLineId: string; quantityAccepted: number; quantityRejected: number; rejectionReason: string | null }[]
}

/** A receipt changes stock, the order's received quantities and its status. */
export function useReceive() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (body: ReceiveInput) => api<{ id: string; number: string }>('/api/inventory/receipts', { method: 'POST', body }),
    onSuccess: () =>
      Promise.all([
        queryClient.invalidateQueries({ queryKey: warehouseKeys.all }),
        queryClient.invalidateQueries({ queryKey: orderKeys.all }),
      ]),
  })
}
