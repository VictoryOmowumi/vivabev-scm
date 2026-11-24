import { toast } from 'sonner'
import { Skeleton } from '@/components/ui/skeleton'
import { Button } from '@/components/ui/button'
import { useCurrentUser } from '@/features/auth/use-auth'
import { ApprovalChain } from '@/features/requisitions/approval-chain'
import { problemMessage } from '@/lib/api-client'
import { formatDateTime, formatNaira } from '@/lib/format'
import { useAwardApproval, useWithdrawRecommendation, type SourcingEventDetail } from './sourcing-api'

/** Where the award stands: pending (with its approval chain), awarded, or last rejected. */
export function AwardStatus({ rfq, manage }: { rfq: SourcingEventDetail; manage: boolean }) {
  const { data: user } = useCurrentUser()
  const showChain = rfq.status === 'AwardPending' || rfq.status === 'Awarded'
  const approval = useAwardApproval(rfq.id, showChain)
  const withdraw = useWithdrawRecommendation(rfq.id)
  const award = rfq.award

  if (rfq.status === 'ResponsesClosed' && rfq.awardRejectionReason) {
    return (
      <div role="status" className="rounded-2xl bg-over px-5 py-4 text-sm text-over-foreground">
        <strong>The last recommendation was rejected:</strong> “{rfq.awardRejectionReason}” Rescore or choose again, then send it back.
      </div>
    )
  }
  if (!showChain || !award) return null

  const over = award.amount - rfq.estimatedTotal

  return (
    <section aria-label="Award" className="flex flex-wrap gap-5 rounded-[28px] bg-inverse p-6 text-inverse-foreground">
      <div className="flex min-w-0 flex-[2_1_380px] flex-col gap-3">
        <p className="text-[13px] font-semibold text-inverse-muted">
          {rfq.status === 'Awarded' ? `Awarded ${formatDateTime(award.awardedAt!)}` : `Recommended ${formatDateTime(award.recommendedAt)} · waiting for approval`}
        </p>
        <h2 className="text-[28px] leading-tight font-light">
          {rfq.status === 'Awarded' ? 'Awarded to ' : 'Award to '}{award.supplierName}
        </h2>
        <div className="flex flex-wrap items-baseline gap-x-4 gap-y-1">
          <span className="font-heading text-[36px] leading-none font-light">{formatNaira(award.amount)}</span>
          <span className="text-sm text-inverse-muted">
            {over === 0 ? 'matches the estimate' : `${formatNaira(Math.abs(over))} ${over < 0 ? 'under' : 'over'} the estimate`}
            {award.topScorer ? ' · top score' : ' · not the top score'}
          </span>
        </div>
        {award.justification && <p className="rounded-2xl bg-inverse-row px-4 py-3 text-sm">“{award.justification}”</p>}
        {rfq.status === 'Awarded' && (
          <p className="text-sm text-inverse-muted">Next: the purchase order to {award.supplierName}. That arrives in the next release step.</p>
        )}
        {manage && rfq.status === 'AwardPending' && (
          <div className="mt-auto flex flex-wrap items-center gap-3">
            <Button size="pill-sm" variant="ghost" className="text-inverse-foreground hover:bg-inverse-row hover:text-inverse-foreground"
              disabled={withdraw.isPending}
              onClick={() => withdraw.mutate(undefined, { onSuccess: () => toast('Recommendation withdrawn; you can rescore and send it again') })}>
              Withdraw recommendation
            </Button>
            {withdraw.error && <span role="alert" className="text-[13px] text-[#f3c9a6]">{problemMessage(withdraw.error)}</span>}
          </div>
        )}
      </div>
      <div className="flex min-w-0 flex-[1_1_300px] flex-col gap-2">
        <h3 className="text-sm font-semibold">Approval</h3>
        {approval.isPending && <Skeleton className="h-32 rounded-[18px] bg-inverse-row" />}
        {approval.data && user && <ApprovalChain approval={approval.data} meId={user.id} />}
      </div>
    </section>
  )
}
