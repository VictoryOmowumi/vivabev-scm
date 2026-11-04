import type { ReactNode } from 'react'
import { HugeiconsIcon, type IconSvgElement } from '@hugeicons/react'
import { Panel } from '@/components/panel'
import { cn } from '@/lib/utils'

const tones = {
  neutral: 'bg-success text-success-foreground',
  error: 'bg-destructive/10 text-destructive',
  warning: 'bg-pending text-pending-foreground',
}

type StateCardProps = {
  icon: IconSvgElement
  tone?: keyof typeof tones
  title: string
  children: ReactNode
  actions?: ReactNode
  footer?: ReactNode
}

/** The shared shape behind every empty, error and no-access state. */
export function StateCard({ icon, tone = 'neutral', title, children, actions, footer }: StateCardProps) {
  return (
    <Panel className="flex min-h-80 flex-col items-center justify-center gap-3 px-7 py-11 text-center">
      <span className={cn('flex size-16 items-center justify-center rounded-[22px]', tones[tone])}>
        <HugeiconsIcon icon={icon} size={28} strokeWidth={1.5} />
      </span>
      <h2 className="text-[22px] font-normal">{title}</h2>
      <div className="max-w-[44ch] text-sm leading-relaxed text-muted-foreground">{children}</div>
      {actions && <div className="mt-1 flex flex-wrap justify-center gap-2.5">{actions}</div>}
      {footer}
    </Panel>
  )
}
