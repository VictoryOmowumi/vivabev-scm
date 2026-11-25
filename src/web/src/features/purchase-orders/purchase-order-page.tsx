import { useState, type ReactNode } from 'react'
import { FieldSelect } from '@/components/field-select'
import { Link, useParams } from 'react-router'
import { toast } from 'sonner'
import { HugeiconsIcon } from '@hugeicons/react'
import { ArrowLeft01Icon, MailSend01Icon, PrinterIcon } from '@hugeicons/core-free-icons'
import { Panel } from '@/components/panel'
import { ErrorState } from '@/components/states/error-state'
import { Button } from '@/components/ui/button'
import { Skeleton } from '@/components/ui/skeleton'
import { useCurrentUser } from '@/features/auth/use-auth'
import { useSites } from '@/features/requisitions/requisition-editor-api'
import { problemMessage } from '@/lib/api-client'
import { fieldClass } from '@/lib/field-class'
import { formatDate, formatDateTime, formatNaira } from '@/lib/format'
import { can, Permission } from '@/lib/permissions'
import { cn } from '@/lib/utils'
import { InvoiceStatusBadge } from '@/features/invoices/invoice-status-badge'
import { useInvoices } from '@/features/invoices/invoices-api'
import { useReceipts } from '@/features/warehouse/warehouse-api'
import { OrderStatusBadge } from './order-status-badge'
import { useCancelOrder, useCloseOrder, useIssueOrder, usePurchaseOrder, useUpdateDelivery, type PurchaseOrderDetail } from './purchase-orders-api'

export function PurchaseOrderPage() {
  const { id } = useParams()
  const { data: user } = useCurrentUser()
  const order = usePurchaseOrder(id)

  if (order.isPending) {
    return (
      <div className="flex flex-col gap-6" role="status" aria-label="Loading purchase order">
        <Skeleton className="h-10 w-1/3 rounded-full" />
        <Skeleton className="h-[480px] rounded-[28px]" />
      </div>
    )
  }
  if (order.isError) {
    return <ErrorState error={order.error} what="this purchase order" onRetry={() => order.refetch()} back={{ to: '/purchase-orders', label: 'Back to orders' }} />
  }

  const po = order.data
  const manage = can(user, Permission.PurchaseOrdersManage)

  return (
    <div className="flex flex-col gap-6">
      <section className="flex flex-wrap items-end justify-between gap-4 print:hidden">
        <div className="flex flex-col gap-2">
          <Link to="/purchase-orders" className="flex items-center gap-1.5 self-start text-sm font-semibold hover:text-brand">
            <HugeiconsIcon icon={ArrowLeft01Icon} size={18} strokeWidth={1.5} /> Purchase orders
          </Link>
          <div className="flex flex-wrap items-center gap-3">
            <h1 className="text-[40px] leading-tight font-light tracking-tight">{po.number}</h1>
            <OrderStatusBadge status={po.status} />
          </div>
          <p className="text-sm text-muted-foreground">
            {po.supplier.name} · from{' '}
            {can(user, Permission.SourcingView)
              ? <Link to={`/sourcing/${po.sourcingEventId}`} className="font-semibold text-foreground hover:text-brand">{po.sourcingEventNumber}</Link>
              : po.sourcingEventNumber}
            {po.issuedAt && ` · issued ${formatDateTime(po.issuedAt)}`}
          </p>
        </div>
        {po.status !== 'Draft' && po.status !== 'Cancelled' && (
          <Button size="pill" variant="outline" onClick={() => window.print()}>
            <HugeiconsIcon icon={PrinterIcon} strokeWidth={1.5} /> Print or save as PDF
          </Button>
        )}
      </section>

      {(po.status === 'Cancelled' || po.status === 'Closed') && (
        <div role="status" className="rounded-2xl bg-cancelled px-5 py-4 text-sm text-cancelled-foreground print:hidden">
          <strong>{po.status === 'Closed' ? 'Closed short' : 'Cancelled'}</strong>
          {po.closedByName && ` by ${po.closedByName}${po.closedAt ? `, ${formatDateTime(po.closedAt)}` : ''}`}: {po.cancellationReason}{' '}
          {po.status === 'Closed'
            ? 'What arrived can still be invoiced; the rest was taken off the budget.'
            : po.issuedAt ? 'Its budget was released.' : 'The RFQ is still awarded, so a new order can be raised from it.'}
        </div>
      )}

      {manage && po.status === 'Draft' && <DraftActions po={po} />}
      {manage && (po.status === 'Issued' || po.status === 'PartiallyReceived') && <IssuedActions po={po} />}
      {(po.status === 'PartiallyReceived' || po.status === 'Received' || po.status === 'Closed') && can(user, Permission.InventoryView) && <Receipts po={po} />}
      {po.status !== 'Draft' && po.status !== 'Cancelled' && can(user, Permission.InvoicesView) && (
        <Invoices po={po} canRecord={can(user, Permission.InvoicesManage)} />
      )}

      <OrderDocument po={po} />
    </div>
  )
}

