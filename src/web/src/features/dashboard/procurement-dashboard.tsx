import { useState } from 'react'
import { Link } from 'react-router'
import { Calendar03Icon, File01Icon, ShoppingCart01Icon } from '@hugeicons/core-free-icons'
import { Panel } from '@/components/panel'
import { Skeleton } from '@/components/ui/skeleton'
import type { CurrentUser } from '@/features/auth/auth-api'
import { EventStatusBadge } from '@/features/sourcing/event-status-badge'
import {
  useSourceableRequisitions, useSourcingEvents, type SourcingEventStatus, type SourcingEventSummary,
} from '@/features/sourcing/sourcing-api'
import { formatDate, formatNaira } from '@/lib/format'
import {
  BigFigure, Bars, Card, CopilotBar, DarkList, DueSoon, Figures, Hero, Strip, UsedBar,
  type DarkItem, type DueItem, type Stat,
} from './dashboard-parts'
import { daysFromToday } from './dashboard-utils'

const onRfq: SourcingEventStatus[] = ['Draft', 'Published']

/** Procurement officers and managers: requisitions to source, RFQs to run, quotes to chase. */
export function ProcurementDashboard({ user }: { user: CurrentUser }) {
  const waitingQuery = useSourceableRequisitions()
  const eventsQuery = useSourcingEvents()
  const [today] = useState(() => new Date().toLocaleDateString('en-CA'))

  const waiting = waitingQuery.data ?? []
  const events = eventsQuery.data ?? []
  const of = (...s: SourcingEventStatus[]) => events.filter((e) => s.includes(e.status))
  const open = of(...onRfq)
  const published = of('Published')
  const closed = of('ResponsesClosed', 'AwardPending') // being evaluated or awarded
  const dueThisWeek = published.filter((e) => { const d = daysFromToday(e.responseDeadline, today); return d >= 0 && d <= 7 })

  const stats: Stat[] = [
    { value: waitingQuery.data ? waiting.length : '–', label: 'Waiting for sourcing', icon: ShoppingCart01Icon, to: '/sourcing' },
    { value: eventsQuery.data ? open.length : '–', label: 'Open RFQs', icon: File01Icon, to: '/sourcing?status=Published' },
    { value: eventsQuery.data ? dueThisWeek.length : '–', label: 'Quotes due this week', icon: Calendar03Icon, to: '/sourcing?status=Published' },
  ]

  const sum = (rows: { estimatedTotal: number }[]) => rows.reduce((s, r) => s + r.estimatedTotal, 0)
  const pipeline = { waiting: sum(waiting), onRfq: sum(open), closed: sum(closed) }

  const items: DarkItem[] | undefined = waitingQuery.data && waiting.map((r) => ({
    key: r.id,
    to: `/requisitions/${r.id}`,
    icon: ShoppingCart01Icon,
    title: r.title,
    meta: `${r.number} · ${r.requesterName} · approved ${ago(r.approvedAt, today)}`,
    amount: r.estimatedTotal,
    flag: daysFromToday(r.requiredBy, today) <= 14 ? `Needed ${formatDate(r.requiredBy)}` : undefined,
  }))

  // Response rate across RFQs out for quotes
  const invited = published.reduce((s, e) => s + e.invited, 0)
  const quoted = published.reduce((s, e) => s + e.quoted, 0)
  const savings = events
    .filter((e) => e.status !== 'Cancelled' && e.lowestQuote !== null)
    .reduce((s, e) => s + (e.estimatedTotal - e.lowestQuote!), 0)

  const due: DueItem[] = [
    ...published.map((e) => ({
      key: e.id, date: e.responseDeadline, to: `/sourcing/${e.id}`, title: e.title,
      note: `Quotes due · ${e.quoted} of ${e.invited} quoted`, needsYou: e.quoted < e.invited,
    })),
    ...waiting.map((r) => ({
      key: r.id, date: r.requiredBy, to: `/requisitions/${r.id}`, title: r.title,
      note: 'Required by · not on an RFQ yet', needsYou: true,
    })),
  ]
    .filter((d) => { const n = daysFromToday(d.date, today); return n >= 0 && n <= 30 })
    .sort((a, b) => a.date.localeCompare(b.date))
    .slice(0, 4)

  return (
    <div className="flex flex-col gap-7">
      <Hero user={user} stats={stats} />

      {(waiting.length > 0 || events.length > 0) && (
        <Strip
          label="Sourcing pipeline by estimated value"
          totalLabel="In the pipeline"
          total={pipeline.waiting + pipeline.onRfq + pipeline.closed}
          segments={[
            { label: 'Waiting for sourcing', amount: pipeline.waiting, tone: 'dark' },
            { label: 'On an RFQ', amount: pipeline.onRfq, tone: 'green' },
            { label: 'Evaluating', amount: pipeline.closed, tone: 'hatch' },
          ]}
        />
      )}

      <CopilotBar
        placeholder="Ask Copilot about RFQs, quotations or suppliers"
        suggestions={['Which suppliers haven\'t replied?', 'Compare quotes on my open RFQs', 'Who can supply IT hardware?']}
      />

      <section className="flex flex-wrap gap-5">
        <DarkList
          title="Waiting for sourcing"
          items={items}
          loading={waitingQuery.isPending}
          error={waitingQuery.isError}
          empty="Every approved requisition is on an RFQ."
          action={{ to: '/sourcing/new', label: 'Start an RFQ' }}
        />
        <Card title="Supplier responses" to="/sourcing?status=Published" linkLabel="Open RFQs out for quotes" className="flex-[1_1_300px]">
          {eventsQuery.isPending ? <Skeleton className="h-40 rounded-2xl" /> : (
            <>
              <BigFigure value={invited ? `${Math.round((quoted / invited) * 100)}%` : '–'} caption="have quoted" />
              <p className="text-[13px] text-muted-foreground">
                {invited ? `${quoted} of ${invited} invited suppliers on ${published.length} open RFQ${published.length === 1 ? '' : 's'}` : 'No RFQs out for quotes'}
              </p>
              <UsedBar used={quoted} total={invited} />
              <Figures rows={[
                { label: 'Still to reply', value: invited - quoted },
                { label: 'Drafts to publish', value: of('Draft').length },
                { label: 'Below estimate so far', value: savings > 0 ? formatNaira(savings) : '–' },
              ]} />
            </>
          )}
        </Card>
        <Card title="RFQs" to="/sourcing" className="flex-[1_1_340px]">
          <BigFigure size="md" value={eventsQuery.data ? events.length : '–'} caption="by status" />
          {eventsQuery.isPending ? <Skeleton className="h-[170px] rounded-2xl" /> : (
            <Bars highlight="Open" bars={[
              { label: 'Draft', count: of('Draft').length, to: '/sourcing?status=Draft', tone: 'dark' },
              { label: 'Open', count: published.length, to: '/sourcing?status=Published', tone: 'green' },
              { label: 'Evaluating', count: of('ResponsesClosed').length, to: '/sourcing?status=ResponsesClosed', tone: 'dark' },
              { label: 'Approval', count: of('AwardPending').length, to: '/sourcing?status=AwardPending', tone: 'dark' },
              { label: 'Awarded', count: of('Awarded').length, to: '/sourcing?status=Awarded', tone: 'dark' },
              { label: 'Cancelled', count: of('Cancelled').length, to: '/sourcing?status=Cancelled', tone: 'grey' },
            ]} />
          )}
        </Card>
      </section>

      <section className="flex flex-wrap gap-5">
        <Panel className="flex min-w-0 flex-[2_1_640px] flex-col gap-3.5 p-[26px]">
          <div className="flex flex-wrap items-center justify-between gap-3">
            <h2 className="text-xl font-normal">Recent RFQs</h2>
            <Link to="/sourcing" className="text-sm font-semibold underline-offset-4 hover:underline">View all RFQs</Link>
          </div>
          {eventsQuery.isPending && <Skeleton className="h-48 rounded-2xl" />}
          {eventsQuery.data?.length === 0 && (
            <p className="py-6 text-sm text-muted-foreground">
              No RFQs yet. <Link to="/sourcing/new" className="font-semibold text-foreground hover:text-brand">Start one</Link> from an approved requisition.
            </p>
          )}
          {events.length > 0 && <RfqTable rows={events.slice(0, 5)} />}
        </Panel>
        <DueSoon subtitle="Quote deadlines and required-by dates in the next 30 days" items={due}
          loading={waitingQuery.isPending || eventsQuery.isPending} empty="Nothing due in the next 30 days." />
      </section>
    </div>
  )
}

