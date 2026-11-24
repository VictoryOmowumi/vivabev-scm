import { cn } from '@/lib/utils'
import { supplierStatusLabels, type SupplierStatus } from './suppliers-api'

const styles: Record<SupplierStatus, string> = {
  Prospect: 'bg-draft text-draft-foreground',
  PendingApproval: 'bg-pending text-pending-foreground',
  Approved: 'bg-success text-success-foreground',
  Suspended: 'bg-over text-over-foreground',
  Blocked: 'bg-cancelled text-cancelled-foreground',
}

export function SupplierStatusBadge({ status, className }: { status: SupplierStatus; className?: string }) {
  return (
    <span className={cn('inline-flex items-center whitespace-nowrap rounded-full px-2.5 py-1 text-xs font-bold', styles[status], className)}>
      {supplierStatusLabels[status]}
    </span>
  )
}