/** Check the delivery details, then issue. Or drop the draft. */
function DraftActions({ po }: { po: PurchaseOrderDetail }) {
  const sites = useSites()
  const update = useUpdateDelivery(po.id)
  const issue = useIssueOrder(po.id)
  const cancel = useCancelOrder(po.id)
  const [siteId, setSiteId] = useState(po.deliverTo.id)
  const [expected, setExpected] = useState(po.expectedDelivery)
  const [notes, setNotes] = useState(po.notes ?? '')
  const [cancelling, setCancelling] = useState(false)
  const [reason, setReason] = useState('')
  const dirty = siteId !== po.deliverTo.id || expected !== po.expectedDelivery || notes.trim() !== (po.notes ?? '')
  const error = update.error ?? issue.error ?? cancel.error

  const run = async (action: () => Promise<unknown>, message: string) => {
    try { await action(); toast(message) } catch { /* shown below */ }
  }

  return (
    <Panel className="flex flex-col gap-4 px-6 py-5 print:hidden">
      <div>
        <h2 className="text-xl font-normal">Check the delivery details, then issue</h2>
        <p className="mt-1 text-sm text-muted-foreground">
          Lines and prices come from the approved award and can't change. Issuing sends the order, marks its requisitions Ordered and
          commits {formatNaira(po.total)} against their budgets in place of the estimates.
        </p>
      </div>
      <div className="grid grid-cols-[repeat(auto-fit,minmax(220px,1fr))] gap-3">
        <label className="flex flex-col gap-1.5 text-[13px] font-semibold">
          Deliver to
          <FieldSelect aria-label="Deliver to" value={siteId} onChange={setSiteId}
            options={sites.data
              ? sites.data.filter((s) => s.isActive).map((s) => ({ value: s.id, label: `${s.code} · ${s.name}, ${s.city}` }))
              : [{ value: po.deliverTo.id, label: `${po.deliverTo.code} · ${po.deliverTo.name}` }]} />
        </label>
        <label className="flex flex-col gap-1.5 text-[13px] font-semibold">
          Expected delivery
          <input type="date" value={expected} onChange={(e) => setExpected(e.target.value)} className={fieldClass} />
        </label>
        <label className="col-span-full flex flex-col gap-1.5 text-[13px] font-semibold">
          Notes for the supplier
          <textarea rows={2} maxLength={2000} value={notes} onChange={(e) => setNotes(e.target.value)}
            className={cn(fieldClass, 'h-auto py-2.5')} placeholder="e.g. Deliver to gate B between 8am and 3pm; call the warehouse on arrival" />
        </label>
      </div>

      {cancelling && (
        <div className="flex flex-col gap-2.5 rounded-2xl bg-destructive/10 p-4">
          <label htmlFor="cancel-po" className="text-[13px] font-semibold text-destructive">Why cancel this draft?</label>
          <textarea id="cancel-po" rows={2} maxLength={500} value={reason} onChange={(e) => setReason(e.target.value)}
            className={cn(fieldClass, 'h-auto py-2.5')} placeholder="e.g. Raised against the wrong site; raising again" />
          <div className="flex justify-end gap-2">
            <Button size="pill-sm" variant="ghost" onClick={() => setCancelling(false)}>Keep it</Button>
            <Button size="pill-sm" variant="destructive" disabled={!reason.trim() || cancel.isPending}
              onClick={() => run(() => cancel.mutateAsync({ reason: reason.trim() }), `${po.number} cancelled`)}>
              {cancel.isPending ? 'Cancelling…' : 'Cancel draft'}
            </Button>
          </div>
        </div>
      )}

      {error && <p role="alert" className="rounded-xl bg-destructive/10 px-3.5 py-3 text-[13px] text-destructive">{problemMessage(error)}</p>}

      <div className="flex flex-wrap items-center justify-between gap-3">
        {!cancelling
          ? <Button size="pill-sm" variant="ghost" className="text-destructive hover:text-destructive" onClick={() => setCancelling(true)}>Cancel draft</Button>
          : <span />}
        <div className="flex flex-wrap gap-2">
          {dirty && (
            <Button size="pill" variant="outline" disabled={update.isPending}
              onClick={() => run(() => update.mutateAsync({ deliverToSiteId: siteId, expectedDelivery: expected, notes: notes.trim() || null }), 'Delivery details saved')}>
              {update.isPending ? 'Saving…' : 'Save changes'}
            </Button>
          )}
          <Button size="pill" variant="brand" disabled={dirty || issue.isPending}
            onClick={() => run(() => issue.mutateAsync(), `${po.number} issued to ${po.supplier.name}`)}>
            <HugeiconsIcon icon={MailSend01Icon} strokeWidth={1.5} /> {issue.isPending ? 'Issuing…' : 'Issue to supplier'}
          </Button>
        </div>
      </div>
      {dirty && <p className="-mt-2 text-right text-xs text-muted-foreground">Save your changes before issuing.</p>}
    </Panel>
  )
}

