import { cn } from '@/lib/utils'
import { stockRequestStatusLabels, type StockRequestStatus } from './stock-api'

const styles: Record<StockRequestStatus, string> = {
  Open: 'bg-pending text-pending-foreground',
  PartiallyIssued: 'bg-over text-over-foreground',
  Issued: 'bg-success text-success-foreground',
  Closed: 'bg-inverse text-inverse-foreground',
  Rejected: 'bg-cancelled text-cancelled-foreground',
  Cancelled: 'bg-cancelled text-cancelled-foreground',
}

export function StockRequestStatusBadge({ status, className }: { status: StockRequestStatus; className?: string }) {
  return (
    <span className={cn('inline-flex items-center whitespace-nowrap rounded-full px-2.5 py-1 text-xs font-bold', styles[status], className)}>
      {stockRequestStatusLabels[status]}
    </span>
  )
}
