import { useState, type ReactNode } from 'react'
import { toast } from 'sonner'
import { Panel } from '@/components/panel'
import { Button } from '@/components/ui/button'
import { problemMessage } from '@/lib/api-client'
import { fieldClass } from '@/lib/field-class'
import { formatDate, formatNaira } from '@/lib/format'
import { cn } from '@/lib/utils'
import { useWithdrawQuotation, type Quotation, type SourcingEventDetail } from './sourcing-api'

/** Lines down, suppliers across. The lowest price on each line, and the lowest total, are highlighted. */
export function QuoteComparison({ rfq, manage }: { rfq: SourcingEventDetail; manage: boolean }) {
  const live = rfq.quotations.filter((q) => q.status === 'Received')
  const withdrawn = rfq.quotations.filter((q) => q.status === 'Withdrawn')
  const [today] = useState(() => new Date().toISOString().slice(0, 10))

  if (live.length === 0) {
    return (
      <Panel className="px-6 py-5">
        <h2 className="text-xl font-normal">Quotations</h2>
        <p className="mt-2 text-sm text-muted-foreground">
          {rfq.status === 'Published' ? 'None recorded yet. Record each one below as it arrives.' : 'No quotations were received.'}
        </p>
        {withdrawn.length > 0 && <Withdrawn quotes={withdrawn} />}
      </Panel>
    )
  }

  const priceOf = (q: Quotation, lineId: string) => q.lines.find((l) => l.sourcingEventLineId === lineId)
  // Totals only compare fairly between quotes that price every line
  const complete = live.filter((q) => q.lines.length === rfq.lines.length)
  const lowestTotal = complete.length ? Math.min(...complete.map((q) => q.total)) : null

  return (
    <Panel className="px-6 py-5">
      <div className="mb-2.5 flex flex-wrap items-baseline justify-between gap-2">
        <h2 className="text-xl font-normal">Quotations</h2>
        <span className="text-[13px] text-muted-foreground">
          {live.length} received · {lowestTotal === null ? 'no quote covers every line yet' : `lowest complete ${formatNaira(lowestTotal)}`}
          {lowestTotal !== null && lowestTotal !== rfq.estimatedTotal && (
            <> · <span className={lowestTotal < rfq.estimatedTotal ? 'text-[#2f7d3a] dark:text-[#8fd19a]' : 'text-destructive'}>
              {formatNaira(Math.abs(rfq.estimatedTotal - lowestTotal))} {lowestTotal < rfq.estimatedTotal ? 'under' : 'over'} estimate
            </span></>
          )}
        </span>
      </div>
      <div className="overflow-x-auto">
        <table className="w-full border-collapse text-sm" style={{ minWidth: 380 + live.length * 180 }}>
          <thead>
            <tr className="text-left align-bottom text-xs text-muted-foreground">
              <th scope="col" className="p-2.5 font-semibold">Line</th>
              <th scope="col" className="p-2.5 text-right font-semibold">Estimate</th>
              {live.map((q) => (
                <th key={q.id} scope="col" className="p-2.5 text-right font-semibold">
                  <span className="block text-sm text-foreground">{q.supplierName}</span>
                  {q.supplierReference && <span className="block font-normal">ref {q.supplierReference}</span>}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {rfq.lines.map((line) => {
              const prices = live.map((q) => priceOf(q, line.id)?.unitPrice).filter((p): p is number => p !== undefined)
              const lowest = prices.length > 1 ? Math.min(...prices) : undefined
              return (
                <tr key={line.id} className="border-t">
                  <th scope="row" className="p-2.5 text-left font-normal">
                    <span className="font-semibold">{line.lineNo}. {line.description}</span>
                    <span className="block text-xs text-muted-foreground">{line.quantity} {line.uomCode}</span>
                  </th>
                  <td className="p-2.5 text-right text-muted-foreground">{formatNaira(line.estimatedUnitPrice)}</td>
                  {live.map((q) => {
                    const p = priceOf(q, line.id)
                    if (!p) return <td key={q.id} className="p-2.5 text-right text-xs text-muted-foreground">Not quoted</td>
                    const best = p.unitPrice === lowest
                    return (
                      <td key={q.id} className="p-2.5 text-right">
                        <span className={cn('rounded-lg px-1.5 py-0.5', best && 'bg-success font-semibold text-success-foreground')}>
                          {formatNaira(p.unitPrice)}
                        </span>
                        {p.quantity !== line.quantity && <span className="block text-xs text-muted-foreground">for {p.quantity}</span>}
                        {p.notes && <span className="block text-xs text-muted-foreground" title={p.notes}>{p.notes}</span>}
                      </td>
                    )
                  })}
                </tr>
              )
            })}
          </tbody>
          <tfoot className="border-t-2">
            <Row label="Total" estimate={formatNaira(rfq.estimatedTotal)} quotes={live}
              cell={(q) => (
                <>
                  <span className={cn('rounded-lg px-1.5 py-0.5 font-semibold', complete.length > 1 && q.total === lowestTotal && 'bg-success text-success-foreground')}>{formatNaira(q.total)}</span>
                  {q.lines.length < rfq.lines.length && (
                    <span className="block text-xs text-muted-foreground">{q.lines.length} of {rfq.lines.length} lines only</span>
                  )}
                </>
              )} />
            <Row label="Lead time" quotes={live} cell={(q) => `${q.leadTimeDays} days`} />
            <Row label="Payment terms" quotes={live} cell={(q) => `${q.paymentTermsDays} days`} />
            <Row label="Valid until" quotes={live}
              cell={(q) => <span className={q.validUntil < today ? 'text-destructive' : undefined}>{formatDate(q.validUntil)}{q.validUntil < today && ' (expired)'}</span>} />
            {manage && rfq.status === 'Published' && (
              <Row label="" quotes={live} cell={(q) => <WithdrawButton rfqId={rfq.id} quote={q} />} />
            )}
          </tfoot>
        </table>
      </div>
      {withdrawn.length > 0 && <Withdrawn quotes={withdrawn} />}
    </Panel>
  )
}

function Row({ label, estimate, quotes, cell }: {
  label: string
  estimate?: string
  quotes: Quotation[]
  cell: (q: Quotation) => ReactNode
}) {
  return (
    <tr>
      <th scope="row" className="p-2.5 text-left text-xs font-semibold text-muted-foreground">{label}</th>
      <td className="p-2.5 text-right text-muted-foreground">{estimate}</td>
      {quotes.map((q) => <td key={q.id} className="p-2.5 text-right">{cell(q)}</td>)}
    </tr>
  )
}

function WithdrawButton({ rfqId, quote }: { rfqId: string; quote: Quotation }) {
  const withdraw = useWithdrawQuotation(rfqId)
  const [open, setOpen] = useState(false)
  const [reason, setReason] = useState('')

  if (!open) {
    return <Button size="pill-sm" variant="ghost" className="h-7 px-2 text-muted-foreground" onClick={() => setOpen(true)}>Withdraw</Button>
  }
  return (
    <div className="flex flex-col items-end gap-1.5">
      <input aria-label={`Why withdraw ${quote.supplierName}'s quotation?`} value={reason} maxLength={500}
        onChange={(e) => setReason(e.target.value)} placeholder="Reason" className={cn(fieldClass, 'h-8 w-36 text-xs')} />
      <div className="flex gap-1">
        <Button size="pill-sm" variant="ghost" className="h-7 px-2" onClick={() => setOpen(false)}>Keep</Button>
        <Button size="pill-sm" variant="destructive" className="h-7 px-2" disabled={!reason.trim() || withdraw.isPending}
          onClick={() => withdraw.mutate({ quotationId: quote.id, reason: reason.trim() },
            { onSuccess: () => toast(`${quote.supplierName}'s quotation withdrawn; they can quote again`) })}>
          Withdraw
        </Button>
      </div>
      {withdraw.error && <span role="alert" className="text-xs text-destructive">{problemMessage(withdraw.error)}</span>}
    </div>
  )
}

function Withdrawn({ quotes }: { quotes: Quotation[] }) {
  return (
    <details className="mt-3 text-[13px] text-muted-foreground">
      <summary className="cursor-pointer font-semibold">{quotes.length} withdrawn</summary>
      <ul className="mt-2 flex flex-col gap-1">
        {quotes.map((q) => (
          <li key={q.id}>{q.supplierName} · {formatNaira(q.total)} · {q.withdrawalReason}</li>
        ))}
      </ul>
    </details>
  )
}
