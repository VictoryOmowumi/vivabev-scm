import { useState, type FormEvent, type ReactNode } from 'react'
import { FieldSelect } from '@/components/field-select'
import { toast } from 'sonner'
import { Panel } from '@/components/panel'
import { Button } from '@/components/ui/button'
import { problemMessage } from '@/lib/api-client'
import { fieldClass } from '@/lib/field-class'
import { formatNaira } from '@/lib/format'
import { cn } from '@/lib/utils'
import { useRecordQuotation, type SourcingEventDetail } from './sourcing-api'

type LineDraft = { unitPrice: string; quantity: string; notes: string }

/** Keys a supplier's quotation in from their email or PDF. Blank price = not quoted. */
export function RecordQuotation({ rfq }: { rfq: SourcingEventDetail }) {
  const waiting = rfq.invitations.filter((i) => i.status === 'Invited')
  const record = useRecordQuotation(rfq.id)

  const [supplierId, setSupplierId] = useState('')
  const [reference, setReference] = useState('')
  const [validUntil, setValidUntil] = useState(() => addDays(30))
  const [leadTime, setLeadTime] = useState('14')
  const [terms, setTerms] = useState('30')
  const blankLines = () => Object.fromEntries(rfq.lines.map((l) => [l.id, { unitPrice: '', quantity: String(l.quantity), notes: '' }]))
  const [lines, setLines] = useState<Record<string, LineDraft>>(blankLines)

  if (waiting.length === 0) {
    return (
      <Panel className="px-6 py-5">
        <h2 className="text-xl font-normal">Record a quotation</h2>
        <p className="mt-2 text-sm text-muted-foreground">Every invited supplier has quoted or declined.</p>
      </Panel>
    )
  }

  const setLine = (id: string, patch: Partial<LineDraft>) => setLines((all) => ({ ...all, [id]: { ...all[id]!, ...patch } }))
  const priced = rfq.lines.filter((l) => lines[l.id]?.unitPrice.trim())
  const total = priced.reduce((sum, l) => sum + Number(lines[l.id]!.unitPrice) * Number(lines[l.id]!.quantity), 0)
  const supplier = waiting.find((i) => i.supplierId === supplierId)

  const submit = async (e: FormEvent) => {
    e.preventDefault()
    try {
      await record.mutateAsync({
        supplierId,
        supplierReference: reference.trim() || null,
        validUntil,
        leadTimeDays: Number(leadTime),
        paymentTermsDays: Number(terms),
        lines: priced.map((l) => ({
          sourcingEventLineId: l.id,
          unitPrice: Number(lines[l.id]!.unitPrice),
          quantity: Number(lines[l.id]!.quantity),
          notes: lines[l.id]!.notes.trim() || null,
        })),
      })
      toast(`${supplier?.supplierName ?? 'Supplier'}'s quotation recorded`)
      setSupplierId('')
      setReference('')
      setLines(blankLines())
    } catch { /* shown below */ }
  }

  return (
    <Panel className="px-6 py-5">
      <h2 className="text-xl font-normal">Record a quotation</h2>
      <p className="mt-1 mb-4 text-sm text-muted-foreground">Key it in from the supplier's email or PDF. Leave a price blank for a line they didn't quote.</p>
      <form onSubmit={submit} className="flex flex-col gap-4">
        <div className="grid grid-cols-[repeat(auto-fit,minmax(150px,1fr))] gap-3">
          <Field label="Supplier" className="col-span-full sm:col-span-2">
            <FieldSelect aria-label="Supplier" value={supplierId} onChange={setSupplierId}
              options={waiting.map((i) => ({ value: i.supplierId, label: i.supplierName }))} />
          </Field>
          <Field label="Their reference">
            <input value={reference} maxLength={50} onChange={(e) => setReference(e.target.value)} className={fieldClass} placeholder="Q-0412" />
          </Field>
          <Field label="Valid until">
            <input required type="date" value={validUntil} onChange={(e) => setValidUntil(e.target.value)} className={fieldClass} />
          </Field>
          <Field label="Lead time (days)">
            <input required type="number" min={0} max={365} value={leadTime} onChange={(e) => setLeadTime(e.target.value)} className={fieldClass} />
          </Field>
          <Field label="Payment terms (days)">
            <input required type="number" min={0} max={180} value={terms} onChange={(e) => setTerms(e.target.value)} className={fieldClass} />
          </Field>
        </div>

        <div className="overflow-x-auto">
          <table className="w-full min-w-[640px] border-collapse text-sm">
            <thead>
              <tr className="text-left text-xs text-muted-foreground">
                <th scope="col" className="p-2 font-semibold">Line</th>
                <th scope="col" className="w-28 p-2 font-semibold">Qty</th>
                <th scope="col" className="w-40 p-2 font-semibold">Unit price (₦)</th>
                <th scope="col" className="p-2 font-semibold">Notes</th>
                <th scope="col" className="p-2 text-right font-semibold">Line total</th>
              </tr>
            </thead>
            <tbody>
              {rfq.lines.map((l) => {
                const d = lines[l.id]!
                const lineTotal = d.unitPrice.trim() ? Number(d.unitPrice) * Number(d.quantity) : null
                return (
                  <tr key={l.id} className="border-t">
                    <th scope="row" className="p-2 text-left font-normal">
                      <span className="font-semibold">{l.lineNo}. {l.description}</span>
                      <span className="block text-xs text-muted-foreground">est. {formatNaira(l.estimatedUnitPrice)} / {l.uomCode}</span>
                    </th>
                    <td className="p-2">
                      <input type="number" min={0.0001} step="any" required={!!d.unitPrice.trim()} value={d.quantity}
                        aria-label={`Quantity for line ${l.lineNo}`} onChange={(e) => setLine(l.id, { quantity: e.target.value })}
                        className={cn(fieldClass, 'h-9')} />
                    </td>
                    <td className="p-2">
                      <input type="number" min={0.01} step="any" value={d.unitPrice} placeholder="Not quoted"
                        aria-label={`Unit price for line ${l.lineNo}`} onChange={(e) => setLine(l.id, { unitPrice: e.target.value })}
                        className={cn(fieldClass, 'h-9')} />
                    </td>
                    <td className="p-2">
                      <input value={d.notes} maxLength={500} aria-label={`Notes for line ${l.lineNo}`}
                        onChange={(e) => setLine(l.id, { notes: e.target.value })} className={cn(fieldClass, 'h-9')} />
                    </td>
                    <td className="p-2 text-right font-semibold">{lineTotal === null ? '–' : formatNaira(lineTotal)}</td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        </div>

        {record.error && <p role="alert" className="rounded-xl bg-destructive/10 px-3.5 py-3 text-[13px] text-destructive">{problemMessage(record.error)}</p>}

        <div className="flex flex-wrap items-center justify-end gap-4">
          <span className="text-sm text-muted-foreground">
            {priced.length} of {rfq.lines.length} lines priced · <strong className="text-foreground">{formatNaira(total)}</strong>
          </span>
          <Button type="submit" size="pill" variant="brand" disabled={!supplierId || priced.length === 0 || record.isPending}>
            {record.isPending ? 'Saving…' : 'Record quotation'}
          </Button>
        </div>
      </form>
    </Panel>
  )
}

function Field({ label, className, children }: { label: string; className?: string; children: ReactNode }) {
  return (
    <label className={cn('flex flex-col gap-1.5 text-[13px] font-semibold', className)}>
      {label}
      {children}
    </label>
  )
}

function addDays(days: number) {
  const d = new Date()
  d.setDate(d.getDate() + days)
  return d.toISOString().slice(0, 10)
}
