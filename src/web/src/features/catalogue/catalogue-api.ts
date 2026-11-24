import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '@/lib/api-client'
import type { CatalogueItem } from '@/features/requisitions/requisition-editor-api'

export type { CatalogueItem, Category, UnitOfMeasure } from '@/features/requisitions/requisition-editor-api'
export { useCategories, useUnits } from '@/features/requisitions/requisition-editor-api'

export const itemTypes = ['RawMaterial', 'Packaging', 'FinishedGood', 'Consumable', 'Asset', 'Service'] as const
export type ItemType = (typeof itemTypes)[number]

export const itemTypeLabels: Record<ItemType, string> = {
  RawMaterial: 'Raw material',
  Packaging: 'Packaging',
  FinishedGood: 'Finished good',
  Consumable: 'Consumable',
  Asset: 'Asset',
  Service: 'Service',
}

// Mirrors ItemDetailDto
export type ItemDetail = {
  id: string
  code: string
  name: string
  description: string | null
  itemType: ItemType
  categoryId: string
  categoryCode: string
  categoryName: string
  baseUomCode: string
  isStocked: boolean
  isLotControlled: boolean
  shelfLifeDays: number | null
  isActive: boolean
}

export type ItemInput = {
  name: string
  description: string | null
  categoryId: string
  isStocked: boolean
  isLotControlled: boolean
  shelfLifeDays: number | null
}

export const useCatalogueItems = () =>
  useQuery({
    queryKey: ['catalogue', 'items', 'all'],
    queryFn: ({ signal }) => api<CatalogueItem[]>('/api/catalogue/items?take=500', { signal }),
  })

export const useItem = (id: string | null) =>
  useQuery({
    queryKey: ['catalogue', 'item', id],
    queryFn: ({ signal }) => api<ItemDetail>(`/api/catalogue/items/${id}`, { signal }),
    enabled: !!id,
  })

function useCatalogueMutation<TVariables, TResult = void>(fn: (v: TVariables) => Promise<TResult>) {
  const queryClient = useQueryClient()
  return useMutation({ mutationFn: fn, onSuccess: () => queryClient.invalidateQueries({ queryKey: ['catalogue'] }) })
}

export const useCreateItem = () =>
  useCatalogueMutation((body: ItemInput & { code: string; itemType: ItemType; baseUomCode: string }) =>
    api<{ id: string; code: string }>('/api/catalogue/items', { method: 'POST', body }))

export const useUpdateItem = (id: string) =>
  useCatalogueMutation((body: ItemInput) => api<void>(`/api/catalogue/items/${id}`, { method: 'PUT', body }))

export const useSetItemActive = (id: string) =>
  useCatalogueMutation((active: boolean) =>
    api<void>(`/api/catalogue/items/${id}/${active ? 'activate' : 'deactivate'}`, { method: 'POST' }))

export const useCreateCategory = () =>
  useCatalogueMutation((body: { code: string; name: string }) =>
    api<{ id: string }>('/api/catalogue/categories', { method: 'POST', body }))

export const useRenameCategory = () =>
  useCatalogueMutation(({ id, name }: { id: string; name: string }) =>
    api<void>(`/api/catalogue/categories/${id}`, { method: 'PUT', body: { name } }))

export const useSetCategoryActive = () =>
  useCatalogueMutation(({ id, active }: { id: string; active: boolean }) =>
    api<void>(`/api/catalogue/categories/${id}/${active ? 'activate' : 'deactivate'}`, { method: 'POST' }))
