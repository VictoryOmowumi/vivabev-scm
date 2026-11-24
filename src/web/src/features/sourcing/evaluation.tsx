import { useState } from 'react'
import { FieldSelect } from '@/components/field-select'
import { toast } from 'sonner'
import { HugeiconsIcon } from '@hugeicons/react'
import { Add01Icon, Delete02Icon } from '@hugeicons/core-free-icons'
import { Panel } from '@/components/panel'
import { Button } from '@/components/ui/button'
import { problemMessage } from '@/lib/api-client'
import { fieldClass } from '@/lib/field-class'
import { formatNaira } from '@/lib/format'
import { cn } from '@/lib/utils'
import {
  criterionKindLabels, useRecommendAward, useSetCriteria, useSetScores,
  type CriterionInput, type CriterionKind, type EvaluationCriterion, type QuotationEvaluation, type SourcingEventDetail,
} from './sourcing-api'

const MIN_REASON = 20

/** Criteria, the score matrix, and the recommendation. Editable only while the RFQ is being evaluated. */
export function Evaluation({ rfq, manage }: { rfq: SourcingEventDetail; manage: boolean }) {
  const editable = manage && rfq.status === 'ResponsesClosed'
  const [editingCriteria, setEditingCriteria] = useState(false)
  const unscored = rfq.quotations.filter((q) => q.status === 'Received').length - rfq.evaluation.length
  // Remount the matrix whenever saved scores change, so its draft starts from what's stored
  const scoreKey = JSON.stringify(rfq.evaluation.map((e) => e.scores)) + rfq.criteria.map((c) => c.id).join()

  return (
    <Panel className="flex flex-col gap-4 px-6 py-5">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h2 className="text-xl font-normal">Evaluation</h2>
          <p className="mt-1 text-sm text-muted-foreground">
            Price, delivery and payment terms are scored against the best offer (best = 10). You score the rest from 0 to 10.
            Totals are out of 100.
          </p>
        </div>
        {editable && !editingCriteria && (
          <Button size="pill-sm" variant="outline" onClick={() => setEditingCriteria(true)}>Edit criteria</Button>
        )}
      </div>

      {editingCriteria
        ? <CriteriaEditor rfq={rfq} onDone={() => setEditingCriteria(false)} />
        : rfq.evaluation.length === 0
          ? <p className="text-sm text-muted-foreground">No quotation prices every line, so none can be scored or awarded the whole RFQ.</p>
          : <ScoreMatrix key={scoreKey} rfq={rfq} editable={editable} />}

      {unscored > 0 && (
        <p className="text-[13px] text-muted-foreground">
          {unscored} quotation{unscored === 1 ? " doesn't" : "s don't"} price every line, so {unscored === 1 ? "it isn't" : "they aren't"} scored.
        </p>
      )}
    </Panel>
  )
}

