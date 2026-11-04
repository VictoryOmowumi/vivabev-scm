import { useState, type ReactNode } from 'react'
import { Link, Navigate, useNavigate, useParams } from 'react-router'
import { toast } from 'sonner'
import { HugeiconsIcon } from '@hugeicons/react'
import { ArrowLeft01Icon, Delete02Icon } from '@hugeicons/core-free-icons'
import { Panel } from '@/components/panel'
import { ErrorState } from '@/components/states/error-state'
import { Button } from '@/components/ui/button'
import { Skeleton } from '@/components/ui/skeleton'
import { useCurrentUser } from '@/features/auth/use-auth'
import { problemMessage } from '@/lib/api-client'
import { formatNaira } from '@/lib/format'
import { BudgetPreview } from './budget-preview'
import { CancelRequisition } from './cancel-requisition'
import { DetailsForm } from './details-form'
import { LineEditor } from './line-editor'
import {
  useCostCentreOptions,
  useCreateRequisition,
  useRemoveLine,
  useSites,
  useSubmitRequisition,
  useUpdateRequisition,
  type RequisitionDetails,
} from './requisition-editor-api'
import { useRequisition, type RequisitionDetail } from './requisitions-api'

/** /requisitions/new and /requisitions/:id/edit */
export function RequisitionEditorPage() {
  const { id } = useParams()
  return id ? <EditRequisition id={id} /> : <NewRequisition />
}

function useFormLists() {
  const costCentres = useCostCentreOptions()
  const sites = useSites()
  return {
    costCentres: costCentres.data ?? [],
    sites: (sites.data ?? []).filter((s) => s.isActive),
    isPending: costCentres.isPending || sites.isPending,
    error: costCentres.error ?? sites.error,
    retry: () => { void costCentres.refetch(); void sites.refetch() },
  }
}

function NewRequisition() {
  const { data: user } = useCurrentUser()
  const lists = useFormLists()
  const create = useCreateRequisition()
  const navigate = useNavigate()
  const [costCentreId, setCostCentreId] = useState<string>()

  if (lists.isPending) return <EditorSkeleton />
  if (lists.error) return <ErrorState error={lists.error} what="the requisition form" onRetry={lists.retry} />

  const initial: RequisitionDetails = {
    title: '',
    justification: '',
    costCentreId: lists.costCentres.length === 1 ? lists.costCentres[0]!.id : '',
    deliverToSiteId: lists.sites.find((s) => s.code === user?.siteCode)?.id ?? '',
    requiredBy: '',
  }

  async function onSubmit(details: RequisitionDetails) {
    try {
      const created = await create.mutateAsync(details)
      toast(`Draft ${created.number} created`, { description: 'Now add what you need.' })
      navigate(`/requisitions/${created.id}/edit`, { replace: true })
    } catch { /* shown in the form */ }
  }

  return (
    <EditorLayout title="New requisition" subtitle="Start with what it's for. You add the lines next.">
      <div className="flex min-w-0 flex-[2_1_620px] flex-col gap-5">
        <Panel className="px-6 py-5">
          <h2 className="mb-4 text-xl font-normal">Details</h2>
          <DetailsForm initial={initial} costCentres={lists.costCentres} sites={lists.sites}
            submitLabel="Create draft" pendingLabel="Creating…" pending={create.isPending}
            error={create.error ? problemMessage(create.error) : undefined}
            onSubmit={onSubmit} onCostCentreChange={setCostCentreId} />
        </Panel>
      </div>
      <Aside>
        <h2 className="text-xl font-normal">Budget</h2>
        <BudgetPreview costCentre={lists.costCentres.find((c) => c.id === (costCentreId ?? initial.costCentreId))} total={0} />
      </Aside>
    </EditorLayout>
  )
}

