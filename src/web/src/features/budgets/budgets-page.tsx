import { useMemo, useState, type FormEvent } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router'
import { useQuery } from '@tanstack/react-query'
import { toast } from 'sonner'
import { WalletIcon } from '@hugeicons/core-free-icons'
import { FormField } from '@/components/form-field'
import { Panel } from '@/components/panel'
import { EmptyState } from '@/components/states/empty-state'
import { ErrorState } from '@/components/states/error-state'
import { TableSkeleton } from '@/components/states/table-skeleton'
import { Button } from '@/components/ui/button'
import { useCurrentUser } from '@/features/auth/use-auth'
import { api, problemMessage } from '@/lib/api-client'
import { fieldClass } from '@/lib/field-class'
import { fiscalYearOf, formatNaira } from '@/lib/format'
import { can, Permission } from '@/lib/permissions'
import { cn } from '@/lib/utils'
import { BudgetBar } from './budget-bar'
import { freeOf, healthOf, useBudgets, useSetBudget, type BudgetSummary } from './budgets-api'

const columns = ['Cost centre', 'Budget', 'Spent', 'Committed', 'In approval', 'Free', 'Use']

export function BudgetsPage() {
  const { data: user } = useCurrentUser()
  const navigate = useNavigate()
  const [params, setParams] = useSearchParams()
  const current = fiscalYearOf()
  const year = Number(params.get('fy')) || current
  const years = [current - 1, current, current + 1]
  const budgets = useBudgets(year)
  const manage = can(user, Permission.BudgetsManage)
  const everyone = can(user, Permission.BudgetsView)

  const totals = useMemo(() => (budgets.data ?? []).reduce(
    (t, b) => ({
      amount: t.amount + b.amount, consumed: t.consumed + b.consumed, committed: t.committed + b.committed,
      inApproval: t.inApproval + b.inApproval, available: t.available + b.available,
    }),
    { amount: 0, consumed: 0, committed: 0, inApproval: 0, available: 0 }), [budgets.data])

  return (
    <div className="flex flex-col gap-6">
      <section className="flex flex-wrap items-end justify-between gap-5">
        <div>
          <h1 className="text-[40px] leading-tight font-light tracking-tight">Budgets</h1>
          <p className="mt-2 text-[15px] text-muted-foreground">
            {everyone ? 'Every cost centre' : `${user?.departmentCode} cost centres, read-only`} · FY{year} runs 1 Jul {year - 1} – 30 Jun {year}
          </p>
        </div>
        <div role="tablist" aria-label="Fiscal year" className="flex gap-1 rounded-full border border-white/90 bg-card p-1.5 dark:border-white/10">
          {years.map((y) => (
            <button key={y} role="tab" aria-selected={y === year} onClick={() => setParams(y === current ? {} : { fy: String(y) }, { replace: true })}
              className={cn('rounded-full px-4 py-2 text-sm font-medium',
                y === year ? 'bg-inverse font-semibold text-inverse-foreground' : 'hover:bg-accent')}>
              FY{y}{y === current && ' · now'}
            </button>
          ))}
        </div>
      </section>

      {budgets.isPending && <TableSkeleton columns={columns} rows={4} />}
      {budgets.isError && <ErrorState error={budgets.error} what="budgets" onRetry={() => budgets.refetch()} />}

      {budgets.data?.length === 0 && (
        <EmptyState icon={WalletIcon} title={`No budgets for FY${year} yet`}>
          {manage ? 'Set one below for each cost centre.' : 'Finance sets them. Until then, requisitions are checked against a zero budget and need Finance approval.'}
        </EmptyState>
      )}

      {!!budgets.data?.length && (
        <>
          <Panel className="flex flex-col gap-4 px-6 py-5">
            <div className="flex flex-wrap items-baseline justify-between gap-3">
              <h2 className="text-xl font-normal">FY{year} at a glance</h2>
              <span className="font-heading text-[28px] font-light">{formatNaira(totals.amount)}</span>
            </div>
            <BudgetBar totals={totals} showLegend />
          </Panel>

          <Panel className="px-3.5 py-2.5">
            <div className="overflow-x-auto">
              <table className="w-full min-w-[860px] border-collapse text-sm">
                <thead>
                  <tr className="text-left text-xs text-muted-foreground">
                    {columns.map((c) => (
                      <th key={c} scope="col" className={cn('p-3 font-semibold', c !== 'Cost centre' && c !== 'Use' && 'text-right')}>{c}</th>
                    ))}
                  </tr>
                </thead>
                <tbody>
                  {budgets.data.map((b) => <BudgetRow key={b.id} budget={b} onOpen={() => navigate(`/budgets/${b.id}`)} />)}
                </tbody>
              </table>
            </div>
          </Panel>
        </>
      )}

      {manage && <SetBudget year={year} />}
    </div>
  )
}