function ScoreMatrix({ rfq, editable }: { rfq: SourcingEventDetail; editable: boolean }) {
  const setScores = useSetScores(rfq.id)
  const initial = Object.fromEntries(rfq.evaluation.flatMap((e) =>
    e.scores.map((s) => [`${s.criterionId}:${e.quotationId}`, s.score === null ? '' : String(s.score)])))
  const [draft, setDraft] = useState<Record<string, string>>(initial)
  const manual = rfq.criteria.filter((c) => c.kind === 'Manual')
  const changed = manual.flatMap((c) => rfq.evaluation.map((e) => `${c.id}:${e.quotationId}`))
    .filter((k) => (draft[k] ?? '') !== (initial[k] ?? ''))
  const invalid = changed.some((k) => !validScore(draft[k] ?? ''))

  const save = async () => {
    try {
      await setScores.mutateAsync(changed.map((k) => {
        const [criterionId, quotationId] = k.split(':') as [string, string]
        const v = draft[k]!.trim()
        return { criterionId, quotationId, score: v === '' ? null : Number(v) }
      }))
      toast('Scores saved')
    } catch { /* shown below */ }
  }

  return (
    <div className="flex flex-col gap-3">
      <div className="overflow-x-auto">
        <table className="w-full border-collapse text-sm" style={{ minWidth: 300 + rfq.evaluation.length * 170 }}>
          <thead>
            <tr className="text-left align-bottom text-xs text-muted-foreground">
              <th scope="col" className="p-2.5 font-semibold">Criterion</th>
              <th scope="col" className="p-2.5 text-right font-semibold">Weight</th>
              {rfq.evaluation.map((e) => (
                <th key={e.quotationId} scope="col" className={cn('p-2.5 text-right font-semibold', e.rank === 1 && 'bg-success/50')}>
                  <span className="block text-sm text-foreground">{e.supplierName}</span>
                  <span className="block font-normal">{formatNaira(e.total)}</span>
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {rfq.criteria.map((c) => (
              <tr key={c.id} className="border-t">
                <th scope="row" className="p-2.5 text-left font-normal">
                  <span className="font-semibold">{c.name}</span>
                  <span className="block text-xs text-muted-foreground">{criterionKindLabels[c.kind]}</span>
                </th>
                <td className="p-2.5 text-right text-muted-foreground">{c.weight}%</td>
                {rfq.evaluation.map((e) => {
                  const key = `${c.id}:${e.quotationId}`
                  const score = e.scores.find((s) => s.criterionId === c.id)?.score ?? null
                  return (
                    <td key={e.quotationId} className={cn('p-2.5 text-right', e.rank === 1 && 'bg-success/50')}>
                      {c.kind === 'Manual' && editable ? (
                        <input type="number" min={0} max={10} step={0.5} inputMode="decimal" placeholder="0–10"
                          aria-label={`${c.name} score for ${e.supplierName}`} value={draft[key] ?? ''}
                          onChange={(ev) => setDraft((d) => ({ ...d, [key]: ev.target.value }))}
                          className={cn(fieldClass, 'ml-auto h-9 w-20 text-right', !validScore(draft[key] ?? '') && 'border-destructive')} />
                      ) : (
                        <span className="font-semibold">{score === null ? <span className="text-destructive">Not scored</span> : score}</span>
                      )}
                      <span className="block text-xs text-muted-foreground">{basis(c.kind, e)}</span>
                    </td>
                  )
                })}
              </tr>
            ))}
          </tbody>
          <tfoot className="border-t-2">
            <tr>
              <th scope="row" className="p-2.5 text-left text-xs font-semibold text-muted-foreground">Total</th>
              <td className="p-2.5 text-right text-muted-foreground">100%</td>
              {rfq.evaluation.map((e) => (
                <td key={e.quotationId} className={cn('p-2.5 text-right', e.rank === 1 && 'bg-success/50')}>
                  <span className="font-heading text-2xl font-light">{e.weightedTotal.toFixed(1)}</span>
                  <span className="block text-xs text-muted-foreground">
                    {e.fullyScored ? (e.rank === 1 ? 'Top score' : ordinal(e.rank)) : 'Incomplete'}
                  </span>
                </td>
              ))}
            </tr>
          </tfoot>
        </table>
      </div>

      {editable && manual.length > 0 && (
        <div className="flex flex-wrap items-center justify-end gap-3">
          {invalid && <span className="text-[13px] text-destructive">Scores run from 0 to 10 in steps of 0.5.</span>}
          {changed.length > 0 && !invalid && <span className="text-[13px] text-muted-foreground">{changed.length} unsaved</span>}
          <Button size="pill-sm" variant="brand" disabled={changed.length === 0 || invalid || setScores.isPending} onClick={save}>
            {setScores.isPending ? 'Saving…' : 'Save scores'}
          </Button>
        </div>
      )}
      {setScores.error && <p role="alert" className="text-[13px] text-destructive">{problemMessage(setScores.error)}</p>}

      {editable && <Recommend rfq={rfq} unsaved={changed.length > 0} />}
    </div>
  )
}

function Recommend({ rfq, unsaved }: { rfq: SourcingEventDetail; unsaved: boolean }) {
  const recommend = useRecommendAward(rfq.id)
  const top = rfq.evaluation[0]
  const [choice, setChoice] = useState(top?.quotationId ?? '')
  const [reason, setReason] = useState('')
  const chosen = rfq.evaluation.find((e) => e.quotationId === choice)
  const allScored = rfq.evaluation.every((e) => e.fullyScored)
  const needsReason = !!chosen && chosen.rank !== 1
  const reasonOk = !needsReason || reason.trim().length >= MIN_REASON
  const over = chosen ? chosen.total - rfq.estimatedTotal : 0

  const submit = async () => {
    try {
      await recommend.mutateAsync({ quotationId: choice, justification: reason.trim() || null })
      toast(`Award to ${chosen?.supplierName} sent for approval`)
    } catch { /* shown below */ }
  }

  return (
    <section aria-labelledby="recommend-h" className="mt-2 flex flex-col gap-3 rounded-[22px] bg-surface p-5">
      <h3 id="recommend-h" className="text-lg font-normal">Recommend a supplier</h3>
      {!allScored && <p className="text-[13px] text-muted-foreground">Score every quotation on every criterion first.</p>}
      <div role="radiogroup" aria-label="Supplier to award" className="flex flex-wrap gap-2">
        {rfq.evaluation.map((e) => (
          <button key={e.quotationId} type="button" role="radio" aria-checked={choice === e.quotationId}
            onClick={() => setChoice(e.quotationId)}
            className={cn('rounded-2xl border px-4 py-2.5 text-left text-sm',
              choice === e.quotationId ? 'border-foreground bg-inverse text-inverse-foreground' : 'border-input hover:border-foreground')}>
            <span className="block font-semibold">{e.supplierName}</span>
            <span className="block text-xs opacity-80">{formatNaira(e.total)} · {e.weightedTotal.toFixed(1)} pts{e.rank === 1 ? ' · top' : ''}</span>
          </button>
        ))}
      </div>
      <label htmlFor="award-reason" className="text-[13px] font-semibold">
        Why this supplier? {needsReason ? `Required: it isn't the top score (at least ${MIN_REASON} characters).` : 'Optional.'}
      </label>
      <textarea id="award-reason" rows={3} maxLength={1000} value={reason} onChange={(e) => setReason(e.target.value)}
        className={cn(fieldClass, 'h-auto py-2.5')}
        placeholder={needsReason ? 'e.g. Only supplier with a local service centre; downtime costs more than the price gap' : 'Anything the approvers should know'} />
      <p className="text-xs text-muted-foreground">
        Goes to the Procurement manager{chosen && chosen.total > 25_000_000 ? ', then the Managing Director (over ₦25m)' : ''}
        {over > 0 ? `, and Finance, because it's ${formatNaira(over)} above the estimate` : ''}.
      </p>
      {recommend.error && <p role="alert" className="rounded-xl bg-destructive/10 px-3.5 py-3 text-[13px] text-destructive">{problemMessage(recommend.error)}</p>}
      <div className="flex flex-wrap items-center justify-end gap-3">
        {unsaved && <span className="text-[13px] text-muted-foreground">Save your scores first.</span>}
        <Button size="pill" variant="brand" disabled={!chosen || !allScored || !reasonOk || unsaved || recommend.isPending} onClick={submit}>
          {recommend.isPending ? 'Sending…' : 'Send for approval'}
        </Button>
      </div>
    </section>
  )
}

function CriteriaEditor({ rfq, onDone }: { rfq: SourcingEventDetail; onDone: () => void }) {
  const save = useSetCriteria(rfq.id)
  const [rows, setRows] = useState<(CriterionInput & { key: string })[]>(
    () => rfq.criteria.map((c: EvaluationCriterion) => ({ key: c.id, id: c.id, name: c.name, kind: c.kind, weight: c.weight })))
  const total = rows.reduce((s, r) => s + (Number.isFinite(r.weight) ? r.weight : 0), 0)
  const usedAuto = new Set(rows.filter((r) => r.kind !== 'Manual').map((r) => r.kind))
  const set = (key: string, patch: Partial<CriterionInput>) => setRows((all) => all.map((r) => (r.key === key ? { ...r, ...patch } : r)))

  const submit = async () => {
    try {
      await save.mutateAsync(rows.map(({ id, name, kind, weight }) => ({ id, name: name.trim(), kind, weight })))
      toast('Criteria saved')
      onDone()
    } catch { /* shown below */ }
  }

  return (
    <div className="flex flex-col gap-3">
      <p className="text-[13px] text-muted-foreground">Removing a manual criterion deletes the scores entered for it.</p>
      <ul className="flex flex-col gap-2">
        {rows.map((r) => (
          <li key={r.key} className="flex flex-wrap items-center gap-2">
            <input aria-label="Criterion name" value={r.name} maxLength={100} onChange={(e) => set(r.key, { name: e.target.value })}
              className={cn(fieldClass, 'h-9 min-w-0 flex-[1_1_200px]')} />
            {r.id ? (
              <span className="w-48 text-xs text-muted-foreground">{criterionKindLabels[r.kind]}</span>
            ) : (
              <FieldSelect aria-label="How it's scored" size="sm" className="w-48" value={r.kind}
                onChange={(v) => set(r.key, { kind: v as CriterionKind })}
                options={(['Manual', 'Price', 'Delivery', 'PaymentTerms'] as CriterionKind[])
                  .filter((k) => k === 'Manual' || k === r.kind || !usedAuto.has(k))
                  .map((k) => ({ value: k, label: criterionKindLabels[k] }))} />
            )}
            <label className="flex items-center gap-1.5 text-[13px]">
              <input type="number" min={1} max={100} aria-label={`Weight of ${r.name || 'criterion'}`} value={Number.isFinite(r.weight) ? r.weight : ''}
                onChange={(e) => set(r.key, { weight: e.target.valueAsNumber })} className={cn(fieldClass, 'h-9 w-20 text-right')} />%
            </label>
            <Button size="icon" variant="ghost" aria-label={`Remove ${r.name || 'criterion'}`} disabled={rows.length === 1}
              onClick={() => setRows((all) => all.filter((x) => x.key !== r.key))}>
              <HugeiconsIcon icon={Delete02Icon} size={16} strokeWidth={1.5} />
            </Button>
          </li>
        ))}
      </ul>
      <div className="flex flex-wrap items-center justify-between gap-3">
        <Button size="pill-sm" variant="outline" disabled={rows.length >= 8}
          onClick={() => setRows((all) => [...all, { key: crypto.randomUUID(), id: null, name: '', kind: 'Manual', weight: 10 }])}>
          <HugeiconsIcon icon={Add01Icon} size={16} strokeWidth={1.5} /> Add a criterion
        </Button>
        <span className={cn('text-sm font-semibold', total === 100 ? 'text-brand' : 'text-destructive')}>
          Weights add up to {total}{total === 100 ? '' : ' (must be 100)'}
        </span>
      </div>
      {save.error && <p role="alert" className="rounded-xl bg-destructive/10 px-3.5 py-3 text-[13px] text-destructive">{problemMessage(save.error)}</p>}
      <div className="flex justify-end gap-2">
        <Button size="pill-sm" variant="ghost" onClick={onDone}>Cancel</Button>
        <Button size="pill-sm" variant="brand" disabled={total !== 100 || rows.some((r) => !r.name.trim()) || save.isPending} onClick={submit}>
          {save.isPending ? 'Saving…' : 'Save criteria'}
        </Button>
      </div>
    </div>
  )
}

function basis(kind: CriterionKind, e: QuotationEvaluation) {
  if (kind === 'Delivery') return `${e.leadTimeDays} days`
  if (kind === 'PaymentTerms') return `${e.paymentTermsDays} days credit`
  return ''
}

function validScore(v: string) {
  if (v.trim() === '') return true
  const n = Number(v)
  return Number.isFinite(n) && n >= 0 && n <= 10 && Number.isInteger(n * 2)
}

function ordinal(n: number) {
  const s = ['th', 'st', 'nd', 'rd']
  const v = n % 100
  return `${n}${s[(v - 20) % 10] ?? s[v] ?? s[0]}`
}
