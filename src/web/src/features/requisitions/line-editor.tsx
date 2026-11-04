import { useDeferredValue, useState, type FormEvent } from 'react'
import { HugeiconsIcon } from '@hugeicons/react'
import { PlusSignIcon, Search01Icon } from '@hugeicons/core-free-icons'
import { FormField } from '@/components/form-field'
import { Button } from '@/components/ui/button'
import { problemMessage } from '@/lib/api-client'
import { fieldClass } from '@/lib/field-class'
import { formatNaira } from '@/lib/format'
import { cn } from '@/lib/utils'
import { useAddLine, useCategories, useItemSearch, useUnits, type CatalogueItem, type NewLine } from './requisition-editor-api'

type Mode = 'catalogue' | 'free-text'

/** Adds one line: an item picked from the catalogue, or a free-text line with its own category. */
export function LineEditor({ requisitionId }: { requisitionId: string }) {
  const [mode, setMode] = useState<Mode>('catalogue')
  const addLine = useAddLine(requisitionId)

  return (
    <div className="flex flex-col gap-4 rounded-2xl border border-dashed border-input p-4">
      <div role="tablist" aria-label="Kind of line" className="flex gap-1 self-start rounded-full bg-accent p-1">
        {([['catalogue', 'From the catalogue'], ['free-text', 'Free text']] as const).map(([value, label]) => (
          <button key={value} role="tab" aria-selected={mode === value} onClick={() => { setMode(value); addLine.reset() }}
            className={cn('rounded-full px-3.5 py-1.5 text-[13px] font-medium',
              mode === value ? 'bg-inverse font-semibold text-inverse-foreground' : 'hover:bg-accent')}>
            {label}
          </button>
        ))}
      </div>

      {mode === 'catalogue'
        ? <CatalogueLineForm onAdd={(line) => addLine.mutateAsync(line)} pending={addLine.isPending} />
        : <FreeTextLineForm onAdd={(line) => addLine.mutateAsync(line)} pending={addLine.isPending} />}

      {addLine.error && (
        <p role="alert" className="rounded-xl bg-destructive/10 px-3.5 py-3 text-[13px] text-destructive">
          <strong>Couldn't add the line.</strong> {problemMessage(addLine.error)}
        </p>
      )}
    </div>
  )
}

type AddFn = (line: NewLine) => Promise<unknown>

function CatalogueLineForm({ onAdd, pending }: { onAdd: AddFn; pending: boolean }) {
  const [search, setSearch] = useState('')
  const term = useDeferredValue(search) // don't query on every keystroke while typing fast
  const items = useItemSearch(term)
  const [picked, setPicked] = useState<CatalogueItem | null>(null)
  const [quantity, setQuantity] = useState('1')
  const [price, setPrice] = useState('')

  const qty = Number(quantity)
  const unitPrice = Number(price)
  const valid = !!picked && qty > 0 && unitPrice > 0

  async function submit(event: FormEvent) {
    event.preventDefault()
    if (!valid || !picked) return
    try {
      await onAdd({ kind: 'catalogue', itemId: picked.id, quantity: qty, estimatedUnitPrice: unitPrice })
      setPicked(null); setSearch(''); setQuantity('1'); setPrice('')
    } catch { /* shown by the parent */ }
  }

  return (
    <form onSubmit={submit} className="flex flex-col gap-3">
      {!picked ? (
        <>
          <label className="flex h-11 items-center gap-2.5 rounded-xl border border-input bg-surface px-3.5 text-muted-foreground">
            <HugeiconsIcon icon={Search01Icon} size={18} strokeWidth={1.5} />
            <span className="sr-only">Search the catalogue</span>
            <input value={search} onChange={(e) => setSearch(e.target.value)} placeholder="Search by code or name, e.g. laptop"
              className="min-w-0 flex-1 bg-transparent text-sm text-foreground outline-none placeholder:text-muted-foreground" />
          </label>
          {term.trim().length >= 2 && (
            <ul className="flex max-h-60 flex-col overflow-y-auto rounded-xl border bg-surface" aria-label="Matching items">
              {items.isPending && <li className="px-3.5 py-3 text-sm text-muted-foreground">Searching…</li>}
              {items.data?.filter((i) => i.isActive).map((item) => (
                <li key={item.id}>
                  <button type="button" onClick={() => setPicked(item)}
                    className="flex w-full items-center justify-between gap-3 border-b px-3.5 py-2.5 text-left text-sm last:border-b-0 hover:bg-accent">
                    <span><strong>{item.name}</strong> <span className="text-muted-foreground">{item.code}</span></span>
                    <span className="text-xs text-muted-foreground">{item.category} · {item.baseUomCode}</span>
                  </button>
                </li>
              ))}
              {items.data?.filter((i) => i.isActive).length === 0 && (
                <li className="px-3.5 py-3 text-sm text-muted-foreground">
                  Nothing matches "{term}". Use a free-text line instead.
                </li>
              )}
            </ul>
          )}
        </>
      ) : (
        <div className="flex items-center justify-between gap-3 rounded-xl bg-surface px-3.5 py-3 text-sm">
          <span><strong>{picked.name}</strong> <span className="text-muted-foreground">{picked.code} · {picked.category}</span></span>
          <Button type="button" size="pill-sm" variant="ghost" onClick={() => setPicked(null)}>Change</Button>
        </div>
      )}

      <QuantityAndPrice quantity={quantity} price={price} uom={picked?.baseUomCode ?? 'EA'}
        onQuantity={setQuantity} onPrice={setPrice} idPrefix="cat" />
      <AddButton disabled={!valid || pending} pending={pending} total={valid ? qty * unitPrice : null} />
    </form>
  )
}

