import { useState } from 'react'
import { Link, useParams } from 'react-router'
import { toast } from 'sonner'
import { HugeiconsIcon } from '@hugeicons/react'
import { ArrowLeft01Icon } from '@hugeicons/core-free-icons'
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
import { StockRequestStatusBadge } from './stock-request-status-badge'
import { isOutstanding, useIssueStock, useStockRequest, useStockRequestAction, type StockRequestDetail } from './stock-api'

export function StockRequestPage() {
  const { id } = useParams()
  const request = useStockRequest(id)
  if (request.isPending) return <Skeleton className="h-96 rounded-[28px]" />
  if (request.isError) {
    return <ErrorState error={request.error} what="this stock request" onRetry={() => request.refetch()} back={{ to: '/stock-requests', label: 'Back to stock requests' }} />
  }
  return <Request r={request.data} />
}

function Request({ r }: { r: StockRequestDetail }) {
  const { data: user } = useCurrentUser()
  const issuer = can(user, Permission.StockIssue) && isOutstanding(r.status)
  const mine = user?.id === r.requestedById
  const back = can(user, Permission.InventoryView) ? { to: '/warehouse?tab=requests', label: 'Warehouse' } : { to: '/stock-requests', label: 'Stock requests' }

  return (
    <div className="flex flex-col gap-6">
      <section className="flex flex-col gap-3">
        <Link to={back.to} className="flex items-center gap-1.5 self-start text-sm font-semibold hover:text-brand">
          <HugeiconsIcon icon={ArrowLeft01Icon} size={18} strokeWidth={1.5} /> {back.label}
        </Link>
        <div>
          <p className="mb-1 text-[13px] font-semibold text-muted-foreground">{r.number} · asked {formatDateTime(r.requestedAt)}</p>
          <h1 className="text-[40px] leading-tight font-light tracking-tight">{r.purpose}</h1>
          <div className="mt-3 flex flex-wrap items-center gap-2 text-sm">
            <StockRequestStatusBadge status={r.status} />
            <span className="text-muted-foreground">
              {r.requestedByName} · charged to {r.departmentCode} · {r.costCentreCode} {r.costCentreName} · from {r.siteCode} {r.siteName}
              {r.neededBy && ` · needed by ${formatDate(r.neededBy)}`}
            </span>
          </div>
        </div>
      </section>

      <Decision r={r} />

      {issuer ? <IssueForm r={r} /> : <Lines r={r} />}

      {mine && r.status === 'Open' && <CancelButton id={r.id} />}

      {r.issues.length > 0 && (
        <Panel className="px-6 py-5">
          <h2 className="mb-3 text-xl font-normal">Issued</h2>
          <ul className="flex flex-col divide-y text-sm">
            {r.issues.map((i, k) => (
              <li key={k} className="flex flex-wrap items-center gap-3 py-2.5">
                <span className="font-semibold">{i.quantity} {i.uomCode} · {i.description}</span>
                <span className="min-w-0 flex-1 text-xs text-muted-foreground">{i.itemCode} · by {i.issuedByName}, {formatDateTime(i.issuedAt)}</span>
                <span className="text-muted-foreground">{formatNaira(i.value)} at cost</span>
              </li>
            ))}
          </ul>
        </Panel>
      )}
    </div>
  )
}

function Decision({ r }: { r: StockRequestDetail }) {
  if (!r.decisionReason && r.status !== 'Cancelled') return null
  const verb = r.status === 'Rejected' ? 'Rejected' : r.status === 'Closed' ? 'Closed short' : 'Cancelled'
  return (
    <Panel className="px-6 py-4 text-sm">
      <span className="font-semibold">{verb}</span>
      <span className="text-muted-foreground"> by {r.decidedByName}{r.decidedAt && `, ${formatDateTime(r.decidedAt)}`}</span>
      {r.decisionReason && <>: “{r.decisionReason}”</>}
    </Panel>
  )
}

