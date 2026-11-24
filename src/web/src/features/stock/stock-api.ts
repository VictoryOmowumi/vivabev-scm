import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '@/lib/api-client'
import { warehouseKeys } from '@/features/warehouse/warehouse-api'

export type StockRequestStatus = 'Open' | 'PartiallyIssued' | 'Issued' | 'Closed' | 'Rejected' | 'Cancelled'

export const stockRequestStatusLabels: Record<StockRequestStatus, string> = {
  Open: 'Waiting',
  PartiallyIssued: 'Partly issued',
  Issued: 'Issued',
  Closed: 'Closed short',
  Rejected: 'Rejected',
  Cancelled: 'Cancelled',
}

export const isOutstanding = (s: StockRequestStatus) => s === 'Open' || s === 'PartiallyIssued'

// Mirrors StockRequestFormDto
export type StockOption = { id: string; code: string; name: string }
export type StockRequestForm = { costCentres: StockOption[]; sites: StockOption[]; defaultSiteId: string | null }

// Mirrors AvailableStockDto
export type AvailableStock = { itemId: string; itemCode: string; description: string; uomCode: string; onHand: number; reserved: number }

// Mirrors StockRequestSummaryDto
export type StockRequestSummary = {
  id: string
  number: string
  status: StockRequestStatus
  siteCode: string
  costCentreCode: string
  departmentCode: string
  requestedById: string
  requestedByName: string
  requestedAt: string
  neededBy: string | null
  purpose: string
  lineCount: number
  linesOutstanding: number
}

export type StockRequestLine = {
  id: string
  lineNo: number
  itemId: string
  itemCode: string
  description: string
  uomCode: string
  requested: number
  issued: number
  outstanding: number
  onHand: number
}

export type StockIssue = { issuedAt: string; issuedByName: string; itemCode: string; description: string; uomCode: string; quantity: number; value: number }

// Mirrors StockRequestDetailDto
export type StockRequestDetail = Omit<StockRequestSummary, 'siteCode' | 'lineCount' | 'linesOutstanding'> & {
  siteId: string
  siteCode: string
  siteName: string
  costCentreName: string
  lastIssuedAt: string | null
  decidedByName: string | null
  decidedAt: string | null
  decisionReason: string | null
  lines: StockRequestLine[]
  issues: StockIssue[]
}

export type StockCountStatus = 'Open' | 'Posted' | 'Cancelled'

// Mirrors StockCountSummaryDto
export type StockCountSummary = {
  id: string
  number: string
  status: StockCountStatus
  siteCode: string
  startedByName: string
  startedAt: string
  lineCount: number
  linesCounted: number
  linesDifferent: number
  varianceValue: number
  postedAt: string | null
}

export type StockCountLine = {
  id: string
  lineNo: number
  itemCode: string
  description: string
  uomCode: string
  expected: number
  counted: number | null
  variance: number
  unitCost: number
  varianceValue: number
}

// Mirrors StockCountDetailDto
export type StockCountDetail = {
  id: string
  number: string
  status: StockCountStatus
  siteId: string
  siteCode: string
  siteName: string
  startedByName: string
  startedAt: string
  postedByName: string | null
  postedAt: string | null
  notes: string | null
  lines: StockCountLine[]
}

// Mirrors StockMovementDto
export type StockMovement = { occurredAt: string; documentType: 'GoodsReceipt' | 'StockRequest' | 'StockCount'; documentId: string; documentNumber: string; quantity: number; unitCost: number }

export const stockKeys = {
  requests: ['inventory', 'stock-requests'] as const,
  request: (id: string) => ['inventory', 'stock-requests', id] as const,
  form: ['inventory', 'stock-requests', 'form'] as const,
  available: (siteId: string) => ['inventory', 'available', siteId] as const,
  counts: ['inventory', 'stock-counts'] as const,
  count: (id: string) => ['inventory', 'stock-counts', id] as const,
  movements: (siteId: string, itemId: string) => ['inventory', 'movements', siteId, itemId] as const,
}

