import { useState, type FormEvent } from 'react'
import { toast } from 'sonner'
import { HugeiconsIcon } from '@hugeicons/react'
import { Delete02Icon, PencilEdit02Icon, PlusSignIcon } from '@hugeicons/core-free-icons'
import { FieldSelect } from '@/components/field-select'
import { FormField } from '@/components/form-field'
import { Panel } from '@/components/panel'
import { Button } from '@/components/ui/button'
import { problemMessage } from '@/lib/api-client'
import { fieldClass } from '@/lib/field-class'
import { cn } from '@/lib/utils'
import {
  useRemoveAddress, useRemoveContact, useSaveAddress, useSaveContact, useUpdateSupplierDetails,
  type AddressInput, type ContactInput, type SupplierAddress, type SupplierContact, type SupplierDetail,
} from './suppliers-api'

/** Edit what can change after onboarding: trading name, payment terms, contacts and addresses. */
export function SupplierEditor({ s, onDone }: { s: SupplierDetail; onDone: () => void }) {
  return (
    <Panel className="flex flex-col gap-6 px-6 py-5">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h2 className="text-xl font-normal">Edit supplier</h2>
          <p className="mt-1 text-sm text-muted-foreground">The legal name and tax ID are fixed. Each change saves on its own.</p>
        </div>
        <Button size="pill-sm" variant="outline" onClick={onDone}>Done</Button>
      </div>
      <Details s={s} />
      <Contacts s={s} />
      <Addresses s={s} />
    </Panel>
  )
}

function Details({ s }: { s: SupplierDetail }) {
  const save = useUpdateSupplierDetails(s.id)
  const [tradingName, setTradingName] = useState(s.tradingName ?? '')
  const [terms, setTerms] = useState(String(s.paymentTermsDays))
  const changed = tradingName.trim() !== (s.tradingName ?? '') || Number(terms) !== s.paymentTermsDays

  const submit = (e: FormEvent) => {
    e.preventDefault()
    save.mutate({ tradingName: tradingName.trim() || null, paymentTermsDays: Number(terms) }, { onSuccess: () => toast('Details saved') })
  }

  return (
    <form onSubmit={submit} className="flex flex-wrap items-end gap-3">
      <FormField id="sup-trading" label="Trading name (optional)" className="min-w-0 flex-[2_1_260px]">
        <input id="sup-trading" maxLength={200} value={tradingName} onChange={(e) => setTradingName(e.target.value)} className={fieldClass} />
      </FormField>
      <FormField id="sup-terms" label="Payment terms (days)" className="w-44">
        <input id="sup-terms" type="number" min={0} max={180} required value={terms} onChange={(e) => setTerms(e.target.value)} className={fieldClass} />
      </FormField>
      <Button type="submit" size="pill" variant="outline" disabled={!changed || save.isPending}>{save.isPending ? 'Saving…' : 'Save details'}</Button>
      {save.error && <p role="alert" className="basis-full text-[13px] text-destructive">{problemMessage(save.error)}</p>}
      <p className="basis-full text-xs text-muted-foreground">New payment terms apply to orders raised from now on; existing orders keep theirs.</p>
    </form>
  )
}

// ---- contacts ----

function Contacts({ s }: { s: SupplierDetail }) {
  const remove = useRemoveContact(s.id)
  const [editing, setEditing] = useState<string | 'new' | null>(null)

  return (
    <section className="flex flex-col gap-2">
      <div className="flex items-center justify-between gap-3 border-t pt-4">
        <h3 className="text-base font-semibold">Contacts</h3>
        {editing !== 'new' && (
          <Button size="pill-sm" variant="ghost" onClick={() => setEditing('new')}><HugeiconsIcon icon={PlusSignIcon} strokeWidth={1.5} /> Add contact</Button>
        )}
      </div>
      <ul className="flex flex-col divide-y">
        {s.contacts.map((c) => editing === c.id ? (
          <li key={c.id} className="py-3"><ContactForm supplierId={s.id} contact={c} onClose={() => setEditing(null)} /></li>
        ) : (
          <li key={c.id} className="flex flex-wrap items-center gap-3 py-2.5 text-sm">
            <span className="min-w-0 flex-1">
              <span className="font-semibold">{c.name}</span>
              {c.isPrimary && <span className="ml-2 rounded-full bg-success px-2 py-0.5 text-[11px] font-bold text-success-foreground">Primary</span>}
              <span className="block text-xs text-muted-foreground">{[c.role, c.email, c.phone].filter(Boolean).join(' · ')}</span>
            </span>
            <IconButton label={`Edit ${c.name}`} icon={PencilEdit02Icon} onClick={() => setEditing(c.id)} />
            <IconButton label={`Remove ${c.name}`} icon={Delete02Icon} danger disabled={s.contacts.length === 1 || remove.isPending}
              onClick={() => remove.mutate(c.id, { onSuccess: () => toast(`${c.name} removed`) })} />
          </li>
        ))}
        {editing === 'new' && <li className="py-3"><ContactForm supplierId={s.id} onClose={() => setEditing(null)} /></li>}
      </ul>
      {s.contacts.length === 1 && <p className="text-xs text-muted-foreground">A supplier needs at least one contact; add another before removing this one.</p>}
      {remove.error && <p role="alert" className="text-[13px] text-destructive">{problemMessage(remove.error)}</p>}
    </section>
  )
}

