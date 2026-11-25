import { useState } from 'react'
import { Link, useParams } from 'react-router'
import { toast } from 'sonner'
import { HugeiconsIcon } from '@hugeicons/react'
import { ArrowLeft01Icon } from '@hugeicons/core-free-icons'
import { Panel } from '@/components/panel'
import { ErrorState } from '@/components/states/error-state'
import { Button } from '@/components/ui/button'
import { Skeleton } from '@/components/ui/skeleton'
import { useCurrentUser } from '@/features/auth/use-auth'
import { problemMessage } from '@/lib/api-client'
import { fieldClass } from '@/lib/field-class'
import { formatDateTime, formatNaira } from '@/lib/format'
import { can, Permission } from '@/lib/permissions'
import { cn } from '@/lib/utils'
import { CountStatusBadge } from './stock-counts'
import { useCountAction, useSaveCounts, useStockCount, type StockCountDetail } from './stock-api'

export function StockCountPage() {
  const { id } = useParams()
  const count = useStockCount(id)
  if (count.isPending) return <Skeleton className="h-96 rounded-[28px]" />
  if (count.isError) {
    return <ErrorState error={count.error} what="this stock count" onRetry={() => count.refetch()} back={{ to: '/warehouse?tab=counts', label: 'Back to counts' }} />
  }
  return <Count key={count.data.status} c={count.data} />
}

const toText = (v: number | null) => (v === null ? '' : String(v))

