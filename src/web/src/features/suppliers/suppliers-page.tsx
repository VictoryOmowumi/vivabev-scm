import { useMemo, useState } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router'
import { HugeiconsIcon } from '@hugeicons/react'
import { PlusSignIcon, Search01Icon, UserGroupIcon } from '@hugeicons/core-free-icons'
import { Panel } from '@/components/panel'
import { EmptyState } from '@/components/states/empty-state'
import { ErrorState } from '@/components/states/error-state'
import { TableSkeleton } from '@/components/states/table-skeleton'
import { Button } from '@/components/ui/button'
import { useCurrentUser } from '@/features/auth/use-auth'
import { fieldClass } from '@/lib/field-class'
import { can, Permission } from '@/lib/permissions'
import { cn } from '@/lib/utils'
import { NewSupplier } from './new-supplier'
import { SupplierStatusBadge } from './supplier-status-badge'
import { supplierStatusLabels, useSuppliers, type SupplierStatus } from './suppliers-api'

const tabs: (SupplierStatus | 'all')[] = ['all', 'Approved', 'PendingApproval', 'Prospect', 'Suspended', 'Blocked']
const columns = ['Supplier', 'Status', 'Approved categories', 'Country']

export function SuppliersPage() {
  const { data: user } = useCurrentUser()
  const manage = can(user, Permission.SuppliersManage)
  const approver = can(user, Permission.SuppliersApprove)
  const navigate = useNavigate()
  const suppliers = useSuppliers()
  const [params, setParams] = useSearchParams()
  const [search, setSearch] = useState('')
  const [adding, setAdding] = useState(false)
  const tab = (params.get('status') ?? 'all') as SupplierStatus | 'all'

  const term = search.trim().toLowerCase()
  const rows = useMemo(() => (suppliers.data ?? []).filter((s) =>
    (tab === 'all' || s.status === tab) &&
    (!term || s.code.toLowerCase().includes(term) || s.legalName.toLowerCase().includes(term))), [suppliers.data, tab, term])
  const countOf = (t: SupplierStatus | 'all') => (suppliers.data ?? []).filter((s) => t === 'all' || s.status === t).length
  const pending = countOf('PendingApproval')

  return (
    <div className="flex flex-col gap-6">
      <section className="flex flex-wrap items-end justify-between gap-5">
        <div>
          <h1 className="text-[40px] leading-tight font-light tracking-tight">Suppliers</h1>
          <p className="mt-2 text-[15px] text-muted-foreground">Who VivaBev buys from, and which categories each is approved for</p>
        </div>
        {manage && !adding && (
          <Button size="pill" onClick={() => setAdding(true)}><HugeiconsIcon icon={PlusSignIcon} strokeWidth={1.5} /> New supplier</Button>
        )}
      </section>

      {adding && <NewSupplier existing={suppliers.data ?? []} onClose={() => setAdding(false)} />}

      {approver && pending > 0 && tab !== 'PendingApproval' && (
        <Panel className="flex flex-wrap items-center justify-between gap-3 rounded-[22px] px-5 py-4">
          <strong className="text-sm">{pending} supplier{pending === 1 ? '' : 's'} waiting for your approval</strong>
          <Button size="pill-sm" variant="outline" onClick={() => setParams({ status: 'PendingApproval' }, { replace: true })}>Review</Button>
        </Panel>
      )}

      <div className="flex flex-wrap items-center gap-2">
        <div role="tablist" aria-label="Supplier status" className="flex flex-wrap gap-2">
          {tabs.map((t) => (
            <button key={t} role="tab" aria-selected={t === tab} onClick={() => setParams(t === 'all' ? {} : { status: t }, { replace: true })}
              className={cn('rounded-full border px-4 py-2 text-[13px]',
                t === tab ? 'border-[1.5px] border-foreground bg-surface font-semibold' : 'border-input hover:bg-accent')}>
              {t === 'all' ? 'All' : supplierStatusLabels[t]} · {suppliers.data ? countOf(t) : '–'}
            </button>
          ))}
        </div>
        <label className="relative ml-auto min-w-0 flex-[0_1_280px]">
          <span className="sr-only">Search suppliers</span>
          <HugeiconsIcon icon={Search01Icon} size={16} strokeWidth={1.5} className="absolute top-1/2 left-3.5 -translate-y-1/2 text-muted-foreground" />
          <input value={search} onChange={(e) => setSearch(e.target.value)} placeholder="Search by code or name" className={cn(fieldClass, 'pl-10')} />
        </label>
      </div>

      {suppliers.isPending && <TableSkeleton columns={columns} rows={6} />}
      {suppliers.isError && <ErrorState error={suppliers.error} what="suppliers" onRetry={() => suppliers.refetch()} />}
      {suppliers.data?.length === 0 && (
        <EmptyState icon={UserGroupIcon} title="No suppliers yet"
          action={manage && <Button size="pill" onClick={() => setAdding(true)}>New supplier</Button>}>
          Add a supplier, request the categories they supply, and send them for approval before inviting them to RFQs.
        </EmptyState>
      )}
      {!!suppliers.data?.length && rows.length === 0 && <p className="text-sm text-muted-foreground">No suppliers match.</p>}

      {rows.length > 0 && (
        <Panel className="px-3.5 py-2.5">
          <div className="overflow-x-auto">
            <table className="w-full min-w-[600px] border-collapse text-sm">
              <thead>
                <tr className="text-left text-xs text-muted-foreground">
                  {columns.map((c) => <th key={c} scope="col" className="p-3 font-semibold">{c}</th>)}
                </tr>
              </thead>
              <tbody>
                {rows.map((s) => (
                  <tr key={s.id} onClick={() => navigate(`/suppliers/${s.id}`)}
                    className={cn('cursor-pointer border-t hover:bg-accent', (s.status === 'Blocked' || s.status === 'Suspended') && 'text-muted-foreground')}>
                    <td className="p-3">
                      <Link to={`/suppliers/${s.id}`} onClick={(e) => e.stopPropagation()} className="font-semibold hover:text-brand">{s.legalName}</Link>
                      <div className="text-xs text-muted-foreground">{s.code}</div>
                    </td>
                    <td className="p-3"><SupplierStatusBadge status={s.status} /></td>
                    <td className="p-3">{s.approvedCategories}</td>
                    <td className="p-3">{s.countryCode}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </Panel>
      )}
    </div>
  )
}
