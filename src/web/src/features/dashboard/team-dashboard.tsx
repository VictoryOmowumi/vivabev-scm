import { useState } from 'react'
import { Link } from 'react-router'
import { Clock01Icon, File01Icon, FileEditIcon, FlashIcon, Wallet01Icon } from '@hugeicons/core-free-icons'
import { Panel } from '@/components/panel'
import { Skeleton } from '@/components/ui/skeleton'
import { useApprovalInbox } from '@/features/approvals/approvals-api'
import type { CurrentUser } from '@/features/auth/auth-api'
import { freeOf, useBudgets, type BudgetSummary } from '@/features/budgets/budgets-api'
import { useRequisitions, type RequisitionScope, type RequisitionStatus, type RequisitionSummary } from '@/features/requisitions/requisitions-api'
import { StatusBadge } from '@/features/requisitions/status-badges'
import { fiscalYearOf, formatDate, formatNaira } from '@/lib/format'
import { can, canSeeBudgets, Permission } from '@/lib/permissions'
import {
  BigFigure, Bars, Card, CopilotBar, DarkList, DueSoon, Figures, Hero, Strip, UsedBar,
  type DarkItem, type DueItem, type Stat,
} from './dashboard-parts'
import { daysFromToday, shortNaira } from './dashboard-utils'

const open: RequisitionStatus[] = ['Draft', 'PendingApproval', 'Approved', 'InSourcing']

