import { useState, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router'
import { toast } from 'sonner'
import { HugeiconsIcon } from '@hugeicons/react'
import { ArrowLeft01Icon, CheckmarkCircle02Icon } from '@hugeicons/core-free-icons'
import { FormField } from '@/components/form-field'
import { Panel } from '@/components/panel'
import { EmptyState } from '@/components/states/empty-state'
import { ErrorState } from '@/components/states/error-state'
import { TableSkeleton } from '@/components/states/table-skeleton'
import { Button } from '@/components/ui/button'
import { problemMessage } from '@/lib/api-client'
import { fieldClass } from '@/lib/field-class'
import { formatDate, formatNaira } from '@/lib/format'
import { cn } from '@/lib/utils'
import { useCreateEvent, useSourceableRequisitions } from './sourcing-api'

const COMPETITIVE_THRESHOLD = 2_000_000 // mirrors SourcingEvent.CompetitiveThreshold; the API enforces it

function inDays(days: number) {
  const d = new Date()
  d.setDate(d.getDate() + days)
  return d.toLocaleDateString('en-CA')
}

export function NewRfqPage() {
  const waiting = useSourceableRequisitions()
  const create = useCreateEvent()
  const navigate = useNavigate()
  const [selected, setSelected] = useState<string[]>([])
  const [title, setTitle] = useState('')
  const [deadline, setDeadline] = useState(() => inDays(7))

  const chosen = (waiting.data ?? []).filter((r) => selected.includes(r.id))
  const total = chosen.reduce((s, r) => s + r.estimatedTotal, 0)
  const toggle = (id: string) => setSelected((s) => (s.includes(id) ? s.filter((x) => x !== id) : [...s, id]))

  async function submit(event: FormEvent) {
    event.preventDefault()
    const finalTitle = title.trim() || chosen[0]?.title || ''
    if (!chosen.length || !finalTitle) return
    try {
      const created = await create.mutateAsync({ title: finalTitle, responseDeadline: deadline, requisitionIds: selected })
      toast(`${created.number} created`, { description: 'Now invite suppliers, then publish it.' })
      navigate(`/sourcing/${created.id}`, { replace: true })
    } catch { /* shown below */ }
  }

  return (
    <div className="flex flex-col gap-6">
      <section className="flex flex-col gap-3">
        <Link to="/sourcing" className="flex items-center gap-1.5 self-start text-sm font-semibold hover:text-brand">
          <HugeiconsIcon icon={ArrowLeft01Icon} size={18} strokeWidth={1.5} /> Sourcing
        </Link>
        <h1 className="text-[40px] leading-tight font-light tracking-tight">New RFQ</h1>
        <p className="text-[15px] text-muted-foreground">Pick the approved requisitions to quote for together. Similar items get better prices in one RFQ.</p>
      </section>

      {waiting.isPending && <TableSkeleton columns={['Requisition', 'Requester', 'Cost centre', 'Required by', 'Estimated']} rows={3} />}
      {waiting.isError && <ErrorState error={waiting.error} what="approved requisitions" onRetry={() => waiting.refetch()} />}
      {waiting.data?.length === 0 && (
        <EmptyState icon={CheckmarkCircle02Icon} title="Nothing waiting for sourcing">
          Every approved requisition is already on an RFQ. New ones appear here once they're approved.
        </EmptyState>
      )}

      {!!waiting.data?.length && (
        <form onSubmit={submit} className="flex flex-wrap items-start gap-5">
          <Panel className="min-w-0 flex-[2_1_620px] px-3.5 py-2.5">
            <table className="w-full border-collapse text-sm">
              <thead>
                <tr className="text-left text-xs text-muted-foreground">
                  <th scope="col" className="w-10 p-3"><span className="sr-only">Include</span></th>
                  <th scope="col" className="p-3 font-semibold">Requisition</th>
                  <th scope="col" className="p-3 font-semibold">Cost centre</th>
                  <th scope="col" className="p-3 font-semibold">Required by</th>
                  <th scope="col" className="p-3 text-right font-semibold">Estimated</th>
                </tr>
              </thead>
              <tbody>
                {waiting.data.map((r) => {
                  const on = selected.includes(r.id)
                  return (
                    <tr key={r.id} onClick={() => toggle(r.id)} className={cn('cursor-pointer border-t hover:bg-accent', on && 'bg-accent')}>
                      <td className="p-3">
                        <input type="checkbox" checked={on} onChange={() => toggle(r.id)} onClick={(e) => e.stopPropagation()}
                          aria-label={`Include ${r.number}`} className="size-4 accent-primary" />
                      </td>
                      <td className="p-3">
                        <div className="font-semibold">{r.title}</div>
                        <div className="text-xs text-muted-foreground">{r.number} · {r.requesterName} · {r.lineCount} line{r.lineCount === 1 ? '' : 's'}</div>
                      </td>
                      <td className="p-3">{r.costCentreCode}</td>
                      <td className="p-3">{formatDate(r.requiredBy)}</td>
                      <td className="p-3 text-right font-semibold">{formatNaira(r.estimatedTotal)}</td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </Panel>

          <aside className="flex min-w-0 flex-[1_1_340px] flex-col gap-4 rounded-[28px] bg-inverse p-6 text-inverse-foreground lg:sticky lg:top-6">
            <div className="flex items-baseline justify-between">
              <h2 className="text-xl font-normal">{chosen.length} selected</h2>
              <span className="font-heading text-[28px] font-light">{formatNaira(total)}</span>
            </div>
            {total >= COMPETITIVE_THRESHOLD && (
              <p className="rounded-xl bg-inverse-row px-3.5 py-3 text-[13px]">
                Worth {formatNaira(COMPETITIVE_THRESHOLD)} or more, so you'll need to invite at least 3 approved suppliers before publishing.
              </p>
            )}
            <div className="flex flex-col gap-3 text-foreground [&_label]:text-inverse-foreground">
              <FormField id="rfq-title" label="Title" hint={chosen[0] && !title ? `Leave blank to use "${chosen[0].title}"` : undefined}>
                <input id="rfq-title" className={fieldClass} value={title} maxLength={200} onChange={(e) => setTitle(e.target.value)}
                  placeholder="e.g. Data centre power equipment" />
              </FormField>
              <FormField id="rfq-deadline" label="Quotes due by">
                <input id="rfq-deadline" type="date" className={fieldClass} min={inDays(1)} value={deadline} onChange={(e) => setDeadline(e.target.value)} />
              </FormField>
            </div>
            {create.error && <p role="alert" className="rounded-xl bg-[#f3c9a6] px-3.5 py-3 text-[13px] text-[#5a2a08]">{problemMessage(create.error)}</p>}
            <Button type="submit" size="pill" variant="brand" disabled={!chosen.length || create.isPending}>
              {create.isPending ? 'Creating…' : 'Create RFQ'}
            </Button>
          </aside>
        </form>
      )}
    </div>
  )
}
