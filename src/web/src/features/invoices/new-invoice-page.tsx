import { useState, type FormEvent } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router'
import { toast } from 'sonner'
import { HugeiconsIcon } from '@hugeicons/react'
import { ArrowLeft01Icon } from '@hugeicons/core-free-icons'
import { FieldSelect } from '@/components/field-select'
import { FormField } from '@/components/form-field'
import { Panel } from '@/components/panel'
import { Skeleton } from '@/components/ui/skeleton'
import { Button } from '@/components/ui/button'
import { problemMessage } from '@/lib/api-client'
import { fieldClass } from '@/lib/field-class'
import { formatNaira } from '@/lib/format'
import { cn } from '@/lib/utils'
import { PRICE_TOLERANCE, VAT_RATE, useInvoiceableOrders, useRecordInvoice, type InvoiceableOrder } from './invoices-api'

/** Key in a supplier's invoice against an order. Each line shows, before saving, whether it will match. */
export function NewInvoicePage() {
  const orders = useInvoiceableOrders()
  const [params, setParams] = useSearchParams()
  const orderId = params.get('order') ?? ''
  const order = orders.data?.find((o) => o.id === orderId)

  return (
    <div className="flex flex-col gap-6">
      <section className="flex flex-col gap-3">
        <Link to="/invoices" className="flex items-center gap-1.5 self-start text-sm font-semibold hover:text-brand">
          <HugeiconsIcon icon={ArrowLeft01Icon} size={18} strokeWidth={1.5} /> Invoices
        </Link>
        <h1 className="text-[40px] leading-tight font-light tracking-tight">Record a supplier invoice</h1>
      </section>

      <Panel className="flex flex-col gap-3 px-6 py-5">
        <FormField id="po" label="Purchase order" hint="Issued orders only. The supplier's invoice should quote the order number.">
          {orders.isPending ? <Skeleton className="h-11 rounded-xl" /> : (
            <FieldSelect id="po" value={orderId} onChange={(v) => setParams({ order: v }, { replace: true })}
              placeholder="Choose the order being invoiced…"
              options={(orders.data ?? []).map((o) => ({
                value: o.id,
                label: `${o.number} · ${o.supplierName} · ${formatNaira(o.total)}${o.invoiced > 0 ? ` (${formatNaira(o.invoiced)} invoiced)` : ''}`,
              }))} />
          )}
        </FormField>
        {orders.data?.length === 0 && <p className="text-sm text-muted-foreground">No issued orders to invoice yet.</p>}
      </Panel>

      {order && <InvoiceForm key={order.id} order={order} />}
    </div>
  )
}

type Draft = { quantity: string; unitPrice: string }