function ContactForm({ supplierId, contact, onClose }: { supplierId: string; contact?: SupplierContact; onClose: () => void }) {
  const save = useSaveContact(supplierId)
  const [v, setV] = useState<ContactInput>({
    name: contact?.name ?? '', email: contact?.email ?? '', phone: contact?.phone ?? '', role: contact?.role ?? '', isPrimary: contact?.isPrimary ?? false,
  })
  const set = (patch: Partial<ContactInput>) => setV((x) => ({ ...x, ...patch }))
  const key = contact?.id ?? 'new'

  const submit = (e: FormEvent) => {
    e.preventDefault()
    save.mutate({ contactId: contact?.id, body: { ...v, phone: v.phone?.trim() || null, role: v.role?.trim() || null } },
      { onSuccess: () => { toast(contact ? 'Contact saved' : 'Contact added'); onClose() } })
  }

  return (
    <form onSubmit={submit} className="flex flex-col gap-3 rounded-2xl bg-surface p-4">
      <div className="grid grid-cols-[repeat(auto-fit,minmax(200px,1fr))] gap-3">
        <FormField id={`c-name-${key}`} label="Name"><input id={`c-name-${key}`} required maxLength={100} value={v.name} onChange={(e) => set({ name: e.target.value })} className={fieldClass} /></FormField>
        <FormField id={`c-email-${key}`} label="Email"><input id={`c-email-${key}`} type="email" required maxLength={200} value={v.email} onChange={(e) => set({ email: e.target.value })} className={fieldClass} /></FormField>
        <FormField id={`c-phone-${key}`} label="Phone (optional)"><input id={`c-phone-${key}`} maxLength={30} value={v.phone ?? ''} onChange={(e) => set({ phone: e.target.value })} className={fieldClass} /></FormField>
        <FormField id={`c-role-${key}`} label="Role (optional)"><input id={`c-role-${key}`} maxLength={100} value={v.role ?? ''} onChange={(e) => set({ role: e.target.value })} className={fieldClass} placeholder="e.g. Accounts receivable" /></FormField>
      </div>
      <label className="flex items-center gap-2 text-sm">
        <input type="checkbox" checked={v.isPrimary} disabled={contact?.isPrimary} onChange={(e) => set({ isPrimary: e.target.checked })} className="accent-brand" />
        Primary contact {contact?.isPrimary ? <span className="text-xs text-muted-foreground">(make someone else primary to change this)</span> : <span className="text-xs text-muted-foreground">(gets purchase orders and RFQs)</span>}
      </label>
      {save.error && <p role="alert" className="text-[13px] text-destructive">{problemMessage(save.error)}</p>}
      <div className="flex justify-end gap-2">
        <Button type="button" size="pill-sm" variant="ghost" onClick={onClose}>Cancel</Button>
        <Button type="submit" size="pill-sm" variant="brand" disabled={save.isPending}>{save.isPending ? 'Saving…' : contact ? 'Save contact' : 'Add contact'}</Button>
      </div>
    </form>
  )
}

// ---- addresses ----

function Addresses({ s }: { s: SupplierDetail }) {
  const remove = useRemoveAddress(s.id)
  const [editing, setEditing] = useState<string | 'new' | null>(null)

  return (
    <section className="flex flex-col gap-2">
      <div className="flex items-center justify-between gap-3 border-t pt-4">
        <h3 className="text-base font-semibold">Addresses</h3>
        {editing !== 'new' && (
          <Button size="pill-sm" variant="ghost" onClick={() => setEditing('new')}><HugeiconsIcon icon={PlusSignIcon} strokeWidth={1.5} /> Add address</Button>
        )}
      </div>
      {s.addresses.length === 0 && editing !== 'new' && <p className="text-sm text-muted-foreground">No addresses yet. The billing address prints on purchase orders.</p>}
      <ul className="flex flex-col divide-y">
        {s.addresses.map((a) => editing === a.id ? (
          <li key={a.id} className="py-3"><AddressForm supplierId={s.id} address={a} onClose={() => setEditing(null)} /></li>
        ) : (
          <li key={a.id} className="flex flex-wrap items-center gap-3 py-2.5 text-sm">
            <span className="min-w-0 flex-1">
              <span className="block text-xs font-semibold text-muted-foreground">{a.addressType}</span>
              {[a.line1, a.line2, a.city, a.state, a.countryCode].filter(Boolean).join(', ')}
            </span>
            <IconButton label={`Edit ${a.addressType.toLowerCase()} address`} icon={PencilEdit02Icon} onClick={() => setEditing(a.id)} />
            <IconButton label={`Remove ${a.addressType.toLowerCase()} address`} icon={Delete02Icon} danger disabled={remove.isPending}
              onClick={() => remove.mutate(a.id, { onSuccess: () => toast('Address removed') })} />
          </li>
        ))}
        {editing === 'new' && <li className="py-3"><AddressForm supplierId={s.id} onClose={() => setEditing(null)} /></li>}
      </ul>
      {remove.error && <p role="alert" className="text-[13px] text-destructive">{problemMessage(remove.error)}</p>}
    </section>
  )
}

