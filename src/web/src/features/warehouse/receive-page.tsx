import { useState, type FormEvent } from 'react'
import { Link, useNavigate, useParams } from 'react-router'
import { toast } from 'sonner'
import { HugeiconsIcon } from '@hugeicons/react'
import { ArrowLeft01Icon } from '@hugeicons/core-free-icons'
import { FormField } from '@/components/form-field'
import { Panel } from '@/components/panel'
import { ErrorState } from '@/components/states/error-state'
import { Button } from '@/components/ui/button'
import { Skeleton } from '@/components/ui/skeleton'
import { useCurrentUser } from '@/features/auth/use-auth'
import { OrderStatusBadge } from '@/features/purchase-orders/order-status-badge'
import type { PurchaseOrderStatus } from '@/features/purchase-orders/purchase-orders-api'
import { problemMessage } from '@/lib/api-client'
import { fieldClass } from '@/lib/field-class'
import { formatDate, formatDateTime, formatNaira } from '@/lib/format'
import { can, Permission } from '@/lib/permissions'
import { cn } from '@/lib/utils'
import { useReceivableOrder, useReceive, type ReceivableOrderDetail } from './warehouse-api'

export function ReceivePage() {
  const { id } = useParams()
  const order = useReceivableOrder(id)

  if (order.isPending) {
    return (
      <div className="flex flex-col gap-6" role="status" aria-label="Loading order">
        <Skeleton className="h-10 w-1/3 rounded-full" />
        <Skeleton className="h-72 rounded-[28px]" />
      </div>
    )
  }
  if (order.isError) {
    return <ErrorState error={order.error} what="this order" onRetry={() => order.refetch()} back={{ to: '/warehouse', label: 'Back to the warehouse' }} />
  }
  return <Receive key={order.dataUpdatedAt} o={order.data} />
}

type Count = { accepted: string; rejected: string; reason: string }

