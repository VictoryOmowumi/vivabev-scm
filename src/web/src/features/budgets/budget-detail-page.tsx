import { Link, useParams } from 'react-router'
import { HugeiconsIcon } from '@hugeicons/react'
import { ArrowLeft01Icon } from '@hugeicons/core-free-icons'
import { Panel } from '@/components/panel'
import { ErrorState } from '@/components/states/error-state'
import { Skeleton } from '@/components/ui/skeleton'
import { formatDateTime, formatNaira } from '@/lib/format'
import { cn } from '@/lib/utils'
import { BudgetBar } from './budget-bar'
import { freeOf, healthOf, useBudget } from './budgets-api'

const statusStyle = {
  Open: 'bg-pending text-pending-foreground',
  Consumed: 'bg-success text-success-foreground',
  Released: 'bg-draft text-draft-foreground',
}

export function BudgetDetailPage() {
  const { id } = useParams()
  const detail = useBudget(id)

  if (detail.isPending) {
    return (
      <div className="flex flex-col gap-6" role="status" aria-label="Loading budget">
        <Skeleton className="h-10 w-1/3 rounded-full" />
        <Skeleton className="h-40 rounded-[28px]" />
        <Skeleton className="h-64 rounded-[28px]" />
      </div>
    )
  }
  if (detail.isError) {
    return <ErrorState error={detail.error} what="this budget" onRetry={() => detail.refetch()}
      back={{ to: '/budgets', label: 'Back to budgets' }} />
  }

  const { budget: b, commitments } = detail.data
  const free = freeOf(b)
  const health = healthOf(b)

  return (
    <div className="flex flex-col gap-6">
      <section className="flex flex-col gap-3">
        <Link to={`/budgets?fy=${b.fiscalYear}`} className="flex items-center gap-1.5 self-start text-sm font-semibold hover:text-brand">
          <HugeiconsIcon icon={ArrowLeft01Icon} size={18} strokeWidth={1.5} /> Budgets
        </Link>
        <div className="flex flex-wrap items-end justify-between gap-5">
          <div>
            <p className="mb-1 text-[13px] font-semibold text-muted-foreground">{b.departmentCode} · FY{b.fiscalYear}</p>
            <h1 className="text-[40px] leading-tight font-light tracking-tight">{b.costCentreCode} · {b.costCentreName}</h1>
          </div>
          <div className="text-right">
            <div className="text-[13px] text-muted-foreground">{free < 0 ? 'Over by' : 'Free now'}</div>
            <div className={cn('font-heading text-[40px] leading-tight font-light', health === 'over' && 'text-destructive')}>
              {formatNaira(Math.abs(free))}
            </div>
          </div>
        </div>
      </section>

      <Panel className="flex flex-col gap-4 px-6 py-5">
        <dl className="grid grid-cols-2 gap-5 sm:grid-cols-5">
          {([['Budget', b.amount], ['Spent', b.consumed], ['Committed', b.committed], ['In approval', b.inApproval], ['Available', b.available]] as const)
            .map(([label, value]) => (
              <div key={label}>
                <dt className="text-xs text-muted-foreground">{label}</dt>
                <dd className="font-heading text-2xl font-light">{formatNaira(value)}</dd>
              </div>
            ))}
        </dl>
        <BudgetBar totals={b} showLegend />
        <p className="text-[13px] text-muted-foreground">
          Committed money is approved and reserved; it becomes spent when invoices are matched. In approval is
          submitted but not yet approved, so it isn't reserved yet. Free is what's left after both.
        </p>
      </Panel>

      <Panel className="px-6 py-5">
        <h2 className="mb-2.5 text-xl font-normal">Commitments</h2>
        {commitments.length === 0 ? (
          <p className="py-6 text-center text-sm text-muted-foreground">Nothing approved against this budget yet.</p>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full min-w-[640px] border-collapse text-sm">
              <thead>
                <tr className="text-left text-xs text-muted-foreground">
                  <th scope="col" className="p-2.5 font-semibold">Document</th>
                  <th scope="col" className="p-2.5 font-semibold">Status</th>
                  <th scope="col" className="p-2.5 font-semibold">Opened</th>
                  <th scope="col" className="p-2.5 font-semibold">Closed</th>
                  <th scope="col" className="p-2.5 text-right font-semibold">Amount</th>
                </tr>
              </thead>
              <tbody>
                {commitments.map((c) => (
                  <tr key={`${c.documentId}-${c.openedAt}`} className={cn('border-t', c.status === 'Released' && 'text-muted-foreground')}>
                    <td className="p-2.5">
                      {c.documentType === 'Requisition'
                        ? <Link to={`/requisitions/${c.documentId}`} className="font-semibold hover:text-brand">{c.documentNumber}</Link>
                        : <span className="font-semibold">{c.documentNumber}</span>}
                      <div className="text-xs text-muted-foreground">{c.documentType}</div>
                    </td>
                    <td className="p-2.5">
                      <span className={cn('rounded-full px-2.5 py-1 text-xs font-bold', statusStyle[c.status])}>{c.status}</span>
                    </td>
                    <td className="p-2.5">{formatDateTime(c.openedAt)}</td>
                    <td className="p-2.5">{c.closedAt ? formatDateTime(c.closedAt) : '–'}</td>
                    <td className="p-2.5 text-right font-semibold">{formatNaira(c.amount)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </Panel>
    </div>
  )
}
