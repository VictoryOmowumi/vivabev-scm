import { cn } from '@/lib/utils'
import { orderStatusLabels, type PurchaseOrderStatus } from './purchase-orders-api'

const styles: Record<PurchaseOrderStatus, string> = {
  Draft: 'bg-draft text-draft-foreground',
  Issued: 'bg-success text-success-foreground',
  Cancelled: 'bg-cancelled text-cancelled-foreground',
}

export function OrderStatusBadge({ status, className }: { status: PurchaseOrderStatus; className?: string }) {
  return (
    <span className={cn('inline-flex items-center whitespace-nowrap rounded-full px-2.5 py-1 text-xs font-bold', styles[status], className)}>
      {orderStatusLabels[status]}
    </span>
  )
}
