import type { ReactNode } from 'react'
import { cn } from '@/lib/utils'

export function FormField({ id, label, hint, error, children, className }: {
  id: string
  label: string
  hint?: string
  error?: string
  children: ReactNode
  className?: string
}) {
  return (
    <div className={cn('flex flex-col gap-1.5', className)}>
      <label htmlFor={id} className="text-[13px] font-semibold">{label}</label>
      {children}
      {error
        ? <p id={`${id}-error`} className="text-xs text-destructive">{error}</p>
        : hint && <p id={`${id}-hint`} className="text-xs text-muted-foreground">{hint}</p>}
    </div>
  )
}
