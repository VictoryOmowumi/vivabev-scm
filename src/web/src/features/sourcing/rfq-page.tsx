import { useState } from 'react'
import { Link, useParams } from 'react-router'
import { toast } from 'sonner'
import { HugeiconsIcon } from '@hugeicons/react'
import { ArrowLeft01Icon, Calendar03Icon, MailSend01Icon } from '@hugeicons/core-free-icons'
import { Panel } from '@/components/panel'
import { ErrorState } from '@/components/states/error-state'
import { Button } from '@/components/ui/button'
import { Skeleton } from '@/components/ui/skeleton'
import { useCurrentUser } from '@/features/auth/use-auth'
import { problemMessage } from '@/lib/api-client'
import { fieldClass } from '@/lib/field-class'
import { formatDate, formatDateTime, formatNaira } from '@/lib/format'
import { can, Permission } from '@/lib/permissions'
import { cn } from '@/lib/utils'
import { AwardStatus } from './award-status'
import { EventStatusBadge } from './event-status-badge'
import { Evaluation } from './evaluation'
import { QuoteComparison } from './quote-comparison'
import { RecordQuotation } from './record-quotation'
import { RfqSuppliers } from './rfq-suppliers'
import {
  useCancelEvent,
  useChangeDeadline,
  useCloseResponses,
  usePublish,
  useRemoveRequisition,
  useSourcingEvent,
  type SourcingEventDetail,
} from './sourcing-api'

export function RfqPage() {
  const { id } = useParams()
  const { data: user } = useCurrentUser()
  const rfq = useSourcingEvent(id)

  if (rfq.isPending) {
    return (
      <div className="flex flex-col gap-6" role="status" aria-label="Loading RFQ">
        <Skeleton className="h-10 w-1/2 rounded-full" />
        <Skeleton className="h-48 rounded-[28px]" />
        <Skeleton className="h-64 rounded-[28px]" />
      </div>
    )
  }
  if (rfq.isError) {
    return <ErrorState error={rfq.error} what="this RFQ" onRetry={() => rfq.refetch()} back={{ to: '/sourcing', label: 'Back to sourcing' }} />
  }

  return <Workspace rfq={rfq.data} manage={can(user, Permission.SourcingManage)} />
}

function Workspace({ rfq, manage }: { rfq: SourcingEventDetail; manage: boolean }) {
  const live = rfq.quotations.filter((q) => q.status === 'Received')

  return (
    <div className="flex flex-col gap-6">
      <section className="flex flex-col gap-3">
        <Link to="/sourcing" className="flex items-center gap-1.5 self-start text-sm font-semibold hover:text-brand">
          <HugeiconsIcon icon={ArrowLeft01Icon} size={18} strokeWidth={1.5} /> Sourcing
        </Link>
        <div className="flex flex-wrap items-end justify-between gap-5">
          <div>
            <p className="mb-1 text-[13px] font-semibold text-muted-foreground">{rfq.number} · buyer {rfq.buyerName}</p>
            <h1 className="text-[40px] leading-tight font-light tracking-tight">{rfq.title}</h1>
            <div className="mt-3 flex flex-wrap items-center gap-2 text-sm">
              <EventStatusBadge status={rfq.status} />
              <span className="text-muted-foreground">
                Quotes due {formatDate(rfq.responseDeadline)}
                {rfq.publishedAt && ` · published ${formatDateTime(rfq.publishedAt)}`}
                {rfq.responsesClosedAt && ` · closed ${formatDateTime(rfq.responsesClosedAt)}`}
              </span>
            </div>
          </div>
          <div className="text-right">
            <div className="text-[13px] text-muted-foreground">Estimated</div>
            <div className="font-heading text-[40px] leading-tight font-light">{formatNaira(rfq.estimatedTotal)}</div>
          </div>
        </div>
      </section>

      {rfq.status === 'Cancelled' && (
        <div role="status" className="rounded-2xl bg-cancelled px-5 py-4 text-sm text-cancelled-foreground">
          <strong>Cancelled:</strong> {rfq.cancellationReason} Its requisitions went back to waiting for sourcing.
        </div>
      )}

      {manage && <NextStep rfq={rfq} quoted={live.length} />}
      <AwardStatus rfq={rfq} manage={manage} />

      <section className="flex flex-wrap items-start gap-5">
        <div className="flex min-w-0 flex-[2_1_620px] flex-col gap-5">
          <Lines rfq={rfq} manage={manage} />
        </div>
        <RfqSuppliers rfq={rfq} manage={manage} />
      </section>

      {/* Full width: one column per supplier needs the room */}
      {rfq.status !== 'Draft' && <QuoteComparison rfq={rfq} manage={manage} />}
      {['ResponsesClosed', 'AwardPending', 'Awarded'].includes(rfq.status) && <Evaluation rfq={rfq} manage={manage} />}
      {manage && rfq.status === 'Published' && <RecordQuotation rfq={rfq} />}
    </div>
  )
}

