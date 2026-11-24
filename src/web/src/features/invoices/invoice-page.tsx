import { useState, type ReactNode } from 'react'
import { Link, useParams } from 'react-router'
import { toast } from 'sonner'
import { HugeiconsIcon } from '@hugeicons/react'
import { ArrowLeft01Icon } from '@hugeicons/core-free-icons'
import { FormField } from '@/components/form-field'
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
import { InvoiceStatusBadge } from './invoice-status-badge'
import { useInvoice, useInvoiceDecision, usePayInvoice, useRematch, type InvoiceDetail } from './invoices-api'

export function InvoicePage() {
  const { id } = useParams()
  const invoice = useInvoice(id)

  if (invoice.isPending) return <Skeleton className="h-96 rounded-[28px]" />
  if (invoice.isError) {
    return <ErrorState error={invoice.error} what="this invoice" onRetry={() => invoice.refetch()} back={{ to: '/invoices', label: 'Back to invoices' }} />
  }
  return <Invoice inv={invoice.data} />
}

function Invoice({ inv }: { inv: InvoiceDetail }) {
  const { data: user } = useCurrentUser()
  const manage = can(user, Permission.InvoicesManage)
  const held = inv.lines.filter((l) => l.exception)

  return (
    <div className="flex flex-col gap-6">
      <section className="flex flex-col gap-3">
        <Link to="/invoices" className="flex items-center gap-1.5 self-start text-sm font-semibold hover:text-brand">
          <HugeiconsIcon icon={ArrowLeft01Icon} size={18} strokeWidth={1.5} /> Invoices
        </Link>
        <div className="flex flex-wrap items-end justify-between gap-5">
          <div>
            <p className="mb-1 text-[13px] font-semibold text-muted-foreground">
              {inv.number} · recorded by {inv.recordedByName} {formatDateTime(inv.recordedAt)}
            </p>
            <h1 className="text-[40px] leading-tight font-light tracking-tight">{inv.supplierName} · {inv.supplierInvoiceNumber}</h1>
            <div className="mt-3 flex flex-wrap items-center gap-2 text-sm">
              <InvoiceStatusBadge status={inv.status} />
              <span className="text-muted-foreground">
                Dated {formatDate(inv.invoiceDate)} · due {formatDate(inv.dueDate)} · against{' '}
                {can(user, Permission.PurchaseOrdersView)
                  ? <Link to={`/purchase-orders/${inv.purchaseOrderId}`} className="font-semibold text-foreground hover:text-brand">{inv.purchaseOrderNumber}</Link>
                  : inv.purchaseOrderNumber}
              </span>
            </div>
          </div>
          <div className="text-right">
            <div className="text-[13px] text-muted-foreground">Total incl. VAT</div>
            <div className="font-heading text-[40px] leading-tight font-light">{formatNaira(inv.total)}</div>
          </div>
        </div>
      </section>

      <Outcome inv={inv} manage={manage} heldLines={held.length} />

      <Panel className="px-6 py-5">
        <h2 className="mb-1 text-xl font-normal">Three-way match</h2>
        <p className="mb-3 text-sm text-muted-foreground">
          Each line is checked against the order's price (2% tolerance) and the quantity the warehouse received{inv.matchedAt && `, last checked ${formatDateTime(inv.matchedAt)}`}.
        </p>
        <div className="overflow-x-auto">
          <table className="w-full min-w-[820px] border-collapse text-sm">
            <thead>
              <tr className="text-left text-xs text-muted-foreground">
                <th scope="col" className="p-2.5 font-semibold">Line</th>
                <th scope="col" className="p-2.5 text-right font-semibold">Invoiced</th>
                <th scope="col" className="p-2.5 text-right font-semibold">Received</th>
                <th scope="col" className="p-2.5 text-right font-semibold">Invoice price</th>
                <th scope="col" className="p-2.5 text-right font-semibold">Order price</th>
                <th scope="col" className="p-2.5 text-right font-semibold">Amount</th>
                <th scope="col" className="p-2.5 font-semibold">Result</th>
              </tr>
            </thead>
            <tbody>
              {inv.lines.map((l) => (
                <tr key={l.lineNo} className="border-t align-top">
                  <th scope="row" className="p-2.5 text-left font-normal"><span className="font-semibold">{l.lineNo}. {l.description}</span></th>
                  <td className="p-2.5 text-right">
                    {l.quantity} {l.uomCode}
                    {!!l.invoicedBefore && <span className="block text-xs text-muted-foreground">+{l.invoicedBefore} billed before</span>}
                  </td>
                  <td className="p-2.5 text-right">{l.received ?? '–'} {l.received !== null && l.uomCode}</td>
                  <td className={cn('p-2.5 text-right', l.unitPrice > l.orderedUnitPrice && 'font-semibold')}>{formatNaira(l.unitPrice)}</td>
                  <td className="p-2.5 text-right text-muted-foreground">{formatNaira(l.orderedUnitPrice)}</td>
                  <td className="p-2.5 text-right font-semibold">{formatNaira(l.lineTotal)}</td>
                  <td className="p-2.5 text-xs">
                    {l.exception
                      ? <span className="text-over-foreground"><span className="rounded-full bg-over px-2 py-0.5 font-bold">Didn't match</span><span className="mt-1 block">{l.exception}</span></span>
                      : <span className="rounded-full bg-success px-2 py-0.5 font-bold text-success-foreground">Matched</span>}
                  </td>
                </tr>
              ))}
            </tbody>
            <tfoot className="border-t-2">
              <Total label="Subtotal">{formatNaira(inv.subtotal)}</Total>
              <Total label="VAT">{formatNaira(inv.vatAmount)}</Total>
              <Total label="Total"><span className="font-heading text-xl">{formatNaira(inv.total)}</span></Total>
            </tfoot>
          </table>
        </div>
      </Panel>
    </div>
  )
}

