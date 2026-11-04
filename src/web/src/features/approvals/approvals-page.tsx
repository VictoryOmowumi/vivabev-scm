import { Link, useSearchParams } from 'react-router'
import { HugeiconsIcon } from '@hugeicons/react'
import { CheckmarkCircle02Icon, SparklesIcon } from '@hugeicons/core-free-icons'
import { Panel } from '@/components/panel'
import { EmptyState } from '@/components/states/empty-state'
import { ErrorState } from '@/components/states/error-state'
import { Skeleton } from '@/components/ui/skeleton'
import { useRequisition } from '@/features/requisitions/requisitions-api'
import { formatDateTime, formatNaira } from '@/lib/format'
import { cn } from '@/lib/utils'
import { DecisionPanel } from './decision-panel'
import { useApprovalInbox, useDecidedApprovals, type ApprovalTask, type DecidedTask } from './approvals-api'

type Tab = 'waiting' | 'decided'

export function ApprovalsPage() {
  const [params, setParams] = useSearchParams()
  const tab: Tab = params.get('tab') === 'decided' ? 'decided' : 'waiting'
  const inbox = useApprovalInbox()
  const decided = useDecidedApprovals()

  const active = tab === 'waiting' ? inbox : decided
  const items: (ApprovalTask | DecidedTask)[] = active.data ?? []
  // The selected item lives in the URL. If it's gone (you just approved it), fall back to the first.
  const selected = items.find((i) => i.requestId === params.get('item')) ?? items[0]

  const select = (next: { tab?: Tab; item?: string }) =>
    setParams((current) => {
      const p = new URLSearchParams(current)
      if (next.tab) { p.set('tab', next.tab); p.delete('item') }
      if (next.item) p.set('item', next.item)
      return p
    }, { replace: true })

  return (
    <div className="flex flex-col gap-6">
      <section className="flex flex-wrap items-end justify-between gap-5">
        <div>
          <h1 className="text-[40px] leading-tight font-light tracking-tight">Approvals</h1>
          <p className="mt-2 text-[15px] text-muted-foreground">
            {tab === 'waiting' ? 'Requests waiting for your decision, oldest first' : 'What you decided in the last 30 days'}
          </p>
        </div>
        <div role="tablist" aria-label="Approvals view"
          className="flex gap-1 rounded-full border border-white/90 bg-card p-1.5 dark:border-white/10">
          {([['waiting', 'Waiting for me', inbox.data?.length], ['decided', 'Decided · 30 days', decided.data?.length]] as const)
            .map(([value, label, count]) => (
              <button key={value} role="tab" aria-selected={tab === value} onClick={() => select({ tab: value })}
                className={cn('rounded-full px-4 py-2 text-sm font-medium',
                  tab === value ? 'bg-inverse font-semibold text-inverse-foreground' : 'hover:bg-accent')}>
                {label}{count !== undefined && ` · ${count}`}
              </button>
            ))}
        </div>
      </section>

      {active.isPending && (
        <div className="flex flex-wrap gap-5" role="status" aria-label="Loading approvals">
          <div className="flex flex-[1_1_440px] flex-col gap-2.5">
            {[0, 1, 2].map((i) => <Skeleton key={i} className="h-24 rounded-3xl" />)}
          </div>
          <Skeleton className="h-[480px] flex-[1_1_460px] rounded-[28px]" />
        </div>
      )}

      {active.isError && <ErrorState error={active.error} what="your approvals" onRetry={() => active.refetch()} />}

      {active.data?.length === 0 && (
        tab === 'waiting'
          ? <EmptyState icon={CheckmarkCircle02Icon} title="Nothing is waiting for you">
              When someone in your team submits a requisition that needs your approval, it appears here.
            </EmptyState>
          : <EmptyState title="No decisions in the last 30 days">Requests you approve or reject show up here.</EmptyState>
      )}

      {selected && (
        <section className="flex flex-wrap items-start gap-5">
          <ul className="flex min-w-0 flex-[1_1_440px] flex-col gap-2.5">
            {items.map((item) => {
              const on = item.requestId === selected.requestId
              return (
                <li key={item.requestId}>
                  <button aria-pressed={on} onClick={() => select({ item: item.requestId })}
                    className={cn('flex w-full items-center gap-4 rounded-3xl border p-4 text-left',
                      on ? 'border-inverse bg-inverse text-inverse-foreground'
                        : 'border-white/90 bg-card hover:bg-accent dark:border-white/10')}>
                    <span className="min-w-0 flex-1">
                      <span className="block text-[15px] font-bold">{item.title}</span>
                      <span className={cn('mt-1 block text-[13px]', on ? 'text-inverse-muted' : 'text-muted-foreground')}>
                        {item.documentNumber} · {item.requestedByName}
                      </span>
                      <span className={cn('mt-0.5 block text-[13px]', on ? 'text-inverse-muted' : 'text-muted-foreground')}>
                        {'stepName' in item
                          ? `${item.stepName} · step ${item.stepNumber} of ${item.totalSteps}`
                          : `${item.myDecision} ${formatDateTime(item.decidedAt)}`}
                      </span>
                    </span>
                    <span className="flex flex-none flex-col items-end gap-1.5">
                      <span className="font-heading text-xl">{formatNaira(item.amount)}</span>
                      {item.overBudget && (
                        <span className={cn('rounded-full px-2.5 py-0.5 text-[11px] font-bold',
                          on ? 'bg-[#f3c9a6] text-[#5a2a08]' : 'bg-over text-over-foreground')}>Over budget</span>
                      )}
                    </span>
                  </button>
                </li>
              )
            })}
          </ul>

          <Preview key={selected.requestId} item={selected} />
        </section>
      )}
    </div>
  )
}

