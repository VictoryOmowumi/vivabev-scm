import { Skeleton } from '@/components/ui/skeleton'
import { formatNaira } from '@/lib/format'
import { cn } from '@/lib/utils'
import { useSourcingEvent } from './sourcing-api'

/** What an approver needs to decide on an award: who, how much, why, and how the others scored. */
export function AwardSummary({ rfqId }: { rfqId: string }) {
  const rfq = useSourcingEvent(rfqId)

  if (rfq.isPending) return <Skeleton className="h-48 rounded-2xl" />
  if (rfq.isError) return <p className="text-sm text-destructive">Couldn't load the RFQ: {rfq.error.message}</p>

  const r = rfq.data
  const award = r.award
  if (!award) return <p className="text-sm text-muted-foreground">This RFQ no longer has a recommendation.</p>
  const over = award.amount - r.estimatedTotal
  const invited = r.invitations.length
  const quoted = r.quotations.filter((q) => q.status === 'Received').length

  return (
    <>
      <dl className="grid grid-cols-2 gap-x-5 gap-y-3 text-sm">
        <div><dt className="text-xs text-muted-foreground">Recommended supplier</dt><dd className="font-semibold">{award.supplierName}</dd></div>
        <div><dt className="text-xs text-muted-foreground">Award value</dt><dd className="font-semibold">{formatNaira(award.amount)}</dd></div>
        <div><dt className="text-xs text-muted-foreground">Buyer</dt><dd className="font-semibold">{r.buyerName}</dd></div>
        <div><dt className="text-xs text-muted-foreground">Competition</dt><dd className="font-semibold">{quoted} of {invited} invited quoted</dd></div>
      </dl>

      <div className={cn('rounded-2xl px-4 py-3.5 text-sm leading-relaxed',
        over > 0 ? 'bg-over text-over-foreground' : 'bg-success text-success-foreground')}>
        <div className="mb-1 text-xs font-bold">Against the estimate</div>
        The requisitions estimated {formatNaira(r.estimatedTotal)}.{' '}
        {over > 0
          ? `This award is ${formatNaira(over)} above that, so a Finance controller step was added.`
          : over < 0 ? `This award is ${formatNaira(-over)} under it.` : 'This award matches it.'}
      </div>

      {(award.justification || !award.topScorer) && (
        <div className="rounded-2xl bg-surface px-4 py-3.5 text-sm leading-relaxed">
          <div className="mb-1 text-xs font-semibold text-muted-foreground">
            {award.topScorer ? "Buyer's note" : "Not the top score. The buyer's reason"}
          </div>
          “{award.justification}”
        </div>
      )}

      <div className="rounded-2xl bg-surface px-4 py-3.5 text-sm">
        <div className="mb-2 text-xs font-semibold text-muted-foreground">How they scored (out of 100)</div>
        <ul className="flex flex-col gap-1.5">
          {r.evaluation.map((e) => (
            <li key={e.quotationId} className={cn('flex items-center gap-3', e.quotationId === award.quotationId && 'font-semibold')}>
              <span className="min-w-0 flex-1 truncate">{e.supplierName}{e.quotationId === award.quotationId && ' (recommended)'}</span>
              <span className="text-muted-foreground">{formatNaira(e.total)}</span>
              <span className="w-12 text-right">{e.weightedTotal.toFixed(1)}</span>
            </li>
          ))}
        </ul>
      </div>
    </>
  )
}
