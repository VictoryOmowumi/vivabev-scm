import type { ReactNode } from 'react'
import { Link, useParams } from 'react-router'
import { HugeiconsIcon } from '@hugeicons/react'
import { ArrowLeft01Icon, PrinterIcon } from '@hugeicons/core-free-icons'
import { ErrorState } from '@/components/states/error-state'
import { Button } from '@/components/ui/button'
import { Skeleton } from '@/components/ui/skeleton'
import { useCurrentUser } from '@/features/auth/use-auth'
import { formatDateTime, formatNaira } from '@/lib/format'
import { can, Permission } from '@/lib/permissions'
import { useReceipt } from './warehouse-api'

/** The goods received note, printable for the supplier's driver and the file. */
export function ReceiptPage() {
  const { id } = useParams()
  const { data: user } = useCurrentUser()
  const receipt = useReceipt(id)

  if (receipt.isPending) return <Skeleton className="h-96 rounded-[28px]" />
  if (receipt.isError) {
    return <ErrorState error={receipt.error} what="this receipt" onRetry={() => receipt.refetch()} back={{ to: '/warehouse?tab=receipts', label: 'Back to receipts' }} />
  }
  const r = receipt.data
  const rejected = r.lines.filter((l) => l.quantityRejected > 0)

  return (
    <div className="flex flex-col gap-6">
      <section className="flex flex-wrap items-end justify-between gap-4 print:hidden">
        <div className="flex flex-col gap-2">
          <Link to="/warehouse?tab=receipts" className="flex items-center gap-1.5 self-start text-sm font-semibold hover:text-brand">
            <HugeiconsIcon icon={ArrowLeft01Icon} size={18} strokeWidth={1.5} /> Warehouse
          </Link>
          <h1 className="text-[40px] leading-tight font-light tracking-tight">{r.number}</h1>
          <p className="text-sm text-muted-foreground">
            Against{' '}
            {can(user, Permission.PurchaseOrdersView)
              ? <Link to={`/purchase-orders/${r.purchaseOrderId}`} className="font-semibold text-foreground hover:text-brand">{r.purchaseOrderNumber}</Link>
              : r.purchaseOrderNumber}
            {' '}· {r.supplierName}
          </p>
        </div>
        <Button size="pill" variant="outline" onClick={() => window.print()}>
          <HugeiconsIcon icon={PrinterIcon} strokeWidth={1.5} /> Print
        </Button>
      </section>

      <article aria-label={`Goods received note ${r.number}`}
        className="flex flex-col gap-6 rounded-[28px] bg-surface p-8 shadow-sm print:rounded-none print:bg-white print:p-0 print:text-black print:shadow-none">
        <header className="flex flex-wrap items-start justify-between gap-6 border-b pb-5">
          <div>
            <div className="font-heading text-2xl">VivaBev Beverages Ltd</div>
            <div className="mt-1 text-sm text-muted-foreground print:text-neutral-600">{r.siteName} ({r.siteCode})</div>
          </div>
          <div className="text-right">
            <div className="text-xs font-bold tracking-[0.18em] text-muted-foreground uppercase print:text-neutral-600">Goods received note</div>
            <div className="font-heading text-3xl font-light">{r.number}</div>
            <div className="mt-1 text-sm">{formatDateTime(r.receivedAt)}</div>
          </div>
        </header>

        <dl className="grid grid-cols-[repeat(auto-fit,minmax(200px,1fr))] gap-4 text-sm">
          <Term label="Supplier">{r.supplierName}</Term>
          <Term label="Purchase order">{r.purchaseOrderNumber}</Term>
          <Term label="Their delivery note">{r.deliveryNoteNumber ?? '–'}</Term>
          <Term label="Received by">{r.receivedByName}</Term>
        </dl>

        <div className="overflow-x-auto">
          <table className="w-full min-w-[600px] border-collapse text-sm">
            <thead>
              <tr className="border-b text-left text-xs text-muted-foreground print:text-neutral-600">
                <th scope="col" className="py-2 pr-3 font-semibold">#</th>
                <th scope="col" className="py-2 pr-3 font-semibold">Description</th>
                <th scope="col" className="py-2 pr-3 text-right font-semibold">Accepted</th>
                <th scope="col" className="py-2 pr-3 text-right font-semibold">Rejected</th>
                <th scope="col" className="py-2 text-right font-semibold">Value accepted</th>
              </tr>
            </thead>
            <tbody>
              {r.lines.map((l) => (
                <tr key={l.lineNo} className="border-b align-top">
                  <td className="py-3 pr-3 text-muted-foreground print:text-neutral-600">{l.lineNo}</td>
                  <td className="py-3 pr-3">
                    <div className="font-semibold">{l.description}</div>
                    <div className="text-xs text-muted-foreground print:text-neutral-600">{l.itemCode ?? 'Free text'} · {formatNaira(l.unitPrice)} / {l.uomCode}</div>
                  </td>
                  <td className="py-3 pr-3 text-right font-semibold">{l.quantityAccepted} {l.uomCode}</td>
                  <td className="py-3 pr-3 text-right">
                    {l.quantityRejected > 0 ? <span className="text-destructive">{l.quantityRejected} {l.uomCode}</span> : '–'}
                  </td>
                  <td className="py-3 text-right font-semibold">{formatNaira(l.acceptedValue)}</td>
                </tr>
              ))}
            </tbody>
            <tfoot>
              <tr>
                <td colSpan={4} className="pt-4 pr-3 text-right font-semibold">Total accepted</td>
                <td className="pt-4 text-right font-heading text-2xl">{formatNaira(r.acceptedValue)}</td>
              </tr>
            </tfoot>
          </table>
        </div>

        {rejected.length > 0 && (
          <section className="rounded-2xl bg-over px-4 py-3.5 text-sm text-over-foreground print:border print:bg-white print:text-black">
            <div className="mb-1 text-xs font-bold">Returned to the supplier</div>
            <ul>{rejected.map((l) => <li key={l.lineNo}>{l.quantityRejected} {l.uomCode} × {l.description}: {l.rejectionReason}</li>)}</ul>
          </section>
        )}
        {r.notes && <p className="text-sm"><span className="text-muted-foreground print:text-neutral-600">Notes:</span> {r.notes}</p>}

        <footer className="grid grid-cols-2 gap-10 border-t pt-8 text-xs text-muted-foreground print:text-neutral-600">
          <div className="border-t border-dashed pt-2">Received for VivaBev</div>
          <div className="border-t border-dashed pt-2">Delivered by (supplier's driver)</div>
        </footer>
      </article>
    </div>
  )
}

function Term({ label, children }: { label: string; children: ReactNode }) {
  return (
    <div>
      <dt className="text-xs text-muted-foreground print:text-neutral-600">{label}</dt>
      <dd className="font-semibold">{children}</dd>
    </div>
  )
}
