import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '@/lib/api-client'
import { requisitionKeys } from './requisitions-api'

// ---- what the form chooses from ----

// Mirrors CostCentreOptionDto
export type CostCentreOption = {
  id: string
  code: string
  name: string
  fiscalYear: number
  hasBudget: boolean
  budget: number
  available: number
  inApproval: number
}

// Mirrors SiteDto (Identity)
export type Site = { id: string; code: string; name: string; siteType: string; city: string; isActive: boolean }
// Mirrors CategorySummaryDto, UnitOfMeasureDto, ItemSummaryDto (Catalogue)
export type Category = { id: string; code: string; name: string; isActive: boolean }
export type UnitOfMeasure = { code: string; name: string; dimension: string }
export type CatalogueItem = {
  id: string
  code: string
  name: string
  itemType: string
  category: string
  baseUomCode: string
  isStocked: boolean
  isActive: boolean
}

// Lists that rarely change: fetch once per session
const forever = { staleTime: Infinity }

export const useCostCentreOptions = () =>
  useQuery({
    queryKey: ['requisitions', 'form-options'],
    queryFn: ({ signal }) => api<CostCentreOption[]>('/api/requisitions/form-options', { signal }),
  })

export const useSites = () =>
  useQuery({ queryKey: ['organisation', 'sites'], queryFn: ({ signal }) => api<Site[]>('/api/organisation/sites', { signal }), ...forever })

export const useCategories = () =>
  useQuery({ queryKey: ['catalogue', 'categories'], queryFn: ({ signal }) => api<Category[]>('/api/catalogue/categories', { signal }), ...forever })

export const useUnits = () =>
  useQuery({ queryKey: ['catalogue', 'units'], queryFn: ({ signal }) => api<UnitOfMeasure[]>('/api/catalogue/units-of-measure', { signal }), ...forever })

export const useItemSearch = (term: string) =>
  useQuery({
    queryKey: ['catalogue', 'items', term],
    queryFn: ({ signal }) => api<CatalogueItem[]>(`/api/catalogue/items?search=${encodeURIComponent(term)}`, { signal }),
    enabled: term.trim().length >= 2,
    staleTime: 60_000,
  })

// ---- changes ----

export type RequisitionDetails = {
  title: string
  justification: string
  costCentreId: string
  deliverToSiteId: string
  requiredBy: string // "2027-01-31"
}

export type NewLine =
  | { kind: 'catalogue'; itemId: string; quantity: number; estimatedUnitPrice: number; uomCode?: string }
  | { kind: 'free-text'; description: string; categoryId: string; lineType: 'Goods' | 'Service'; uomCode: string; quantity: number; estimatedUnitPrice: number }

/** Every change to a requisition refreshes everything requisition-shaped, and the inbox. */
function useRequisitionMutation<TVariables, TResult = void>(fn: (v: TVariables) => Promise<TResult>) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: fn,
    onSuccess: () =>
      Promise.all([
        queryClient.invalidateQueries({ queryKey: requisitionKeys.all }),
        queryClient.invalidateQueries({ queryKey: ['approvals'] }),
      ]),
  })
}

export const useCreateRequisition = () =>
  useRequisitionMutation((details: RequisitionDetails) =>
    api<{ id: string; number: string }>('/api/requisitions', { method: 'POST', body: details }))

export const useUpdateRequisition = (id: string) =>
  useRequisitionMutation((details: RequisitionDetails) =>
    api<void>(`/api/requisitions/${id}`, { method: 'PUT', body: details }))

export const useAddLine = (id: string) =>
  useRequisitionMutation((line: NewLine) =>
    api<{ lineNo: number }>(`/api/requisitions/${id}/lines`, {
      method: 'POST',
      body: line.kind === 'catalogue'
        ? { itemId: line.itemId, quantity: line.quantity, estimatedUnitPrice: line.estimatedUnitPrice, uomCode: line.uomCode ?? null }
        : { description: line.description, categoryId: line.categoryId, lineType: line.lineType, uomCode: line.uomCode, quantity: line.quantity, estimatedUnitPrice: line.estimatedUnitPrice },
    }))

export const useRemoveLine = (id: string) =>
  useRequisitionMutation((lineNo: number) => api<void>(`/api/requisitions/${id}/lines/${lineNo}`, { method: 'DELETE' }))

export const useSubmitRequisition = (id: string) =>
  useRequisitionMutation(() => api<void>(`/api/requisitions/${id}/submit`, { method: 'POST' }))

export const useCancelRequisition = (id: string) =>
  useRequisitionMutation((reason: string) =>
    api<void>(`/api/requisitions/${id}/cancel`, { method: 'POST', body: { reason } }))