/** The one thing to do next, with its button. */
function NextStep({ rfq, quoted }: { rfq: SourcingEventDetail; quoted: number }) {
  const publish = usePublish(rfq.id)
  const close = useCloseResponses(rfq.id)
  const changeDeadline = useChangeDeadline(rfq.id)
  const cancel = useCancelEvent(rfq.id)
  const [deadline, setDeadline] = useState(rfq.responseDeadline)
  const [cancelling, setCancelling] = useState(false)
  const [reason, setReason] = useState('')

  const invited = rfq.invitations.length
  const needed = rfq.needsCompetition ? rfq.minimumSuppliers : 1
  const error = publish.error ?? close.error ?? changeDeadline.error ?? cancel.error

  const run = async (action: () => Promise<unknown>, message: string) => {
    try { await action(); toast(message) } catch { /* shown below */ }
  }

  if (rfq.status === 'Cancelled' || rfq.status === 'Awarded' || rfq.status === 'AwardPending') return null

  return (
    <Panel className="flex flex-col gap-4 px-6 py-5">
      <div className="flex flex-wrap items-center justify-between gap-4">
        <div className="min-w-0">
          {rfq.status === 'Draft' && (
            <>
              <h2 className="text-xl font-normal">Invite suppliers, then publish</h2>
              <p className="mt-1 text-sm text-muted-foreground">
                {invited} of {needed} supplier{needed === 1 ? '' : 's'} invited
                {rfq.needsCompetition && ` · worth ${formatNaira(rfq.competitiveThreshold)} or more, so ${rfq.minimumSuppliers} are required`}
              </p>
            </>
          )}
          {rfq.status === 'Published' && (
            <>
              <h2 className="text-xl font-normal">Record quotations as they arrive</h2>
              <p className="mt-1 text-sm text-muted-foreground">{quoted} of {invited} quoted. Close responses when you're ready to evaluate.</p>
            </>
          )}
          {rfq.status === 'ResponsesClosed' && (
            <>
              <h2 className="text-xl font-normal">Score the quotations, then recommend a supplier</h2>
              <p className="mt-1 text-sm text-muted-foreground">Adjust the criteria if you need to, enter your scores below, and send your choice for approval.</p>
            </>
          )}
        </div>
        <div className="flex flex-wrap items-center gap-2">
          {(rfq.status === 'Draft' || rfq.status === 'Published') && (
            <label className="flex items-center gap-2 text-[13px]">
              <HugeiconsIcon icon={Calendar03Icon} size={16} strokeWidth={1.5} className="text-muted-foreground" />
              <span className="sr-only">Quotes due by</span>
              <input type="date" value={deadline} onChange={(e) => setDeadline(e.target.value)} className={cn(fieldClass, 'h-9 w-40')} />
              {deadline !== rfq.responseDeadline && (
                <Button size="pill-sm" variant="outline" disabled={changeDeadline.isPending}
                  onClick={() => run(() => changeDeadline.mutateAsync(deadline), 'Deadline changed')}>Save date</Button>
              )}
            </label>
          )}
          {rfq.status === 'Draft' && (
            <Button size="pill" variant="brand" disabled={invited < needed || publish.isPending}
              onClick={() => run(() => publish.mutateAsync(), `${rfq.number} published`)}>
              <HugeiconsIcon icon={MailSend01Icon} strokeWidth={1.5} /> {publish.isPending ? 'Publishing…' : 'Publish'}
            </Button>
          )}
          {rfq.status === 'Published' && (
            <Button size="pill" disabled={quoted === 0 || close.isPending}
              onClick={() => run(() => close.mutateAsync(), 'Responses closed')}>
              {close.isPending ? 'Closing…' : 'Close responses'}
            </Button>
          )}
          {!cancelling && (
            <Button size="pill-sm" variant="ghost" className="text-destructive hover:text-destructive" onClick={() => setCancelling(true)}>
              Cancel RFQ
            </Button>
          )}
        </div>
      </div>

      {cancelling && (
        <div className="flex flex-col gap-2.5 rounded-2xl bg-destructive/10 p-4">
          <label htmlFor="cancel-rfq" className="text-[13px] font-semibold text-destructive">
            Why cancel {rfq.number}? Its requisitions go back to waiting for sourcing.
          </label>
          <textarea id="cancel-rfq" rows={2} value={reason} maxLength={500} onChange={(e) => setReason(e.target.value)}
            className={cn(fieldClass, 'h-auto py-2.5')} placeholder="e.g. Requirement changed; re-tendering with a new spec" />
          <div className="flex justify-end gap-2">
            <Button size="pill-sm" variant="ghost" onClick={() => setCancelling(false)}>Keep it</Button>
            <Button size="pill-sm" variant="destructive" disabled={!reason.trim() || cancel.isPending}
              onClick={() => run(() => cancel.mutateAsync(reason.trim()), `${rfq.number} cancelled`)}>
              {cancel.isPending ? 'Cancelling…' : 'Cancel RFQ'}
            </Button>
          </div>
        </div>
      )}

      {error && <p role="alert" className="rounded-xl bg-destructive/10 px-3.5 py-3 text-[13px] text-destructive">{problemMessage(error)}</p>}
    </Panel>
  )
}

