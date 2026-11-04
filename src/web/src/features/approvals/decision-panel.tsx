import { useState } from 'react'
import { toast } from 'sonner'
import { Button } from '@/components/ui/button'
import { Textarea } from '@/components/ui/textarea'
import { ApiError } from '@/lib/api-client'
import { cn } from '@/lib/utils'
import { useDecide, type Decision } from './approvals-api'

type DecisionPanelProps = {
  requestId: string
  documentNumber: string
  requesterName: string
  /** The dark aside on the detail page, or a normal card in the inbox */
  onInverse?: boolean
  onDecided?: (decision: Decision) => void
}

export function DecisionPanel({ requestId, documentNumber, requesterName, onInverse = false, onDecided }: DecisionPanelProps) {
  const decide = useDecide()
  const [comment, setComment] = useState('')
  const [missingReason, setMissingReason] = useState(false)

  async function submit(decision: Decision) {
    // The API also insists on this; checking here saves a round trip
    if (decision === 'reject' && !comment.trim()) {
      setMissingReason(true)
      return
    }
    setMissingReason(false)
    try {
      await decide.mutateAsync({ requestId, decision, comment })
      toast(decision === 'approve' ? `Approved ${documentNumber}` : `Rejected ${documentNumber}`, {
        description: decision === 'approve'
          ? 'It moves to the next approver, or to Procurement if you were the last.'
          : `${requesterName} can see your comment and resubmit.`,
      })
      setComment('')
      onDecided?.(decision)
    } catch {
      // decide.error is shown below
    }
  }

  const failure = decide.error instanceof ApiError
    ? decide.error.problem.detail ?? decide.error.problem.title
    : decide.error?.message
  const conflict = decide.error instanceof ApiError && decide.error.status === 409

  return (
    <div className="flex flex-col gap-3">
      <label htmlFor={`comment-${requestId}`} className="text-[13px] font-semibold">Comment</label>
      <Textarea id={`comment-${requestId}`} rows={3} value={comment}
        onChange={(e) => setComment(e.target.value)}
        aria-invalid={missingReason}
        aria-describedby={missingReason ? `comment-error-${requestId}` : undefined}
        placeholder="Optional when approving, required when rejecting"
        className={cn('rounded-2xl px-3.5 py-3 text-sm',
          onInverse && 'border-inverse-muted/40 bg-inverse-row text-inverse-foreground placeholder:text-inverse-muted')} />
      {missingReason && (
        <p id={`comment-error-${requestId}`} className={cn('text-[13px]', onInverse ? 'text-inverse-foreground' : 'text-destructive')}>
          Say why you're rejecting it, so {requesterName} knows what to change.
        </p>
      )}

      {failure && (
        <div role="alert" className="rounded-xl bg-destructive/10 px-3.5 py-3 text-[13px] text-destructive">
          <strong>Couldn't record your decision.</strong> {failure}
          {conflict && ' Reload to see the latest.'}
        </div>
      )}

      <div className="flex flex-wrap gap-2.5">
        <Button size="pill" variant={onInverse ? 'brand' : 'default'} className="flex-1"
          disabled={decide.isPending} onClick={() => submit('approve')}>
          {decide.isPending && decide.variables?.decision === 'approve' ? 'Approving…' : 'Approve'}
        </Button>
        <Button size="pill" variant="outline" disabled={decide.isPending} onClick={() => submit('reject')}
          className={cn('flex-1 border-[1.5px]',
            onInverse ? 'border-inverse-foreground bg-transparent text-inverse-foreground hover:bg-inverse-row hover:text-inverse-foreground'
              : 'border-foreground')}>
          {decide.isPending && decide.variables?.decision === 'reject' ? 'Rejecting…' : 'Reject'}
        </Button>
      </div>
    </div>
  )
}