function Count({ c }: { c: StockCountDetail }) {
  const { data: user } = useCurrentUser()
  const editable = c.status === 'Open' && can(user, Permission.StockCount)
  const save = useSaveCounts(c.id)
  const action = useCountAction(c.id)
  const [counted, setCounted] = useState<Record<string, string>>(() => Object.fromEntries(c.lines.map((l) => [l.id, toText(l.counted)])))
  const [notes, setNotes] = useState('')
  const [search, setSearch] = useState('')

  const parse = (v: string | undefined) => (!v || v.trim() === '' ? null : Number(v))
  const dirty = c.lines.filter((l) => parse(counted[l.id]) !== l.counted)
  const preview = c.lines.map((l) => {
    const n = editable ? parse(counted[l.id]) : l.counted
    const variance = n === null ? 0 : n - l.expected
    return { ...l, now: n, variance, value: Math.round(variance * l.unitCost * 100) / 100 }
  })
  const done = preview.filter((l) => l.now !== null).length
  const different = preview.filter((l) => l.variance !== 0)
  const net = different.reduce((s, l) => s + l.value, 0)
  const shown = preview.filter((l) => !search || `${l.itemCode} ${l.description}`.toLowerCase().includes(search.toLowerCase()))
  const error = save.error ?? action.error

  const saveCounts = (then?: () => void) =>
    save.mutate(dirty.map((l) => ({ lineId: l.id, counted: parse(counted[l.id]) })), { onSuccess: () => (then ? then() : toast('Counts saved')) })
  const post = () => action.mutate({ action: 'post', notes: notes.trim() || null }, { onSuccess: () => toast(`${c.number} posted`) })

  return (
    <div className="flex flex-col gap-6">
      <section className="flex flex-col gap-3">
        <Link to="/warehouse?tab=counts" className="flex items-center gap-1.5 self-start text-sm font-semibold hover:text-brand">
          <HugeiconsIcon icon={ArrowLeft01Icon} size={18} strokeWidth={1.5} /> Counts
        </Link>
        <div className="flex flex-wrap items-end justify-between gap-5">
          <div>
            <p className="mb-1 text-[13px] font-semibold text-muted-foreground">{c.number} · started by {c.startedByName} {formatDateTime(c.startedAt)}</p>
            <h1 className="text-[40px] leading-tight font-light tracking-tight">Stock count · {c.siteCode} {c.siteName}</h1>
            <div className="mt-3 flex flex-wrap items-center gap-2 text-sm">
              <CountStatusBadge status={c.status} />
              <span className="text-muted-foreground">
                {c.status === 'Posted' ? `Posted by ${c.postedByName} ${c.postedAt ? formatDateTime(c.postedAt) : ''}` : `${done} of ${c.lines.length} counted`}
              </span>
            </div>
          </div>
          <div className="text-right">
            <div className="text-[13px] text-muted-foreground">{c.status === 'Posted' ? 'Adjusted at cost' : 'Difference so far'}</div>
            <div className={cn('font-heading text-[40px] leading-tight font-light', net < 0 && 'text-destructive')}>{formatNaira(net)}</div>
          </div>
        </div>
      </section>

      {c.notes && <Panel className="px-6 py-4 text-sm"><span className="font-semibold">Note: </span>{c.notes}</Panel>}

      <Panel className="px-6 py-5">
        <div className="mb-3 flex flex-wrap items-center justify-between gap-3">
          <p className="text-sm text-muted-foreground">
            {editable ? 'Count what is physically on the shelf. "System" is the ledger now; posting compares against it again at that moment.' : 'What was counted against what the system said.'}
          </p>
          {c.lines.length > 8 && (
            <input type="search" aria-label="Find an item" placeholder="Find an item…" value={search} onChange={(e) => setSearch(e.target.value)} className={cn(fieldClass, 'h-9 w-56')} />
          )}
        </div>
        <div className="overflow-x-auto">
          <table className="w-full min-w-[720px] border-collapse text-sm">
            <thead>
              <tr className="text-left text-xs text-muted-foreground">
                <th scope="col" className="p-2 font-semibold">Item</th>
                <th scope="col" className="p-2 text-right font-semibold">System</th>
                <th scope="col" className="w-36 p-2 font-semibold">Counted</th>
                <th scope="col" className="p-2 text-right font-semibold">Difference</th>
                <th scope="col" className="p-2 text-right font-semibold">At cost</th>
              </tr>
            </thead>
            <tbody>
              {shown.map((l) => (
                <tr key={l.id} className="border-t">
                  <th scope="row" className="p-2 text-left font-normal"><span className="font-semibold">{l.description}</span><span className="block text-xs text-muted-foreground">{l.itemCode}</span></th>
                  <td className="p-2 text-right">{l.expected} {l.uomCode}</td>
                  <td className="p-2">
                    {editable ? (
                      <input type="number" min={0} step="any" aria-label={`Counted ${l.description}`} value={counted[l.id] ?? ''} placeholder="–"
                        onChange={(e) => setCounted((m) => ({ ...m, [l.id]: e.target.value }))} className={cn(fieldClass, 'h-9 text-right')} />
                    ) : <span className="font-semibold">{l.counted ?? '–'} {l.counted !== null && l.uomCode}</span>}
                  </td>
                  <td className={cn('p-2 text-right font-semibold', l.variance < 0 && 'text-destructive', l.variance > 0 && 'text-success-foreground')}>
                    {l.now === null ? '' : l.variance === 0 ? <span className="font-normal text-muted-foreground">Matches</span> : `${l.variance > 0 ? '+' : ''}${l.variance}`}
                  </td>
                  <td className="p-2 text-right">{l.variance === 0 ? '' : formatNaira(l.value)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </Panel>

      {editable && (
        <Panel className="flex flex-col gap-3 px-6 py-5">
          {different.length > 0 && (
            <div className="flex flex-col gap-1.5">
              <label htmlFor="count-notes" className="text-[13px] font-semibold">Why do {different.length} item{different.length === 1 ? '' : 's'} differ? (needed to post)</label>
              <textarea id="count-notes" rows={2} maxLength={1000} value={notes} onChange={(e) => setNotes(e.target.value)} className={cn(fieldClass, 'h-auto py-2.5')}
                placeholder="e.g. Two toner cartridges damaged by the roof leak in bay 3; one laptop found unrecorded in the cage" />
            </div>
          )}
          {error && <p role="alert" className="rounded-xl bg-destructive/10 px-3.5 py-3 text-[13px] text-destructive">{problemMessage(error)}</p>}
          <div className="flex flex-wrap items-center justify-end gap-2">
            <Button size="pill" variant="ghost" className="text-destructive hover:text-destructive" disabled={action.isPending}
              onClick={() => action.mutate({ action: 'cancel' }, { onSuccess: () => toast('Count cancelled') })}>Cancel count</Button>
            <Button size="pill" variant="outline" disabled={dirty.length === 0 || save.isPending} onClick={() => saveCounts()}>
              {save.isPending ? 'Saving…' : 'Save counts'}
            </Button>
            <Button size="pill" variant="brand" disabled={done < c.lines.length || (different.length > 0 && !notes.trim()) || action.isPending || save.isPending}
              onClick={() => (dirty.length ? saveCounts(post) : post())}>
              Post count
            </Button>
          </div>
        </Panel>
      )}
    </div>
  )
}
