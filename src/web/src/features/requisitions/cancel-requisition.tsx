import { useState } from 'react'
import { toast } from 'sonner'
import { Button } from '@/components/ui/button'
import { problemMessage } from '@/lib/api-client'
import { fieldClass } from '@/lib/field-class'
import { cn } from '@/lib/utils'
import { useCancelRequisition } from './requisition-editor-api'

/** "Cancel requisition", asking for a reason first. Withdraws a pending approval or releases an approved commitment. */
export function CancelRequisition({ id, number, status }: { id: string; number: string; status: string }) {
  const cancel = useCancelRequisition(id)
  const [open, setOpen] = useState(false)
  const [reason, setReason] = useState('')

  if (!open) {
    return (
      <Button size="pill-sm" variant="ghost" className="text-destructive hover:text-destructive" onClick={() => setOpen(true)}>
        Cancel requisition
      </Button>
    )
  }

  const effect = status === 'PendingApproval' ? ' Its approval request is withdrawn.'
    : status === 'Approved' ? ' The amount committed against the budget is released.' : ''

  async function confirm() {
    try {
      await cancel.mutateAsync(reason.trim())
      toast(`Cancelled ${number}`, { description: 'It stays in the list, marked cancelled.' })
      setOpen(false)
    } catch { /* shown below */ }
  }

  return (
    <div className="flex w-full flex-col gap-2.5 rounded-2xl bg-destructive/10 p-4">
      <label htmlFor={`cancel-${id}`} className="text-[13px] font-semibold text-destructive">
        Why are you cancelling {number}?{effect}
      </label>
      <textarea id={`cancel-${id}`} rows={2} value={reason} onChange={(e) => setReason(e.target.value)} maxLength={500}
        className={cn(fieldClass, 'h-auto py-2.5')} placeholder="e.g. No longer needed; merged into REQ-2027-00012" />
      {cancel.error && <p role="alert" className="text-[13px] text-destructive">{problemMessage(cancel.error)}</p>}
      <div className="flex justify-end gap-2">
        <Button size="pill-sm" variant="ghost" onClick={() => { setOpen(false); cancel.reset() }}>Keep it</Button>
        <Button size="pill-sm" variant="destructive" disabled={!reason.trim() || cancel.isPending} onClick={confirm}>
          {cancel.isPending ? 'Cancelling…' : 'Cancel requisition'}
        </Button>
      </div>
    </div>
  )
}