function InvoiceForm({ order }: { order: InvoiceableOrder }) {
  const record = useRecordInvoice()
  const navigate = useNavigate()
  const [today] = useState(() => new Date().toLocaleDateString('en-CA'))
  const [supplierNumber, setSupplierNumber] = useState('')
  const [invoiceDate, setInvoiceDate] = useState(today)
  // Start from what's received and not yet billed, at the order price
  const [lines, setLines] = useState<Record<string, Draft>>(() => Object.fromEntries(order.lines.map((l) => {
    const billable = Math.max(l.received - l.invoiced, 0)
    return [l.id, { quantity: billable ? String(billable) : '', unitPrice: String(l.unitPrice) }]
  })))
  const num = (v: string) => (v.trim() === '' ? 0 : Number(v))
  const subtotal = order.lines.reduce((s, l) => s + num(lines[l.id]!.quantity) * num(lines[l.id]!.unitPrice), 0)
  const [vat, setVat] = useState<string | null>(null) // null: follow 7.5% of the subtotal
  const vatAmount = vat === null ? Math.round(subtotal * VAT_RATE * 100) / 100 : num(vat)
  const billed = order.lines.filter((l) => num(lines[l.id]!.quantity) > 0)

  const preview = (l: InvoiceableOrder['lines'][number]) => {
    const q = num(lines[l.id]!.quantity), p = num(lines[l.id]!.unitPrice)
    if (q <= 0) return null
    const problems: string[] = []
    if (p > Math.round(l.unitPrice * (1 + PRICE_TOLERANCE) * 100) / 100) problems.push('price above the order')
    if (l.invoiced + q > l.received) problems.push(`only ${Math.max(l.received - l.invoiced, 0)} received and not yet billed`)
    return problems
  }
  const holds = billed.filter((l) => (preview(l)?.length ?? 0) > 0).length
  const set = (id: string, patch: Partial<Draft>) => setLines((all) => ({ ...all, [id]: { ...all[id]!, ...patch } }))

  const submit = async (e: FormEvent) => {
    e.preventDefault()
    try {
      const created = await record.mutateAsync({
        purchaseOrderId: order.id,
        supplierInvoiceNumber: supplierNumber.trim(),
        invoiceDate,
        vatAmount,
        lines: billed.map((l) => ({ purchaseOrderLineId: l.id, quantity: num(lines[l.id]!.quantity), unitPrice: num(lines[l.id]!.unitPrice) })),
      })
      toast(created.status === 'Matched' ? `${created.number} matched and approved for payment` : `${created.number} recorded and put on hold`,
        { description: created.status === 'Matched' ? 'The budget now shows it as spent.' : 'Check the lines that did not match.' })
      navigate(`/invoices/${created.id}`)
    } catch { /* shown below */ }
  }

  return (
    <Panel className="px-6 py-5">
      <h2 className="text-xl font-normal">{order.supplierName}'s invoice for {order.number}</h2>
      <p className="mt-1 mb-4 text-sm text-muted-foreground">
        Copy each line from the invoice. Quantities start at what's been received and not yet billed. Payment terms: {order.paymentTermsDays} days.
      </p>
      <form onSubmit={submit} className="flex flex-col gap-4">
        <div className="grid grid-cols-[repeat(auto-fit,minmax(220px,1fr))] gap-3">
          <FormField id="sup-inv" label="Supplier's invoice number">
            <input id="sup-inv" required maxLength={50} value={supplierNumber} onChange={(e) => setSupplierNumber(e.target.value)} className={fieldClass} placeholder="e.g. SDS-INV-9001" />
          </FormField>
          <FormField id="inv-date" label="Invoice date">
            <input id="inv-date" type="date" required max={today} value={invoiceDate} onChange={(e) => setInvoiceDate(e.target.value)} className={fieldClass} />
          </FormField>
        </div>

        <div className="overflow-x-auto">
          <table className="w-full min-w-[860px] border-collapse text-sm">
            <thead>
              <tr className="text-left text-xs text-muted-foreground">
                <th scope="col" className="p-2 font-semibold">Order line</th>
                <th scope="col" className="p-2 text-right font-semibold">Ordered</th>
                <th scope="col" className="p-2 text-right font-semibold">Received</th>
                <th scope="col" className="p-2 text-right font-semibold">Billed before</th>
                <th scope="col" className="w-28 p-2 font-semibold">Quantity</th>
                <th scope="col" className="w-40 p-2 font-semibold">Unit price (₦)</th>
                <th scope="col" className="p-2 font-semibold">Will it match?</th>
              </tr>
            </thead>
            <tbody>
              {order.lines.map((l) => {
                const d = lines[l.id]!
                const problems = preview(l)
                return (
                  <tr key={l.id} className="border-t align-top">
                    <th scope="row" className="p-2 text-left font-normal">
                      <span className="font-semibold">{l.lineNo}. {l.description}</span>
                      <span className="block text-xs text-muted-foreground">Ordered at {formatNaira(l.unitPrice)} / {l.uomCode}</span>
                    </th>
                    <td className="p-2 text-right">{l.ordered} {l.uomCode}</td>
                    <td className="p-2 text-right">{l.received} {l.uomCode}</td>
                    <td className="p-2 text-right">{l.invoiced} {l.uomCode}</td>
                    <td className="p-2">
                      <input type="number" min={0} step="any" value={d.quantity} placeholder="0" aria-label={`Quantity for line ${l.lineNo}`}
                        onChange={(e) => set(l.id, { quantity: e.target.value })} className={cn(fieldClass, 'h-9 text-right')} />
                    </td>
                    <td className="p-2">
                      <input type="number" min={0} step="any" value={d.unitPrice} aria-label={`Unit price for line ${l.lineNo}`}
                        onChange={(e) => set(l.id, { unitPrice: e.target.value })} className={cn(fieldClass, 'h-9 text-right')} />
                    </td>
                    <td className="p-2 text-xs">
                      {problems === null ? <span className="text-muted-foreground">Not billed</span>
                        : problems.length === 0 ? <span className="rounded-full bg-success px-2 py-0.5 font-bold text-success-foreground">Matches</span>
                        : <span className="text-over-foreground"><span className="rounded-full bg-over px-2 py-0.5 font-bold">Will be held</span><span className="mt-1 block">{problems.join('; ')}</span></span>}
                    </td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        </div>

        <div className="ml-auto grid w-full max-w-sm grid-cols-[1fr_auto] items-center gap-x-4 gap-y-2 text-sm">
          <span className="text-muted-foreground">Subtotal</span><span className="text-right font-semibold">{formatNaira(subtotal)}</span>
          <label htmlFor="vat" className="text-muted-foreground">VAT {vat === null && '(7.5%)'}</label>
          <input id="vat" type="number" min={0} step="any" value={vat ?? String(vatAmount)} onChange={(e) => setVat(e.target.value)}
            className={cn(fieldClass, 'h-9 w-40 text-right')} />
          <span className="font-semibold">Invoice total</span><span className="text-right font-heading text-2xl">{formatNaira(subtotal + vatAmount)}</span>
        </div>

        {record.error && <p role="alert" className="rounded-xl bg-destructive/10 px-3.5 py-3 text-[13px] text-destructive">{problemMessage(record.error)}</p>}

        <div className="flex flex-wrap items-center justify-end gap-3">
          {billed.length > 0 && (
            <span className="text-sm text-muted-foreground">
              {holds === 0 ? 'Every line matches: it will be approved for payment.' : `${holds} line${holds === 1 ? '' : 's'} won't match: it will be put on hold.`}
            </span>
          )}
          <Button type="submit" size="pill" variant="brand" disabled={!supplierNumber.trim() || billed.length === 0 || record.isPending}>
            {record.isPending ? 'Recording…' : 'Record invoice'}
          </Button>
        </div>
      </form>
    </Panel>
  )
}