function Receive({ o }: { o: ReceivableOrderDetail }) {
  const { data: user } = useCurrentUser()
  const canReceive = can(user, Permission.InventoryReceive) && (o.status === 'Issued' || o.status === 'PartiallyReceived')
  const receive = useReceive()
  const navigate = useNavigate()
  const open = o.lines.filter((l) => l.outstanding > 0)
  const [counts, setCounts] = useState<Record<string, Count>>(() =>
    Object.fromEntries(open.map((l) => [l.id, { accepted: '', rejected: '', reason: '' }])))
  const [deliveryNote, setDeliveryNote] = useState('')
  const [notes, setNotes] = useState('')

  const set = (id: string, patch: Partial<Count>) => setCounts((c) => ({ ...c, [id]: { ...c[id]!, ...patch } }))
  const num = (v: string) => (v.trim() === '' ? 0 : Number(v))
  const problems = open.flatMap((l) => {
    const c = counts[l.id]!
    const a = num(c.accepted), r = num(c.rejected)
    if (!Number.isFinite(a) || !Number.isFinite(r) || a < 0 || r < 0) return [`Line ${l.lineNo}: quantities can't be negative`]
    if (a > l.outstanding) return [`Line ${l.lineNo}: only ${l.outstanding} ${l.uomCode} left to receive`]
    if (r > 0 && !c.reason.trim()) return [`Line ${l.lineNo}: say why it was rejected`]
    return []
  })
  const counted = open.filter((l) => num(counts[l.id]!.accepted) + num(counts[l.id]!.rejected) > 0)

  const submit = async (e: FormEvent) => {
    e.preventDefault()
    try {
      const created = await receive.mutateAsync({
        purchaseOrderId: o.id,
        deliveryNoteNumber: deliveryNote.trim() || null,
        notes: notes.trim() || null,
        lines: counted.map((l) => ({
          purchaseOrderLineId: l.id,
          quantityAccepted: num(counts[l.id]!.accepted),
          quantityRejected: num(counts[l.id]!.rejected),
          rejectionReason: counts[l.id]!.reason.trim() || null,
        })),
      })
      toast(`${created.number} recorded`, { description: `Received against ${o.number}.` })
      navigate(`/warehouse/receipts/${created.id}`)
    } catch { /* shown below */ }
  }

  return (
    <div className="flex flex-col gap-6">
      <section className="flex flex-col gap-3">
        <Link to="/warehouse" className="flex items-center gap-1.5 self-start text-sm font-semibold hover:text-brand">
          <HugeiconsIcon icon={ArrowLeft01Icon} size={18} strokeWidth={1.5} /> Warehouse
        </Link>
        <div>
          <p className="mb-1 text-[13px] font-semibold text-muted-foreground">Deliver to {o.siteCode} · {o.siteName} · expected {formatDate(o.expectedDelivery)}</p>
          <h1 className="text-[40px] leading-tight font-light tracking-tight">{o.number} · {o.supplierName}</h1>
          <div className="mt-3"><OrderStatusBadge status={o.status as PurchaseOrderStatus} /></div>
        </div>
      </section>

      {canReceive ? (
        <Panel className="px-6 py-5">
          <h2 className="text-xl font-normal">Record what arrived</h2>
          <p className="mt-1 mb-4 text-sm text-muted-foreground">
            Count what's on the truck. Accept what's right; reject anything damaged or wrong, with a reason, and it goes back to the supplier.
            Leave a line blank if it didn't come.
          </p>
          <form onSubmit={submit} className="flex flex-col gap-4">
            <div className="grid grid-cols-[repeat(auto-fit,minmax(220px,1fr))] gap-3">
              <FormField id="dn" label="Supplier's delivery note number">
                <input id="dn" maxLength={50} value={deliveryNote} onChange={(e) => setDeliveryNote(e.target.value)} className={fieldClass} placeholder="e.g. DN-4471" />
              </FormField>
              <FormField id="grn-notes" label="Notes" className="col-span-full sm:col-span-1">
                <input id="grn-notes" maxLength={1000} value={notes} onChange={(e) => setNotes(e.target.value)} className={fieldClass} placeholder="e.g. Arrived 2 hours late; driver Musa" />
              </FormField>
            </div>

            <div className="overflow-x-auto">
              <table className="w-full min-w-[820px] border-collapse text-sm">
                <thead>
                  <tr className="text-left text-xs text-muted-foreground">
                    <th scope="col" className="p-2 font-semibold">Line</th>
                    <th scope="col" className="p-2 text-right font-semibold">Ordered</th>
                    <th scope="col" className="p-2 text-right font-semibold">Still to come</th>
                    <th scope="col" className="w-28 p-2 font-semibold">Accepted</th>
                    <th scope="col" className="w-28 p-2 font-semibold">Rejected</th>
                    <th scope="col" className="p-2 font-semibold">Why rejected</th>
                  </tr>
                </thead>
                <tbody>
                  {open.map((l) => {
                    const c = counts[l.id]!
                    return (
                      <tr key={l.id} className="border-t align-top">
                        <th scope="row" className="p-2 text-left font-normal">
                          <span className="font-semibold">{l.lineNo}. {l.description}</span>
                          <span className="block text-xs text-muted-foreground">{l.itemCode ?? 'Free text'}{l.stocked ? ' · goes into stock' : ''}</span>
                        </th>
                        <td className="p-2 text-right">{l.ordered} {l.uomCode}</td>
                        <td className="p-2 text-right font-semibold">
                          {l.outstanding} {l.uomCode}
                          <button type="button" className="block w-full text-right text-xs font-normal text-brand hover:underline"
                            onClick={() => set(l.id, { accepted: String(l.outstanding) })}>all of it</button>
                        </td>
                        <td className="p-2">
                          <input type="number" min={0} step="any" inputMode="decimal" value={c.accepted} placeholder="0"
                            aria-label={`Accepted for line ${l.lineNo}`} onChange={(e) => set(l.id, { accepted: e.target.value })}
                            className={cn(fieldClass, 'h-9 text-right', num(c.accepted) > l.outstanding && 'border-destructive')} />
                        </td>
                        <td className="p-2">
                          <input type="number" min={0} step="any" inputMode="decimal" value={c.rejected} placeholder="0"
                            aria-label={`Rejected for line ${l.lineNo}`} onChange={(e) => set(l.id, { rejected: e.target.value })}
                            className={cn(fieldClass, 'h-9 text-right')} />
                        </td>
                        <td className="p-2">
                          <input value={c.reason} maxLength={500} disabled={num(c.rejected) <= 0} placeholder={num(c.rejected) > 0 ? 'e.g. Cracked screen' : ''}
                            aria-label={`Why line ${l.lineNo} was rejected`} onChange={(e) => set(l.id, { reason: e.target.value })}
                            className={cn(fieldClass, 'h-9', num(c.rejected) > 0 && !c.reason.trim() && 'border-destructive')} />
                        </td>
                      </tr>
                    )
                  })}
                </tbody>
              </table>
            </div>
            {o.lines.length > open.length && (
              <p className="text-xs text-muted-foreground">{o.lines.length - open.length} line{o.lines.length - open.length === 1 ? ' is' : 's are'} already received in full.</p>
            )}

            {problems.length > 0 && counted.length > 0 && <ul className="text-[13px] text-destructive">{problems.map((p) => <li key={p}>{p}</li>)}</ul>}
            {receive.error && <p role="alert" className="rounded-xl bg-destructive/10 px-3.5 py-3 text-[13px] text-destructive">{problemMessage(receive.error)}</p>}

            <div className="flex flex-wrap items-center justify-end gap-3">
              <Button type="button" size="pill" variant="ghost"
                onClick={() => setCounts(Object.fromEntries(open.map((l) => [l.id, { accepted: String(l.outstanding), rejected: '', reason: '' }])))}>
                Everything arrived
              </Button>
              <Button type="submit" size="pill" variant="brand" disabled={counted.length === 0 || problems.length > 0 || receive.isPending}>
                {receive.isPending ? 'Recording…' : 'Record receipt'}
              </Button>
            </div>
          </form>
        </Panel>
      ) : (
        <Panel className="px-6 py-5 text-sm text-muted-foreground">
          {o.status === 'Received' ? 'Everything on this order has been received.' : "You can see this order, but recording receipts is the warehouse team's job."}
        </Panel>
      )}

      {o.receipts.length > 0 && (
        <Panel className="flex flex-col gap-2 px-6 py-5">
          <h2 className="text-xl font-normal">Already received</h2>
          <ul className="flex flex-col divide-y">
            {o.receipts.map((r) => (
              <li key={r.id} className="flex flex-wrap items-center gap-3 py-2.5 text-sm">
                <Link to={`/warehouse/receipts/${r.id}`} className="font-semibold hover:text-brand">{r.number}</Link>
                <span className="min-w-0 flex-1 text-xs text-muted-foreground">{formatDateTime(r.receivedAt)} · {r.receivedByName}</span>
                <span className="font-semibold">{formatNaira(r.acceptedValue)}</span>
              </li>
            ))}
          </ul>
        </Panel>
      )}
    </div>
  )
}
