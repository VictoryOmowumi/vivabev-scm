import { cn } from '@/lib/utils'
import { statusLabels, type BudgetStatus, type RequisitionStatus } from './requisitions-api'

const base = 'inline-flex items-center whitespace-nowrap rounded-full px-2.5 py-1 text-xs font-bold'

const statusStyles: Record<RequisitionStatus, string> = {
  Draft: 'bg-draft text-draft-foreground',
  PendingApproval: 'bg-pending text-pending-foreground',
  Approved: 'bg-success text-success-foreground',
  Cancelled: 'bg-cancelled text-cancelled-foreground',
}

export function StatusBadge({ status, returned = false, suffix, className }: {
  status: RequisitionStatus
  returned?: boolean
  suffix?: string
  className?: string
}) {
  // A rejected requisition goes back to Draft; "Returned" tells the requester why it's there
  if (returned && status === 'Draft') {
    return <span className={cn(base, 'bg-over text-over-foreground', className)}>Returned</span>
  }
  return (
    <span className={cn(base, statusStyles[status], className)}>
      {statusLabels[status]}{suffix && ` · ${suffix}`}
    </span>
  )
}

export function BudgetBadge({ status, label }: { status: BudgetStatus; label?: string }) {
  if (status === 'NotChecked') return <span className="text-sm text-muted-foreground">Not checked</span>
  return status === 'OverBudget'
    ? <span className={cn(base, 'bg-over text-over-foreground')}>{label ?? 'Over budget'}</span>
    : <span className={cn(base, 'bg-success text-success-foreground')}>{label ?? 'Within'}</span>
}
