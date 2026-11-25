import { useState, type ReactNode } from 'react'
import { FieldSelect } from '@/components/field-select'
import { Link, useParams } from 'react-router'
import { toast } from 'sonner'
import { HugeiconsIcon } from '@hugeicons/react'
import { ArrowLeft01Icon } from '@hugeicons/core-free-icons'
import { Panel } from '@/components/panel'
import { ErrorState } from '@/components/states/error-state'
import { Button } from '@/components/ui/button'
import { Skeleton } from '@/components/ui/skeleton'
import { useCurrentUser } from '@/features/auth/use-auth'
import { useCategories } from '@/features/catalogue/catalogue-api'
import { OrderStatusBadge } from '@/features/purchase-orders/order-status-badge'
import { usePurchaseOrders } from '@/features/purchase-orders/purchase-orders-api'
import { problemMessage } from '@/lib/api-client'
import { fieldClass } from '@/lib/field-class'
import { formatDate, formatNaira } from '@/lib/format'
import { can, Permission } from '@/lib/permissions'
import { cn } from '@/lib/utils'
import { SupplierEditor } from './supplier-editor'
import { SupplierStatusBadge } from './supplier-status-badge'
import {
  useCategoryDecision, useRequestCategory, useSupplier, useSupplierAction,
  type SupplierAction, type SupplierDetail,
} from './suppliers-api'

export function SupplierPage() {
  const { id } = useParams()
  const supplier = useSupplier(id)

  if (supplier.isPending) {
    return (
      <div className="flex flex-col gap-6" role="status" aria-label="Loading supplier">
        <Skeleton className="h-10 w-1/3 rounded-full" />
        <Skeleton className="h-64 rounded-[28px]" />
      </div>
    )
  }
  if (supplier.isError) {
    return <ErrorState error={supplier.error} what="this supplier" onRetry={() => supplier.refetch()} back={{ to: '/suppliers', label: 'Back to suppliers' }} />
  }
  return <Supplier s={supplier.data} />
}

function Supplier({ s }: { s: SupplierDetail }) {
  const { data: user } = useCurrentUser()
  const manage = can(user, Permission.SuppliersManage)
  const approver = can(user, Permission.SuppliersApprove)
  const [editing, setEditing] = useState(false)
  const editable = manage && s.status !== 'Blocked'

  return (
    <div className="flex flex-col gap-6">
      <section className="flex flex-col gap-3">
        <Link to="/suppliers" className="flex items-center gap-1.5 self-start text-sm font-semibold hover:text-brand">
          <HugeiconsIcon icon={ArrowLeft01Icon} size={18} strokeWidth={1.5} /> Suppliers
        </Link>
        <div>
          <p className="mb-1 text-[13px] font-semibold text-muted-foreground">{s.code}{s.tradingName && ` · trading as ${s.tradingName}`}</p>
          <h1 className="text-[40px] leading-tight font-light tracking-tight">{s.legalName}</h1>
          <div className="mt-3"><SupplierStatusBadge status={s.status} /></div>
        </div>
      </section>

      {s.statusReason && (s.status === 'Prospect' || s.status === 'Suspended' || s.status === 'Blocked') && (
        <div role="status" className="rounded-2xl bg-over px-5 py-4 text-sm text-over-foreground">
          <strong>{s.status === 'Prospect' ? 'Sent back' : s.status}:</strong> {s.statusReason}
        </div>
      )}

      <NextStep s={s} manage={manage} approver={approver} />

      {editing && <SupplierEditor s={s} onDone={() => setEditing(false)} />}

      <section className="flex flex-wrap items-start gap-5">
        <div className="flex min-w-0 flex-[2_1_560px] flex-col gap-5">
          <Categories s={s} manage={manage} approver={approver} />
          {can(user, Permission.PurchaseOrdersView) && <Orders supplierId={s.id} />}
        </div>
        <aside aria-label="Details" className="flex min-w-0 flex-[1_1_300px] flex-col gap-4 rounded-[28px] bg-inverse p-6 text-inverse-foreground">
          <div className="flex items-center justify-between gap-3">
            <h2 className="text-xl font-normal">Details</h2>
            {editable && !editing && (
              <Button size="pill-sm" variant="outline" className="border-inverse-muted/40 bg-transparent text-inverse-foreground hover:bg-inverse-row hover:text-inverse-foreground"
                onClick={() => { setEditing(true); window.scrollTo({ top: 0, behavior: 'smooth' }) }}>
                Edit
              </Button>
            )}
          </div>
          <dl className="grid grid-cols-[auto_1fr] gap-x-4 gap-y-2 text-sm">
            <Term label="Tax ID">{s.taxId}</Term>
            <Term label="Country">{s.countryCode}</Term>
            <Term label="Currency">{s.defaultCurrencyCode}</Term>
            <Term label="Payment terms">{s.paymentTermsDays} days</Term>
          </dl>
          <h3 className="mt-2 text-sm font-semibold">Contacts</h3>
          <ul className="flex flex-col gap-1.5">
            {s.contacts.map((c) => (
              <li key={c.id} className="rounded-[18px] bg-inverse-row p-3 text-sm">
                <span className="block font-semibold">{c.name}{c.isPrimary && <span className="ml-2 text-xs font-normal text-inverse-muted">primary</span>}</span>
                <span className="block text-xs text-inverse-muted">{[c.role, c.email, c.phone].filter(Boolean).join(' · ')}</span>
              </li>
            ))}
          </ul>
          {s.addresses.length > 0 && (
            <>
              <h3 className="mt-2 text-sm font-semibold">Addresses</h3>
              <ul className="flex flex-col gap-1.5 text-sm">
                {s.addresses.map((a) => (
                  <li key={a.id} className="rounded-[18px] bg-inverse-row p-3">
                    <span className="block text-xs text-inverse-muted">{a.addressType}</span>
                    {[a.line1, a.line2, a.city, a.state].filter(Boolean).join(', ')}
                  </li>
                ))}
              </ul>
            </>
          )}
        </aside>
      </section>
    </div>
  )
}