/**
 * After issue: cancel while nothing has arrived (the requisitions go back for sourcing, or are cancelled too),
 * or close short once part has arrived.
 */
function IssuedActions({ po }: { po: PurchaseOrderDetail }) {
  const cancel = useCancelOrder(po.id)
  const close = useCloseOrder(po.id)
  const [open, setOpen] = useState(false)
  const [reason, setReason] = useState('')
  const [resource, setResource] = useState(true)
  const closing = po.status === 'PartiallyReceived'
  const outstanding = po.lines.reduce((s, l) => s + Math.max(l.quantity - l.quantityReceived, 0) * l.unitPrice, 0)
  const requisitions = [...new Set(po.lines.map((l) => l.requisitionNumber))]
  const pending = cancel.isPending || close.isPending
  const error = cancel.error ?? close.error

  const submit = () => {
    const done = { onSuccess: () => { toast(closing ? `${po.number} closed short` : `${po.number} cancelled`); setOpen(false) } }
    if (closing) close.mutate(reason.trim(), done)
    else cancel.mutate({ reason: reason.trim(), returnRequisitionsToSourcing: resource }, done)
  }

  if (!open) {
    return (
      <div className="flex justify-end print:hidden">
        <Button size="pill-sm" variant="ghost" className="text-destructive hover:text-destructive" onClick={() => setOpen(true)}>
          {closing ? 'Close short…' : 'Cancel order…'}
        </Button>
      </div>
    )
  }

  return (
    <Panel className="flex flex-col gap-3 px-6 py-5 print:hidden">
      <div>
        <h2 className="text-xl font-normal">{closing ? `Close ${po.number} short` : `Cancel ${po.number}`}</h2>
        <p className="mt-1 text-sm text-muted-foreground">
          {closing
            ? `Stop waiting for what hasn't arrived. ${formatNaira(outstanding)} comes off the budget; what arrived stays received and can still be invoiced.`
            : `Nothing has arrived yet. Tell ${po.supplier.name} the order is withdrawn; ${formatNaira(po.total)} goes back to the budget.`}
        </p>
      </div>
      {!closing && (
        <fieldset className="flex flex-col gap-2 text-sm">
          <legend className="mb-1 text-[13px] font-semibold">What happens to {requisitions.join(', ')}?</legend>
          <label className="flex items-start gap-2.5">
            <input type="radio" name="po-reqs" checked={resource} onChange={() => setResource(true)} className="mt-1 accent-brand" />
            <span><strong className="font-semibold">Source again.</strong> <span className="text-muted-foreground">Back to Approved, waiting for a new RFQ; their estimates are committed to the budget again.</span></span>
          </label>
          <label className="flex items-start gap-2.5">
            <input type="radio" name="po-reqs" checked={!resource} onChange={() => setResource(false)} className="mt-1 accent-brand" />
            <span><strong className="font-semibold">Cancel them too.</strong> <span className="text-muted-foreground">The need has gone; nothing stays on the budget.</span></span>
          </label>
        </fieldset>
      )}
      <label htmlFor="po-end-reason" className="text-[13px] font-semibold">{closing ? 'Why won\'t the rest come?' : 'Why cancel it?'}</label>
      <textarea id="po-end-reason" rows={2} maxLength={500} value={reason} onChange={(e) => setReason(e.target.value)} className={cn(fieldClass, 'h-auto py-2.5')}
        placeholder={closing ? 'e.g. Supplier discontinued the model; the third laptop will be sourced separately' : 'e.g. Supplier can no longer deliver before the launch'} />
      {error && <p role="alert" className="rounded-xl bg-destructive/10 px-3.5 py-3 text-[13px] text-destructive">{problemMessage(error)}</p>}
      <div className="flex justify-end gap-2">
        <Button size="pill-sm" variant="ghost" onClick={() => setOpen(false)}>Keep it</Button>
        <Button size="pill-sm" variant="destructive" disabled={!reason.trim() || pending} onClick={submit}>
          {pending ? 'Saving…' : closing ? 'Close short' : 'Cancel order'}
        </Button>
      </div>
    </Panel>
  )
}

