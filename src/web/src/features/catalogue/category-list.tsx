import { useState } from 'react'
import { toast } from 'sonner'
import { HugeiconsIcon } from '@hugeicons/react'
import { PlusSignIcon } from '@hugeicons/core-free-icons'
import { Button } from '@/components/ui/button'
import { problemMessage } from '@/lib/api-client'
import { fieldClass } from '@/lib/field-class'
import { cn } from '@/lib/utils'
import { useCategories, useCreateCategory, useRenameCategory, useSetCategoryActive, type CatalogueItem } from './catalogue-api'

/** The dark aside: categories with item counts, to filter by; Procurement can add, rename and retire them. */
export function CategoryList({ items, selected, onSelect, manage }: {
  items: CatalogueItem[]
  selected: string | null
  onSelect: (id: string | null) => void
  manage: boolean
}) {
  const categories = useCategories()
  const create = useCreateCategory()
  const rename = useRenameCategory()
  const setActive = useSetCategoryActive()
  const [adding, setAdding] = useState(false)
  const [code, setCode] = useState('')
  const [name, setName] = useState('')
  const [editing, setEditing] = useState<string | null>(null)
  const [newName, setNewName] = useState('')
  const error = create.error ?? rename.error ?? setActive.error
  const count = (id: string) => items.filter((i) => i.categoryId === id).length
  const current = categories.data?.find((c) => c.id === selected)

  const add = async () => {
    try {
      await create.mutateAsync({ code: code.trim(), name: name.trim() })
      toast(`${name.trim()} added`)
      setAdding(false); setCode(''); setName('')
    } catch { /* shown below */ }
  }

  return (
    <aside aria-label="Categories" className="flex min-w-0 flex-[1_1_240px] flex-col gap-2 rounded-[28px] bg-inverse p-5 text-inverse-foreground lg:max-w-[280px]">
      <div className="flex items-center justify-between">
        <h2 className="text-lg font-normal">Categories</h2>
        {manage && !adding && (
          <button aria-label="Add a category" onClick={() => setAdding(true)}
            className="flex size-8 items-center justify-center rounded-full bg-inverse-row hover:opacity-90">
            <HugeiconsIcon icon={PlusSignIcon} size={16} strokeWidth={1.5} />
          </button>
        )}
      </div>

      {adding && (
        <div className="flex flex-col gap-2 rounded-[18px] bg-inverse-row p-3">
          <input aria-label="Category code" value={code} maxLength={30} onChange={(e) => setCode(e.target.value)} placeholder="Code, e.g. CAT-LAB"
            className={cn(fieldClass, 'h-9 uppercase')} />
          <input aria-label="Category name" value={name} maxLength={100} onChange={(e) => setName(e.target.value)} placeholder="Name, e.g. Lab supplies"
            className={cn(fieldClass, 'h-9')} />
          <div className="flex justify-end gap-1.5">
            <Button size="pill-sm" variant="ghost" className="text-inverse-foreground hover:bg-inverse hover:text-inverse-foreground" onClick={() => setAdding(false)}>Cancel</Button>
            <Button size="pill-sm" variant="brand" disabled={!code.trim() || !name.trim() || create.isPending} onClick={add}>Add</Button>
          </div>
        </div>
      )}

      <ul className="flex flex-col gap-1">
        <li>
          <button onClick={() => onSelect(null)} aria-pressed={!selected}
            className={cn('flex w-full items-center justify-between rounded-[14px] px-3 py-2 text-left text-sm', !selected ? 'bg-inverse-foreground text-inverse font-semibold' : 'hover:bg-inverse-row')}>
            All items <span>{items.length}</span>
          </button>
        </li>
        {categories.data?.map((c) => (
          <li key={c.id}>
            {editing === c.id ? (
              <div className="flex gap-1.5 rounded-[14px] bg-inverse-row p-2">
                <input aria-label={`New name for ${c.name}`} value={newName} maxLength={100} onChange={(e) => setNewName(e.target.value)}
                  className={cn(fieldClass, 'h-8 min-w-0 flex-1')} />
                <Button size="pill-sm" variant="brand" disabled={!newName.trim() || rename.isPending}
                  onClick={() => rename.mutate({ id: c.id, name: newName.trim() }, { onSuccess: () => setEditing(null) })}>Save</Button>
              </div>
            ) : (
              <button onClick={() => onSelect(c.id)} aria-pressed={selected === c.id}
                className={cn('flex w-full items-center justify-between gap-2 rounded-[14px] px-3 py-2 text-left text-sm',
                  selected === c.id ? 'bg-inverse-foreground text-inverse font-semibold' : 'hover:bg-inverse-row',
                  !c.isActive && 'opacity-60')}>
                <span className="min-w-0 truncate">{c.name}{!c.isActive && ' (retired)'}</span>
                <span>{count(c.id)}</span>
              </button>
            )}
          </li>
        ))}
      </ul>

      {manage && current && editing !== current.id && (
        <div className="mt-1 flex flex-wrap gap-1.5 border-t border-inverse-muted/30 pt-3 text-xs">
          <span className="w-full text-inverse-muted">{current.code}</span>
          <Button size="pill-sm" variant="ghost" className="h-7 px-2.5 text-inverse-foreground hover:bg-inverse-row hover:text-inverse-foreground"
            onClick={() => { setEditing(current.id); setNewName(current.name) }}>Rename</Button>
          <Button size="pill-sm" variant="ghost" className="h-7 px-2.5 text-inverse-foreground hover:bg-inverse-row hover:text-inverse-foreground"
            disabled={setActive.isPending}
            onClick={() => setActive.mutate({ id: current.id, active: !current.isActive },
              { onSuccess: () => toast(current.isActive ? `${current.name} retired; existing items keep it` : `${current.name} is back in use`) })}>
            {current.isActive ? 'Retire' : 'Bring back'}
          </Button>
        </div>
      )}

      {error && <p role="alert" className="rounded-xl bg-[#f3c9a6] px-3 py-2 text-[13px] text-[#5a2a08]">{problemMessage(error)}</p>}
    </aside>
  )
}
