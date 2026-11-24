import { cn } from '@/lib/utils'
import { eventStatusLabels, type SourcingEventStatus } from './sourcing-api'

const styles: Record<SourcingEventStatus, string> = {
  Draft: 'bg-draft text-draft-foreground',
  Published: 'bg-[#dde7f3] text-[#1f3d5c] dark:bg-[#6aa0d8]/20 dark:text-[#a9cdeb]',
  ResponsesClosed: 'bg-pending text-pending-foreground',
  Awarded: 'bg-success text-success-foreground',
  Cancelled: 'bg-cancelled text-cancelled-foreground',
}

export function EventStatusBadge({ status, className }: { status: SourcingEventStatus; className?: string }) {
  return (
    <span className={cn('inline-flex items-center whitespace-nowrap rounded-full px-2.5 py-1 text-xs font-bold', styles[status], className)}>
      {eventStatusLabels[status]}
    </span>
  )
}
