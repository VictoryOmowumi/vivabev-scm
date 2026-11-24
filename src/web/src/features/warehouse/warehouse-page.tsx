import { useMemo, useState } from 'react'
import { FieldSelect } from '@/components/field-select'
import { Link, useNavigate, useSearchParams } from 'react-router'
import { DeliveryTruck01Icon, PackageIcon } from '@hugeicons/core-free-icons'
import { Panel } from '@/components/panel'
import { EmptyState } from '@/components/states/empty-state'
import { ErrorState } from '@/components/states/error-state'
import { TableSkeleton } from '@/components/states/table-skeleton'
import { Button } from '@/components/ui/button'
import { useCurrentUser } from '@/features/auth/use-auth'
import { OrderStatusBadge } from '@/features/purchase-orders/order-status-badge'
import { formatDate, formatDateTime, formatNaira } from '@/lib/format'
import { can, Permission } from '@/lib/permissions'
import { cn } from '@/lib/utils'
import { useDueIn, useReceipts, useStock } from './warehouse-api'

const tabs = [
  { key: 'due', label: 'Due in' },
  { key: 'receipts', label: 'Received' },
  { key: 'stock', label: 'Stock on hand' },
] as const
type Tab = (typeof tabs)[number]['key']

export function WarehousePage() {
  const [params, setParams] = useSearchParams()
  const tab = (params.get('tab') ?? 'due') as Tab
  const due = useDueIn()

  return (
    <div className="flex flex-col gap-6">
      <section>
        <h1 className="text-[40px] leading-tight font-light tracking-tight">Warehouse</h1>
        <p className="mt-2 text-[15px] text-muted-foreground">Receive deliveries against purchase orders and see what's in stock</p>
      </section>

      <div role="tablist" aria-label="Warehouse" className="flex flex-wrap gap-2">
        {tabs.map((t) => (
          <button key={t.key} role="tab" aria-selected={t.key === tab} onClick={() => setParams(t.key === 'due' ? {} : { tab: t.key }, { replace: true })}
            className={cn('rounded-full border px-4 py-2 text-[13px]',
              t.key === tab ? 'border-[1.5px] border-foreground bg-surface font-semibold' : 'border-input hover:bg-accent')}>
            {t.label}{t.key === 'due' && due.data ? ` · ${due.data.length}` : ''}
          </button>
        ))}
      </div>

      {tab === 'due' && <DueIn />}
      {tab === 'receipts' && <ReceiptList />}
      {tab === 'stock' && <Stock />}
    </div>
  )
}

function DueIn() {
  const { data: user } = useCurrentUser()
  const receive = can(user, Permission.InventoryReceive)
  const due = useDueIn()
  const navigate = useNavigate()
  const [today] = useState(() => new Date().toLocaleDateString('en-CA'))
  const columns = ['Order', 'Supplier', 'Deliver to', 'Expected', 'Lines to receive', 'Status', '']

  if (due.isPending) return <TableSkeleton columns={columns} rows={4} />
  if (due.isError) return <ErrorState error={due.error} what="deliveries due in" onRetry={() => due.refetch()} />
  if (due.data.length === 0) {
    return (
      <EmptyState icon={DeliveryTruck01Icon} title="Nothing due in">
        When Procurement issues a purchase order, it shows here until everything on it has been received.
      </EmptyState>
    )
  }

  return (
    <Panel className="px-3.5 py-2.5">
      <div className="overflow-x-auto">
        <table className="w-full min-w-[760px] border-collapse text-sm">
          <thead>
            <tr className="text-left text-xs text-muted-foreground">
              {columns.map((c, i) => <th key={i} scope="col" className="p-3 font-semibold">{c}</th>)}
            </tr>
          </thead>
          <tbody>
            {due.data.map((o) => {
              const late = o.expectedDelivery < today
              return (
                <tr key={o.id} onClick={() => navigate(`/warehouse/orders/${o.id}`)} className="cursor-pointer border-t hover:bg-accent">
                  <td className="p-3">
                    <Link to={`/warehouse/orders/${o.id}`} onClick={(e) => e.stopPropagation()} className="font-semibold hover:text-brand">{o.number}</Link>
                    <div className="text-xs text-muted-foreground">{formatNaira(o.total)}</div>
                  </td>
                  <td className="p-3 font-semibold">{o.supplierName}</td>
                  <td className="p-3">{o.siteCode} · {o.siteName}</td>
                  <td className="p-3">
                    {formatDate(o.expectedDelivery)}
                    {late && <span className="ml-2 rounded-full bg-over px-2 py-0.5 text-[11px] font-bold text-over-foreground">Late</span>}
                  </td>
                  <td className="p-3">{o.linesOutstanding} of {o.lineCount}</td>
                  <td className="p-3"><OrderStatusBadge status={o.status} /></td>
                  <td className="p-3 text-right">
                    {receive && <Button asChild size="pill-sm" variant="brand"><Link to={`/warehouse/orders/${o.id}`} onClick={(e) => e.stopPropagation()}>Receive</Link></Button>}
                  </td>
                </tr>
              )
            })}
          </tbody>
        </table>
      </div>
    </Panel>
  )
}