function EditRequisition({ id }: { id: string }) {
  const { data: user } = useCurrentUser()
  const requisition = useRequisition(id)
  const lists = useFormLists()

  if (requisition.isPending || lists.isPending) return <EditorSkeleton />
  if (requisition.isError) {
    return <ErrorState error={requisition.error} what="this requisition" onRetry={() => requisition.refetch()}
      back={{ to: '/requisitions', label: 'Back to requisitions' }} />
  }
  if (lists.error) return <ErrorState error={lists.error} what="the requisition form" onRetry={lists.retry} />

  // Only the requester edits, and only while it's a draft; anyone else sees the normal page
  const r = requisition.data
  if (r.status !== 'Draft' || r.requesterId !== user?.id) return <Navigate to={`/requisitions/${id}`} replace />

  return <DraftEditor requisition={r} lists={lists} />
}

function DraftEditor({ requisition: r, lists }: { requisition: RequisitionDetail; lists: ReturnType<typeof useFormLists> }) {
  const update = useUpdateRequisition(r.id)
  const removeLine = useRemoveLine(r.id)
  const submit = useSubmitRequisition(r.id)
  const navigate = useNavigate()
  const [costCentreId, setCostCentreId] = useState(r.costCentreId)

  const initial: RequisitionDetails = {
    title: r.title,
    justification: r.justification,
    costCentreId: r.costCentreId,
    deliverToSiteId: r.deliverToSiteId,
    requiredBy: r.requiredBy,
  }

  async function onSave(details: RequisitionDetails) {
    try {
      await update.mutateAsync(details)
      toast(`Saved ${r.number}`)
    } catch { /* shown in the form */ }
  }

  async function onSubmit() {
    try {
      await submit.mutateAsync()
      toast(`Submitted ${r.number}`, { description: 'It has gone to your manager first. You can follow it here.' })
      navigate(`/requisitions/${r.id}`)
    } catch { /* shown below */ }
  }

  return (
    <EditorLayout title={r.title} number={r.number} subtitle="Draft · only you can see and change it until you submit">
      {r.rejectionReason && (
        <div role="status" className="w-full rounded-2xl bg-over px-5 py-4 text-sm text-over-foreground">
          <strong>Returned:</strong> {r.rejectionReason} Change what's needed, then submit again.
        </div>
      )}

      <div className="flex min-w-0 flex-[2_1_620px] flex-col gap-5">
        <Panel className="px-6 py-5">
          <h2 className="mb-2.5 text-xl font-normal">Lines</h2>
          {r.lines.length > 0 && (
            <div className="mb-4 overflow-x-auto">
              <table className="w-full min-w-[560px] border-collapse text-sm">
                <thead>
                  <tr className="text-left text-xs text-muted-foreground">
                    <th scope="col" className="p-2.5 font-semibold">Description</th>
                    <th scope="col" className="p-2.5 text-right font-semibold">Qty</th>
                    <th scope="col" className="p-2.5 text-right font-semibold">Unit price</th>
                    <th scope="col" className="p-2.5 text-right font-semibold">Line total</th>
                    <th scope="col" className="p-2.5"><span className="sr-only">Remove</span></th>
                  </tr>
                </thead>
                <tbody>
                  {r.lines.map((l) => (
                    <tr key={l.lineNo} className="border-t">
                      <td className="p-2.5">
                        <div className="font-semibold">{l.description}</div>
                        <div className="text-xs text-muted-foreground">{l.itemCode ?? 'Free text'} · {l.lineType}</div>
                      </td>
                      <td className="p-2.5 text-right">{l.quantity} {l.uomCode}</td>
                      <td className="p-2.5 text-right">{formatNaira(l.estimatedUnitPrice)}</td>
                      <td className="p-2.5 text-right font-semibold">{formatNaira(l.lineTotal)}</td>
                      <td className="p-2.5 text-right">
                        <Button size="icon-sm" variant="ghost" aria-label={`Remove ${l.description}`}
                          disabled={removeLine.isPending} onClick={() => removeLine.mutate(l.lineNo)}>
                          <HugeiconsIcon icon={Delete02Icon} strokeWidth={1.5} />
                        </Button>
                      </td>
                    </tr>
                  ))}
                </tbody>
                <tfoot>
                  <tr className="border-t-[1.5px] border-foreground">
                    <td colSpan={3} className="p-2.5 font-semibold">Estimated total</td>
                    <td className="p-2.5 text-right font-bold">{formatNaira(r.estimatedTotal)}</td>
                    <td />
                  </tr>
                </tfoot>
              </table>
            </div>
          )}
          {removeLine.error && <p role="alert" className="mb-3 text-[13px] text-destructive">{problemMessage(removeLine.error)}</p>}
          <LineEditor requisitionId={r.id} />
        </Panel>

        <Panel className="px-6 py-5">
          <h2 className="mb-4 text-xl font-normal">Details</h2>
          <DetailsForm initial={initial} costCentres={lists.costCentres} sites={lists.sites}
            submitLabel="Save details" pendingLabel="Saving…" pending={update.isPending}
            error={update.error ? problemMessage(update.error) : undefined}
            onSubmit={onSave} onCostCentreChange={setCostCentreId} />
        </Panel>

        <div className="flex justify-end">
          <CancelRequisition id={r.id} number={r.number} status={r.status} />
        </div>
      </div>

      <Aside>
        <div className="flex items-baseline justify-between">
          <h2 className="text-xl font-normal">Budget</h2>
          <span className="font-heading text-[28px] font-light">{formatNaira(r.estimatedTotal)}</span>
        </div>
        <BudgetPreview costCentre={lists.costCentres.find((c) => c.id === costCentreId)} total={r.estimatedTotal} />
        {costCentreId !== r.costCentreId && (
          <p className="text-xs text-inverse-muted">Save the details to switch to this cost centre.</p>
        )}

        {submit.error && (
          <p role="alert" className="rounded-xl bg-[#f3c9a6] px-3.5 py-3 text-[13px] text-[#5a2a08]">
            <strong>Couldn't submit.</strong> {problemMessage(submit.error)}
          </p>
        )}
        <Button size="pill" variant="brand" disabled={r.lines.length === 0 || submit.isPending} onClick={onSubmit}>
          {submit.isPending ? 'Submitting…' : 'Submit for approval'}
        </Button>
        {r.lines.length === 0 && <p className="text-center text-xs text-inverse-muted">Add at least one line to submit.</p>}
      </Aside>
    </EditorLayout>
  )
}

