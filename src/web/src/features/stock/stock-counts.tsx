import { useState } from 'react'
import { Link, useNavigate } from 'react-router'
import { toast } from 'sonner'
import { ClipboardIcon } from '@hugeicons/core-free-icons'
import { FieldSelect } from '@/components/field-select'
import { Panel } from '@/components/panel'
import { EmptyState } from '@/components/states/empty-state'
import { ErrorState } from '@/components/states/error-state'
import { TableSkeleton } from '@/components/states/table-skeleton'
import { Button } from '@/components/ui/button'
import { useCurrentUser } from '@/features/auth/use-auth'
import { useStock } from '@/features/warehouse/warehouse-api'
import { problemMessage } from '@/lib/api-client'
import { formatDateTime, formatNaira } from '@/lib/format'
import { can, Permission } from '@/lib/permissions'
import { cn } from '@/lib/utils'
import { useStartCount, useStockCounts, type StockCountStatus } from './stock-api'

const statusStyles: Record<StockCountStatus, string> = {
  Open: 'bg-pending text-pending-foreground',
  Posted: 'bg-success text-success-foreground',
  Cancelled: 'bg-cancelled text-cancelled-foreground',
}
export const CountStatusBadge = ({ status }: { status: StockCountStatus }) => (
  <span className={cn('inline-flex rounded-full px-2.5 py-1 text-xs font-bold', statusStyles[status])}>{status === 'Open' ? 'Counting' : status}</span>
)

/** The Counts tab on the warehouse page: past counts, and starting a new one. */
export function StockCounts() {
  const { data: user } = useCurrentUser()
  const counter = can(user, Permission.StockCount)
  const counts = useStockCounts()
  const stock = useStock(counter)
  const start = useStartCount()
  const navigate = useNavigate()
  const [site, setSite] = useState('')
  const sites = [...new Map((stock.data ?? []).map((s) => [s.siteId, s.siteCode])).entries()]
  const columns = ['Count', 'Site', 'Started', 'Progress', 'Difference at cost', 'Status']

  const begin = () => start.mutate(site || sites[0]![0], {
    onSuccess: (c) => { toast(`${c.number} started`); navigate(`/warehouse/counts/${c.id}`) },
  })

  return (
    <div className="flex flex-col gap-3">
      {counter && sites.length > 0 && (
        <Panel className="flex flex-wrap items-center gap-3 px-6 py-4">
          <div className="min-w-0 flex-1">
            <h2 className="text-base font-semibold">Start a stock count</h2>
            <p className="text-sm text-muted-foreground">Lists every item the site has held. Posting fixes the system's figures to what you counted.</p>
          </div>
          {sites.length > 1 && (
            <FieldSelect aria-label="Site to count" size="sm" className="w-48" value={site || sites[0]![0]} onChange={setSite}
              options={sites.map(([id, code]) => ({ value: id, label: code }))} />
          )}
          <Button size="pill-sm" variant="brand" disabled={start.isPending} onClick={begin}>{start.isPending ? 'Starting…' : 'Start count'}</Button>
          {start.error && <p role="alert" className="basis-full text-[13px] text-destructive">{problemMessage(start.error)}</p>}
        </Panel>
      )}

      {counts.isPending && <TableSkeleton columns={columns} rows={3} />}
      {counts.isError && <ErrorState error={counts.error} what="stock counts" onRetry={() => counts.refetch()} />}
      {counts.data?.length === 0 && (
        <EmptyState icon={ClipboardIcon} title="No counts yet">Count the shelves now and then; differences are written to the stock ledger with a note.</EmptyState>
      )}
      {!!counts.data?.length && (
        <Panel className="px-3.5 py-2.5">
          <div className="overflow-x-auto">
            <table className="w-full min-w-[720px] border-collapse text-sm">
              <thead>
                <tr className="text-left text-xs text-muted-foreground">
                  {columns.map((c) => <th key={c} scope="col" className={cn('p-3 font-semibold', c.startsWith('Difference') && 'text-right')}>{c}</th>)}
                </tr>
              </thead>
              <tbody>
                {counts.data.map((c) => (
                  <tr key={c.id} onClick={() => navigate(`/warehouse/counts/${c.id}`)} className="cursor-pointer border-t hover:bg-accent">
                    <td className="p-3"><Link to={`/warehouse/counts/${c.id}`} onClick={(e) => e.stopPropagation()} className="font-semibold hover:text-brand">{c.number}</Link></td>
                    <td className="p-3">{c.siteCode}</td>
                    <td className="p-3">{formatDateTime(c.startedAt)}<div className="text-xs text-muted-foreground">{c.startedByName}</div></td>
                    <td className="p-3">
                      {c.status === 'Open' ? `${c.linesCounted} of ${c.lineCount} counted` : `${c.lineCount} items`}
                      {c.status === 'Posted' && c.linesDifferent > 0 && <div className="text-xs text-muted-foreground">{c.linesDifferent} differed</div>}
                    </td>
                    <td className={cn('p-3 text-right font-semibold', c.varianceValue < 0 && 'text-destructive')}>
                      {c.status === 'Posted' ? formatNaira(c.varianceValue) : '–'}
                    </td>
                    <td className="p-3"><CountStatusBadge status={c.status} /></td>
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