function ReceiptList() {
  const receipts = useReceipts()
  const navigate = useNavigate()
  const columns = ['Receipt', 'Order', 'Supplier', 'Received', 'Value', '']

  if (receipts.isPending) return <TableSkeleton columns={columns} rows={4} />
  if (receipts.isError) return <ErrorState error={receipts.error} what="receipts" onRetry={() => receipts.refetch()} />
  if (receipts.data.length === 0) {
    return <EmptyState icon={PackageIcon} title="Nothing received yet">Goods received notes appear here once deliveries are recorded.</EmptyState>
  }

  return (
    <Panel className="px-3.5 py-2.5">
      <div className="overflow-x-auto">
        <table className="w-full min-w-[720px] border-collapse text-sm">
          <thead>
            <tr className="text-left text-xs text-muted-foreground">
              {columns.map((c, i) => <th key={i} scope="col" className={cn('p-3 font-semibold', c === 'Value' && 'text-right')}>{c}</th>)}
            </tr>
          </thead>
          <tbody>
            {receipts.data.map((r) => (
              <tr key={r.id} onClick={() => navigate(`/warehouse/receipts/${r.id}`)} className="cursor-pointer border-t hover:bg-accent">
                <td className="p-3">
                  <Link to={`/warehouse/receipts/${r.id}`} onClick={(e) => e.stopPropagation()} className="font-semibold hover:text-brand">{r.number}</Link>
                  <div className="text-xs text-muted-foreground">{r.siteCode}{r.deliveryNoteNumber && ` · DN ${r.deliveryNoteNumber}`}</div>
                </td>
                <td className="p-3">{r.purchaseOrderNumber}</td>
                <td className="p-3 font-semibold">{r.supplierName}</td>
                <td className="p-3">{formatDateTime(r.receivedAt)}<div className="text-xs text-muted-foreground">{r.receivedByName}</div></td>
                <td className="p-3 text-right font-semibold">{formatNaira(r.acceptedValue)}</td>
                <td className="p-3">{r.hasRejections && <span className="rounded-full bg-over px-2.5 py-0.5 text-xs font-bold text-over-foreground">Some rejected</span>}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </Panel>
  )
}

function Stock() {
  const stock = useStock()
  const [site, setSite] = useState('')
  const sites = useMemo(() => [...new Map((stock.data ?? []).map((s) => [s.siteId, s.siteCode])).entries()], [stock.data])
  const rows = (stock.data ?? []).filter((s) => !site || s.siteId === site)
  const columns = ['Item', 'Site', 'On hand', 'Average cost', 'Value', 'Last movement']

  if (stock.isPending) return <TableSkeleton columns={columns} rows={4} />
  if (stock.isError) return <ErrorState error={stock.error} what="stock" onRetry={() => stock.refetch()} />
  if (stock.data.length === 0) {
    return <EmptyState icon={PackageIcon} title="No stock yet">Stocked catalogue items appear here once they're received.</EmptyState>
  }

  return (
    <div className="flex flex-col gap-3">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <FieldSelect aria-label="Site" size="sm" className="w-56" value={site || 'all'} onChange={(v) => setSite(v === 'all' ? '' : v)}
          options={[{ value: 'all', label: 'All sites' }, ...sites.map(([id, code]) => ({ value: id, label: code }))]} />
        <span className="text-sm text-muted-foreground">
          {rows.length} item{rows.length === 1 ? '' : 's'} · <strong className="text-foreground">{formatNaira(rows.reduce((s, r) => s + r.value, 0))}</strong> at cost
        </span>
      </div>
      <Panel className="px-3.5 py-2.5">
        <div className="overflow-x-auto">
          <table className="w-full min-w-[720px] border-collapse text-sm">
            <thead>
              <tr className="text-left text-xs text-muted-foreground">
                {columns.map((c) => <th key={c} scope="col" className={cn('p-3 font-semibold', ['On hand', 'Average cost', 'Value'].includes(c) && 'text-right')}>{c}</th>)}
              </tr>
            </thead>
            <tbody>
              {rows.map((s) => (
                <tr key={`${s.siteId}:${s.itemId}`} className="border-t">
                  <td className="p-3"><div className="font-semibold">{s.description}</div><div className="text-xs text-muted-foreground">{s.itemCode}</div></td>
                  <td className="p-3">{s.siteCode}</td>
                  <td className="p-3 text-right font-semibold">{s.onHand.toLocaleString('en-NG')} {s.uomCode}</td>
                  <td className="p-3 text-right">{formatNaira(s.averageCost)}</td>
                  <td className="p-3 text-right font-semibold">{formatNaira(s.value)}</td>
                  <td className="p-3">{formatDateTime(s.lastMovementAt)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </Panel>
    </div>
  )
}