/** What happens next in the supplier's lifecycle, for whoever can do it. */
function NextStep({ s, manage, approver }: { s: SupplierDetail; manage: boolean; approver: boolean }) {
  const act = useSupplierAction(s.id)
  const [asking, setAsking] = useState<SupplierAction | null>(null)
  const [reason, setReason] = useState('')

  const run = (action: SupplierAction, message: string, withReason?: string) =>
    act.mutate({ action, reason: withReason }, { onSuccess: () => { toast(message); setAsking(null); setReason('') } })

  const pendingCategories = s.categories.filter((c) => c.status === 'Pending').length
  let title: string, body: string
  let buttons: ReactNode = null

  switch (s.status) {
    case 'Prospect':
      title = 'Request categories, then send for approval'
      body = pendingCategories ? `${pendingCategories} categor${pendingCategories === 1 ? 'y' : 'ies'} requested. A Procurement manager approves the supplier and its categories together.`
        : 'Request at least one category below before sending it for approval.'
      if (manage) buttons = <Button size="pill" variant="brand" disabled={!pendingCategories || act.isPending} onClick={() => run('submit', `${s.legalName} sent for approval`)}>Send for approval</Button>
      break
    case 'PendingApproval':
      title = approver ? 'Review and approve' : 'Waiting for a Procurement manager'
      body = 'Approving also approves every requested category, so the supplier can be invited to RFQs in them.'
      if (approver) buttons = (
        <>
          <Button size="pill" variant="outline" onClick={() => setAsking('reject')}>Send back</Button>
          <Button size="pill" variant="brand" disabled={act.isPending} onClick={() => run('approve', `${s.legalName} approved`)}>Approve</Button>
        </>
      )
      break
    case 'Approved':
      title = 'Approved'
      body = 'Can be invited to RFQs in its approved categories.'
      if (approver) buttons = <Button size="pill-sm" variant="ghost" className="text-destructive hover:text-destructive" onClick={() => setAsking('suspend')}>Suspend</Button>
      break
    case 'Suspended':
      title = 'Suspended'
      body = "Can't be invited to new RFQs until reinstated. Open orders aren't affected."
      if (approver) buttons = <Button size="pill" disabled={act.isPending} onClick={() => run('reinstate', `${s.legalName} reinstated`)}>Reinstate</Button>
      break
    default:
      title = 'Blocked'
      body = 'This supplier is blocked and cannot be used.'
  }

  return (
    <Panel className="flex flex-col gap-3 px-6 py-5">
      <div className="flex flex-wrap items-center justify-between gap-4">
        <div className="min-w-0">
          <h2 className="text-xl font-normal">{title}</h2>
          <p className="mt-1 text-sm text-muted-foreground">{body}</p>
        </div>
        <div className="flex flex-wrap gap-2">{buttons}</div>
      </div>
      {asking && (
        <div className="flex flex-col gap-2.5 rounded-2xl bg-destructive/10 p-4">
          <label htmlFor="supplier-reason" className="text-[13px] font-semibold text-destructive">
            {asking === 'reject' ? 'What needs fixing before it can be approved?' : 'Why suspend this supplier?'}
          </label>
          <textarea id="supplier-reason" rows={2} maxLength={500} value={reason} onChange={(e) => setReason(e.target.value)}
            className={cn(fieldClass, 'h-auto py-2.5')} />
          <div className="flex justify-end gap-2">
            <Button size="pill-sm" variant="ghost" onClick={() => setAsking(null)}>Cancel</Button>
            <Button size="pill-sm" variant="destructive" disabled={!reason.trim() || act.isPending}
              onClick={() => run(asking, asking === 'reject' ? `${s.legalName} sent back` : `${s.legalName} suspended`, reason.trim())}>
              {asking === 'reject' ? 'Send back' : 'Suspend'}
            </Button>
          </div>
        </div>
      )}
      {act.error && <p role="alert" className="rounded-xl bg-destructive/10 px-3.5 py-3 text-[13px] text-destructive">{problemMessage(act.error)}</p>}
    </Panel>
  )
}

const categoryStyle = {
  Pending: 'bg-pending text-pending-foreground',
  Approved: 'bg-success text-success-foreground',
  Revoked: 'bg-cancelled text-cancelled-foreground',
}

