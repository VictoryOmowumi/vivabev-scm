import { Link } from 'react-router'
import { HugeiconsIcon } from '@hugeicons/react'
import { ArrowUpRight01Icon } from '@hugeicons/core-free-icons'
import { Badge } from '@/components/ui/badge'
import { useApprovalInbox } from '@/features/approvals/approvals-api'
import { useCurrentUser } from '@/features/auth/use-auth'
import { formatNaira } from '@/lib/format'

function greeting(now = new Date()) {
  const hour = now.getHours()
  return hour < 12 ? 'Good morning' : hour < 17 ? 'Good afternoon' : 'Good evening'
}

export function DashboardPage() {
  const { data: user } = useCurrentUser()
  const inbox = useApprovalInbox()
  const firstName = user?.displayName.split(' ')[0] ?? ''

  return (
    <div className="flex flex-col gap-6">
      <section>
        <h1 className="text-[40px] leading-tight font-light tracking-tight">
          {greeting()}, {firstName}
        </h1>
        <p className="mt-2 text-[15px] text-muted-foreground">Here is what needs you today.</p>
      </section>

      <section aria-labelledby="waiting-heading" className="flex max-w-xl flex-col gap-4 rounded-2xl bg-inverse p-6 text-inverse-foreground">
        <div className="flex items-baseline justify-between">
          <h2 id="waiting-heading" className="text-xl font-normal">Waiting for you</h2>
          <span className="font-heading text-4xl font-light">{inbox.data?.length ?? '–'}</span>
        </div>

        {inbox.isPending && <p className="text-sm text-inverse-muted">Loading…</p>}
        {inbox.isError && <p className="text-sm text-inverse-muted">Couldn't load approvals: {inbox.error.message}</p>}
        {inbox.data?.length === 0 && <p className="text-sm text-inverse-muted">Nothing waiting. Nice.</p>}

        <ul className="flex flex-col gap-1.5">
          {inbox.data?.slice(0, 3).map((task) => (
            <li key={task.requestId} className="flex items-center gap-3 rounded-xl bg-inverse-row p-3">
              <div className="min-w-0 flex-1">
                <p className="truncate text-sm font-semibold">{task.title}</p>
                <p className="text-xs text-inverse-muted">
                  {task.documentNumber} · {task.requestedByName} · step {task.stepNumber} of {task.totalSteps}
                </p>
              </div>
              <div className="flex flex-col items-end gap-1">
                <span className="text-sm font-semibold">{formatNaira(task.amount)}</span>
                {task.overBudget && <Badge className="bg-[#f3c9a6] text-[#5a2a08]">Over budget</Badge>}
              </div>
            </li>
          ))}
        </ul>

        <Link to="/approvals" className="flex items-center gap-1 self-start text-sm font-semibold text-inverse-foreground hover:underline">
          Open approvals <HugeiconsIcon icon={ArrowUpRight01Icon} size={16} />
        </Link>
      </section>
    </div>
  )
}
