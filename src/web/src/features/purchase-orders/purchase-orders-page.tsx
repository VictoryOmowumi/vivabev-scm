import { useMemo } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router'
import { PackageIcon } from '@hugeicons/core-free-icons'
import { Panel } from '@/components/panel'
import { EmptyState } from '@/components/states/empty-state'
import { ErrorState } from '@/components/states/error-state'
import { TableSkeleton } from '@/components/states/table-skeleton'
import { Button } from '@/components/ui/button'
import { useCurrentUser } from '@/features/auth/use-auth'
import { useSourcingEvents } from '@/features/sourcing/sourcing-api'
import { formatDate, formatNaira } from '@/lib/format'
import { can, Permission } from '@/lib/permissions'
import { cn } from '@/lib/utils'
import { OrderStatusBadge } from './order-status-badge'
import { orderStatusLabels, usePurchaseOrders, type PurchaseOrderStatus } from './purchase-orders-api'

const tabs: (PurchaseOrderStatus | 'all')[] = ['all', 'Draft', 'Issued', 'Cancelled']
const columns = ['Order', 'Supplier', 'From', 'Expected delivery', 'Total', 'Status']

export function PurchaseOrdersPage() {
  const { data: user } = useCurrentUser()
  const manage = can(user, Permission.PurchaseOrdersManage)
  const navigate = useNavigate()
  const [params, setParams] = useSearchParams()
  const tab = (params.get('status') ?? 'all') as PurchaseOrderStatus | 'all'
  const orders = usePurchaseOrders()
  const rfqs = useSourcingEvents(manage)

  const rows = useMemo(() => (orders.data ?? []).filter((o) => tab === 'all' || o.status === tab), [orders.data, tab])
  const countOf = (t: PurchaseOrderStatus | 'all') => (orders.data ?? []).filter((o) => t === 'all' || o.status === t).length

  // Awarded RFQs with no live order yet
  const ordered = new Set((orders.data ?? []).filter((o) => o.status !== 'Cancelled').map((o) => o.sourcingEventId))
  const toOrder = manage && orders.data ? (rfqs.data ?? []).filter((e) => e.status === 'Awarded' && !ordered.has(e.id)) : []

  return (
    <div className="flex flex-col gap-6">
      <section>
        <h1 className="text-[40px] leading-tight font-light tracking-tight">Purchase orders</h1>
        <p className="mt-2 text-[15px] text-muted-foreground">Raised from approved awards and sent to suppliers</p>
      </section>

      {toOrder.length > 0 && (
        <Panel className="flex flex-col gap-2 rounded-[22px] px-5 py-4">
          <strong className="text-sm">{toOrder.length} awarded RFQ{toOrder.length === 1 ? '' : 's'} waiting for an order</strong>
          <ul className="flex flex-wrap gap-2">
            {toOrder.map((e) => (
              <li key={e.id}>
                <Button asChild size="pill-sm" variant="outline">
                  <Link to={`/sourcing/${e.id}`}>{e.number} · {e.title}</Link>
                </Button>
              </li>
            ))}
          </ul>
        </Panel>
      )}

      <div role="tablist" aria-label="Order status" className="flex flex-wrap gap-2">
        {tabs.map((t) => (
          <button key={t} role="tab" aria-selected={t === tab} onClick={() => setParams(t === 'all' ? {} : { status: t }, { replace: true })}
            className={cn('rounded-full border px-4 py-2 text-[13px]',
              t === tab ? 'border-[1.5px] border-foreground bg-surface font-semibold' : 'border-input hover:bg-accent')}>
            {t === 'all' ? 'All' : orderStatusLabels[t]} · {orders.data ? countOf(t) : '–'}
          </button>
        ))}
      </div>

      {orders.isPending && <TableSkeleton columns={columns} rows={4} />}
      {orders.isError && <ErrorState error={orders.error} what="purchase orders" onRetry={() => orders.refetch()} />}
      {orders.data?.length === 0 && (
        <EmptyState icon={PackageIcon} title="No purchase orders yet">
          When an RFQ's award is approved, Procurement raises the order from it and issues it to the supplier.
        </EmptyState>
      )}
      {!!orders.data?.length && rows.length === 0 && (
        <p className="text-sm text-muted-foreground">No orders are {orderStatusLabels[tab as PurchaseOrderStatus].toLowerCase()}.</p>
      )}

      {rows.length > 0 && (
        <Panel className="px-3.5 py-2.5">
          <div className="overflow-x-auto">
            <table className="w-full min-w-[820px] border-collapse text-sm">
              <thead>
                <tr className="text-left text-xs text-muted-foreground">
                  {columns.map((c) => <th key={c} scope="col" className={cn('p-3 font-semibold', c === 'Total' && 'text-right')}>{c}</th>)}
                </tr>
              </thead>
              <tbody>
                {rows.map((o) => (
                  <tr key={o.id} onClick={() => navigate(`/purchase-orders/${o.id}`)}
                    className={cn('cursor-pointer border-t hover:bg-accent', o.status === 'Cancelled' && 'text-muted-foreground')}>
                    <td className="p-3">
                      <Link to={`/purchase-orders/${o.id}`} onClick={(ev) => ev.stopPropagation()} className="font-semibold hover:text-brand">{o.number}</Link>
                      <div className="text-xs text-muted-foreground">{o.lineCount} line{o.lineCount === 1 ? '' : 's'}{o.issuedAt && ` · issued ${formatDate(o.issuedAt.slice(0, 10))}`}</div>
                    </td>
                    <td className="p-3 font-semibold">{o.supplierName}</td>
                    <td className="p-3 text-xs">{o.sourcingEventNumber} · {o.requisitionNumbers.join(', ')}</td>
                    <td className="p-3">{formatDate(o.expectedDelivery)}</td>
                    <td className="p-3 text-right font-semibold">{formatNaira(o.total)}</td>
                    <td className="p-3"><OrderStatusBadge status={o.status} /></td>
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
