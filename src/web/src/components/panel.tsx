import type { ComponentProps } from 'react'
import { cn } from '@/lib/utils'

/** The frosted card from the mockups. shadcn's Card is tighter; this is our page-level surface. */
export function Panel({ className, ...props }: ComponentProps<'div'>) {
  return (
    <div
      className={cn(
        'rounded-[28px] border border-white/90 bg-card backdrop-blur-sm dark:border-white/10',
        className,
      )}
      {...props}
    />
  )
}
