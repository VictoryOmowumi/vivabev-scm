import { useState, type FormEvent } from 'react'
import { FieldSelect } from '@/components/field-select'
import { toast } from 'sonner'
import { HugeiconsIcon } from '@hugeicons/react'
import { Cancel01Icon } from '@hugeicons/core-free-icons'
import { FormField } from '@/components/form-field'
import { Panel } from '@/components/panel'
import { Button } from '@/components/ui/button'
import { Skeleton } from '@/components/ui/skeleton'
import { problemMessage } from '@/lib/api-client'
import { fieldClass } from '@/lib/field-class'
import { cn } from '@/lib/utils'
import {
  itemTypeLabels, itemTypes, useCategories, useCreateItem, useItem, useSetItemActive, useUnits, useUpdateItem,
  type ItemDetail, type ItemType,
} from './catalogue-api'

/** The right-hand card: view or edit one item, or create a new one (id null). */
export function ItemPanel({ id, manage, defaultCategoryId, onClose, onCreated }: {
  id: string | null
  manage: boolean
  defaultCategoryId: string | null
  onClose: () => void
  onCreated: (id: string) => void
}) {
  const item = useItem(id)

  return (
    <Panel aria-label={id ? 'Item' : 'New item'} className="flex min-w-0 flex-[1_1_360px] flex-col gap-4 p-6 lg:sticky lg:top-6 lg:max-w-[440px]">
      <div className="flex items-start justify-between gap-3">
        <div>
          <p className="text-[13px] font-semibold text-muted-foreground">{id ? item.data?.code ?? 'Item' : 'New catalogue item'}</p>
          <h2 className="mt-1 text-[26px] leading-tight font-light">{id ? item.data?.name ?? '…' : 'Add an item'}</h2>
        </div>
        <button aria-label="Close" onClick={onClose} className="flex size-9 flex-none items-center justify-center rounded-full bg-surface hover:opacity-80">
          <HugeiconsIcon icon={Cancel01Icon} size={16} strokeWidth={1.5} />
        </button>
      </div>

      {id && item.isPending && <Skeleton className="h-64 rounded-2xl" />}
      {id && item.isError && <p className="text-sm text-destructive">{problemMessage(item.error)}</p>}
      {(!id || item.data) && (
        <ItemForm key={item.data?.id ?? 'new'} item={item.data ?? null} manage={manage} defaultCategoryId={defaultCategoryId}
          onCreated={onCreated} />
      )}
    </Panel>
  )
}