function BudgetRow({ budget: b, onOpen }: { budget: BudgetSummary; onOpen: () => void }) {
  const health = healthOf(b)
  const free = freeOf(b)
  return (
    <tr onClick={onOpen} className="cursor-pointer border-t hover:bg-accent">
      <td className="p-3">
        <Link to={`/budgets/${b.id}`} onClick={(e) => e.stopPropagation()} className="font-semibold hover:text-brand">
          {b.costCentreCode}
        </Link>
        <div className="text-xs text-muted-foreground">{b.costCentreName} · {b.departmentCode}</div>
      </td>
      <td className="p-3 text-right font-semibold">{formatNaira(b.amount)}</td>
      <td className="p-3 text-right">{formatNaira(b.consumed)}</td>
      <td className="p-3 text-right">{formatNaira(b.committed)}</td>
      <td className="p-3 text-right">{formatNaira(b.inApproval)}</td>
      <td className="p-3 text-right">
        <span className={cn('font-semibold', health === 'over' && 'text-destructive')}>
          {free < 0 ? `−${formatNaira(-free)}` : formatNaira(free)}
        </span>
        {health !== 'ok' && (
          <div className={cn('text-xs', health === 'over' ? 'text-destructive' : 'text-pending-foreground')}>
            {health === 'over' ? 'Over budget' : 'Under 10% left'}
          </div>
        )}
      </td>
      <td className="w-48 p-3"><BudgetBar totals={b} size="sm" /></td>
    </tr>
  )
}

type DepartmentDto = { id: string; code: string; name: string; isActive: boolean; costCentres: { id: string; code: string; name: string; isActive: boolean }[] }

/** Finance only: set or change a cost centre's budget for the year. */
function SetBudget({ year }: { year: number }) {
  const departments = useQuery({
    queryKey: ['organisation', 'departments'],
    queryFn: ({ signal }) => api<DepartmentDto[]>('/api/organisation/departments', { signal }),
    staleTime: Infinity,
  })
  const setBudget = useSetBudget()
  const [costCentreId, setCostCentreId] = useState('')
  const [amount, setAmount] = useState('')
  const value = Number(amount)

  async function submit(event: FormEvent) {
    event.preventDefault()
    if (!costCentreId || !(value >= 0) || amount === '') return
    try {
      await setBudget.mutateAsync({ costCentreId, fiscalYear: year, amount: value })
      toast(`Budget set for FY${year}`, { description: formatNaira(value) })
      setAmount('')
    } catch { /* shown below */ }
  }

  return (
    <Panel className="flex flex-col gap-4 px-6 py-5">
      <div>
        <h2 className="text-xl font-normal">Set a budget</h2>
        <p className="mt-1 text-sm text-muted-foreground">Creates FY{year}'s budget for a cost centre, or changes it. Commitments already made stay.</p>
      </div>
      <form onSubmit={submit} className="grid items-end gap-3 sm:grid-cols-[2fr_1fr_auto]">
        <FormField id="set-cc" label="Cost centre">
          <select id="set-cc" className={fieldClass} value={costCentreId} onChange={(e) => setCostCentreId(e.target.value)}>
            <option value="">Choose…</option>
            {departments.data?.filter((d) => d.isActive).map((d) => (
              <optgroup key={d.id} label={`${d.code} · ${d.name}`}>
                {d.costCentres.filter((c) => c.isActive).map((c) => <option key={c.id} value={c.id}>{c.code} · {c.name}</option>)}
              </optgroup>
            ))}
          </select>
        </FormField>
        <FormField id="set-amount" label="Amount (₦)">
          <input id="set-amount" type="number" min="0" step="1000" inputMode="numeric" className={fieldClass}
            value={amount} onChange={(e) => setAmount(e.target.value)} placeholder="20000000" />
        </FormField>
        <Button type="submit" size="pill" disabled={!costCentreId || amount === '' || setBudget.isPending}>
          {setBudget.isPending ? 'Saving…' : 'Set budget'}
        </Button>
      </form>
      {setBudget.error && <p role="alert" className="text-[13px] text-destructive">{problemMessage(setBudget.error)}</p>}
    </Panel>
  )
}