function RfqTable({ rows }: { rows: SourcingEventSummary[] }) {
  return (
    <div className="overflow-x-auto">
      <table className="w-full min-w-[560px] border-collapse text-sm">
        <thead>
          <tr className="text-left text-xs text-muted-foreground">
            <th scope="col" className="px-2.5 py-2 font-semibold">RFQ</th>
            <th scope="col" className="px-2.5 py-2 font-semibold">Status</th>
            <th scope="col" className="px-2.5 py-2 font-semibold">Quotes</th>
            <th scope="col" className="px-2.5 py-2 text-right font-semibold">Estimated</th>
            <th scope="col" className="px-2.5 py-2 font-semibold">Quotes due</th>
          </tr>
        </thead>
        <tbody>
          {rows.map((e) => (
            <tr key={e.id} className="border-t">
              <td className="px-2.5 py-3">
                <Link to={`/sourcing/${e.id}`} className="font-semibold hover:text-brand">{e.title}</Link>
                <div className="text-xs text-muted-foreground">{e.number} · {e.requisitionNumbers.join(', ')}</div>
              </td>
              <td className="px-2.5 py-3"><EventStatusBadge status={e.status} /></td>
              <td className="px-2.5 py-3">
                {e.quoted} of {e.invited}
                {e.lowestQuote !== null && <div className="text-xs text-muted-foreground">lowest {formatNaira(e.lowestQuote)}</div>}
              </td>
              <td className="px-2.5 py-3 text-right font-semibold">{formatNaira(e.estimatedTotal)}</td>
              <td className="px-2.5 py-3">{formatDate(e.responseDeadline)}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}

function ago(iso: string | null, today: string) {
  if (!iso) return 'recently'
  const days = -daysFromToday(iso.slice(0, 10), today)
  return days <= 0 ? 'today' : days === 1 ? 'yesterday' : `${days}d ago`
}

