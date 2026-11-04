import { HugeiconsIcon } from '@hugeicons/react'
import { CancelCircleIcon, Tick02Icon } from '@hugeicons/core-free-icons'
import { formatDateTime } from '@/lib/format'
import { cn } from '@/lib/utils'
import { currentStep, type ApprovalStatus } from './requisitions-api'

/** The numbered steps on the dark aside. */
export function ApprovalChain({ approval, meId }: { approval: ApprovalStatus; meId: string }) {
  const current = currentStep(approval)

  return (
    <ol className="flex flex-col gap-1.5">
      {approval.steps.map((step) => {
        const isCurrent = step.sequence === current?.sequence
        const why = step.decision === 'Approved' ? `Approved ${step.decidedAt ? formatDateTime(step.decidedAt) : ''}`
          : step.decision === 'Rejected' ? 'Rejected'
          : isCurrent ? (step.approverId === meId ? 'Waiting for you' : 'Waiting')
          : approval.status === 'Pending' ? 'Next' : 'Not reached'

        return (
          <li key={step.sequence}
            className={cn('flex gap-3 rounded-[18px] bg-inverse-row p-3', isCurrent && 'ring-[1.5px] ring-chart-2')}>
            <span className={cn('flex size-[34px] flex-none items-center justify-center rounded-full text-[13px] font-bold',
              isCurrent ? 'bg-chart-2 text-[#12301a]'
                // Fixed colours: this card flips light/dark opposite to the page, so theme tokens would vanish
                : step.decision === 'Approved' ? 'bg-[#d3ebcf] text-[#1d4f24]'
                : step.decision === 'Rejected' ? 'bg-[#f3c9a6] text-[#5a2a08]'
                : 'bg-inverse-muted/25')}>
              {step.decision === 'Approved' ? <HugeiconsIcon icon={Tick02Icon} size={16} />
                : step.decision === 'Rejected' ? <HugeiconsIcon icon={CancelCircleIcon} size={16} />
                : step.sequence}
            </span>
            <span className="min-w-0 flex-1">
              <span className="block text-sm font-semibold">{step.approverName}</span>
              <span className="block text-xs text-inverse-muted">{step.name} · {why}</span>
              {step.comment && <span className="mt-1 block text-xs italic">“{step.comment}”</span>}
            </span>
          </li>
        )
      })}
    </ol>
  )
}
