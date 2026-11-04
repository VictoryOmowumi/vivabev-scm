import type { ReactNode } from 'react'
import { type IconSvgElement } from '@hugeicons/react'
import { InboxIcon } from '@hugeicons/core-free-icons'
import { Panel } from '@/components/panel'
import { Button } from '@/components/ui/button'
import { StateCard } from './state-card'

/** Nothing exists yet: say what goes here and offer the first step. */
export function EmptyState({ icon = InboxIcon, title, children, action }: {
  icon?: IconSvgElement
  title: string
  children: ReactNode
  action?: ReactNode
}) {
  return <StateCard icon={icon} title={title} actions={action}>{children}</StateCard>
}

/** Filters hide everything: say what was searched and offer to clear. */
export function NoMatches({ summary, onClear }: { summary: ReactNode; onClear: () => void }) {
  return (
    <Panel className="flex flex-wrap items-center justify-between gap-3 rounded-[22px] px-5 py-4">
      <span className="text-sm">{summary}</span>
      <Button size="pill-sm" variant="outline" onClick={onClear}>Clear filters</Button>
    </Panel>
  )
}