function Total({ label, children }: { label: string; children: ReactNode }) {
  return (
    <tr>
      <td colSpan={5} className="p-2.5 text-right text-xs font-semibold text-muted-foreground">{label}</td>
      <td className="p-2.5 text-right font-semibold">{children}</td>
      <td />
    </tr>
  )
}

/** What happened, and the next action for Finance. */
function Outcome({ inv, manage, heldLines }: { inv: InvoiceDetail; manage: boolean; heldLines: number }) {
  const rematch = useRematch(inv.id)
  const decide = useInvoiceDecision(inv.id)
  const pay = usePayInvoice(inv.id)
  const [deciding, setDeciding] = useState<'approve' | 'reject' | null>(null)
  const [reason, setReason] = useState('')
  const [reference, setReference] = useState('')
  const [paidOn, setPaidOn] = useState(() => new Date().toLocaleDateString('en-CA'))
  const error = rematch.error ?? decide.error ?? pay.error

  const decided = inv.decisionReason && (
    <p className="text-sm">
      <span className="text-muted-foreground">{inv.status === 'Rejected' ? 'Rejected' : 'Approved anyway'} by {inv.decidedByName}{inv.decidedAt && `, ${formatDateTime(inv.decidedAt)}`}:</span> “{inv.decisionReason}”
    </p>
  )

  return (
    <Panel className={cn('flex flex-col gap-3 px-6 py-5', inv.status === 'OnHold' && 'border-over-foreground/30')}>
      {inv.status === 'OnHold' && (
        <>
          <div className="flex flex-wrap items-center justify-between gap-4">
            <div>
              <h2 className="text-xl font-normal">On hold: {heldLines} line{heldLines === 1 ? '' : 's'} didn't match</h2>
              <p className="mt-1 text-sm text-muted-foreground">
                If goods were short, check again once the rest is received. If the supplier is wrong, reject it so they reissue.
                Approving anyway needs a reason.
              </p>
            </div>
            {manage && (
              <div className="flex flex-wrap gap-2">
                <Button size="pill" variant="outline" disabled={rematch.isPending}
                  onClick={() => rematch.mutate(undefined, { onSuccess: (r) => toast(r.status === 'Matched' ? 'It matches now and is approved for payment' : 'Still doesn\'t match') })}>
                  {rematch.isPending ? 'Checking…' : 'Check again'}
                </Button>
                <Button size="pill" variant="ghost" className="text-destructive hover:text-destructive" onClick={() => setDeciding('reject')}>Reject</Button>
                <Button size="pill" onClick={() => setDeciding('approve')}>Approve anyway</Button>
              </div>
            )}
          </div>
          {deciding && (
            <div className={cn('flex flex-col gap-2.5 rounded-2xl p-4', deciding === 'reject' ? 'bg-destructive/10' : 'bg-surface')}>
              <label htmlFor="inv-reason" className="text-[13px] font-semibold">
                {deciding === 'reject' ? 'Why reject it? The supplier needs to know what to correct.' : 'Why approve it despite the mismatch?'}
              </label>
              <textarea id="inv-reason" rows={2} maxLength={500} value={reason} onChange={(e) => setReason(e.target.value)}
                className={cn(fieldClass, 'h-auto py-2.5')}
                placeholder={deciding === 'reject' ? 'e.g. Unit price differs from PO-2027-00001; please reissue at ₦850,000' : 'e.g. Price increase agreed with the Procurement manager on 20 Nov'} />
              <div className="flex justify-end gap-2">
                <Button size="pill-sm" variant="ghost" onClick={() => setDeciding(null)}>Cancel</Button>
                <Button size="pill-sm" variant={deciding === 'reject' ? 'destructive' : 'brand'} disabled={!reason.trim() || decide.isPending}
                  onClick={() => decide.mutate({ decision: deciding, reason: reason.trim() },
                    { onSuccess: () => { toast(deciding === 'reject' ? 'Invoice rejected' : 'Approved for payment'); setDeciding(null) } })}>
                  {deciding === 'reject' ? 'Reject invoice' : 'Approve for payment'}
                </Button>
              </div>
            </div>
          )}
        </>
      )}

      {(inv.status === 'Matched' || inv.status === 'Approved') && (
        <>
          <div>
            <h2 className="text-xl font-normal">Approved for payment · due {formatDate(inv.dueDate)}</h2>
            <p className="mt-1 text-sm text-muted-foreground">
              {inv.status === 'Matched' ? 'Every line matched the order and the goods received.' : 'Approved despite a mismatch.'} The budget shows {formatNaira(inv.subtotal)} as spent.
            </p>
          </div>
          {decided}
          {manage && (
            <div className="flex flex-wrap items-end gap-3 border-t pt-3">
              <FormField id="pay-ref" label="Payment reference" className="min-w-0 flex-[1_1_220px]">
                <input id="pay-ref" maxLength={50} value={reference} onChange={(e) => setReference(e.target.value)} className={fieldClass} placeholder="e.g. bank transfer TRF-28841" />
              </FormField>
              <FormField id="paid-on" label="Paid on">
                <input id="paid-on" type="date" value={paidOn} onChange={(e) => setPaidOn(e.target.value)} className={fieldClass} />
              </FormField>
              <Button size="pill" variant="brand" disabled={!reference.trim() || pay.isPending}
                onClick={() => pay.mutate({ paymentReference: reference.trim(), paidOn }, { onSuccess: () => toast('Marked as paid') })}>
                {pay.isPending ? 'Saving…' : 'Mark as paid'}
              </Button>
            </div>
          )}
        </>
      )}

      {inv.status === 'Paid' && (
        <div>
          <h2 className="text-xl font-normal">Paid {inv.paidOn && formatDate(inv.paidOn)}</h2>
          <p className="mt-1 text-sm text-muted-foreground">Reference {inv.paymentReference}</p>
          {decided}
        </div>
      )}

      {inv.status === 'Rejected' && (
        <div>
          <h2 className="text-xl font-normal">Rejected</h2>
          {decided}
          <p className="mt-1 text-sm text-muted-foreground">The supplier can send a corrected invoice; it can be recorded under the same number.</p>
        </div>
      )}

      {error && <p role="alert" className="rounded-xl bg-destructive/10 px-3.5 py-3 text-[13px] text-destructive">{problemMessage(error)}</p>}
    </Panel>
  )
}
