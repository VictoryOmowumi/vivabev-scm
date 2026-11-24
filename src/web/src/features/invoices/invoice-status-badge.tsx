import { cn } from '@/lib/utils'
import { invoiceStatusLabels, type InvoiceStatus } from './invoices-api'

const styles: Record<InvoiceStatus, string> = {
  OnHold: 'bg-over text-over-foreground',
  Matched: 'bg-success text-success-foreground',
  Approved: 'bg-success text-success-foreground',
  Rejected: 'bg-cancelled text-cancelled-foreground',
  Paid: 'bg-inverse text-inverse-foreground',
}

export function InvoiceStatusBadge({ status, className }: { status: InvoiceStatus; className?: string }) {
  return (
    <span className={cn('inline-flex items-center whitespace-nowrap rounded-full px-2.5 py-1 text-xs font-bold', styles[status], className)}>
      {invoiceStatusLabels[status]}
    </span>
  )
}