function Lines({ r }: { r: StockRequestDetail }) {
  return (
    <Panel className="px-6 py-5">
      <h2 className="mb-3 text-xl font-normal">Items</h2>
      <table className="w-full border-collapse text-sm">
        <thead>
          <tr className="text-left text-xs text-muted-foreground">
            <th scope="col" className="p-2 font-semibold">Item</th>
            <th scope="col" className="p-2 text-right font-semibold">Asked for</th>
            <th scope="col" className="p-2 text-right font-semibold">Issued</th>
            <th scope="col" className="p-2 text-right font-semibold">To come</th>
          </tr>
        </thead>
        <tbody>
          {r.lines.map((l) => (
            <tr key={l.id} className="border-t">
              <th scope="row" className="p-2 text-left font-normal"><span className="font-semibold">{l.description}</span><span className="block text-xs text-muted-foreground">{l.itemCode}</span></th>
              <td className="p-2 text-right">{l.requested} {l.uomCode}</td>
              <td className="p-2 text-right">{l.issued} {l.uomCode}</td>
              <td className="p-2 text-right font-semibold">{isOutstanding(r.status) ? `${l.outstanding} ${l.uomCode}` : '–'}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </Panel>
  )
}

/** The warehouse issues what it can now; quantities start at what's asked for, capped at what's on the shelf. */
function IssueForm({ r }: { r: StockRequestDetail }) {
  const issue = useIssueStock(r.id)
  const action = useStockRequestAction(r.id)
  const [qty, setQty] = useState<Record<string, string>>(() =>
    Object.fromEntries(r.lines.map((l) => [l.id, String(Math.max(Math.min(l.outstanding, l.onHand), 0))])))
  const [deciding, setDeciding] = useState(false)
  const [reason, setReason] = useState('')
  const num = (v: string | undefined) => (!v || v.trim() === '' ? 0 : Number(v))
  const toIssue = r.lines.filter((l) => num(qty[l.id]) > 0)
  const invalid = r.lines.some((l) => num(qty[l.id]) > Math.min(l.outstanding, l.onHand))
  const decision = r.status === 'Open' ? 'reject' : 'close'
  const error = issue.error ?? action.error

  return (
    <Panel className="flex flex-col gap-4 px-6 py-5">
      <div>
        <h2 className="text-xl font-normal">Issue</h2>
        <p className="mt-1 text-sm text-muted-foreground">Issue what you can now. Anything left stays open until you issue it or close the request.</p>
      </div>
      <div className="overflow-x-auto">
        <table className="w-full min-w-[640px] border-collapse text-sm">
          <thead>
            <tr className="text-left text-xs text-muted-foreground">
              <th scope="col" className="p-2 font-semibold">Item</th>
              <th scope="col" className="p-2 text-right font-semibold">Asked for</th>
              <th scope="col" className="p-2 text-right font-semibold">Issued</th>
              <th scope="col" className="p-2 text-right font-semibold">On the shelf</th>
              <th scope="col" className="w-36 p-2 font-semibold">Issue now</th>
            </tr>
          </thead>
          <tbody>
            {r.lines.map((l) => {
              const max = Math.max(Math.min(l.outstanding, l.onHand), 0)
              const bad = num(qty[l.id]) > max
              return (
                <tr key={l.id} className={cn('border-t align-top', l.outstanding === 0 && 'text-muted-foreground')}>
                  <th scope="row" className="p-2 text-left font-normal"><span className="font-semibold">{l.description}</span><span className="block text-xs text-muted-foreground">{l.itemCode}</span></th>
                  <td className="p-2 text-right">{l.requested} {l.uomCode}</td>
                  <td className="p-2 text-right">{l.issued}</td>
                  <td className={cn('p-2 text-right', l.onHand < l.outstanding && 'font-semibold text-over-foreground')}>{l.onHand}</td>
                  <td className="p-2">
                    {l.outstanding === 0 ? <span className="text-xs">Done</span> : (
                      <>
                        <input type="number" min={0} max={max} step="any" aria-label={`Issue ${l.description}`} value={qty[l.id] ?? ''}
                          onChange={(e) => setQty((q) => ({ ...q, [l.id]: e.target.value }))} aria-invalid={bad} className={cn(fieldClass, 'h-9 text-right')} />
                        {bad && <span className="mt-1 block text-xs text-destructive">At most {max}</span>}
                      </>
                    )}
                  </td>
                </tr>
              )
            })}
          </tbody>
        </table>
      </div>

      {deciding && (
        <div className="flex flex-col gap-2.5 rounded-2xl bg-destructive/10 p-4">
          <label htmlFor="sr-reason" className="text-[13px] font-semibold">
            {decision === 'reject' ? 'Why turn it down? The requester sees this.' : "Why won't the rest be issued?"}
          </label>
          <textarea id="sr-reason" rows={2} maxLength={500} value={reason} onChange={(e) => setReason(e.target.value)} className={cn(fieldClass, 'h-auto py-2.5')}
            placeholder={decision === 'reject' ? 'e.g. These are reserved for the Kano plant rollout' : 'e.g. Remaining units are reserved; raise a requisition for more'} />
          <div className="flex justify-end gap-2">
            <Button type="button" size="pill-sm" variant="ghost" onClick={() => setDeciding(false)}>Back</Button>
            <Button type="button" size="pill-sm" variant="destructive" disabled={!reason.trim() || action.isPending}
              onClick={() => action.mutate({ action: decision, reason: reason.trim() }, { onSuccess: () => { toast(decision === 'reject' ? 'Request rejected' : 'Request closed'); setDeciding(false) } })}>
              {decision === 'reject' ? 'Reject request' : 'Close request'}
            </Button>
          </div>
        </div>
      )}

      {error && <p role="alert" className="rounded-xl bg-destructive/10 px-3.5 py-3 text-[13px] text-destructive">{problemMessage(error)}</p>}

      <div className="flex flex-wrap items-center justify-end gap-2">
        {!deciding && (
          <Button type="button" size="pill" variant="ghost" className="text-destructive hover:text-destructive" onClick={() => setDeciding(true)}>
            {decision === 'reject' ? 'Reject' : 'Close short'}
          </Button>
        )}
        <Button type="button" size="pill" variant="brand" disabled={toIssue.length === 0 || invalid || issue.isPending}
          onClick={() => issue.mutate(toIssue.map((l) => ({ lineId: l.id, quantity: num(qty[l.id]) })), { onSuccess: () => toast('Stock issued') })}>
          {issue.isPending ? 'Issuing…' : 'Issue stock'}
        </Button>
      </div>
    </Panel>
  )
}

function CancelButton({ id }: { id: string }) {
  const action = useStockRequestAction(id)
  return (
    <div className="flex items-center justify-end gap-3">
      {action.error && <span role="alert" className="text-[13px] text-destructive">{problemMessage(action.error)}</span>}
      <Button size="pill" variant="outline" disabled={action.isPending}
        onClick={() => action.mutate({ action: 'cancel' }, { onSuccess: () => toast('Request cancelled') })}>
        Cancel request
      </Button>
    </div>
  )
}