function ItemForm({ item, manage, defaultCategoryId, onCreated }: {
  item: ItemDetail | null
  manage: boolean
  defaultCategoryId: string | null
  onCreated: (id: string) => void
}) {
  const categories = useCategories()
  const units = useUnits()
  const create = useCreateItem()
  const update = useUpdateItem(item?.id ?? '')
  const setActive = useSetItemActive(item?.id ?? '')
  const [code, setCode] = useState('')
  const [name, setName] = useState(item?.name ?? '')
  const [description, setDescription] = useState(item?.description ?? '')
  const [itemType, setItemType] = useState<ItemType>(item?.itemType ?? 'Consumable')
  const [categoryId, setCategoryId] = useState(item?.categoryId ?? defaultCategoryId ?? '')
  const [uom, setUom] = useState(item?.baseUomCode ?? 'EA')
  const [stocked, setStocked] = useState(item?.isStocked ?? false)
  const [lot, setLot] = useState(item?.isLotControlled ?? false)
  const [shelfLife, setShelfLife] = useState(item?.shelfLifeDays?.toString() ?? '')
  const readOnly = !manage
  const service = itemType === 'Service'
  const error = create.error ?? update.error ?? setActive.error

  const submit = async (e: FormEvent) => {
    e.preventDefault()
    const input = {
      name: name.trim(),
      description: description.trim() || null,
      categoryId,
      isStocked: !service && stocked,
      isLotControlled: !service && stocked && lot,
      shelfLifeDays: shelfLife.trim() ? Number(shelfLife) : null,
    }
    try {
      if (item) {
        await update.mutateAsync(input)
        toast(`${item.code} saved`)
      } else {
        const created = await create.mutateAsync({ ...input, code: code.trim(), itemType, baseUomCode: uom })
        toast(`${created.code} added to the catalogue`)
        onCreated(created.id)
      }
    } catch { /* shown below */ }
  }

  return (
    <form onSubmit={submit} className="flex flex-col gap-3.5">
      {!item && (
        <FormField id="item-code" label="Code" hint="Short and unique, e.g. IT-MOUSE. It can't change later.">
          <input id="item-code" required maxLength={30} value={code} onChange={(e) => setCode(e.target.value)}
            className={cn(fieldClass, 'uppercase')} />
        </FormField>
      )}
      <FormField id="item-name" label="Name">
        <input id="item-name" required maxLength={200} value={name} disabled={readOnly} onChange={(e) => setName(e.target.value)} className={fieldClass} />
      </FormField>
      <div className="grid grid-cols-2 gap-3">
        <FormField id="item-type" label="Type">
          <FieldSelect id="item-type" value={itemType} disabled={!!item || readOnly} onChange={(v) => setItemType(v as ItemType)}
            options={itemTypes.map((t) => ({ value: t, label: itemTypeLabels[t] }))} />
        </FormField>
        <FormField id="item-uom" label="Unit">
          <FieldSelect id="item-uom" value={uom} disabled={!!item || readOnly} onChange={setUom}
            options={units.data ? units.data.map((u) => ({ value: u.code, label: `${u.code} · ${u.name}` })) : [{ value: uom, label: uom }]} />
        </FormField>
      </div>
      <FormField id="item-category" label="Category" hint="Suppliers are approved by category, so this decides who can quote for it.">
        <FieldSelect id="item-category" value={categoryId} disabled={readOnly} onChange={setCategoryId}
          options={(categories.data ?? []).filter((c) => c.isActive || c.id === categoryId).map((c) => ({ value: c.id, label: `${c.name} (${c.code})` }))} />
      </FormField>
      <FormField id="item-description" label="Description">
        <textarea id="item-description" rows={2} maxLength={1000} value={description} disabled={readOnly}
          onChange={(e) => setDescription(e.target.value)} className={cn(fieldClass, 'h-auto py-2.5')}
          placeholder="Spec, brand or size that buyers and suppliers need" />
      </FormField>

      <fieldset disabled={readOnly || service} className="flex flex-col gap-2 rounded-2xl bg-surface p-4 disabled:opacity-70">
        <legend className="sr-only">Stock control</legend>
        <label className="flex items-center gap-2.5 text-sm">
          <input type="checkbox" checked={!service && stocked} onChange={(e) => { setStocked(e.target.checked); if (!e.target.checked) setLot(false) }} />
          <span><strong>Kept in stock</strong> · the warehouse tracks what's on hand</span>
        </label>
        <label className="flex items-center gap-2.5 text-sm">
          <input type="checkbox" checked={!service && stocked && lot} disabled={!stocked} onChange={(e) => setLot(e.target.checked)} />
          <span><strong>Lot controlled</strong> · each delivery is tracked by batch</span>
        </label>
        <label className="flex items-center gap-2.5 text-sm">
          Shelf life
          <input type="number" min={1} value={shelfLife} onChange={(e) => setShelfLife(e.target.value)} placeholder="–"
            aria-label="Shelf life in days" className={cn(fieldClass, 'h-9 w-24')} /> days
        </label>
        {service && <p className="text-xs text-muted-foreground">Services aren't stocked.</p>}
      </fieldset>

      {error && <p role="alert" className="rounded-xl bg-destructive/10 px-3.5 py-3 text-[13px] text-destructive">{problemMessage(error)}</p>}

      {manage && (
        <div className="flex flex-wrap items-center justify-between gap-2">
          {item ? (
            <Button type="button" size="pill-sm" variant="ghost" disabled={setActive.isPending}
              className={item.isActive ? 'text-destructive hover:text-destructive' : undefined}
              onClick={() => setActive.mutate(!item.isActive, { onSuccess: () => toast(item.isActive ? `${item.code} deactivated; it can't be requested` : `${item.code} is active again`) })}>
              {item.isActive ? 'Deactivate' : 'Activate'}
            </Button>
          ) : <span />}
          <Button type="submit" size="pill" variant="brand" disabled={create.isPending || update.isPending || !categoryId}>
            {create.isPending || update.isPending ? 'Saving…' : item ? 'Save changes' : 'Add item'}
          </Button>
        </div>
      )}
      {item && !item.isActive && <p className="text-xs text-muted-foreground">Inactive items can't be put on new requisitions.</p>}
    </form>
  )
}
