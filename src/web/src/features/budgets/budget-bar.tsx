import { formatNaira } from '@/lib/format'
import { cn } from '@/lib/utils'
import { freeOf, type BudgetSummary } from './budgets-api'

type Totals = Pick<BudgetSummary, 'amount' | 'committed' | 'consumed' | 'inApproval' | 'available'>

const parts = [
  { key: 'consumed', label: 'Spent', className: 'bg-primary/80' },
  { key: 'committed', label: 'Committed', className: 'bg-chart-3' },
  { key: 'inApproval', label: 'In approval', className: 'bg-[#e9b44c]' },
] as const

/** One stacked bar: spent, committed, in approval, then what's free (hatched). Overspend shows as a dashed tail. */
export function BudgetBar({ totals, size = 'md', showLegend = false }: { totals: Totals; size?: 'sm' | 'md'; showLegend?: boolean }) {
  const free = freeOf(totals)
  const used = totals.consumed + totals.committed + totals.inApproval
  const scale = Math.max(totals.amount, used, 1)
  const pct = (n: number) => `${(Math.max(n, 0) / scale) * 100}%`

  return (
    <div className="flex flex-col gap-2">
      <div role="img" className={cn('flex w-full overflow-hidden rounded-full bg-muted', size === 'sm' ? 'h-2' : 'h-3.5')}
        aria-label={`Budget ${formatNaira(totals.amount)}: spent ${formatNaira(totals.consumed)}, committed ${formatNaira(totals.committed)}, in approval ${formatNaira(totals.inApproval)}, ${free >= 0 ? `free ${formatNaira(free)}` : `over by ${formatNaira(-free)}`}`}>
        {parts.map((p) => <span key={p.key} className={p.className} style={{ width: pct(totals[p.key]) }} />)}
        {free > 0 && (
          <span className="bg-[repeating-linear-gradient(135deg,var(--color-chart-3)_0_2px,transparent_2px_8px)] opacity-40"
            style={{ width: pct(free) }} />
        )}
      </div>
      {showLegend && (
        <ul className="flex flex-wrap gap-x-4 gap-y-1 text-xs text-muted-foreground">
          {parts.map((p) => (
            <li key={p.key} className="flex items-center gap-1.5">
              <span className={cn('size-2.5 rounded-full', p.className)} />{p.label} {formatNaira(totals[p.key])}
            </li>
          ))}
          <li className="flex items-center gap-1.5">
            <span className="size-2.5 rounded-full border border-chart-3" />
            {free >= 0 ? `Free ${formatNaira(free)}` : `Over by ${formatNaira(-free)}`}
          </li>
        </ul>
      )}
    </div>
  )
}
