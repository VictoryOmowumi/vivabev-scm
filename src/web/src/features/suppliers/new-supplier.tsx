import { useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router'
import { toast } from 'sonner'
import { FormField } from '@/components/form-field'
import { Panel } from '@/components/panel'
import { Button } from '@/components/ui/button'
import { problemMessage } from '@/lib/api-client'
import { fieldClass } from '@/lib/field-class'
import { cn } from '@/lib/utils'
import { useCreateSupplier, type SupplierSummary } from './suppliers-api'

/** Next free code in the SUP-0000 series. */
function nextCode(existing: SupplierSummary[]) {
  const numbers = existing.map((s) => /^SUP-(\d+)$/.exec(s.code)?.[1]).filter(Boolean).map(Number)
  return `SUP-${String((numbers.length ? Math.max(...numbers) : 0) + 1).padStart(4, '0')}`
}

/** A new supplier starts as a prospect: add categories on its page, then send it for approval. */
export function NewSupplier({ existing, onClose }: { existing: SupplierSummary[]; onClose: () => void }) {
  const create = useCreateSupplier()
  const navigate = useNavigate()
  const [form, setForm] = useState(() => ({
    code: nextCode(existing), legalName: '', tradingName: '', taxId: '', contactName: '', contactEmail: '', contactPhone: '', paymentTermsDays: '30',
  }))
  const set = (key: keyof typeof form) => (e: { target: { value: string } }) => setForm((f) => ({ ...f, [key]: e.target.value }))

  const submit = async (e: FormEvent) => {
    e.preventDefault()
    try {
      const created = await create.mutateAsync({
        code: form.code.trim(),
        legalName: form.legalName.trim(),
        tradingName: form.tradingName.trim() || null,
        taxId: form.taxId.trim(),
        contactName: form.contactName.trim(),
        contactEmail: form.contactEmail.trim(),
        contactPhone: form.contactPhone.trim() || null,
        paymentTermsDays: Number(form.paymentTermsDays),
      })
      toast(`${form.legalName.trim()} added as a prospect`, { description: 'Request the categories they supply, then send for approval.' })
      navigate(`/suppliers/${created.id}`)
    } catch { /* shown below */ }
  }

  return (
    <Panel className="px-6 py-5">
      <h2 className="text-xl font-normal">New supplier</h2>
      <p className="mt-1 mb-4 text-sm text-muted-foreground">They start as a prospect and can't be invited to RFQs until approved.</p>
      <form onSubmit={submit} className="grid grid-cols-[repeat(auto-fit,minmax(220px,1fr))] gap-3">
        <FormField id="s-code" label="Code"><input id="s-code" required maxLength={20} value={form.code} onChange={set('code')} className={cn(fieldClass, 'uppercase')} /></FormField>
        <FormField id="s-legal" label="Legal name"><input id="s-legal" required maxLength={200} value={form.legalName} onChange={set('legalName')} className={fieldClass} placeholder="As registered with CAC" /></FormField>
        <FormField id="s-trading" label="Trading name"><input id="s-trading" maxLength={200} value={form.tradingName} onChange={set('tradingName')} className={fieldClass} placeholder="If different" /></FormField>
        <FormField id="s-tax" label="Tax ID (TIN)"><input id="s-tax" required maxLength={20} value={form.taxId} onChange={set('taxId')} className={fieldClass} /></FormField>
        <FormField id="s-contact" label="Contact name"><input id="s-contact" required maxLength={100} value={form.contactName} onChange={set('contactName')} className={fieldClass} /></FormField>
        <FormField id="s-email" label="Contact email"><input id="s-email" type="email" required maxLength={200} value={form.contactEmail} onChange={set('contactEmail')} className={fieldClass} /></FormField>
        <FormField id="s-phone" label="Contact phone"><input id="s-phone" maxLength={30} value={form.contactPhone} onChange={set('contactPhone')} className={fieldClass} placeholder="+234…" /></FormField>
        <FormField id="s-terms" label="Payment terms (days)"><input id="s-terms" type="number" min={0} max={180} required value={form.paymentTermsDays} onChange={set('paymentTermsDays')} className={fieldClass} /></FormField>
        {create.error && <p role="alert" className="col-span-full rounded-xl bg-destructive/10 px-3.5 py-3 text-[13px] text-destructive">{problemMessage(create.error)}</p>}
        <div className="col-span-full flex justify-end gap-2">
          <Button type="button" size="pill" variant="ghost" onClick={onClose}>Cancel</Button>
          <Button type="submit" size="pill" variant="brand" disabled={create.isPending}>{create.isPending ? 'Adding…' : 'Add supplier'}</Button>
        </div>
      </form>
    </Panel>
  )
}