/** Requesters, department managers, Finance and the MD: requisitions, approvals and budgets. */
export function TeamDashboard({ user }: { user: CurrentUser }) {
  const scope: RequisitionScope = can(user, Permission.RequisitionsViewAll) ? 'all'
    : can(user, Permission.RequisitionsViewDepartment) ? 'department' : 'mine'
  const approver = scope !== 'mine'
  const showBudgets = canSeeBudgets(user)
  const year = fiscalYearOf()

  const inbox = useApprovalInbox()
  const requisitions = useRequisitions(scope)
  const budgets = useBudgets(year, showBudgets)
  const [today] = useState(() => new Date().toLocaleDateString('en-CA'))

  const rows = requisitions.data ?? []
  const budgetRows = budgets.data ?? []
  const where = scope === 'all' ? 'all departments' : user.departmentCode
  const totals = sumBudgets(budgetRows)
  const free = freeOf(totals)

  const stats: Stat[] = [
    { value: inbox.data?.length ?? '–', label: 'Awaiting you', icon: Clock01Icon, to: '/approvals' },
    {
      value: requisitions.data ? rows.filter((r) => open.includes(r.status)).length : '–',
      label: scope === 'mine' ? 'Your open requisitions' : scope === 'all' ? 'Open requisitions' : `Open in ${user.departmentCode}`,
      icon: File01Icon,
      to: `/requisitions?scope=${scope}`,
    },
    showBudgets
      ? { value: budgets.data ? shortNaira(free) : '–', label: `Free to commit, ${scope === 'all' ? 'all budgets' : user.departmentCode}`, icon: Wallet01Icon, to: '/budgets' }
      : { value: requisitions.data ? rows.filter((r) => r.status === 'PendingApproval').length : '–', label: 'In approval', icon: Wallet01Icon, to: '/requisitions?scope=mine&status=PendingApproval' },
  ]

  // Approvers see their inbox; requesters see what they still have to finish
  const inboxIds = new Set((inbox.data ?? []).map((t) => t.documentId))
  const waiting: DarkItem[] | undefined = approver || (inbox.data?.length ?? 0) > 0
    ? inbox.data?.map((t) => ({
        key: t.requestId,
        to: `/approvals?item=${t.requestId}`,
        icon: t.overBudget ? FlashIcon : File01Icon,
        title: t.title,
        meta: `${t.documentNumber} · ${t.requestedByName} · step ${t.stepNumber} of ${t.totalSteps}`,
        amount: t.amount,
        flag: t.overBudget ? 'Over budget' : undefined,
      }))
    : requisitions.data && rows
        .filter((r) => r.status === 'Draft' && r.requesterId === user.id)
        .map((r) => ({
          key: r.id,
          to: `/requisitions/${r.id}/edit`,
          icon: FileEditIcon,
          title: r.title,
          meta: `${r.number} · ${r.returned ? 'returned to you' : 'draft'} · needed ${formatDate(r.requiredBy)}`,
          amount: r.estimatedTotal,
          flag: r.returned ? 'Returned' : undefined,
        }))

  const due: DueItem[] = rows
    .filter((r) => open.includes(r.status))
    .filter((r) => { const d = daysFromToday(r.requiredBy, today); return d >= 0 && d <= 45 })
    .sort((a, b) => a.requiredBy.localeCompare(b.requiredBy))
    .slice(0, 4)
    .map((r) => ({
      key: r.id,
      date: r.requiredBy,
      to: `/requisitions/${r.id}`,
      title: r.title,
      note: dueNote(r, inboxIds.has(r.id), user.id),
      needsYou: inboxIds.has(r.id) || (r.status === 'Draft' && r.requesterId === user.id),
    }))

  return (
    <div className="flex flex-col gap-7">
      <Hero user={user} stats={stats} />

      {showBudgets && budgetRows.length > 0 && (
        <Strip
          label={`${where} budget, FY${year}`}
          totalLabel={`FY${year} budget`}
          total={totals.amount}
          segments={[
            { label: 'Committed', amount: totals.committed + totals.consumed, tone: 'dark' },
            { label: 'In approval', amount: totals.inApproval, tone: 'green' },
            { label: 'Free to commit', amount: Math.max(free, 0), tone: 'hatch' },
          ]}
        />
      )}

      <CopilotBar
        placeholder="Ask Copilot about requisitions, budgets or suppliers"
        suggestions={approver
          ? ['What has waited longest for me?', 'Which requests are over budget?', `Summarise ${user.departmentCode} spend committed this year`]
          : ['Where is my latest requisition?', 'Who approves my requests?', 'How do I add a free-text line?']}
      />

      <section className="flex flex-wrap gap-5">
        <DarkList
          title={approver || (inbox.data?.length ?? 0) > 0 ? 'Waiting for you' : 'To finish'}
          items={waiting}
          loading={inbox.isPending || (!approver && requisitions.isPending)}
          error={inbox.isError}
          empty={approver ? 'Nothing waiting for your decision.' : 'No drafts waiting. Raise a requisition when you need something.'}
          action={approver || (inbox.data?.length ?? 0) > 0
            ? { to: '/approvals', label: 'Open approvals' }
            : { to: '/requisitions/new', label: 'New requisition' }}
        />
        {showBudgets && <BudgetCard rows={budgetRows} loading={budgets.isPending} year={year} />}
        <Card title={scope === 'mine' ? 'Your requisitions' : scope === 'all' ? 'All requisitions' : `${user.departmentCode} requisitions`}
          to={`/requisitions?scope=${scope}`} className="flex-[1_1_340px]">
          <BigFigure size="md" value={requisitions.data ? rows.length : '–'} caption="by status" />
          {requisitions.isPending ? <Skeleton className="h-[170px] rounded-2xl" /> : (
            <Bars highlight="Pending" bars={[
              { label: 'Draft', count: count(rows, 'Draft'), to: `/requisitions?scope=${scope}&status=Draft`, tone: 'dark' },
              { label: 'Pending', count: count(rows, 'PendingApproval'), to: `/requisitions?scope=${scope}&status=PendingApproval`, tone: 'green' },
              { label: 'Approved', count: count(rows, 'Approved'), to: `/requisitions?scope=${scope}&status=Approved`, tone: 'dark' },
              { label: 'Sourcing', count: count(rows, 'InSourcing'), to: `/requisitions?scope=${scope}&status=InSourcing`, tone: 'dark' },
              { label: 'Cancelled', count: count(rows, 'Cancelled'), to: `/requisitions?scope=${scope}&status=Cancelled`, tone: 'grey' },
            ]} />
          )}
        </Card>
      </section>

      <section className="flex flex-wrap gap-5">
        <Panel className="flex min-w-0 flex-[2_1_640px] flex-col gap-3.5 p-[26px]">
          <div className="flex flex-wrap items-center justify-between gap-3">
            <h2 className="text-xl font-normal">{scope === 'mine' ? 'Your recent requisitions' : scope === 'all' ? 'Recent requisitions' : `Recent in ${user.departmentCode}`}</h2>
            <Link to={`/requisitions?scope=${scope}`} className="text-sm font-semibold underline-offset-4 hover:underline">View all requisitions</Link>
          </div>
          {requisitions.isPending && <Skeleton className="h-48 rounded-2xl" />}
          {requisitions.data?.length === 0 && <p className="py-6 text-sm text-muted-foreground">No requisitions yet.</p>}
          {rows.length > 0 && (
            <div className="overflow-x-auto">
              <table className="w-full min-w-[560px] border-collapse text-sm">
                <thead>
                  <tr className="text-left text-xs text-muted-foreground">
                    <th scope="col" className="px-2.5 py-2 font-semibold">Requisition</th>
                    <th scope="col" className="px-2.5 py-2 font-semibold">Status</th>
                    <th scope="col" className="px-2.5 py-2 text-right font-semibold">Total</th>
                    <th scope="col" className="px-2.5 py-2 font-semibold">Required by</th>
                  </tr>
                </thead>
                <tbody>
                  {rows.slice(0, 5).map((r) => (
                    <tr key={r.id} className="border-t">
                      <td className="px-2.5 py-3">
                        <Link to={`/requisitions/${r.id}`} className="font-semibold hover:text-brand">{r.title}</Link>
                        <div className="text-xs text-muted-foreground">{r.number} · {r.requesterName}</div>
                      </td>
                      <td className="px-2.5 py-3"><StatusBadge status={r.status} returned={r.returned} /></td>
                      <td className="px-2.5 py-3 text-right font-semibold">{formatNaira(r.estimatedTotal)}</td>
                      <td className="px-2.5 py-3">{formatDate(r.requiredBy)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </Panel>
        <DueSoon subtitle="Required-by dates in the next 45 days" items={due} loading={requisitions.isPending} empty="Nothing due in the next 45 days." />
      </section>
    </div>
  )
}

/** The cost centre with the least room left, as in the design's single-budget card. */
function BudgetCard({ rows, loading, year }: { rows: BudgetSummary[]; loading: boolean; year: number }) {
  const tightest = [...rows].sort((a, b) => headroom(a) - headroom(b))[0]

  return (
    <Card title={tightest?.costCentreName ?? 'Budget'} to={tightest ? `/budgets/${tightest.id}` : '/budgets'}
      linkLabel={tightest ? `Open ${tightest.costCentreName} budget` : 'Open budgets'} className="flex-[1_1_300px]">
      {loading && <Skeleton className="h-40 rounded-2xl" />}
      {!loading && !tightest && <p className="text-sm text-muted-foreground">No budget set for FY{year} yet.</p>}
      {tightest && (
        <>
          {tightest.available >= 0
            ? <BigFigure value={`${Math.round(ratio(tightest) * 100)}%`} caption="available" />
            : <BigFigure value="Over" caption={`by ${formatNaira(-tightest.available)}`} tone="over" />}
          <p className="text-[13px] text-muted-foreground">
            {formatNaira(tightest.available)} of {formatNaira(tightest.amount)} · {tightest.costCentreCode} · FY{year}
          </p>
          <UsedBar used={tightest.committed + tightest.consumed} total={tightest.amount} />
          {tightest.available >= 0 && freeOf(tightest) < 0 && (
            <p className="rounded-xl bg-over px-3 py-2 text-xs font-semibold text-over-foreground">
              {formatNaira(-freeOf(tightest))} over once the requests in approval go through
            </p>
          )}
          <Figures rows={[
            { label: 'Committed', value: formatNaira(tightest.committed) },
            { label: 'In approval', value: formatNaira(tightest.inApproval) },
            { label: 'Consumed', value: formatNaira(tightest.consumed) },
          ]} />
          {rows.length > 1 && <p className="mt-auto text-xs text-muted-foreground">Tightest of {rows.length} cost centres</p>}
        </>
      )}
    </Card>
  )
}

const ratio = (b: BudgetSummary) => (b.amount > 0 ? b.available / b.amount : 0)
/** Room left once what's in approval goes through: how the tightest cost centre is picked. */
const headroom = (b: BudgetSummary) => (b.amount > 0 ? freeOf(b) / b.amount : 0)
const count = (rows: RequisitionSummary[], status: RequisitionStatus) => rows.filter((r) => r.status === status).length

function sumBudgets(rows: BudgetSummary[]) {
  return rows.reduce(
    (t, b) => ({ amount: t.amount + b.amount, consumed: t.consumed + b.consumed, committed: t.committed + b.committed, inApproval: t.inApproval + b.inApproval, available: t.available + b.available }),
    { amount: 0, consumed: 0, committed: 0, inApproval: 0, available: 0 })
}

function dueNote(r: RequisitionSummary, inMyInbox: boolean, me: string) {
  if (inMyInbox) return 'Waiting for your approval'
  if (r.status === 'Draft') return r.requesterId === me ? (r.returned ? 'Returned to you' : 'Still a draft') : 'Still a draft'
  if (r.status === 'PendingApproval') return 'Pending approval'
  if (r.status === 'Approved') return 'Approved · waiting for sourcing'
  return 'On an RFQ'
}