function Lines({ rfq, manage }: { rfq: SourcingEventDetail; manage: boolean }) {
  const remove = useRemoveRequisition(rfq.id)
  const requisitions = [...new Map(rfq.lines.map((l) => [l.requisitionId, l.requisitionNumber])).entries()]

  return (
    <Panel className="px-6 py-5">
      <div className="mb-2.5 flex flex-wrap items-baseline justify-between gap-2">
        <h2 className="text-xl font-normal">Lines</h2>
        <span className="text-[13px] text-muted-foreground">
          From {requisitions.map(([reqId, number], i) => (
            <span key={reqId}>{i > 0 && ', '}<Link to={`/requisitions/${reqId}`} className="font-semibold hover:text-brand">{number}</Link></span>
          ))}
        </span>
      </div>
      <div className="overflow-x-auto">
        <table className="w-full min-w-[600px] border-collapse text-sm">
          <thead>
            <tr className="text-left text-xs text-muted-foreground">
              <th scope="col" className="p-2.5 font-semibold">#</th>
              <th scope="col" className="p-2.5 font-semibold">Description</th>
              <th scope="col" className="p-2.5 text-right font-semibold">Qty</th>
              <th scope="col" className="p-2.5 text-right font-semibold">Estimated unit</th>
              <th scope="col" className="p-2.5 text-right font-semibold">Estimated total</th>
            </tr>
          </thead>
          <tbody>
            {rfq.lines.map((l) => (
              <tr key={l.id} className="border-t">
                <td className="p-2.5 text-muted-foreground">{l.lineNo}</td>
                <td className="p-2.5">
                  <div className="font-semibold">{l.description}</div>
                  <div className="text-xs text-muted-foreground">{l.itemCode ?? 'Free text'} · {l.requisitionNumber}</div>
                </td>
                <td className="p-2.5 text-right">{l.quantity} {l.uomCode}</td>
                <td className="p-2.5 text-right">{formatNaira(l.estimatedUnitPrice)}</td>
                <td className="p-2.5 text-right font-semibold">{formatNaira(l.estimatedTotal)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      {manage && rfq.status === 'Draft' && requisitions.length > 1 && (
        <div className="mt-3 flex flex-wrap items-center gap-2 text-[13px] text-muted-foreground">
          Take off:
          {requisitions.map(([reqId, number]) => (
            <Button key={reqId} size="pill-sm" variant="outline" disabled={remove.isPending}
              onClick={() => remove.mutate(reqId, { onSuccess: () => toast(`${number} taken off; it's waiting for sourcing again`) })}>
              {number}
            </Button>
          ))}
        </div>
      )}
      {remove.error && <p role="alert" className="mt-2 text-[13px] text-destructive">{problemMessage(remove.error)}</p>}
    </Panel>
  )
}
