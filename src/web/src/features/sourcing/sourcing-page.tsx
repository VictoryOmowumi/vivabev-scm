import { useMemo } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router'
import { HugeiconsIcon } from '@hugeicons/react'
import { PlusSignIcon, ShoppingBasket01Icon } from '@hugeicons/core-free-icons'
import { Panel } from '@/components/panel'
import { EmptyState } from '@/components/states/empty-state'
import { ErrorState } from '@/components/states/error-state'
import { TableSkeleton } from '@/components/states/table-skeleton'
import { Button } from '@/components/ui/button'
import { useCurrentUser } from '@/features/auth/use-auth'
import { formatDate, formatNaira } from '@/lib/format'
import { can, Permission } from '@/lib/permissions'
import { cn } from '@/lib/utils'
import { EventStatusBadge } from './event-status-badge'
import { eventStatusLabels, useSourceableRequisitions, useSourcingEvents, type SourcingEventStatus } from './sourcing-api'

const tabs: (SourcingEventStatus | 'all')[] = ['all', 'Draft', 'Published', 'ResponsesClosed', 'AwardPending', 'Awarded', 'Cancelled']
const columns = ['RFQ', 'Requisitions', 'Deadline', 'Suppliers', 'Estimated', 'Lowest quote', 'Status']

export function SourcingPage() {
  const { data: user } = useCurrentUser()
  const manage = can(user, Permission.SourcingManage)
  const navigate = useNavigate()
  const [params, setParams] = useSearchParams()
  const tab = (params.get('status') ?? 'all') as SourcingEventStatus | 'all'
  const events = useSourcingEvents()
  const waiting = useSourceableRequisitions(manage)

  const rows = useMemo(() => (events.data ?? []).filter((e) => tab === 'all' || e.status === tab), [events.data, tab])
  const countOf = (t: SourcingEventStatus | 'all') => (events.data ?? []).filter((e) => t === 'all' || e.status === t).length

  return (
    <div className="flex flex-col gap-6">
      <section className="flex flex-wrap items-end justify-between gap-5">
        <div>
          <h1 className="text-[40px] leading-tight font-light tracking-tight">Sourcing</h1>
          <p className="mt-2 text-[15px] text-muted-foreground">Requests for quotation built from approved requisitions</p>
        </div>
        {manage && (
          <Button asChild size="pill">
            <Link to="/sourcing/new"><HugeiconsIcon icon={PlusSignIcon} strokeWidth={1.5} /> New RFQ</Link>
          </Button>
        )}
      </section>

      {manage && !!waiting.data?.length && (
        <Panel className="flex flex-wrap items-center justify-between gap-3 rounded-[22px] px-5 py-4">
          <span className="text-sm">
            <strong>{waiting.data.length} approved requisition{waiting.data.length === 1 ? '' : 's'} waiting for sourcing</strong>
            <span className="text-muted-foreground"> · {formatNaira(waiting.data.reduce((s, r) => s + r.estimatedTotal, 0))} estimated</span>
          </span>
          <Button asChild size="pill-sm" variant="outline"><Link to="/sourcing/new">Start an RFQ</Link></Button>
        </Panel>
      )}

      <div role="tablist" aria-label="RFQ status" className="flex flex-wrap gap-2">
        {tabs.map((t) => (
          <button key={t} role="tab" aria-selected={t === tab} onClick={() => setParams(t === 'all' ? {} : { status: t }, { replace: true })}
            className={cn('rounded-full border px-4 py-2 text-[13px]',
              t === tab ? 'border-[1.5px] border-foreground bg-surface font-semibold' : 'border-input hover:bg-accent')}>
            {t === 'all' ? 'All' : eventStatusLabels[t]} · {events.data ? countOf(t) : '–'}
          </button>
        ))}
      </div>

      {events.isPending && <TableSkeleton columns={columns} rows={4} />}
      {events.isError && <ErrorState error={events.error} what="RFQs" onRetry={() => events.refetch()} />}
      {events.data?.length === 0 && (
        <EmptyState icon={ShoppingBasket01Icon} title="No RFQs yet"
          action={manage && <Button asChild size="pill"><Link to="/sourcing/new">New RFQ</Link></Button>}>
          When requisitions are approved, Procurement groups them into an RFQ and invites approved suppliers to quote.
        </EmptyState>
      )}
      {!!events.data?.length && rows.length === 0 && (
        <p className="text-sm text-muted-foreground">No RFQs are {eventStatusLabels[tab as SourcingEventStatus].toLowerCase()}.</p>
      )}

      {rows.length > 0 && (
        <Panel className="px-3.5 py-2.5">
          <div className="overflow-x-auto">
            <table className="w-full min-w-[900px] border-collapse text-sm">
              <thead>
                <tr className="text-left text-xs text-muted-foreground">
                  {columns.map((c) => (
                    <th key={c} scope="col" className={cn('p-3 font-semibold', (c === 'Estimated' || c === 'Lowest quote') && 'text-right')}>{c}</th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {rows.map((e) => {
                  const saving = e.lowestQuote !== null ? e.estimatedTotal - e.lowestQuote : null
                  return (
                    <tr key={e.id} onClick={() => navigate(`/sourcing/${e.id}`)}
                      className={cn('cursor-pointer border-t hover:bg-accent', e.status === 'Cancelled' && 'text-muted-foreground')}>
                      <td className="p-3">
                        <Link to={`/sourcing/${e.id}`} onClick={(ev) => ev.stopPropagation()} className="font-semibold hover:text-brand">{e.title}</Link>
                        <div className="text-xs text-muted-foreground">{e.number} · {e.buyerName}</div>
                      </td>
                      <td className="p-3 text-xs">{e.requisitionNumbers.join(', ')}</td>
                      <td className="p-3">{formatDate(e.responseDeadline)}</td>
                      <td className="p-3">{e.quoted} of {e.invited} quoted</td>
                      <td className="p-3 text-right">{formatNaira(e.estimatedTotal)}</td>
                      <td className="p-3 text-right">
                        {e.lowestQuote === null ? '–' : (
                          <>
                            <span className="font-semibold">{formatNaira(e.lowestQuote)}</span>
                            {saving !== null && saving !== 0 && (
                              <div className={cn('text-xs', saving > 0 ? 'text-success-foreground' : 'text-destructive')}>
                                {saving > 0 ? `${formatNaira(saving)} under estimate` : `${formatNaira(-saving)} over estimate`}
                              </div>
                            )}
                          </>
                        )}
                      </td>
                      <td className="p-3"><EventStatusBadge status={e.status} /></td>
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
