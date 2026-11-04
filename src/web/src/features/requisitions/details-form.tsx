import { useState, type FormEvent } from 'react'
import { FormField } from '@/components/form-field'
import { Button } from '@/components/ui/button'
import { fieldClass } from '@/lib/field-class'
import { formatNaira } from '@/lib/format'
import { cn } from '@/lib/utils'
import type { CostCentreOption, RequisitionDetails, Site } from './requisition-editor-api'

type DetailsFormProps = {
  initial: RequisitionDetails
  costCentres: CostCentreOption[]
  sites: Site[]
  submitLabel: string
  pendingLabel: string
  pending: boolean
  error?: string
  onSubmit: (details: RequisitionDetails) => void
  /** Lets the budget preview follow the cost centre before it's saved */
  onCostCentreChange?: (costCentreId: string) => void
}

const today = () => new Date().toLocaleDateString('en-CA') // "2026-11-04", in the viewer's time zone

export function DetailsForm({ initial, costCentres, sites, submitLabel, pendingLabel, pending, error, onSubmit, onCostCentreChange }: DetailsFormProps) {
  const [values, setValues] = useState(initial)
  const [touched, setTouched] = useState(false)

  const set = <K extends keyof RequisitionDetails>(key: K, value: RequisitionDetails[K]) =>
    setValues((v) => ({ ...v, [key]: value }))

  // Checked here for quick feedback; the API checks the same rules
  const problems: Partial<Record<keyof RequisitionDetails, string>> = {}
  if (!values.title.trim()) problems.title = 'Give it a short title.'
  if (!values.justification.trim()) problems.justification = 'Say why it is needed. Approvers read this first.'
  if (!values.costCentreId) problems.costCentreId = 'Choose who pays.'
  if (!values.deliverToSiteId) problems.deliverToSiteId = 'Choose where it goes.'
  if (!values.requiredBy) problems.requiredBy = 'Pick a date.'
  else if (values.requiredBy < today()) problems.requiredBy = 'The date has passed.'
  const show = (key: keyof RequisitionDetails) => (touched ? problems[key] : undefined)

  function submit(event: FormEvent) {
    event.preventDefault()
    setTouched(true)
    if (Object.keys(problems).length === 0) onSubmit({ ...values, title: values.title.trim(), justification: values.justification.trim() })
  }

  const dirty = JSON.stringify(values) !== JSON.stringify(initial)

  return (
    <form onSubmit={submit} noValidate className="flex flex-col gap-4">
      <FormField id="title" label="Title" error={show('title')}>
        <input id="title" className={fieldClass} maxLength={200} value={values.title}
          placeholder="e.g. Laptops for the new analysts" aria-invalid={!!show('title')}
          onChange={(e) => set('title', e.target.value)} />
      </FormField>

      <div className="grid gap-4 sm:grid-cols-2">
        <FormField id="costCentre" label="Cost centre" error={show('costCentreId')}
          hint={costCentres.length === 0 ? 'Your department has no active cost centres. Ask Finance.' : undefined}>
          <select id="costCentre" className={fieldClass} value={values.costCentreId} aria-invalid={!!show('costCentreId')}
            onChange={(e) => { set('costCentreId', e.target.value); onCostCentreChange?.(e.target.value) }}>
            <option value="">Choose…</option>
            {costCentres.map((c) => (
              <option key={c.id} value={c.id}>
                {c.code} · {c.name}{c.hasBudget ? ` · ${formatNaira(c.available - c.inApproval)} free` : ' · no budget'}
              </option>
            ))}
          </select>
        </FormField>

        <FormField id="site" label="Deliver to" error={show('deliverToSiteId')}>
          <select id="site" className={fieldClass} value={values.deliverToSiteId} aria-invalid={!!show('deliverToSiteId')}
            onChange={(e) => set('deliverToSiteId', e.target.value)}>
            <option value="">Choose…</option>
            {sites.map((s) => <option key={s.id} value={s.id}>{s.code} · {s.name}, {s.city}</option>)}
          </select>
        </FormField>
      </div>

      <FormField id="requiredBy" label="Required by" error={show('requiredBy')} className="sm:max-w-[50%]">
        <input id="requiredBy" type="date" className={fieldClass} min={today()} value={values.requiredBy}
          aria-invalid={!!show('requiredBy')} onChange={(e) => set('requiredBy', e.target.value)} />
      </FormField>

      <FormField id="justification" label="Justification" error={show('justification')}
        hint="What it's for and why now. Approvers decide from this.">
        <textarea id="justification" rows={4} maxLength={1000} value={values.justification}
          className={cn(fieldClass, 'h-auto py-3 leading-relaxed')} aria-invalid={!!show('justification')}
          onChange={(e) => set('justification', e.target.value)} />
      </FormField>

      {error && <p role="alert" className="rounded-xl bg-destructive/10 px-3.5 py-3 text-[13px] text-destructive">{error}</p>}

      <div className="flex justify-end">
        <Button type="submit" size="pill" disabled={pending || (!dirty && submitLabel !== 'Create draft')}>
          {pending ? pendingLabel : submitLabel}
        </Button>
      </div>
    </form>
  )
}
