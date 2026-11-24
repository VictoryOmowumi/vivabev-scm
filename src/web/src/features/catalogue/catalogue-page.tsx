import { useMemo, useState } from 'react'
import { useSearchParams } from 'react-router'
import { HugeiconsIcon } from '@hugeicons/react'
import { PackageIcon, PlusSignIcon, Search01Icon } from '@hugeicons/core-free-icons'
import { Panel } from '@/components/panel'
import { EmptyState, NoMatches } from '@/components/states/empty-state'
import { ErrorState } from '@/components/states/error-state'
import { TableSkeleton } from '@/components/states/table-skeleton'
import { Button } from '@/components/ui/button'
import { useCurrentUser } from '@/features/auth/use-auth'
import { useStock } from '@/features/warehouse/warehouse-api'
import { fieldClass } from '@/lib/field-class'
import { can, Permission } from '@/lib/permissions'
import { cn } from '@/lib/utils'
import { CategoryList } from './category-list'
import { itemTypeLabels, itemTypes, useCatalogueItems, useCategories, type ItemType } from './catalogue-api'
import { ItemPanel } from './item-panel'

const columns = ['Item', 'Type', 'Category', 'Unit', 'Stock', 'Status']

export function CataloguePage() {
  const { data: user } = useCurrentUser()
  const manage = can(user, Permission.CatalogueManage)
  const items = useCatalogueItems()
  const categories = useCategories()
  const stock = useStock(can(user, Permission.InventoryView))
  const [params, setParams] = useSearchParams()
  const [search, setSearch] = useState('')
  const categoryId = params.get('category')
  const type = params.get('type') as ItemType | null
  const selected = params.get('item') // an item id, or "new"

  const set = (key: string, value: string | null) =>
    setParams((p) => { const n = new URLSearchParams(p); if (value) n.set(key, value); else n.delete(key); return n }, { replace: true })

  const onHand = useMemo(() => {
    const totals = new Map<string, number>()
    for (const s of stock.data ?? []) totals.set(s.itemId, (totals.get(s.itemId) ?? 0) + s.onHand)
    return totals
  }, [stock.data])

  const term = search.trim().toLowerCase()
  const rows = (items.data ?? []).filter((i) =>
    (!categoryId || i.categoryId === categoryId) &&
    (!type || i.itemType === type) &&
    (!term || i.code.toLowerCase().includes(term) || i.name.toLowerCase().includes(term)))

  return (
    <div className="flex flex-col gap-6">
      <section className="flex flex-wrap items-end justify-between gap-5">
        <div>
          <h1 className="text-[40px] leading-tight font-light tracking-tight">Catalogue</h1>
          <p className="mt-2 text-[15px] text-muted-foreground">
            What VivaBev buys: {items.data ? `${items.data.filter((i) => i.isActive).length} active items` : 'items'} in{' '}
            {categories.data ? `${categories.data.filter((c) => c.isActive).length} categories` : 'categories'}
          </p>
        </div>
        {manage && (
          <Button size="pill" onClick={() => set('item', 'new')}>
            <HugeiconsIcon icon={PlusSignIcon} strokeWidth={1.5} /> New item
          </Button>
        )}
      </section>

      <section className="flex flex-wrap items-start gap-5">
        <CategoryList items={items.data ?? []} selected={categoryId} onSelect={(id) => set('category', id)} manage={manage} />

        <div className="flex min-w-0 flex-[3_1_560px] flex-col gap-4">
          <div className="flex flex-wrap items-center gap-2">
            <label className="relative min-w-0 flex-[1_1_240px]">
              <span className="sr-only">Search items</span>
              <HugeiconsIcon icon={Search01Icon} size={16} strokeWidth={1.5} className="absolute top-1/2 left-3.5 -translate-y-1/2 text-muted-foreground" />
              <input value={search} onChange={(e) => setSearch(e.target.value)} placeholder="Search by code or name"
                className={cn(fieldClass, 'pl-10')} />
            </label>
            <div role="group" aria-label="Item type" className="flex flex-wrap gap-1.5">
              {[null, ...itemTypes].map((t) => (
                <button key={t ?? 'all'} aria-pressed={type === t} onClick={() => set('type', t)}
                  className={cn('rounded-full border px-3 py-1.5 text-[13px]',
                    type === t ? 'border-[1.5px] border-foreground bg-surface font-semibold' : 'border-input hover:bg-accent')}>
                  {t ? itemTypeLabels[t] : 'All types'}
                </button>
              ))}
            </div>
          </div>

          {items.isPending && <TableSkeleton columns={columns} rows={6} />}
          {items.isError && <ErrorState error={items.error} what="the catalogue" onRetry={() => items.refetch()} />}
          {items.data?.length === 0 && (
            <EmptyState icon={PackageIcon} title="The catalogue is empty"
              action={manage && <Button size="pill" onClick={() => set('item', 'new')}>New item</Button>}>
              Catalogue items make requisitions faster and let the warehouse track stock.
            </EmptyState>
          )}
          {!!items.data?.length && rows.length === 0 && (
            <NoMatches summary="No items match these filters." onClear={() => { setSearch(''); setParams({}, { replace: true }) }} />
          )}

          {rows.length > 0 && (
            <Panel className="px-3.5 py-2.5">
              <div className="overflow-x-auto">
                <table className="w-full min-w-[640px] border-collapse text-sm">
                  <thead>
                    <tr className="text-left text-xs text-muted-foreground">
                      {columns.map((c) => <th key={c} scope="col" className={cn('p-3 font-semibold', c === 'Stock' && 'text-right')}>{c}</th>)}
                    </tr>
                  </thead>
                  <tbody>
                    {rows.map((i) => (
                      <tr key={i.id} onClick={() => set('item', i.id)} aria-selected={selected === i.id}
                        className={cn('cursor-pointer border-t hover:bg-accent', selected === i.id && 'bg-accent', !i.isActive && 'text-muted-foreground')}>
                        <td className="p-3">
                          <button className="text-left font-semibold hover:text-brand" onClick={(e) => { e.stopPropagation(); set('item', i.id) }}>{i.name}</button>
                          <div className="text-xs text-muted-foreground">{i.code}</div>
                        </td>
                        <td className="p-3">{itemTypeLabels[i.itemType as ItemType] ?? i.itemType}</td>
                        <td className="p-3">{i.category}</td>
                        <td className="p-3">{i.baseUomCode}</td>
                        <td className="p-3 text-right">
                          {!i.isStocked ? <span className="text-xs text-muted-foreground">Not stocked</span>
                            : stock.data ? <span className="font-semibold">{(onHand.get(i.id) ?? 0).toLocaleString('en-NG')}</span>
                            : <span className="text-xs text-muted-foreground">Stocked</span>}
                        </td>
                        <td className="p-3">
                          <span className={cn('rounded-full px-2.5 py-1 text-xs font-bold', i.isActive ? 'bg-success text-success-foreground' : 'bg-draft text-draft-foreground')}>
                            {i.isActive ? 'Active' : 'Inactive'}
                          </span>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </Panel>
          )}
        </div>

        {selected && (
          <ItemPanel key={selected} id={selected === 'new' ? null : selected} manage={manage}
            defaultCategoryId={categoryId} onClose={() => set('item', null)} onCreated={(id) => set('item', id)} />
        )}
      </section>
    </div>
  )
}
