import { toast } from 'sonner'
import { HugeiconsIcon } from '@hugeicons/react'
import { UserAdd01Icon } from '@hugeicons/core-free-icons'
import { Button } from '@/components/ui/button'
import { Skeleton } from '@/components/ui/skeleton'
import { problemMessage } from '@/lib/api-client'
import { cn } from '@/lib/utils'
import { useEligibleSuppliers, useInvite, useRecordDecline, useUninvite, type SourcingEventDetail } from './sourcing-api'

const statusStyle = {
  Invited: 'bg-inverse-row text-inverse-foreground',
  Responded: 'bg-[#d3ebcf] text-[#1d4f24]',
  Declined: 'bg-[#ece2df] text-[#5e3128]',
}

/** The dark aside: who's invited, and who else could be. */
export function RfqSuppliers({ rfq, manage }: { rfq: SourcingEventDetail; manage: boolean }) {
  const canInvite = manage && (rfq.status === 'Draft' || rfq.status === 'Published')
  const eligible = useEligibleSuppliers(rfq.id, canInvite)
  const invite = useInvite(rfq.id)
  const uninvite = useUninvite(rfq.id)
  const decline = useRecordDecline(rfq.id)
  const error = invite.error ?? uninvite.error ?? decline.error
  const others = (eligible.data ?? []).filter((s) => !s.invited)

  return (
    <aside aria-label="Suppliers" className="flex min-w-0 flex-[1_1_340px] flex-col gap-4 rounded-[28px] bg-inverse p-6 text-inverse-foreground">
      <div className="flex items-baseline justify-between">
        <h2 className="text-xl font-normal">Suppliers</h2>
        <span className="font-heading text-[28px] leading-none font-light">{rfq.invitations.length}</span>
      </div>

      {rfq.invitations.length === 0 && <p className="text-sm text-inverse-muted">Nobody invited yet.</p>}
      <ul className="flex flex-col gap-1.5">
        {rfq.invitations.map((i) => (
          <li key={i.supplierId} className="flex items-center gap-3 rounded-[18px] bg-inverse-row p-3">
            <span className="min-w-0 flex-1">
              <span className="block truncate text-sm font-semibold">{i.supplierName}</span>
              <span className="block text-xs text-inverse-muted">{i.invitedAt ? `Invited ${new Date(i.invitedAt.endsWith('Z') ? i.invitedAt : `${i.invitedAt}Z`).toLocaleDateString('en-GB')}` : 'Not sent yet'}</span>
            </span>
            <span className={cn('rounded-full px-2.5 py-0.5 text-[11px] font-bold', statusStyle[i.status])}>{i.status}</span>
            {manage && rfq.status === 'Draft' && (
              <Button size="pill-sm" variant="ghost" className="h-7 px-2 text-inverse-muted hover:bg-inverse hover:text-inverse-foreground"
                disabled={uninvite.isPending} onClick={() => uninvite.mutate(i.supplierId)} aria-label={`Remove ${i.supplierName}`}>
                Remove
              </Button>
            )}
            {manage && rfq.status === 'Published' && i.status === 'Invited' && (
              <Button size="pill-sm" variant="ghost" className="h-7 px-2 text-inverse-muted hover:bg-inverse hover:text-inverse-foreground"
                disabled={decline.isPending} onClick={() => decline.mutate(i.supplierId, { onSuccess: () => toast(`${i.supplierName} declined`) })}>
                Declined
              </Button>
            )}
          </li>
        ))}
      </ul>

      {canInvite && (
        <div className="flex flex-col gap-2 border-t border-inverse-muted/30 pt-4">
          <h3 className="text-sm font-semibold">Approved for these categories</h3>
          {eligible.isPending && <Skeleton className="h-16 rounded-2xl bg-inverse-row" />}
          {eligible.data && others.length === 0 && (
            <p className="text-[13px] text-inverse-muted">
              {eligible.data.length === 0
                ? 'No approved supplier covers these categories. Ask Procurement management to approve one.'
                : 'Every eligible supplier is invited.'}
            </p>
          )}
          <ul className="flex flex-col gap-1.5">
            {others.map((s) => (
              <li key={s.id} className="flex items-center gap-3 rounded-[18px] border border-inverse-muted/30 p-3">
                <span className="min-w-0 flex-1">
                  <span className="block truncate text-sm font-semibold">{s.name}</span>
                  <span className="block text-xs text-inverse-muted">
                    {s.code} · covers {s.linesCovered} of {s.totalLines} line{s.totalLines === 1 ? '' : 's'} · {s.paymentTermsDays} days
                  </span>
                </span>
                <Button size="pill-sm" variant="brand" disabled={invite.isPending}
                  onClick={() => invite.mutate(s.id, { onSuccess: () => toast(`${s.name} invited`) })}>
                  <HugeiconsIcon icon={UserAdd01Icon} strokeWidth={1.5} /> Invite
                </Button>
              </li>
            ))}
          </ul>
          <p className="text-xs text-inverse-muted">Suspended suppliers and those not approved for a category don't appear.</p>
        </div>
      )}

      {error && <p role="alert" className="rounded-xl bg-[#f3c9a6] px-3.5 py-3 text-[13px] text-[#5a2a08]">{problemMessage(error)}</p>}
    </aside>
  )
}