function FreeTextLineForm({ onAdd, pending }: { onAdd: AddFn; pending: boolean }) {
  const categories = useCategories()
  const units = useUnits()
  const [description, setDescription] = useState('')
  const [categoryId, setCategoryId] = useState('')
  const [lineType, setLineType] = useState<'Goods' | 'Service'>('Goods')
  const [uomCode, setUomCode] = useState('EA')
  const [quantity, setQuantity] = useState('1')
  const [price, setPrice] = useState('')

  const qty = Number(quantity)
  const unitPrice = Number(price)
  const valid = !!description.trim() && !!categoryId && qty > 0 && unitPrice > 0

  async function submit(event: FormEvent) {
    event.preventDefault()
    if (!valid) return
    try {
      await onAdd({ kind: 'free-text', description: description.trim(), categoryId, lineType, uomCode, quantity: qty, estimatedUnitPrice: unitPrice })
      setDescription(''); setQuantity('1'); setPrice('')
    } catch { /* shown by the parent */ }
  }

  return (
    <form onSubmit={submit} className="flex flex-col gap-3">
      <FormField id="ft-description" label="Description">
        <input id="ft-description" className={fieldClass} maxLength={200} value={description}
          placeholder="e.g. UPS 120 kVA modular unit" onChange={(e) => setDescription(e.target.value)} />
      </FormField>
      <div className="grid gap-3 sm:grid-cols-3">
        <FormField id="ft-category" label="Category">
          <select id="ft-category" className={fieldClass} value={categoryId} onChange={(e) => setCategoryId(e.target.value)}>
            <option value="">Choose…</option>
            {categories.data?.filter((c) => c.isActive).map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}
          </select>
        </FormField>
        <FormField id="ft-type" label="Type">
          <select id="ft-type" className={fieldClass} value={lineType} onChange={(e) => setLineType(e.target.value as 'Goods' | 'Service')}>
            <option value="Goods">Goods</option>
            <option value="Service">Service</option>
          </select>
        </FormField>
        <FormField id="ft-uom" label="Unit">
          <select id="ft-uom" className={fieldClass} value={uomCode} onChange={(e) => setUomCode(e.target.value)}>
            {(units.data ?? [{ code: 'EA', name: 'Each', dimension: 'Count' }]).map((u) => (
              <option key={u.code} value={u.code}>{u.code} · {u.name}</option>
            ))}
          </select>
        </FormField>
      </div>
      <QuantityAndPrice quantity={quantity} price={price} uom={uomCode}
        onQuantity={setQuantity} onPrice={setPrice} idPrefix="ft" />
      <AddButton disabled={!valid || pending} pending={pending} total={valid ? qty * unitPrice : null} />
    </form>
  )
}

function QuantityAndPrice({ quantity, price, uom, onQuantity, onPrice, idPrefix }: {
  quantity: string; price: string; uom: string; idPrefix: string
  onQuantity: (v: string) => void; onPrice: (v: string) => void
}) {
  return (
    <div className="grid gap-3 sm:grid-cols-2">
      <FormField id={`${idPrefix}-qty`} label={`Quantity (${uom})`}>
        <input id={`${idPrefix}-qty`} type="number" inputMode="decimal" min="0" step="any" className={fieldClass}
          value={quantity} onChange={(e) => onQuantity(e.target.value)} />
      </FormField>
      <FormField id={`${idPrefix}-price`} label="Estimated unit price (₦)" hint="Your best estimate; Procurement firms it up.">
        <input id={`${idPrefix}-price`} type="number" inputMode="decimal" min="0" step="any" className={fieldClass}
          value={price} placeholder="0" onChange={(e) => onPrice(e.target.value)} />
      </FormField>
    </div>
  )
}

function AddButton({ disabled, pending, total }: { disabled: boolean; pending: boolean; total: number | null }) {
  return (
    <div className="flex items-center justify-end gap-3">
      {total !== null && <span className="text-sm text-muted-foreground">Line total {formatNaira(total)}</span>}
      <Button type="submit" size="pill-sm" disabled={disabled}>
        <HugeiconsIcon icon={PlusSignIcon} strokeWidth={1.5} /> {pending ? 'Adding…' : 'Add line'}
      </Button>
    </div>
  )
}
