import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '@/lib/api-client'

export type SupplierStatus = 'Prospect' | 'PendingApproval' | 'Approved' | 'Suspended' | 'Blocked'

export const supplierStatusLabels: Record<SupplierStatus, string> = {
  Prospect: 'Prospect',
  PendingApproval: 'Pending approval',
  Approved: 'Approved',
  Suspended: 'Suspended',
  Blocked: 'Blocked',
}

// Mirrors SupplierSummaryDto
export type SupplierSummary = {
  id: string
  code: string
  legalName: string
  status: SupplierStatus
  countryCode: string
  approvedCategories: number
}

export type SupplierCategory = {
  categoryId: string
  categoryCode: string
  categoryName: string
  status: 'Pending' | 'Approved' | 'Revoked'
  approvedAt: string | null
}

export type SupplierContact = { id: string; name: string; email: string; phone: string | null; role: string | null; isPrimary: boolean }
export type SupplierAddress = { id: string; addressType: 'Billing' | 'Shipping'; line1: string; line2: string | null; city: string; state: string; countryCode: string }

// Mirrors SupplierDetailDto
export type SupplierDetail = {
  id: string
  code: string
  legalName: string
  tradingName: string | null
  taxId: string
  status: SupplierStatus
  statusReason: string | null
  countryCode: string
  defaultCurrencyCode: string
  paymentTermsDays: number
  contacts: SupplierContact[]
  addresses: SupplierAddress[]
  categories: SupplierCategory[]
}

export type NewSupplier = {
  code: string
  legalName: string
  tradingName: string | null
  taxId: string
  contactName: string
  contactEmail: string
  contactPhone: string | null
  paymentTermsDays: number
}

export const supplierKeys = {
  all: ['suppliers'] as const,
  list: ['suppliers', 'list'] as const,
  detail: (id: string) => ['suppliers', 'detail', id] as const,
}

export const useSuppliers = () =>
  useQuery({ queryKey: supplierKeys.list, queryFn: ({ signal }) => api<SupplierSummary[]>('/api/suppliers', { signal }) })

export const useSupplier = (id: string | undefined) =>
  useQuery({
    queryKey: supplierKeys.detail(id ?? ''),
    queryFn: ({ signal }) => api<SupplierDetail>(`/api/suppliers/${id}`, { signal }),
    enabled: !!id,
  })

/** Supplier changes also change who can be invited to RFQs. */
function useSupplierMutation<TVariables, TResult = void>(fn: (v: TVariables) => Promise<TResult>) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: fn,
    onSuccess: () =>
      Promise.all([
        queryClient.invalidateQueries({ queryKey: supplierKeys.all }),
        queryClient.invalidateQueries({ queryKey: ['sourcing'] }),
      ]),
  })
}

export const useCreateSupplier = () =>
  useSupplierMutation((body: NewSupplier) => api<{ id: string }>('/api/suppliers', { method: 'POST', body }))

export type SupplierAction = 'submit' | 'approve' | 'reject' | 'suspend' | 'reinstate'

export const useSupplierAction = (id: string) =>
  useSupplierMutation(({ action, reason }: { action: SupplierAction; reason?: string }) =>
    api<void>(`/api/suppliers/${id}/${action}`, { method: 'POST', body: reason ? { reason } : undefined }))

export const useRequestCategory = (id: string) =>
  useSupplierMutation((categoryId: string) => api<void>(`/api/suppliers/${id}/categories`, { method: 'POST', body: { categoryId } }))

export const useCategoryDecision = (id: string) =>
  useSupplierMutation(({ categoryId, decision }: { categoryId: string; decision: 'approve' | 'revoke' }) =>
    api<void>(`/api/suppliers/${id}/categories/${categoryId}/${decision}`, { method: 'POST' }))

export type ContactInput = Omit<SupplierContact, 'id'>
export type AddressInput = Omit<SupplierAddress, 'id'>

export const useUpdateSupplierDetails = (id: string) =>
  useSupplierMutation((body: { tradingName: string | null; paymentTermsDays: number }) =>
    api<void>(`/api/suppliers/${id}/details`, { method: 'PUT', body }))

/** Add (no id), change or remove a contact. */
export const useSaveContact = (id: string) =>
  useSupplierMutation(({ contactId, body }: { contactId?: string; body: ContactInput }) =>
    contactId
      ? api<void>(`/api/suppliers/${id}/contacts/${contactId}`, { method: 'PUT', body })
      : api<{ id: string }>(`/api/suppliers/${id}/contacts`, { method: 'POST', body }).then(() => undefined))

export const useRemoveContact = (id: string) =>
  useSupplierMutation((contactId: string) => api<void>(`/api/suppliers/${id}/contacts/${contactId}`, { method: 'DELETE' }))

export const useSaveAddress = (id: string) =>
  useSupplierMutation(({ addressId, body }: { addressId?: string; body: AddressInput }) =>
    addressId
      ? api<void>(`/api/suppliers/${id}/addresses/${addressId}`, { method: 'PUT', body })
      : api<{ id: string }>(`/api/suppliers/${id}/addresses`, { method: 'POST', body }).then(() => undefined))

export const useRemoveAddress = (id: string) =>
  useSupplierMutation((addressId: string) => api<void>(`/api/suppliers/${id}/addresses/${addressId}`, { method: 'DELETE' }))