/** The order as the supplier sees it. This is the part that prints. */
function OrderDocument({ po }: { po: PurchaseOrderDetail }) {
  const receiving = po.status === 'PartiallyReceived' || po.status === 'Received' || po.status === 'Closed'
  return (
    <article aria-label={`Purchase order ${po.number}`}
      className="flex flex-col gap-7 rounded-[28px] bg-surface p-8 shadow-sm print:rounded-none print:bg-white print:p-0 print:shadow-none print:text-black">
      {(po.status === 'Draft' || po.status === 'Cancelled' || po.status === 'Closed') && (
        <p className="self-start rounded-full bg-draft px-3 py-1 text-xs font-bold text-draft-foreground">
          {po.status === 'Draft' ? 'Draft: not yet sent to the supplier' : po.status === 'Closed' ? 'Closed short: nothing more to deliver' : 'Cancelled: not valid'}
        </p>
      )}
      <header className="flex flex-wrap items-start justify-between gap-6 border-b pb-6">
        <div>
          <div className="font-heading text-2xl">VivaBev Beverages Ltd</div>
          <div className="mt-1 text-sm text-muted-foreground print:text-neutral-600">Procurement · Head Office, Lagos</div>
        </div>
        <div className="text-right">
          <div className="text-xs font-bold tracking-[0.18em] text-muted-foreground uppercase print:text-neutral-600">Purchase order</div>
          <div className="font-heading text-3xl font-light">{po.number}</div>
          <div className="mt-1 text-sm">{po.issuedAt ? `Dated ${formatDate(po.issuedAt.slice(0, 10))}` : 'Not issued'}</div>
        </div>
      </header>

      <section className="grid grid-cols-[repeat(auto-fit,minmax(240px,1fr))] gap-6 text-sm">
        <Block title="Supplier">
          <div className="font-semibold">{po.supplier.legalName}</div>
          {po.supplier.address && <div>{po.supplier.address}</div>}
          <div className="text-muted-foreground print:text-neutral-600">{po.supplier.code} · TIN {po.supplier.taxId}</div>
          {po.supplier.contactName && (
            <div className="mt-1">
              {po.supplier.contactName}
              {po.supplier.contactEmail && <> · {po.supplier.contactEmail}</>}
              {po.supplier.contactPhone && <> · {po.supplier.contactPhone}</>}
            </div>
          )}
        </Block>
        <Block title="Deliver to">
          <div className="font-semibold">{po.deliverTo.name}</div>
          <div>{[po.deliverTo.city, po.deliverTo.state].filter(Boolean).join(', ')}</div>
          <div className="text-muted-foreground print:text-neutral-600">Site {po.deliverTo.code}</div>
        </Block>
        <dl className="grid grid-cols-[auto_1fr] gap-x-4 gap-y-1.5">
          <Term label="Expected delivery">{formatDate(po.expectedDelivery)}</Term>
          <Term label="Payment terms">{po.paymentTermsDays} days from invoice</Term>
          <Term label="Your quotation">{po.supplierReference ?? '–'}</Term>
          <Term label="Our RFQ">{po.sourcingEventNumber}</Term>
          <Term label="Buyer">{po.buyerName}</Term>
        </dl>
      </section>

      <div className="overflow-x-auto">
        <table className="w-full min-w-[640px] border-collapse text-sm">
          <thead>
            <tr className="border-b text-left text-xs text-muted-foreground print:text-neutral-600">
              <th scope="col" className="py-2 pr-3 font-semibold">#</th>
              <th scope="col" className="py-2 pr-3 font-semibold">Description</th>
              <th scope="col" className="py-2 pr-3 text-right font-semibold">Qty</th>
              {receiving && <th scope="col" className="py-2 pr-3 text-right font-semibold print:hidden">Received</th>}
              <th scope="col" className="py-2 pr-3 text-right font-semibold">Unit price</th>
              <th scope="col" className="py-2 text-right font-semibold">Amount</th>
            </tr>
          </thead>
          <tbody>
            {po.lines.map((l) => (
              <tr key={l.id} className="border-b align-top">
                <td className="py-3 pr-3 text-muted-foreground print:text-neutral-600">{l.lineNo}</td>
                <td className="py-3 pr-3">
                  <div className="font-semibold">{l.description}</div>
                  <div className="text-xs text-muted-foreground print:text-neutral-600">
                    {l.itemCode ?? 'Free text'} · ref {l.requisitionNumber} · {l.costCentreCode}
                  </div>
                </td>
                <td className="py-3 pr-3 text-right">{l.quantity} {l.uomCode}</td>
                {receiving && (
                  <td className={cn('py-3 pr-3 text-right print:hidden', l.quantityReceived >= l.quantity ? 'text-brand' : 'text-muted-foreground')}>
                    {l.quantityReceived} {l.uomCode}
                  </td>
                )}
                <td className="py-3 pr-3 text-right">{formatNaira(l.unitPrice)}</td>
                <td className="py-3 text-right font-semibold">{formatNaira(l.lineTotal)}</td>
              </tr>
            ))}
          </tbody>
          <tfoot>
            <tr>
              <td colSpan={receiving ? 5 : 4} className="pt-4 pr-3 text-right text-sm font-semibold">Total ({po.currencyCode})</td>
              <td className="pt-4 text-right font-heading text-2xl">{formatNaira(po.total)}</td>
            </tr>
          </tfoot>
        </table>
      </div>

      {po.notes && (
        <Block title="Notes">
          <p className="whitespace-pre-line">{po.notes}</p>
        </Block>
      )}

      <footer className="border-t pt-4 text-xs text-muted-foreground print:text-neutral-600">
        Quote {po.number} on your delivery note and invoice. Goods are received against this order at the delivery site;
        invoices are matched to the order and the goods received before payment.
      </footer>
    </article>
  )
}

