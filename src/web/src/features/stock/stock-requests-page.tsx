import { useMemo, useState } from 'react'
import { Link, useNavigate } from 'react-router'
import { HugeiconsIcon } from '@hugeicons/react'
import { PackageIcon, PlusSignIcon } from '@hugeicons/core-free-icons'
import { Panel } from '@/components/panel'
import { EmptyState } from '@/components/states/empty-state'
import { ErrorState } from '@/components/states/error-state'
import { TableSkeleton } from '@/components/states/table-skeleton'
import { Button } from '@/components/ui/button'
import { useCurrentUser } from '@/features/auth/use-auth'
import { formatDate, formatDateTime } from '@/lib/format'
import { can, Permission } from '@/lib/permissions'
import { cn } from '@/lib/utils'
import { StockRequestStatusBadge } from './stock-request-status-badge'
import { isOutstanding, useStockRequests, type StockRequestSummary } from './stock-api'

export function StockRequestsPage() {
  const { data: user } = useCurrentUser()
  return (
    <div className="flex flex-col gap-6">
      <section className="flex flex-wrap items-end justify-between gap-5">
        <div>
          <h1 className="text-[40px] leading-tight font-light tracking-tight">Stock requests</h1>
          <p className="mt-2 text-[15px] text-muted-foreground">Ask the warehouse for items it already holds. No purchase needed.</p>
        </div>
        {can(user, Permission.StockRequest) && (
          <Button asChild size="pill">
            <Link to="/stock-requests/new"><HugeiconsIcon icon={PlusSignIcon} strokeWidth={1.5} /> Request stock</Link>
          </Button>
        )}
      </section>
      <StockRequestList />
    </div>
  )
}

const tabs = [
  { key: 'open', label: 'Waiting', test: (r: StockRequestSummary) => isOutstanding(r.status) },
  { key: 'done', label: 'Done', test: (r: StockRequestSummary) => !isOutstanding(r.status) },
  { key: 'all', label: 'All', test: () => true },
] as const

/** The request list: your own, your department's, or (for the warehouse) everyone's, as the API allows. */
export function StockRequestList() {
  const { data: user } = useCurrentUser()
  const issuer = can(user, Permission.StockIssue)
  const requests = useStockRequests()
  const navigate = useNavigate()
  const [tabKey, setTabKey] = useState<(typeof tabs)[number]['key']>('open')
  const [today] = useState(() => new Date().toLocaleDateString('en-CA'))
  const tab = tabs.find((t) => t.key === tabKey)!
  const rows = useMemo(() => (requests.data ?? []).filter(tab.test), [requests.data, tab])
  const columns = ['Request', 'For', 'Requested', 'Needed by', 'Lines', 'Status', '']

  if (requests.isPending) return <TableSkeleton columns={columns} rows={4} />
  if (requests.isError) return <ErrorState error={requests.error} what="stock requests" onRetry={() => requests.refetch()} />
  if (requests.data.length === 0) {
    return (
      <EmptyState icon={PackageIcon} title="No stock requests yet"
        action={can(user, Permission.StockRequest) && <Button asChild size="pill"><Link to="/stock-requests/new">Request stock</Link></Button>}>
        {issuer ? 'When someone asks for stock the warehouse holds, it shows here to issue.' : 'Need toner, a spare laptop or PPE the warehouse already has? Ask for it here.'}
      </EmptyState>
    )
  }

  return (
    <div className="flex flex-col gap-3">
      <div role="tablist" aria-label="Request status" className="flex flex-wrap gap-2">
        {tabs.map((t) => (
          <button key={t.key} role="tab" aria-selected={t.key === tab.key} onClick={() => setTabKey(t.key)}
            className={cn('rounded-full border px-4 py-2 text-[13px]',
              t.key === tab.key ? 'border-[1.5px] border-foreground bg-surface font-semibold' : 'border-input hover:bg-accent')}>
            {t.label} · {requests.data.filter(t.test).length}
          </button>
        ))}
      </div>
      {rows.length === 0 ? (
        <p className="text-sm text-muted-foreground">{tab.key === 'open' ? 'Nothing waiting.' : 'Nothing here yet.'}</p>
      ) : (
        <Panel className="px-3.5 py-2.5">
          <div className="overflow-x-auto">
            <table className="w-full min-w-[760px] border-collapse text-sm">
              <thead>
                <tr className="text-left text-xs text-muted-foreground">
                  {columns.map((c, i) => <th key={i} scope="col" className="p-3 font-semibold">{c}</th>)}
                </tr>
              </thead>
              <tbody>
                {rows.map((r) => {
                  const late = isOutstanding(r.status) && r.neededBy !== null && r.neededBy < today
                  return (
                    <tr key={r.id} onClick={() => navigate(`/stock-requests/${r.id}`)} className="cursor-pointer border-t hover:bg-accent">
                      <td className="p-3">
                        <Link to={`/stock-requests/${r.id}`} onClick={(e) => e.stopPropagation()} className="font-semibold hover:text-brand">{r.number}</Link>
                        <div className="max-w-64 truncate text-xs text-muted-foreground">{r.purpose}</div>
                      </td>
                      <td className="p-3">
                        <span className="font-semibold">{r.requestedByName}</span>
                        <div className="text-xs text-muted-foreground">{r.departmentCode} · {r.costCentreCode} · from {r.siteCode}</div>
                      </td>
                      <td className="p-3">{formatDateTime(r.requestedAt)}</td>
                      <td className="p-3">
                        {r.neededBy ? formatDate(r.neededBy) : <span className="text-muted-foreground">–</span>}
                        {late && <span className="ml-2 rounded-full bg-over px-2 py-0.5 text-[11px] font-bold text-over-foreground">Late</span>}
                      </td>
                      <td className="p-3">{isOutstanding(r.status) ? `${r.linesOutstanding} of ${r.lineCount} to issue` : r.lineCount}</td>
                      <td className="p-3"><StockRequestStatusBadge status={r.status} /></td>
                      <td className="p-3 text-right">
                        {issuer && isOutstanding(r.status) && (
                          <Button asChild size="pill-sm" variant="brand"><Link to={`/stock-requests/${r.id}`} onClick={(e) => e.stopPropagation()}>Issue</Link></Button>
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