function AddressForm({ supplierId, address, onClose }: { supplierId: string; address?: SupplierAddress; onClose: () => void }) {
  const save = useSaveAddress(supplierId)
  const [v, setV] = useState<AddressInput>({
    addressType: address?.addressType ?? 'Billing', line1: address?.line1 ?? '', line2: address?.line2 ?? '',
    city: address?.city ?? '', state: address?.state ?? '', countryCode: address?.countryCode ?? 'NG',
  })
  const set = (patch: Partial<AddressInput>) => setV((x) => ({ ...x, ...patch }))
  const key = address?.id ?? 'new'

  const submit = (e: FormEvent) => {
    e.preventDefault()
    save.mutate({ addressId: address?.id, body: { ...v, line2: v.line2?.trim() || null, countryCode: v.countryCode.trim().toUpperCase() } },
      { onSuccess: () => { toast(address ? 'Address saved' : 'Address added'); onClose() } })
  }

  return (
    <form onSubmit={submit} className="flex flex-col gap-3 rounded-2xl bg-surface p-4">
      <div className="grid grid-cols-[repeat(auto-fit,minmax(200px,1fr))] gap-3">
        <FormField id={`a-type-${key}`} label="Type">
          <FieldSelect id={`a-type-${key}`} value={v.addressType} onChange={(t) => set({ addressType: t as AddressInput['addressType'] })}
            options={[{ value: 'Billing', label: 'Billing' }, { value: 'Shipping', label: 'Shipping' }]} />
        </FormField>
        <FormField id={`a-l1-${key}`} label="Address line 1" className="col-span-2 max-sm:col-span-1">
          <input id={`a-l1-${key}`} required maxLength={200} value={v.line1} onChange={(e) => set({ line1: e.target.value })} className={fieldClass} />
        </FormField>
        <FormField id={`a-l2-${key}`} label="Address line 2 (optional)">
          <input id={`a-l2-${key}`} maxLength={200} value={v.line2 ?? ''} onChange={(e) => set({ line2: e.target.value })} className={fieldClass} />
        </FormField>
        <FormField id={`a-city-${key}`} label="City"><input id={`a-city-${key}`} required maxLength={100} value={v.city} onChange={(e) => set({ city: e.target.value })} className={fieldClass} /></FormField>
        <FormField id={`a-state-${key}`} label="State"><input id={`a-state-${key}`} required maxLength={100} value={v.state} onChange={(e) => set({ state: e.target.value })} className={fieldClass} /></FormField>
        <FormField id={`a-country-${key}`} label="Country code">
          <input id={`a-country-${key}`} required minLength={2} maxLength={2} value={v.countryCode} onChange={(e) => set({ countryCode: e.target.value })} className={cn(fieldClass, 'uppercase')} />
        </FormField>
      </div>
      {save.error && <p role="alert" className="text-[13px] text-destructive">{problemMessage(save.error)}</p>}
      <div className="flex justify-end gap-2">
        <Button type="button" size="pill-sm" variant="ghost" onClick={onClose}>Cancel</Button>
        <Button type="submit" size="pill-sm" variant="brand" disabled={save.isPending}>{save.isPending ? 'Saving…' : address ? 'Save address' : 'Add address'}</Button>
      </div>
    </form>
  )
}

function IconButton({ label, icon, onClick, danger, disabled }: {
  label: string; icon: typeof PencilEdit02Icon; onClick: () => void; danger?: boolean; disabled?: boolean
}) {
  return (
    <button type="button" aria-label={label} title={label} onClick={onClick} disabled={disabled}
      className={cn('grid size-9 place-items-center rounded-full hover:bg-accent disabled:pointer-events-none disabled:opacity-40', danger && 'text-destructive')}>
      <HugeiconsIcon icon={icon} size={18} strokeWidth={1.5} />
    </button>
  )
}
