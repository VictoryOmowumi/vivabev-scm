import { useState, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router'
import { toast } from 'sonner'
import { HugeiconsIcon } from '@hugeicons/react'
import { ArrowLeft01Icon } from '@hugeicons/core-free-icons'
import { FieldSelect } from '@/components/field-select'
import { FormField } from '@/components/form-field'
import { Panel } from '@/components/panel'
import { ErrorState } from '@/components/states/error-state'
import { Button } from '@/components/ui/button'
import { Skeleton } from '@/components/ui/skeleton'
import { problemMessage } from '@/lib/api-client'
import { fieldClass } from '@/lib/field-class'
import { cn } from '@/lib/utils'
import { useAvailableStock, useCreateStockRequest, useStockRequestForm, type StockRequestForm } from './stock-api'

export function NewStockRequestPage() {
  const form = useStockRequestForm()
  return (
    <div className="flex flex-col gap-6">
      <section className="flex flex-col gap-3">
        <Link to="/stock-requests" className="flex items-center gap-1.5 self-start text-sm font-semibold hover:text-brand">
          <HugeiconsIcon icon={ArrowLeft01Icon} size={18} strokeWidth={1.5} /> Stock requests
        </Link>
        <h1 className="text-[40px] leading-tight font-light tracking-tight">Request stock</h1>
        <p className="text-[15px] text-muted-foreground">Only items the warehouse holds are listed. For anything else, raise a requisition.</p>
      </section>
      {form.isPending && <Skeleton className="h-96 rounded-[28px]" />}
      {form.isError && <ErrorState error={form.error} what="the request form" onRetry={() => form.refetch()} />}
      {form.data && <RequestForm options={form.data} />}
    </div>
  )
}

function RequestForm({ options }: { options: StockRequestForm }) {
  const create = useCreateStockRequest()
  const navigate = useNavigate()
  const [today] = useState(() => new Date().toLocaleDateString('en-CA'))
  const [siteId, setSiteId] = useState(options.defaultSiteId ?? '')
  const [costCentreId, setCostCentreId] = useState(options.costCentres.length === 1 ? options.costCentres[0]!.id : '')
  const [purpose, setPurpose] = useState('')
  const [neededBy, setNeededBy] = useState('')
  const [quantities, setQuantities] = useState<Record<string, string>>({})
  const available = useAvailableStock(siteId)

  const num = (v: string | undefined) => (!v || v.trim() === '' ? 0 : Number(v))
  const chosen = (available.data ?? []).filter((a) => num(quantities[a.itemId]) > 0)
  const ready = siteId && costCentreId && purpose.trim() && chosen.length > 0

  const submit = async (e: FormEvent) => {
    e.preventDefault()
    try {
      const created = await create.mutateAsync({
        siteId, costCentreId, purpose: purpose.trim(), neededBy: neededBy || null,
        lines: chosen.map((a) => ({ itemId: a.itemId, quantity: num(quantities[a.itemId]) })),
      })
      toast(`${created.number} sent to the warehouse`)
      navigate(`/stock-requests/${created.id}`)
    } catch { /* shown below */ }
  }

  if (options.sites.length === 0) {
    return <Panel className="px-6 py-5 text-sm text-muted-foreground">The warehouse has no stock at the moment. Raise a requisition to buy what you need.</Panel>
  }

  return (
    <form onSubmit={submit} className="flex flex-col gap-6">
      <Panel className="grid grid-cols-[repeat(auto-fit,minmax(240px,1fr))] gap-4 px-6 py-5">
        <FormField id="site" label="From">
          <FieldSelect id="site" value={siteId} onChange={(v) => { setSiteId(v); setQuantities({}) }} placeholder="Choose a site…"
            options={options.sites.map((s) => ({ value: s.id, label: `${s.code} · ${s.name}` }))} />
        </FormField>
        <FormField id="cc" label="Charge to" hint={options.costCentres.length === 0 ? 'Your department has no active cost centres. Ask Finance.' : undefined}>
          <FieldSelect id="cc" value={costCentreId} onChange={setCostCentreId} placeholder="Choose a cost centre…"
            options={options.costCentres.map((c) => ({ value: c.id, label: `${c.code} · ${c.name}` }))} />
        </FormField>
        <FormField id="needed" label="Needed by (optional)">
          <input id="needed" type="date" min={today} value={neededBy} onChange={(e) => setNeededBy(e.target.value)} className={fieldClass} />
        </FormField>
        <FormField id="purpose" label="What it's for" className="col-span-full">
          <input id="purpose" required maxLength={500} value={purpose} onChange={(e) => setPurpose(e.target.value)} className={fieldClass}
            placeholder="e.g. Replacement laptop for the new Ikeja sales rep" />
        </FormField>
      </Panel>

      {siteId && (
        <Panel className="px-6 py-5">
          <h2 className="mb-1 text-xl font-normal">What you need</h2>
          <p className="mb-3 text-sm text-muted-foreground">"Spoken for" is what earlier requests are still waiting for. You can still ask; the warehouse decides.</p>
          {available.isPending && <Skeleton className="h-40 rounded-2xl" />}
          {available.data?.length === 0 && <p className="text-sm text-muted-foreground">Nothing in stock at this site.</p>}
          {!!available.data?.length && (
            <div className="overflow-x-auto">
              <table className="w-full min-w-[640px] border-collapse text-sm">
                <thead>
                  <tr className="text-left text-xs text-muted-foreground">
                    <th scope="col" className="p-2 font-semibold">Item</th>
                    <th scope="col" className="p-2 text-right font-semibold">In stock</th>
                    <th scope="col" className="p-2 text-right font-semibold">Spoken for</th>
                    <th scope="col" className="w-36 p-2 font-semibold">Quantity</th>
                  </tr>
                </thead>
                <tbody>
                  {available.data.map((a) => {
                    const free = Math.max(a.onHand - a.reserved, 0)
                    const over = num(quantities[a.itemId]) > free
                    return (
                      <tr key={a.itemId} className="border-t">
                        <th scope="row" className="p-2 text-left font-normal">
                          <span className="font-semibold">{a.description}</span>
                          <span className="block text-xs text-muted-foreground">{a.itemCode}</span>
                        </th>
                        <td className="p-2 text-right">{a.onHand} {a.uomCode}</td>
                        <td className="p-2 text-right text-muted-foreground">{a.reserved || '–'}</td>
                        <td className="p-2">
                          <input type="number" min={0} step="any" placeholder="0" aria-label={`Quantity of ${a.description}`}
                            value={quantities[a.itemId] ?? ''} onChange={(e) => setQuantities((q) => ({ ...q, [a.itemId]: e.target.value }))}
                            className={cn(fieldClass, 'h-9 text-right')} />
                          {over && <span className="mt-1 block text-xs text-over-foreground">Only {free} free; may be issued in part</span>}
                        </td>
                      </tr>
                    )
                  })}
                </tbody>
              </table>
            </div>
          )}
        </Panel>
      )}

      {create.error && <p role="alert" className="rounded-xl bg-destructive/10 px-3.5 py-3 text-[13px] text-destructive">{problemMessage(create.error)}</p>}
      <div className="flex justify-end">
        <Button type="submit" size="pill" variant="brand" disabled={!ready || create.isPending}>
          {create.isPending ? 'Sending…' : `Send request${chosen.length ? ` · ${chosen.length} item${chosen.length === 1 ? '' : 's'}` : ''}`}
        </Button>
      </div>
    </form>
  )
}
