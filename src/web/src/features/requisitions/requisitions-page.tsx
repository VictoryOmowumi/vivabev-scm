import { useMemo } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router'
import { HugeiconsIcon } from '@hugeicons/react'
import { FileAddIcon, PlusSignIcon, Search01Icon } from '@hugeicons/core-free-icons'
import { Panel } from '@/components/panel'
import { EmptyState, NoMatches } from '@/components/states/empty-state'
import { ErrorState } from '@/components/states/error-state'
import { TableSkeleton } from '@/components/states/table-skeleton'
import { Button } from '@/components/ui/button'
import { useCurrentUser } from '@/features/auth/use-auth'
import { fiscalYearOf, formatDate, formatNaira } from '@/lib/format'
import { can, Permission } from '@/lib/permissions'
import { cn } from '@/lib/utils'
import {
  statusLabels,
  useRequisitions,
  type RequisitionScope,
  type RequisitionStatus,
} from './requisitions-api'
import { BudgetBadge, StatusBadge } from './status-badges'

const statuses: RequisitionStatus[] = ['Draft', 'PendingApproval', 'Approved', 'Cancelled']
const columns = ['Requisition', 'Requester', 'Cost centre', 'Status', 'Budget', 'Estimated total', 'Required by']

export function RequisitionsPage() {
  const { data: user } = useCurrentUser()
  const navigate = useNavigate()

  // Which tabs this user gets, widest last
  const scopes: { value: RequisitionScope; label: string }[] = [
    { value: 'mine', label: 'Mine' },
    ...(can(user, Permission.RequisitionsViewDepartment) || can(user, Permission.RequisitionsViewAll)
      ? [{ value: 'department' as const, label: 'My department' }] : []),
    ...(can(user, Permission.RequisitionsViewAll) ? [{ value: 'all' as const, label: 'All' }] : []),
  ]

  // Filters live in the URL, so a refresh or a shared link shows the same list
  const [params, setParams] = useSearchParams()
  const scopeParam = params.get('scope') as RequisitionScope | null
  const scope = scopes.some((s) => s.value === scopeParam) ? scopeParam! : scopes[scopes.length - 1]!.value
  const status = (params.get('status') ?? 'all') as RequisitionStatus | 'all'
  const search = params.get('q') ?? ''

  const setParam = (key: string, value: string | null) =>
    setParams((current) => {
      const next = new URLSearchParams(current)
      if (value) next.set(key, value)
      else next.delete(key)
      return next
    }, { replace: true })

  const list = useRequisitions(scope)

  const rows = useMemo(() => {
    const term = search.trim().toLowerCase()
    return (list.data ?? []).filter((r) =>
      (status === 'all' || r.status === status) &&
      (!term || r.number.toLowerCase().includes(term) || r.title.toLowerCase().includes(term)))
  }, [list.data, status, search])

  const countOf = (s: RequisitionStatus | 'all') =>
    (list.data ?? []).filter((r) => s === 'all' || r.status === s).length
  const total = rows.filter((r) => r.status !== 'Cancelled').reduce((sum, r) => sum + r.estimatedTotal, 0)
  const fy = fiscalYearOf()

  return (
    <div className="flex flex-col gap-6">
      <section className="flex flex-wrap items-end justify-between gap-5">
        <div>
          <h1 className="text-[40px] leading-tight font-light tracking-tight">Requisitions</h1>
          <p className="mt-2 text-[15px] text-muted-foreground">Requests for goods and services</p>
        </div>
        {can(user, Permission.RequisitionsCreate) && (
          <Button asChild size="pill">
            <Link to="/requisitions/new"><HugeiconsIcon icon={PlusSignIcon} strokeWidth={1.5} /> New requisition</Link>
          </Button>
        )}
      </section>

      <section className="flex flex-wrap items-center justify-between gap-3.5">
        <div role="tablist" aria-label="Whose requisitions"
          className="flex gap-1 rounded-full border border-white/90 bg-card p-1.5 dark:border-white/10">
          {scopes.map((s) => (
            <button key={s.value} role="tab" aria-selected={s.value === scope}
              onClick={() => setParam('scope', s.value)}
              className={cn('rounded-full px-4 py-2 text-sm font-medium',
                s.value === scope ? 'bg-inverse font-semibold text-inverse-foreground' : 'hover:bg-accent')}>
              {s.label}
            </button>
          ))}
        </div>

        <div className="flex flex-wrap gap-2" role="group" aria-label="Filter by status">
          {(['all', ...statuses] as const).map((s) => (
            <button key={s} aria-pressed={s === status} onClick={() => setParam('status', s === 'all' ? null : s)}
              className={cn('rounded-full border px-4 py-2 text-[13px]',
                s === status ? 'border-[1.5px] border-foreground bg-surface font-semibold' : 'border-input hover:bg-accent')}>
              {s === 'all' ? 'All' : statusLabels[s]} · {list.data ? countOf(s) : '–'}
            </button>
          ))}
        </div>

        <label className="flex h-11 min-w-56 flex-[0_1_300px] items-center gap-2.5 rounded-full border bg-surface px-4 text-muted-foreground">
          <HugeiconsIcon icon={Search01Icon} size={18} strokeWidth={1.5} />
          <span className="sr-only">Search requisitions</span>
          <input type="search" value={search} onChange={(e) => setParam('q', e.target.value || null)}
            placeholder="Search by number or title"
            className="min-w-0 flex-1 bg-transparent text-sm text-foreground outline-none placeholder:text-muted-foreground" />
        </label>
      </section>

      {list.isPending && <TableSkeleton columns={columns} />}

      {list.isError && <ErrorState error={list.error} what="requisitions" onRetry={() => list.refetch()} />}

      {list.data?.length === 0 && (
        <EmptyState icon={FileAddIcon} title="No requisitions yet"
          action={can(user, Permission.RequisitionsCreate) && (
            <Button asChild size="pill"><Link to="/requisitions/new">New requisition</Link></Button>
          )}>
          When you need goods or a service for your team, raise a requisition. It's checked against your budget and
          sent for approval.
        </EmptyState>
      )}

      {!!list.data?.length && rows.length === 0 && (
        <NoMatches
          summary={<><strong>No matches{search && ` for "${search}"`}</strong>{' '}
            <span className="text-muted-foreground">in {status === 'all' ? 'any status' : statusLabels[status]}</span></>}
          onClear={() => setParams(scopeParam ? { scope: scopeParam } : {}, { replace: true })} />
      )}

      {rows.length > 0 && (
        <Panel className="px-3.5 py-2.5">
          <div className="overflow-x-auto">
            <table className="w-full min-w-[900px] border-collapse text-sm">
              <thead>
                <tr className="text-left text-xs text-muted-foreground">
                  {columns.map((c) => (
                    <th key={c} scope="col" className={cn('p-3 font-semibold', c === 'Estimated total' && 'text-right')}>{c}</th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {rows.map((r) => {
                  const muted = r.status === 'Cancelled'
                  return (
                    <tr key={r.id} onClick={() => navigate(`/requisitions/${r.id}`)}
                      className={cn('cursor-pointer border-t hover:bg-accent', muted && 'text-muted-foreground')}>
                      <td className="p-3">
                        {/* The link is the real target for keyboards and screen readers; the row click is a shortcut */}
                        <Link to={`/requisitions/${r.id}`} onClick={(e) => e.stopPropagation()}
                          className="font-semibold hover:text-brand">{r.title}</Link>
                        <div className="text-xs text-muted-foreground">{r.number}</div>
                      </td>
                      <td className="p-3">{r.requesterName}</td>
                      <td className="p-3">{r.costCentreCode}</td>
                      <td className="p-3"><StatusBadge status={r.status} returned={r.returned} /></td>
                      <td className="p-3">
                        {muted ? '–' : <BudgetBadge status={r.budgetStatus} />}
                      </td>
                      <td className="p-3 text-right font-semibold">{formatNaira(r.estimatedTotal)}</td>
                      <td className="p-3">{formatDate(r.requiredBy)}</td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>
          <div className="flex flex-wrap justify-between gap-2.5 px-3 pt-3.5 pb-2 text-[13px] text-muted-foreground">
            <span>{rows.length} requisition{rows.length === 1 ? '' : 's'} · {formatNaira(total)} estimated, excluding cancelled</span>
            <span>FY{fy} · 1 Jul {fy - 1} – 30 Jun {fy}</span>
          </div>
        </Panel>
      )}
    </div>
  )
}