function Block({ title, children }: { title: string; children: ReactNode }) {
  return (
    <div className="flex flex-col gap-0.5">
      <div className="mb-1 text-xs font-bold tracking-wide text-muted-foreground uppercase print:text-neutral-600">{title}</div>
      {children}
    </div>
  )
}

function Term({ label, children }: { label: string; children: ReactNode }) {
  return (
    <>
      <dt className="text-muted-foreground print:text-neutral-600">{label}</dt>
      <dd className="font-semibold">{children}</dd>
    </>
  )
}

/** Deliveries recorded against this order by the warehouse. */
function Receipts({ po }: { po: PurchaseOrderDetail }) {
  const receipts = useReceipts(po.id)
  return (
    <Panel className="flex flex-col gap-2 px-6 py-5 print:hidden">
      <h2 className="text-xl font-normal">Goods received</h2>
      {receipts.isPending && <Skeleton className="h-16 rounded-2xl" />}
      <ul className="flex flex-col divide-y">
        {receipts.data?.map((r) => (
          <li key={r.id} className="flex flex-wrap items-center gap-3 py-2.5 text-sm">
            <Link to={`/warehouse/receipts/${r.id}`} className="font-semibold hover:text-brand">{r.number}</Link>
            <span className="min-w-0 flex-1 text-xs text-muted-foreground">
              {formatDateTime(r.receivedAt)} · {r.receivedByName}{r.deliveryNoteNumber && ` · delivery note ${r.deliveryNoteNumber}`}
            </span>
            {r.hasRejections && <span className="rounded-full bg-over px-2.5 py-0.5 text-xs font-bold text-over-foreground">Some rejected</span>}
            <span className="font-semibold">{formatNaira(r.acceptedValue)}</span>
          </li>
        ))}
      </ul>
    </Panel>
  )
}

