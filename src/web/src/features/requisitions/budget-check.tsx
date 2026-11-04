import { Panel } from '@/components/panel'
import { formatNaira, formatNairaShort } from '@/lib/format'
import type { RequisitionDetail } from './requisitions-api'

/** What the budget looked like when this was submitted. Budgets are soft: over budget adds a Finance step. */
export function BudgetCheck({ requisition: r }: { requisition: RequisitionDetail }) {
  if (r.budgetStatus === 'NotChecked' || r.budgetAvailableAtSubmit === null) {
    return (
      <Panel className="flex flex-col gap-2 px-6 py-5">
        <h2 className="text-xl font-normal">Budget check</h2>
        <p className="text-sm text-muted-foreground">Checked against {r.costCentreCode}'s budget when it's submitted.</p>
      </Panel>
    )
  }

  const available = r.budgetAvailableAtSubmit
  const total = r.estimatedTotal
  const over = Math.max(0, total - available)
  const left = Math.max(0, available - total)
  // Widths for the bar: this requisition up to what was available, then the overflow
  const fill = available > 0 ? Math.min(total, available) / available : 0

  return (
    <Panel className="flex flex-col gap-3.5 px-6 py-5">
      <div className="flex flex-wrap justify-between gap-2.5">
        <h2 className="text-xl font-normal">Budget check</h2>
        <span className="text-[13px] text-muted-foreground">{r.costCentreCode} · checked at submit</span>
      </div>
      <dl className="flex flex-wrap gap-7">
        <Figure label="Available at submit" value={formatNaira(available)} />
        <Figure label="This requisition" value={formatNaira(total)} />
        <Figure label={over > 0 ? 'Over by' : 'Left after this'} value={formatNaira(over > 0 ? over : left)} />
      </dl>
      <div role="img" className="flex h-9 gap-1.5"
        aria-label={`${formatNaira(total)} requested against ${formatNaira(available)} available${over > 0 ? `: over by ${formatNaira(over)}` : ''}`}>
        <div className="flex min-w-0 items-center rounded-full bg-chart-2 px-4 text-xs font-semibold text-[#12301a]"
          style={{ flex: `${Math.max(fill, 0.08)} 1 0` }}>
          This requisition
        </div>
        {over > 0 ? (
          <div className="flex flex-[0_0_140px] items-center justify-center rounded-full border-[1.5px] border-dashed border-over-foreground bg-over text-xs font-bold text-over-foreground">
            +{formatNairaShort(over)} over
          </div>
        ) : (
          <div className="rounded-full border border-chart-3/40 bg-[repeating-linear-gradient(135deg,var(--color-chart-3)_0_2px,transparent_2px_9px)] opacity-60"
            style={{ flex: `${1 - fill} 1 0` }} />
        )}
      </div>
      <p className="text-[13px] text-muted-foreground">
        {over > 0
          ? 'Over budget does not block the requisition. A Finance controller step was added to the approval chain.'
          : 'Within budget. Approving it commits the amount against this cost centre.'}
      </p>
    </Panel>
  )
}

function Figure({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <dt className="text-xs text-muted-foreground">{label}</dt>
      <dd className="font-heading text-2xl font-light">{value}</dd>
    </div>
  )
}
