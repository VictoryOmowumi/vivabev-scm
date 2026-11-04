import { Link, useParams } from 'react-router'
import { HugeiconsIcon } from '@hugeicons/react'
import { ArrowLeft01Icon, SparklesIcon } from '@hugeicons/core-free-icons'
import { Panel } from '@/components/panel'
import { ErrorState } from '@/components/states/error-state'
import { Skeleton } from '@/components/ui/skeleton'
import { DecisionPanel } from '@/features/approvals/decision-panel'
import { useCurrentUser } from '@/features/auth/use-auth'
import { formatDate, formatDateTime, formatNaira } from '@/lib/format'
import { ApprovalChain } from './approval-chain'
import { BudgetCheck } from './budget-check'
import { currentStep, useRequisition, useRequisitionApproval } from './requisitions-api'
import { BudgetBadge, StatusBadge } from './status-badges'

const back = { to: '/requisitions', label: 'Back to requisitions' }

export function RequisitionDetailPage() {
  const { id } = useParams()
  const { data: user } = useCurrentUser()
  const requisition = useRequisition(id)
  const approval = useRequisitionApproval(id)

  if (requisition.isPending) return <DetailSkeleton />
  if (requisition.isError) {
    return (
      <div className="flex flex-col gap-4">
        <BackLink />
        <ErrorState error={requisition.error} what="this requisition" back={back}
          onRetry={() => requisition.refetch()} />
      </div>
    )
  }

  const r = requisition.data
  const step = currentStep(approval.data)
  const waitingForMe = !!step && step.approverId === user?.id
  const over = r.budgetAvailableAtSubmit !== null ? r.estimatedTotal - r.budgetAvailableAtSubmit : 0
  const rejectedBy = approval.data?.steps.find((s) => s.decision === 'Rejected')

  return (
    <div className="flex flex-col gap-6">
      <section className="flex flex-col gap-3">
        <BackLink />
        <div className="flex flex-wrap items-end justify-between gap-5">
          <div>
            <p className="mb-1 text-[13px] font-semibold text-muted-foreground">{r.number}</p>
            <h1 className="text-[40px] leading-tight font-light tracking-tight">{r.title}</h1>
            <div className="mt-3 flex flex-wrap items-center gap-2">
              <StatusBadge status={r.status} returned={!!r.rejectionReason}
                suffix={step && approval.data ? `step ${step.sequence} of ${approval.data.steps.length}` : undefined} />
              {r.budgetStatus === 'OverBudget' && (
                <BudgetBadge status="OverBudget" label={`Over budget by ${formatNaira(over)}`} />
              )}
              <Link to="/copilot"
                className="ml-1 inline-flex items-center gap-1.5 rounded-full border px-3 py-1 text-xs font-semibold hover:text-brand">
                <HugeiconsIcon icon={SparklesIcon} size={14} strokeWidth={1.5} /> Ask Copilot about this request
              </Link>
            </div>
          </div>
          <div className="text-right">
            <div className="text-[13px] text-muted-foreground">Estimated total</div>
            <div className="font-heading text-[40px] leading-tight font-light">{formatNaira(r.estimatedTotal)}</div>
          </div>
        </div>
      </section>

      {r.status === 'Draft' && r.rejectionReason && (
        <div role="status" className="rounded-2xl bg-over px-5 py-4 text-sm text-over-foreground">
          <strong>Returned{rejectedBy ? ` by ${rejectedBy.approverName}` : ''}:</strong> {r.rejectionReason}
        </div>
      )}
      {r.status === 'Cancelled' && r.cancellationReason && (
        <div role="status" className="rounded-2xl bg-cancelled px-5 py-4 text-sm text-cancelled-foreground">
          <strong>Cancelled:</strong> {r.cancellationReason}
        </div>
      )}

      <section className="grid grid-cols-[repeat(auto-fit,minmax(200px,1fr))] gap-3">
        <Meta label="Requester" value={r.requesterName} />
        <Meta label="Cost centre" value={r.costCentreCode} />
        <Meta label="Deliver to" value={r.deliverToSiteCode} />
        <Meta label="Required by" value={formatDate(r.requiredBy)}
          sub={r.submittedAt ? `Submitted ${formatDateTime(r.submittedAt)}` : 'Not submitted yet'} />
      </section>

      <section className="flex flex-wrap items-start gap-5">
        <div className="flex min-w-0 flex-[2_1_620px] flex-col gap-5">
          <Panel className="px-6 py-5">
            <h2 className="mb-2.5 text-xl font-normal">Lines</h2>
            <div className="overflow-x-auto">
              <table className="w-full min-w-[600px] border-collapse text-sm">
                <thead>
                  <tr className="text-left text-xs text-muted-foreground">
                    <th scope="col" className="p-2.5 font-semibold">#</th>
                    <th scope="col" className="p-2.5 font-semibold">Description</th>
                    <th scope="col" className="p-2.5 font-semibold">Type</th>
                    <th scope="col" className="p-2.5 text-right font-semibold">Qty</th>
                    <th scope="col" className="p-2.5 text-right font-semibold">Unit price</th>
                    <th scope="col" className="p-2.5 text-right font-semibold">Line total</th>
                  </tr>
                </thead>
                <tbody>
                  {r.lines.map((l) => (
                    <tr key={l.lineNo} className="border-t">
                      <td className="p-2.5 text-muted-foreground">{l.lineNo}</td>
                      <td className="p-2.5">
                        <div className="font-semibold">{l.description}</div>
                        <div className="text-xs text-muted-foreground">{l.itemCode ?? 'Free text'}</div>
                      </td>
                      <td className="p-2.5">{l.lineType}</td>
                      <td className="p-2.5 text-right">{l.quantity} {l.uomCode}</td>
                      <td className="p-2.5 text-right">{formatNaira(l.estimatedUnitPrice)}</td>
                      <td className="p-2.5 text-right font-semibold">{formatNaira(l.lineTotal)}</td>
                    </tr>
                  ))}
                  {r.lines.length === 0 && (
                    <tr className="border-t"><td colSpan={6} className="p-2.5 text-muted-foreground">No lines yet.</td></tr>
                  )}
                </tbody>
                <tfoot>
                  <tr className="border-t-[1.5px] border-foreground">
                    <td colSpan={5} className="p-2.5 font-semibold">Estimated total</td>
                    <td className="p-2.5 text-right font-bold">{formatNaira(r.estimatedTotal)}</td>
                  </tr>
                </tfoot>
              </table>
            </div>
          </Panel>

          <Panel className="px-6 py-5">
            <h2 className="mb-2 text-xl font-normal">Justification</h2>
            <p className="max-w-[70ch] text-[15px] leading-relaxed">{r.justification}</p>
          </Panel>

          <BudgetCheck requisition={r} />
        </div>

        <aside aria-label="Approval"
          className="flex min-w-0 flex-[1_1_360px] flex-col gap-4 rounded-[28px] bg-inverse p-6 text-inverse-foreground">
          <div className="flex items-center justify-between">
            <h2 className="text-xl font-normal">Approval chain</h2>
            {approval.data && (
              <span className="font-heading text-[28px] leading-none font-light">
                {approval.data.steps.filter((s) => s.decision === 'Approved').length}/{approval.data.steps.length}
              </span>
            )}
          </div>

          {approval.isPending && <Skeleton className="h-40 rounded-2xl bg-inverse-row" />}
          {approval.isError && <p className="text-sm text-inverse-muted">Couldn't load the approval chain.</p>}
          {approval.data === null && (
            <p className="text-sm text-inverse-muted">Not submitted yet. The chain is worked out when it is.</p>
          )}
          {approval.data && user && <ApprovalChain approval={approval.data} meId={user.id} />}

          {waitingForMe && approval.data && (
            <>
              <Link to="/copilot" className="flex items-center gap-2.5 rounded-[18px] bg-inverse-row px-3.5 py-3 text-[13px]">
                <HugeiconsIcon icon={SparklesIcon} size={18} strokeWidth={1.5} className="flex-none text-chart-2" />
                <span><strong>Not sure?</strong> Ask Copilot why it's over budget, or have it draft your comment.</span>
              </Link>
              <DecisionPanel onInverse requestId={approval.data.requestId}
                documentNumber={r.number} requesterName={r.requesterName} />
            </>
          )}
        </aside>
      </section>
    </div>
  )
}

function BackLink() {
  return (
    <Link to="/requisitions" className="flex items-center gap-1.5 self-start text-sm font-semibold hover:text-brand">
      <HugeiconsIcon icon={ArrowLeft01Icon} size={18} strokeWidth={1.5} /> Requisitions
    </Link>
  )
}

function Meta({ label, value, sub }: { label: string; value: string; sub?: string }) {
  return (
    <Panel className="rounded-[22px] px-[18px] py-3.5">
      <div className="text-xs text-muted-foreground">{label}</div>
      <div className="mt-1 text-[15px] font-semibold">{value}</div>
      {sub && <div className="text-xs text-muted-foreground">{sub}</div>}
    </Panel>
  )
}

function DetailSkeleton() {
  return (
    <div className="flex flex-col gap-6" role="status" aria-label="Loading requisition">
      <Skeleton className="h-4 w-28 rounded-full" />
      <Skeleton className="h-10 w-2/3 rounded-full" />
      <div className="grid grid-cols-4 gap-3">
        {[0, 1, 2, 3].map((i) => <Skeleton key={i} className="h-20 rounded-[22px]" />)}
      </div>
      <div className="flex gap-5">
        <Skeleton className="h-72 flex-[2] rounded-[28px]" />
        <Skeleton className="h-72 flex-1 rounded-[28px]" />
      </div>
    </div>
  )
}