function Categories({ s, manage, approver }: { s: SupplierDetail; manage: boolean; approver: boolean }) {
  const all = useCategories()
  const request = useRequestCategory(s.id)
  const decide = useCategoryDecision(s.id)
  const [choice, setChoice] = useState('')
  const live = new Set(s.categories.filter((c) => c.status !== 'Revoked').map((c) => c.categoryId))
  const options = (all.data ?? []).filter((c) => c.isActive && !live.has(c.id))
  const error = request.error ?? decide.error

  return (
    <Panel className="flex flex-col gap-3 px-6 py-5">
      <div>
        <h2 className="text-xl font-normal">Categories</h2>
        <p className="mt-1 text-sm text-muted-foreground">A supplier can quote only for lines in categories it's approved for.</p>
      </div>
      {s.categories.length === 0 && <p className="text-sm text-muted-foreground">No categories requested yet.</p>}
      <ul className="flex flex-col divide-y">
        {s.categories.map((c) => (
          <li key={c.categoryId} className="flex flex-wrap items-center gap-3 py-2.5">
            <span className="min-w-0 flex-1">
              <span className="block text-sm font-semibold">{c.categoryName}</span>
              <span className="block text-xs text-muted-foreground">{c.categoryCode}{c.approvedAt && ` · approved ${formatDate(c.approvedAt.slice(0, 10))}`}</span>
            </span>
            <span className={cn('rounded-full px-2.5 py-1 text-xs font-bold', categoryStyle[c.status])}>{c.status}</span>
            {approver && s.status === 'Approved' && c.status === 'Pending' && (
              <Button size="pill-sm" variant="outline" disabled={decide.isPending}
                onClick={() => decide.mutate({ categoryId: c.categoryId, decision: 'approve' }, { onSuccess: () => toast(`${c.categoryName} approved`) })}>Approve</Button>
            )}
            {approver && c.status !== 'Revoked' && s.status !== 'Prospect' && (
              <Button size="pill-sm" variant="ghost" className="text-destructive hover:text-destructive" disabled={decide.isPending}
                onClick={() => decide.mutate({ categoryId: c.categoryId, decision: 'revoke' }, { onSuccess: () => toast(`${c.categoryName} revoked`) })}>Revoke</Button>
            )}
          </li>
        ))}
      </ul>
      {manage && s.status !== 'Blocked' && (
        <div className="flex flex-wrap items-center gap-2 border-t pt-3">
          <FieldSelect aria-label="Category to request" size="sm" className="min-w-0 flex-[1_1_220px]" value={choice} onChange={setChoice}
            placeholder="Request a category…" options={options.map((c) => ({ value: c.id, label: `${c.name} (${c.code})` }))} />
          <Button size="pill-sm" variant="brand" disabled={!choice || request.isPending}
            onClick={() => request.mutate(choice, { onSuccess: () => { toast('Category requested'); setChoice('') } })}>Request</Button>
          {s.status === 'Approved' && <span className="text-xs text-muted-foreground">New requests on an approved supplier need a manager's approval.</span>}
        </div>
      )}
      {error && <p role="alert" className="rounded-xl bg-destructive/10 px-3.5 py-3 text-[13px] text-destructive">{problemMessage(error)}</p>}
    </Panel>
  )
}

function Orders({ supplierId }: { supplierId: string }) {
  const orders = usePurchaseOrders()
  const mine = (orders.data ?? []).filter((o) => o.supplierId === supplierId)
  const live = mine.filter((o) => o.status !== 'Cancelled' && o.status !== 'Draft')

  return (
    <Panel className="flex flex-col gap-3 px-6 py-5">
      <div className="flex flex-wrap items-baseline justify-between gap-2">
        <h2 className="text-xl font-normal">Purchase orders</h2>
        {live.length > 0 && <span className="text-[13px] text-muted-foreground">{live.length} issued · {formatNaira(live.reduce((s, o) => s + o.total, 0))}</span>}
      </div>
      {orders.isPending && <Skeleton className="h-24 rounded-2xl" />}
      {orders.data && mine.length === 0 && <p className="text-sm text-muted-foreground">No orders with this supplier yet.</p>}
      <ul className="flex flex-col divide-y">
        {mine.slice(0, 8).map((o) => (
          <li key={o.id} className="flex flex-wrap items-center gap-3 py-2.5 text-sm">
            <Link to={`/purchase-orders/${o.id}`} className="font-semibold hover:text-brand">{o.number}</Link>
            <span className="min-w-0 flex-1 truncate text-xs text-muted-foreground">{o.sourcingEventNumber} · expected {formatDate(o.expectedDelivery)}</span>
            <span className="font-semibold">{formatNaira(o.total)}</span>
            <OrderStatusBadge status={o.status} />
          </li>
        ))}
      </ul>
    </Panel>
  )
}

function Term({ label, children }: { label: string; children: ReactNode }) {
  return (
    <>
      <dt className="text-inverse-muted">{label}</dt>
      <dd className="font-semibold">{children}</dd>
    </>
  )
}
