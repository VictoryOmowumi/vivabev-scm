import { useState } from 'react'
import { Link } from 'react-router'
import { Calendar03Icon, DeliveryTruck01Icon, PackageIcon } from '@hugeicons/core-free-icons'
import { Panel } from '@/components/panel'
import { Skeleton } from '@/components/ui/skeleton'
import type { CurrentUser } from '@/features/auth/auth-api'
import { useDueIn, useReceipts, useStock } from '@/features/warehouse/warehouse-api'
import { formatDate, formatDateTime, formatNaira } from '@/lib/format'
import { BigFigure, Card, CopilotBar, DarkList, DueSoon, Figures, Hero, type DarkItem, type DueItem, type Stat } from './dashboard-parts'
import { daysFromToday, shortNaira } from './dashboard-utils'

/** Warehouse officers: deliveries due in, what was received, what's in stock. */
export function WarehouseDashboard({ user }: { user: CurrentUser }) {
  const due = useDueIn()
  const receipts = useReceipts()
  const stock = useStock()
  const [today] = useState(() => new Date().toLocaleDateString('en-CA'))

  const orders = due.data ?? []
  const late = orders.filter((o) => o.expectedDelivery < today)
  const thisWeek = (receipts.data ?? []).filter((r) => -daysFromToday(r.receivedAt.slice(0, 10), today) <= 7)
  const stockValue = (stock.data ?? []).reduce((s, r) => s + r.value, 0)

  const stats: Stat[] = [
    { value: due.data ? orders.length : '–', label: 'Orders due in', icon: DeliveryTruck01Icon, to: '/warehouse' },
    { value: due.data ? late.length : '–', label: 'Late', icon: Calendar03Icon, to: '/warehouse' },
    { value: stock.data ? shortNaira(stockValue) : '–', label: 'Stock at cost', icon: PackageIcon, to: '/warehouse?tab=stock' },
  ]

  const items: DarkItem[] | undefined = due.data?.map((o) => ({
    key: o.id,
    to: `/warehouse/orders/${o.id}`,
    icon: DeliveryTruck01Icon,
    title: `${o.number} · ${o.supplierName}`,
    meta: `${o.siteCode} · ${o.linesOutstanding} of ${o.lineCount} lines to receive · expected ${formatDate(o.expectedDelivery)}`,
    amount: o.total,
    flag: o.expectedDelivery < today ? 'Late' : undefined,
  }))

  const dueSoon: DueItem[] = orders
    .filter((o) => daysFromToday(o.expectedDelivery, today) <= 14)
    .slice(0, 4)
    .map((o) => ({
      key: o.id, date: o.expectedDelivery, to: `/warehouse/orders/${o.id}`, title: `${o.number} · ${o.supplierName}`,
      note: `${o.siteCode} · ${o.linesOutstanding} line${o.linesOutstanding === 1 ? '' : 's'} to receive`, needsYou: o.expectedDelivery <= today,
    }))

  return (
    <div className="flex flex-col gap-7">
      <Hero user={user} stats={stats} />
      <CopilotBar placeholder="Ask Copilot about deliveries or stock"
        suggestions={['What is arriving this week?', 'Which deliveries are late?', 'How much IT equipment is in stock?']} />

      <section className="flex flex-wrap gap-5">
        <DarkList title="Due in" items={items} loading={due.isPending} error={due.isError}
          empty="Nothing is due in. Issued orders appear here until they're received." action={{ to: '/warehouse', label: 'Open the warehouse' }} />
        <Card title="Received this week" to="/warehouse?tab=receipts" className="flex-[1_1_300px]">
          {receipts.isPending ? <Skeleton className="h-40 rounded-2xl" /> : (
            <>
              <BigFigure value={thisWeek.length} caption={`receipt${thisWeek.length === 1 ? '' : 's'}`} />
              <Figures rows={[
                { label: 'Value accepted', value: formatNaira(thisWeek.reduce((s, r) => s + r.acceptedValue, 0)) },
                { label: 'With rejections', value: thisWeek.filter((r) => r.hasRejections).length },
                { label: 'All time', value: receipts.data?.length ?? 0 },
              ]} />
            </>
          )}
        </Card>
        <Card title="Stock on hand" to="/warehouse?tab=stock" className="flex-[1_1_300px]">
          {stock.isPending ? <Skeleton className="h-40 rounded-2xl" /> : (
            <>
              <BigFigure value={stock.data?.length ?? 0} caption="items in stock" />
              <ul className="flex flex-col gap-1.5 text-sm">
                {[...(stock.data ?? [])].sort((a, b) => b.value - a.value).slice(0, 4).map((s) => (
                  <li key={`${s.siteId}:${s.itemId}`} className="flex justify-between gap-3">
                    <span className="min-w-0 truncate">{s.description} <span className="text-muted-foreground">· {s.siteCode}</span></span>
                    <span className="font-semibold">{s.onHand} {s.uomCode}</span>
                  </li>
                ))}
              </ul>
            </>
          )}
        </Card>
      </section>

      <section className="flex flex-wrap gap-5">
        <Panel className="flex min-w-0 flex-[2_1_640px] flex-col gap-3.5 p-[26px]">
          <div className="flex flex-wrap items-center justify-between gap-3">
            <h2 className="text-xl font-normal">Recent receipts</h2>
            <Link to="/warehouse?tab=receipts" className="text-sm font-semibold underline-offset-4 hover:underline">View all receipts</Link>
          </div>
          {receipts.isPending && <Skeleton className="h-40 rounded-2xl" />}
          {receipts.data?.length === 0 && <p className="py-6 text-sm text-muted-foreground">Nothing received yet.</p>}
          <ul className="flex flex-col divide-y">
            {receipts.data?.slice(0, 5).map((r) => (
              <li key={r.id} className="flex flex-wrap items-center gap-3 py-3 text-sm">
                <Link to={`/warehouse/receipts/${r.id}`} className="font-semibold hover:text-brand">{r.number}</Link>
                <span className="min-w-0 flex-1 truncate text-xs text-muted-foreground">{r.purchaseOrderNumber} · {r.supplierName} · {formatDateTime(r.receivedAt)}</span>
                <span className="font-semibold">{formatNaira(r.acceptedValue)}</span>
              </li>
            ))}
          </ul>
        </Panel>
        <DueSoon subtitle="Expected deliveries in the next 14 days, late ones first" items={dueSoon} loading={due.isPending}
          empty="No deliveries expected in the next 14 days." />
      </section>
    </div>
  )
}
