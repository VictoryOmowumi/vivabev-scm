import { cn } from '@/lib/utils'
import { orderStatusLabels, type PurchaseOrderStatus } from './purchase-orders-api'

const styles: Record<PurchaseOrderStatus, string> = {
  Draft: 'bg-draft text-draft-foreground',
  Issued: 'bg-[#dde7f3] text-[#1f3d5c] dark:bg-[#6aa0d8]/20 dark:text-[#a9cdeb]',
  PartiallyReceived: 'bg-pending text-pending-foreground',
  Received: 'bg-success text-success-foreground',
  Closed: 'bg-inverse text-inverse-foreground',
  Cancelled: 'bg-cancelled text-cancelled-foreground',
}

export function OrderStatusBadge({ status, className }: { status: PurchaseOrderStatus; className?: string }) {
  return (
    <span className={cn('inline-flex items-center whitespace-nowrap rounded-full px-2.5 py-1 text-xs font-bold', styles[status], className)}>
      {orderStatusLabels[status]}
    </span>
  )
}
