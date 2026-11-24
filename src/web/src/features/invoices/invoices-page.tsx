import { useMemo, useState } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router'
import { HugeiconsIcon } from '@hugeicons/react'
import { Invoice03Icon, PlusSignIcon } from '@hugeicons/core-free-icons'
import { Panel } from '@/components/panel'
import { EmptyState } from '@/components/states/empty-state'
import { ErrorState } from '@/components/states/error-state'
import { TableSkeleton } from '@/components/states/table-skeleton'
import { Button } from '@/components/ui/button'
import { useCurrentUser } from '@/features/auth/use-auth'
import { formatDate, formatNaira } from '@/lib/format'
import { can, Permission } from '@/lib/permissions'
import { cn } from '@/lib/utils'
import { InvoiceStatusBadge } from './invoice-status-badge'
import { useInvoices, type InvoiceSummary } from './invoices-api'

// Tabs follow the work: what needs a decision, what's ready to pay, what's done
const tabs = [
  { key: 'hold', label: 'On hold', test: (i: InvoiceSummary) => i.status === 'OnHold' },
  { key: 'pay', label: 'Ready to pay', test: (i: InvoiceSummary) => i.status === 'Matched' || i.status === 'Approved' },
  { key: 'paid', label: 'Paid', test: (i: InvoiceSummary) => i.status === 'Paid' },
  { key: 'rejected', label: 'Rejected', test: (i: InvoiceSummary) => i.status === 'Rejected' },
  { key: 'all', label: 'All', test: () => true },
] as const
const columns = ['Invoice', 'Supplier', 'Order', 'Due', 'Total', 'Status']

export function InvoicesPage() {
  const { data: user } = useCurrentUser()
  const manage = can(user, Permission.InvoicesManage)
  const invoices = useInvoices()
  const navigate = useNavigate()
  const [params, setParams] = useSearchParams()
  const [today] = useState(() => new Date().toLocaleDateString('en-CA'))
  const tab = tabs.find((t) => t.key === params.get('tab')) ?? tabs[0]

  const rows = useMemo(() => (invoices.data ?? []).filter(tab.test), [invoices.data, tab])
  const readyTotal = (invoices.data ?? []).filter(tabs[1].test).reduce((s, i) => s + i.total, 0)

  return (
    <div className="flex flex-col gap-6">
      <section className="flex flex-wrap items-end justify-between gap-5">
        <div>
          <h1 className="text-[40px] leading-tight font-light tracking-tight">Invoices</h1>
          <p className="mt-2 text-[15px] text-muted-foreground">
            Supplier invoices, matched against the order and the goods received before they're paid
          </p>
        </div>
        <div className="flex items-end gap-6">
          {invoices.data && (
            <div className="text-right">
              <div className="text-[13px] text-muted-foreground">Ready to pay</div>
              <div className="font-heading text-[32px] leading-tight font-light">{formatNaira(readyTotal)}</div>
            </div>
          )}
          {manage && (
            <Button asChild size="pill">
              <Link to="/invoices/new"><HugeiconsIcon icon={PlusSignIcon} strokeWidth={1.5} /> Record invoice</Link>
            </Button>
          )}
        </div>
      </section>

      <div role="tablist" aria-label="Invoice status" className="flex flex-wrap gap-2">
        {tabs.map((t) => (
          <button key={t.key} role="tab" aria-selected={t.key === tab.key}
            onClick={() => setParams(t.key === 'hold' ? {} : { tab: t.key }, { replace: true })}
            className={cn('rounded-full border px-4 py-2 text-[13px]',
              t.key === tab.key ? 'border-[1.5px] border-foreground bg-surface font-semibold' : 'border-input hover:bg-accent')}>
            {t.label} · {invoices.data ? invoices.data.filter(t.test).length : '–'}
          </button>
        ))}
      </div>

      {invoices.isPending && <TableSkeleton columns={columns} rows={4} />}
      {invoices.isError && <ErrorState error={invoices.error} what="invoices" onRetry={() => invoices.refetch()} />}
      {invoices.data?.length === 0 && (
        <EmptyState icon={Invoice03Icon} title="No invoices yet"
          action={manage && <Button asChild size="pill"><Link to="/invoices/new">Record invoice</Link></Button>}>
          When a supplier invoices an issued order, record it here. It's checked against the order's prices and what the warehouse received.
        </EmptyState>
      )}
      {!!invoices.data?.length && rows.length === 0 && (
        <p className="text-sm text-muted-foreground">{tab.key === 'hold' ? 'Nothing on hold. Every invoice matched.' : `No invoices here.`}</p>
      )}

      {rows.length > 0 && (
        <Panel className="px-3.5 py-2.5">
          <div className="overflow-x-auto">
            <table className="w-full min-w-[760px] border-collapse text-sm">
              <thead>
                <tr className="text-left text-xs text-muted-foreground">
                  {columns.map((c) => <th key={c} scope="col" className={cn('p-3 font-semibold', c === 'Total' && 'text-right')}>{c}</th>)}
                </tr>
              </thead>
              <tbody>
                {rows.map((i) => {
                  const overdue = (i.status === 'Matched' || i.status === 'Approved') && i.dueDate < today
                  return (
                    <tr key={i.id} onClick={() => navigate(`/invoices/${i.id}`)} className={cn('cursor-pointer border-t hover:bg-accent', i.status === 'Rejected' && 'text-muted-foreground')}>
                      <td className="p-3">
                        <Link to={`/invoices/${i.id}`} onClick={(e) => e.stopPropagation()} className="font-semibold hover:text-brand">{i.supplierInvoiceNumber}</Link>
                        <div className="text-xs text-muted-foreground">{i.number} · dated {formatDate(i.invoiceDate)}</div>
                      </td>
                      <td className="p-3 font-semibold">{i.supplierName}</td>
                      <td className="p-3">{i.purchaseOrderNumber}</td>
                      <td className="p-3">
                        {formatDate(i.dueDate)}
                        {overdue && <span className="ml-2 rounded-full bg-over px-2 py-0.5 text-[11px] font-bold text-over-foreground">Overdue</span>}
                      </td>
                      <td className="p-3 text-right font-semibold">{formatNaira(i.total)}</td>
                      <td className="p-3">
                        <InvoiceStatusBadge status={i.status} />
                        {i.exceptions > 0 && i.status === 'OnHold' && (
                          <div className="mt-1 text-xs text-muted-foreground">{i.exceptions} line{i.exceptions === 1 ? '' : 's'} didn't match</div>
                        )}
                      </td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>
        </Panel>
      )}
    </div>
  )
}
