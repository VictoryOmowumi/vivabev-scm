import { formatNaira } from '@/lib/format'
import type { CostCentreOption } from './requisition-editor-api'

/** Live view of what this requisition would do to the cost centre's budget. Shown before submitting. */
export function BudgetPreview({ costCentre, total }: { costCentre: CostCentreOption | undefined; total: number }) {
  if (!costCentre) {
    return <p className="text-sm text-inverse-muted">Choose a cost centre to see its budget.</p>
  }
  if (!costCentre.hasBudget) {
    return (
      <p className="rounded-2xl bg-[#f3c9a6] px-4 py-3 text-sm text-[#5a2a08]">
        {costCentre.code} has no budget for FY{costCentre.fiscalYear}. You can still submit; Finance will be asked to approve.
      </p>
    )
  }

  const free = costCentre.available - costCentre.inApproval
  const after = free - total
  const over = after < 0
  // Bar segments as shares of the whole budget
  const share = (n: number) => `${Math.max(0, Math.min(100, (n / costCentre.budget) * 100))}%`
  const used = costCentre.budget - costCentre.available

  return (
    <div className="flex flex-col gap-3">
      <div role="img" className="flex h-3 overflow-hidden rounded-full bg-inverse-row"
        aria-label={`Budget ${formatNaira(costCentre.budget)}: ${formatNaira(used)} committed or spent, ${formatNaira(costCentre.inApproval)} in approval, this requisition ${formatNaira(total)}`}>
        <span className="bg-inverse-muted/60" style={{ width: share(used) }} />
        <span className="bg-[#f2cf7a]" style={{ width: share(costCentre.inApproval) }} />
        <span className={over ? 'bg-[#f3c9a6]' : 'bg-chart-2'} style={{ width: share(Math.min(total, Math.max(free, 0))) }} />
      </div>
      <dl className="grid grid-cols-2 gap-x-4 gap-y-2 text-[13px]">
        <dt className="text-inverse-muted">Budget FY{costCentre.fiscalYear}</dt><dd className="text-right">{formatNaira(costCentre.budget)}</dd>
        <dt className="text-inverse-muted">Committed or spent</dt><dd className="text-right">{formatNaira(used)}</dd>
        <dt className="text-inverse-muted">Waiting for approval</dt><dd className="text-right">{formatNaira(costCentre.inApproval)}</dd>
        <dt className="text-inverse-muted">This requisition</dt><dd className="text-right font-semibold">{formatNaira(total)}</dd>
        <dt className="border-t border-inverse-muted/30 pt-2 font-semibold">{over ? 'Over by' : 'Left after this'}</dt>
        <dd className="border-t border-inverse-muted/30 pt-2 text-right font-semibold">
          {formatNaira(Math.abs(after))}
        </dd>
      </dl>
      {over && (
        <p className="text-xs text-inverse-muted">
          Over budget doesn't block it. A Finance controller approval step is added when you submit.
        </p>
      )}
    </div>
  )
}