export const useStockRequests = (enabled = true) =>
  useQuery({ queryKey: stockKeys.requests, queryFn: ({ signal }) => api<StockRequestSummary[]>('/api/inventory/stock-requests', { signal }), enabled })

export const useStockRequest = (id: string | undefined) =>
  useQuery({
    queryKey: stockKeys.request(id ?? ''),
    queryFn: ({ signal }) => api<StockRequestDetail>(`/api/inventory/stock-requests/${id}`, { signal }),
    enabled: !!id,
  })

export const useStockRequestForm = () =>
  useQuery({ queryKey: stockKeys.form, queryFn: ({ signal }) => api<StockRequestForm>('/api/inventory/stock-requests/form-options', { signal }) })

export const useAvailableStock = (siteId: string) =>
  useQuery({
    queryKey: stockKeys.available(siteId),
    queryFn: ({ signal }) => api<AvailableStock[]>(`/api/inventory/stock-requests/available?siteId=${siteId}`, { signal }),
    enabled: !!siteId,
  })

export const useStockCounts = (enabled = true) =>
  useQuery({ queryKey: stockKeys.counts, queryFn: ({ signal }) => api<StockCountSummary[]>('/api/inventory/stock-counts', { signal }), enabled })

export const useStockCount = (id: string | undefined) =>
  useQuery({
    queryKey: stockKeys.count(id ?? ''),
    queryFn: ({ signal }) => api<StockCountDetail>(`/api/inventory/stock-counts/${id}`, { signal }),
    enabled: !!id,
  })

export const useMovements = (siteId: string, itemId: string, enabled: boolean) =>
  useQuery({
    queryKey: stockKeys.movements(siteId, itemId),
    queryFn: ({ signal }) => api<StockMovement[]>(`/api/inventory/stock/movements?siteId=${siteId}&itemId=${itemId}`, { signal }),
    enabled,
  })

/** Anything that touches stock refreshes everything inventory-shaped: balances, requests, counts. */
function useInventoryMutation<TVariables, TResult = void>(fn: (v: TVariables) => Promise<TResult>) {
  const queryClient = useQueryClient()
  return useMutation({ mutationFn: fn, onSuccess: () => queryClient.invalidateQueries({ queryKey: warehouseKeys.all }) })
}

export type NewStockRequest = { siteId: string; costCentreId: string; purpose: string; neededBy: string | null; lines: { itemId: string; quantity: number }[] }

export const useCreateStockRequest = () =>
  useInventoryMutation((body: NewStockRequest) => api<{ id: string; number: string }>('/api/inventory/stock-requests', { method: 'POST', body }))

export const useIssueStock = (id: string) =>
  useInventoryMutation((lines: { lineId: string; quantity: number }[]) =>
    api<void>(`/api/inventory/stock-requests/${id}/issue`, { method: 'POST', body: { lines } }))

export const useStockRequestAction = (id: string) =>
  useInventoryMutation(({ action, reason }: { action: 'reject' | 'close' | 'cancel'; reason?: string }) =>
    api<void>(`/api/inventory/stock-requests/${id}/${action}`, { method: 'POST', body: action === 'cancel' ? undefined : { reason } }))

export const useStartCount = () =>
  useInventoryMutation((siteId: string) => api<{ id: string; number: string }>('/api/inventory/stock-counts', { method: 'POST', body: { siteId } }))

export const useSaveCounts = (id: string) =>
  useInventoryMutation((lines: { lineId: string; counted: number | null }[]) =>
    api<void>(`/api/inventory/stock-counts/${id}/counts`, { method: 'PUT', body: { lines } }))

export const useCountAction = (id: string) =>
  useInventoryMutation(({ action, notes }: { action: 'post' | 'cancel'; notes?: string | null }) =>
    api<void>(`/api/inventory/stock-counts/${id}/${action}`, { method: 'POST', body: action === 'post' ? { notes } : undefined }))