function EditorLayout({ title, number, subtitle, children }: { title: string; number?: string; subtitle: string; children: ReactNode }) {
  return (
    <div className="flex flex-col gap-6">
      <section className="flex flex-col gap-3">
        <Link to="/requisitions" className="flex items-center gap-1.5 self-start text-sm font-semibold hover:text-brand">
          <HugeiconsIcon icon={ArrowLeft01Icon} size={18} strokeWidth={1.5} /> Requisitions
        </Link>
        {number && <p className="text-[13px] font-semibold text-muted-foreground">{number}</p>}
        <h1 className="text-[40px] leading-tight font-light tracking-tight">{title}</h1>
        <p className="text-[15px] text-muted-foreground">{subtitle}</p>
      </section>
      <section className="flex flex-wrap items-start gap-5">{children}</section>
    </div>
  )
}

function Aside({ children }: { children: ReactNode }) {
  return (
    <aside className="flex min-w-0 flex-[1_1_340px] flex-col gap-4 rounded-[28px] bg-inverse p-6 text-inverse-foreground lg:sticky lg:top-6">
      {children}
    </aside>
  )
}

function EditorSkeleton() {
  return (
    <div className="flex flex-col gap-6" role="status" aria-label="Loading">
      <Skeleton className="h-10 w-1/2 rounded-full" />
      <div className="flex gap-5">
        <Skeleton className="h-96 flex-[2] rounded-[28px]" />
        <Skeleton className="h-72 flex-1 rounded-[28px]" />
      </div>
    </div>
  )
}