/** Supplier invoices against this order and how they matched. */
function Invoices({ po, canRecord }: { po: PurchaseOrderDetail; canRecord: boolean }) {
  const invoices = useInvoices(po.id)
  const live = (invoices.data ?? []).filter((i) => i.status !== 'Rejected')
  return (
    <Panel className="flex flex-col gap-2 px-6 py-5 print:hidden">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h2 className="text-xl font-normal">Invoices</h2>
        <div className="flex items-center gap-3">
          {live.length > 0 && <span className="text-[13px] text-muted-foreground">{formatNaira(live.reduce((s, i) => s + i.total, 0))} invoiced incl. VAT</span>}
          {canRecord && <Button asChild size="pill-sm" variant="outline"><Link to={`/invoices/new?order=${po.id}`}>Record invoice</Link></Button>}
        </div>
      </div>
      {invoices.isPending && <Skeleton className="h-12 rounded-2xl" />}
      {invoices.data?.length === 0 && <p className="text-sm text-muted-foreground">No invoices from {po.supplier.name} yet.</p>}
      <ul className="flex flex-col divide-y">
        {invoices.data?.map((i) => (
          <li key={i.id} className="flex flex-wrap items-center gap-3 py-2.5 text-sm">
            <Link to={`/invoices/${i.id}`} className="font-semibold hover:text-brand">{i.supplierInvoiceNumber}</Link>
            <span className="min-w-0 flex-1 text-xs text-muted-foreground">{i.number} · dated {formatDate(i.invoiceDate)} · due {formatDate(i.dueDate)}</span>
            <span className="font-semibold">{formatNaira(i.total)}</span>
            <InvoiceStatusBadge status={i.status} />
          </li>
        ))}
      </ul>
    </Panel>
  )
}