/** The right-hand card: the requisition behind the selected item, plus approve and reject. */
function Preview({ item }: { item: ApprovalTask | DecidedTask }) {
  const requisition = useRequisition(item.documentId)
  const waiting = 'stepName' in item

  return (
    <Panel aria-label="Selected request" className="flex min-w-0 flex-[1_1_460px] flex-col gap-4 p-6">
      <div>
        <p className="text-[13px] font-semibold text-muted-foreground">
          {item.documentNumber} · {waiting ? `submitted ${formatDateTime(item.requestedAt)}` : `you ${item.myDecision.toLowerCase()} it`}
        </p>
        <h2 className="mt-1.5 text-[28px] leading-tight font-light">{item.title}</h2>
      </div>

      {requisition.isPending && <Skeleton className="h-48 rounded-2xl" />}
      {requisition.isError && <p className="text-sm text-destructive">Couldn't load the requisition: {requisition.error.message}</p>}

      {requisition.data && (() => {
        const r = requisition.data
        const available = r.budgetAvailableAtSubmit
        return (
          <>
            <dl className="grid grid-cols-2 gap-x-5 gap-y-3 text-sm">
              <div><dt className="text-xs text-muted-foreground">Requester</dt><dd className="font-semibold">{r.requesterName}</dd></div>
              <div><dt className="text-xs text-muted-foreground">Estimated total</dt><dd className="font-semibold">{formatNaira(r.estimatedTotal)}</dd></div>
              <div><dt className="text-xs text-muted-foreground">Cost centre</dt><dd className="font-semibold">{r.costCentreCode}</dd></div>
              <div><dt className="text-xs text-muted-foreground">Lines</dt><dd className="font-semibold">{r.lines.length}</dd></div>
            </dl>
            <div className="rounded-2xl bg-surface px-4 py-3.5 text-sm leading-relaxed">
              <div className="mb-1 text-xs font-semibold text-muted-foreground">Lines</div>
              {r.lines.map((l) => `${l.quantity} × ${l.description} (${formatNaira(l.lineTotal)})`).join('; ')}
            </div>
            {available !== null && (
              <div className={cn('rounded-2xl px-4 py-3.5 text-sm leading-relaxed',
                r.budgetStatus === 'OverBudget' ? 'bg-over text-over-foreground' : 'bg-success text-success-foreground')}>
                <div className="mb-1 text-xs font-bold">Budget</div>
                {formatNaira(available)} was available in {r.costCentreCode} at submit.{' '}
                {r.budgetStatus === 'OverBudget'
                  ? `This requisition is ${formatNaira(r.estimatedTotal - available)} over, so a Finance controller step was added.`
                  : 'It fits within that.'}
              </div>
            )}
          </>
        )
      })()}

      {waiting ? (
        <>
          <Link to="/copilot" className="flex items-center gap-2.5 rounded-[18px] border border-dashed border-input px-3.5 py-3 text-[13px]">
            <HugeiconsIcon icon={SparklesIcon} size={18} strokeWidth={1.5} className="flex-none text-brand" />
            Ask Copilot about {item.documentNumber}
          </Link>
          <DecisionPanel requestId={item.requestId} documentNumber={item.documentNumber}
            requesterName={item.requestedByName} />
        </>
      ) : (
        item.myComment && <p className="text-sm"><span className="text-muted-foreground">Your comment:</span> “{item.myComment}”</p>
      )}

      <Link to={`/requisitions/${item.documentId}`} className="text-center text-sm font-semibold hover:text-brand">
        Open the full requisition
      </Link>
    </Panel>
  )
}
