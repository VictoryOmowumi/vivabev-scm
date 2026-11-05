import { useState, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router'
import { HugeiconsIcon } from '@hugeicons/react'
import { ArrowUpRight01Icon, SparklesIcon } from '@hugeicons/core-free-icons'
import { Panel } from '@/components/panel'
import { Skeleton } from '@/components/ui/skeleton'
import { useApprovalInbox } from '@/features/approvals/approvals-api'
import { useCurrentUser } from '@/features/auth/use-auth'
import { BudgetBar } from '@/features/budgets/budget-bar'
import { freeOf, useBudgets } from '@/features/budgets/budgets-api'
import { statusLabels, useRequisitions, type RequisitionScope, type RequisitionStatus } from '@/features/requisitions/requisitions-api'
import { StatusBadge } from '@/features/requisitions/status-badges'
import { fiscalYearOf, formatDate, formatNaira } from '@/lib/format'
import { can, canSeeBudgets, Permission } from '@/lib/permissions'
import { cn } from '@/lib/utils'

function greeting(now = new Date()) {
  const hour = now.getHours()
  return hour < 12 ? 'Good morning' : hour < 17 ? 'Good afternoon' : 'Good evening'
}

const suggestions = ['What has waited longest for me?', 'Which requests are over budget?', 'Summarise spend committed this year']

export function DashboardPage() {
  const { data: user } = useCurrentUser()
  const firstName = user?.displayName.split(' ')[0] ?? ''
  // Managers watch their department; everyone else their own requests
  const scope: RequisitionScope = can(user, Permission.RequisitionsViewDepartment) || can(user, Permission.RequisitionsViewAll)
    ? 'department' : 'mine'

  return (
    <div className="flex flex-col gap-6">
      <section>
        <h1 className="text-[40px] leading-tight font-light tracking-tight">{greeting()}, {firstName}</h1>
        <p className="mt-2 text-[15px] text-muted-foreground">Here is what needs you today.</p>
      </section>

      <CopilotBar />

      <section className="grid gap-5 lg:grid-cols-3">
        <WaitingForYou />
        {canSeeBudgets(user) ? <BudgetCard everyone={can(user, Permission.BudgetsView)} department={user?.departmentCode ?? ''} /> : null}
        <StatusSummary scope={scope} />
      </section>

      <RecentAndDue scope={scope} />
    </div>
  )
}

function CopilotBar() {
  const navigate = useNavigate()
  const [question, setQuestion] = useState('')
  const ask = (event: FormEvent) => { event.preventDefault(); navigate('/copilot') }

  return (
    <Panel aria-label="VivaBev Copilot" className="flex flex-col gap-3 px-5 py-4">
      <form onSubmit={ask} className="flex items-center gap-3">
        <HugeiconsIcon icon={SparklesIcon} size={22} strokeWidth={1.5} className="flex-none text-brand" />
        <label htmlFor="copilot-ask" className="sr-only">Ask Copilot</label>
        <input id="copilot-ask" value={question} onChange={(e) => setQuestion(e.target.value)}
          placeholder="Ask about requisitions, budgets or approvals"
          className="min-w-0 flex-1 bg-transparent text-[15px] outline-none placeholder:text-muted-foreground" />
        <button type="submit" className="rounded-full bg-brand px-4 py-2 text-sm font-semibold text-brand-foreground">Ask</button>
      </form>
      <div className="flex flex-wrap items-center gap-2">
        {suggestions.map((s) => (
          <Link key={s} to="/copilot" className="rounded-full border px-3 py-1.5 text-[13px] hover:bg-accent">{s}</Link>
        ))}
        <span className="ml-auto text-xs text-muted-foreground">Answers use only data you can access. Copilot drafts; it never approves.</span>
      </div>
    </Panel>
  )
}

function WaitingForYou() {
  const inbox = useApprovalInbox()
  return (
    <section aria-labelledby="waiting-heading" className="flex flex-col gap-4 rounded-[28px] bg-inverse p-6 text-inverse-foreground">
      <div className="flex items-baseline justify-between">
        <h2 id="waiting-heading" className="text-xl font-normal">Waiting for you</h2>
        <span className="font-heading text-4xl font-light">{inbox.data?.length ?? '–'}</span>
      </div>
      {inbox.isPending && <Skeleton className="h-24 rounded-2xl bg-inverse-row" />}
      {inbox.isError && <p className="text-sm text-inverse-muted">Couldn't load approvals.</p>}
      {inbox.data?.length === 0 && <p className="text-sm text-inverse-muted">Nothing waiting. Nice.</p>}
      <ul className="flex flex-col gap-1.5">
        {inbox.data?.slice(0, 3).map((task) => (
          <li key={task.requestId}>
            <Link to={`/approvals?item=${task.requestId}`} className="flex items-center gap-3 rounded-xl bg-inverse-row p-3 hover:opacity-90">
              <span className="min-w-0 flex-1">
                <span className="block truncate text-sm font-semibold">{task.title}</span>
                <span className="block text-xs text-inverse-muted">{task.documentNumber} · {task.requestedByName}</span>
              </span>
              <span className="flex flex-col items-end gap-1">
                <span className="text-sm font-semibold">{formatNaira(task.amount)}</span>
                {task.overBudget && <span className="rounded-full bg-[#f3c9a6] px-2 py-0.5 text-[10px] font-bold text-[#5a2a08]">Over budget</span>}
              </span>
            </Link>
          </li>
        ))}
      </ul>
      <Link to="/approvals" className="mt-auto flex items-center gap-1 self-start text-sm font-semibold hover:underline">
        Open approvals <HugeiconsIcon icon={ArrowUpRight01Icon} size={16} />
      </Link>
    </section>
  )
}

function BudgetCard({ everyone, department }: { everyone: boolean; department: string }) {
  const year = fiscalYearOf()
  const budgets = useBudgets(year)
  const rows = budgets.data ?? []
  const totals = rows.reduce(
    (t, b) => ({ amount: t.amount + b.amount, consumed: t.consumed + b.consumed, committed: t.committed + b.committed, inApproval: t.inApproval + b.inApproval, available: t.available + b.available }),
    { amount: 0, consumed: 0, committed: 0, inApproval: 0, available: 0 })
  const free = freeOf(totals)
  const freePct = totals.amount > 0 ? Math.round((free / totals.amount) * 100) : 0

  return (
    <Panel className="flex flex-col gap-4 p-6">
      <div className="flex items-baseline justify-between gap-3">
        <h2 className="text-xl font-normal">{everyone ? 'All budgets' : `${department} budget`}</h2>
        <span className="text-xs text-muted-foreground">FY{year}</span>
      </div>
      {budgets.isPending && <Skeleton className="h-28 rounded-2xl" />}
      {budgets.isError && <p className="text-sm text-muted-foreground">Couldn't load budgets.</p>}
      {budgets.data && rows.length === 0 && <p className="text-sm text-muted-foreground">No budget set for FY{year} yet.</p>}
      {rows.length > 0 && (
        <>
          {free >= 0 ? (
            <div>
              <span className="font-heading text-5xl font-light">{freePct}%</span>
              <span className="ml-2 text-sm text-muted-foreground">free · {formatNaira(free)} of {formatNaira(totals.amount)}</span>
            </div>
          ) : (
            <div>
              <span className="font-heading text-4xl font-light text-destructive">Over budget</span>
              <span className="mt-1 block text-sm text-muted-foreground">
                {formatNaira(-free)} more is committed or in approval than the {formatNaira(totals.amount)} budget
              </span>
            </div>
          )}
          <BudgetBar totals={totals} showLegend />
        </>
      )}
      <Link to="/budgets" className="mt-auto flex items-center gap-1 self-start text-sm font-semibold hover:text-brand">
        Open budgets <HugeiconsIcon icon={ArrowUpRight01Icon} size={16} />
      </Link>
    </Panel>
  )
}

const statuses: RequisitionStatus[] = ['Draft', 'PendingApproval', 'Approved', 'Cancelled']
const statusBar: Record<RequisitionStatus, string> = {
  Draft: 'bg-draft-foreground/40', PendingApproval: 'bg-[#e9b44c]', Approved: 'bg-chart-3', Cancelled: 'bg-cancelled-foreground/40',
}

function StatusSummary({ scope }: { scope: RequisitionScope }) {
  const list = useRequisitions(scope)
  const counts = statuses.map((s) => ({ status: s, count: (list.data ?? []).filter((r) => r.status === s).length }))
  const max = Math.max(1, ...counts.map((c) => c.count))

  return (
    <Panel className="flex flex-col gap-4 p-6">
      <div className="flex items-baseline justify-between">
        <h2 className="text-xl font-normal">{scope === 'department' ? 'Department requisitions' : 'My requisitions'}</h2>
        <span className="font-heading text-4xl font-light">{list.data?.length ?? '–'}</span>
      </div>
      {list.isPending ? <Skeleton className="h-28 rounded-2xl" /> : (
        <ul className="flex flex-col gap-2.5">
          {counts.map(({ status, count }) => (
            <li key={status}>
              <Link to={`/requisitions?scope=${scope}&status=${status}`} className="grid grid-cols-[110px_1fr_28px] items-center gap-3 text-sm hover:text-brand">
                <span>{statusLabels[status]}</span>
                <span className="h-2 rounded-full bg-muted">
                  <span className={cn('block h-2 rounded-full', statusBar[status])} style={{ width: `${(count / max) * 100}%` }} />
                </span>
                <span className="text-right font-semibold">{count}</span>
              </Link>
            </li>
          ))}
        </ul>
      )}
    </Panel>
  )
}

function RecentAndDue({ scope }: { scope: RequisitionScope }) {
  const list = useRequisitions(scope)
  const rows = list.data ?? []
  const [today] = useState(() => new Date().toLocaleDateString('en-CA')) // fixed for this visit
  const due = rows
    .filter((r) => (r.status === 'PendingApproval' || r.status === 'Approved') && r.requiredBy >= today)
    .sort((a, b) => a.requiredBy.localeCompare(b.requiredBy))
    .slice(0, 4)

  return (
    <section className="grid gap-5 lg:grid-cols-3">
      <Panel className="px-5 py-4 lg:col-span-2">
        <div className="mb-2 flex items-baseline justify-between px-1">
          <h2 className="text-xl font-normal">Recent requisitions</h2>
          <Link to={`/requisitions?scope=${scope}`} className="text-sm font-semibold hover:text-brand">See all</Link>
        </div>
        {list.isPending && <Skeleton className="h-40 rounded-2xl" />}
        {list.data?.length === 0 && <p className="px-1 py-6 text-sm text-muted-foreground">No requisitions yet.</p>}
        {rows.length > 0 && (
          <table className="w-full border-collapse text-sm">
            <tbody>
              {rows.slice(0, 5).map((r) => (
                <tr key={r.id} className="border-t first:border-t-0">
                  <td className="p-2.5">
                    <Link to={`/requisitions/${r.id}`} className="font-semibold hover:text-brand">{r.title}</Link>
                    <div className="text-xs text-muted-foreground">{r.number} · {r.requesterName}</div>
                  </td>
                  <td className="p-2.5"><StatusBadge status={r.status} returned={r.returned} /></td>
                  <td className="p-2.5 text-right font-semibold">{formatNaira(r.estimatedTotal)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </Panel>

      <Panel className="flex flex-col gap-3 p-6">
        <h2 className="text-xl font-normal">Due soon</h2>
        {due.length === 0 && !list.isPending && <p className="text-sm text-muted-foreground">Nothing due.</p>}
        <ul className="flex flex-col gap-2.5">
          {due.map((r) => (
            <li key={r.id}>
              <Link to={`/requisitions/${r.id}`} className="flex items-center gap-3 hover:text-brand">
                <span className="flex w-14 flex-none flex-col items-center rounded-xl bg-surface py-1.5 leading-tight">
                  <span className="text-lg font-semibold">{formatDate(r.requiredBy).split(' ')[0]}</span>
                  <span className="text-[11px] text-muted-foreground">{formatDate(r.requiredBy).split(' ')[1]}</span>
                </span>
                <span className="min-w-0">
                  <span className="block truncate text-sm font-semibold">{r.title}</span>
                  <span className="block text-xs text-muted-foreground">{r.number} · {statusLabels[r.status]}</span>
                </span>
              </Link>
            </li>
          ))}
        </ul>
      </Panel>
    </section>
  )
}
