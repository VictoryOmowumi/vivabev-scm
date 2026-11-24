import { useState, type FormEvent, type ReactNode } from 'react'
import { Link, useNavigate } from 'react-router'
import { HugeiconsIcon, type IconSvgElement } from '@hugeicons/react'
import { ArrowUpRight01Icon, SparklesIcon } from '@hugeicons/core-free-icons'
import { Panel } from '@/components/panel'
import { Skeleton } from '@/components/ui/skeleton'
import type { CurrentUser } from '@/features/auth/auth-api'
import { formatNaira } from '@/lib/format'
import { cn } from '@/lib/utils'

// Shared pieces of the dashboard, laid out as in the Main design board.

function greeting(now = new Date()) {
  const hour = now.getHours()
  return hour < 12 ? 'Good morning' : hour < 17 ? 'Good afternoon' : 'Good evening'
}

export type Stat = { value: ReactNode; label: string; icon: IconSvgElement; to?: string }

/** Greeting on the left, up to three headline numbers on the right. */
export function Hero({ user, stats }: { user: CurrentUser; stats: Stat[] }) {
  const [hello] = useState(() => greeting())
  const who = [user.jobTitle, user.departmentName, user.siteName].filter(Boolean).join(' · ')

  return (
    <section className="flex flex-wrap items-end justify-between gap-7">
      <div>
        <h1 className="text-[32px] leading-[1.1] font-light tracking-tight">{hello}, {user.displayName.split(' ')[0]}</h1>
        <p className="mt-2.5 text-[15px] text-muted-foreground">{who}</p>
      </div>
      <div className="flex flex-wrap gap-10">
        {stats.map((s) => {
          const body = (
            <>
              <span className="block font-heading text-[44px] leading-none font-light">{s.value}</span>
              <span className="mt-1.5 flex items-center gap-1.5 text-[13px] text-muted-foreground">
                <HugeiconsIcon icon={s.icon} size={15} strokeWidth={1.5} /> {s.label}
              </span>
            </>
          )
          return s.to
            ? <Link key={s.label} to={s.to} className="block hover:text-brand">{body}</Link>
            : <div key={s.label}>{body}</div>
        })}
      </div>
    </section>
  )
}

export type Segment = { label: string; amount: number; tone: 'dark' | 'green' | 'hatch' }

const segmentTone = {
  dark: 'bg-inverse text-inverse-foreground',
  green: 'bg-green text-green-foreground',
  hatch: 'hatch',
}

/**
 * The full-width pill strip: labelled segments sized by amount, then an outlined total.
 * When the segments add up to more than the total, the total chip turns orange and says by how much.
 */
export function Strip({ label, segments, total, totalLabel }: {
  label: string
  segments: Segment[]
  total: number
  totalLabel: string
}) {
  const sum = segments.reduce((s, x) => s + Math.max(x.amount, 0), 0)
  const scale = Math.max(total, sum, 1)
  const shown = segments.filter((s) => s.amount > 0)
  const over = sum - total
  const pct = (n: number) => Math.round((n / scale) * 100)

  return (
    <section aria-label={label} className="flex flex-col gap-2">
      <div className="flex gap-1.5 text-[13px] text-muted-foreground">
        {/* Labels on slivers under 8% would only truncate; the pill's tooltip still names them */}
        {shown.map((s) => <span key={s.label} className="min-w-11 truncate" style={{ flex: `${s.amount} 1 0` }}>{pct(s.amount) >= 8 ? s.label : ''}</span>)}
        <span className="w-[132px] flex-none max-sm:hidden">{totalLabel}</span>
      </div>
      <div className="flex h-11 gap-1.5">
        {shown.length === 0 && <div className="hatch flex-1 rounded-full" />}
        {shown.map((s) => (
          <div key={s.label} title={`${s.label}: ${formatNaira(s.amount)}`}
            className={cn('flex min-w-11 items-center overflow-hidden rounded-full px-4 text-[13px] font-semibold', segmentTone[s.tone])}
            style={{ flex: `${s.amount} 1 0` }}>
            {pct(s.amount)}%
          </div>
        ))}
        <div className={cn('flex w-[132px] flex-none items-center justify-center rounded-full border-[1.5px] text-[13px] font-semibold max-sm:hidden',
          over > 0 ? 'border-over-foreground bg-over text-over-foreground' : 'border-foreground')}>
          {over > 0 ? `Over ${formatNaira(over)}` : formatNaira(total)}
        </div>
      </div>
    </section>
  )
}

export function CopilotBar({ suggestions, placeholder }: { suggestions: string[]; placeholder: string }) {
  const navigate = useNavigate()
  const [question, setQuestion] = useState('')
  const ask = (event: FormEvent) => { event.preventDefault(); navigate('/copilot') }

  return (
    <Panel aria-label="VivaBev Copilot" className="flex flex-col gap-3 px-5 py-[18px]">
      <form onSubmit={ask} className="flex flex-wrap items-center gap-3">
        <span className="flex size-10 flex-none items-center justify-center rounded-full bg-brand text-brand-foreground">
          <HugeiconsIcon icon={SparklesIcon} size={18} strokeWidth={1.5} />
        </span>
        <label htmlFor="copilot-ask" className="sr-only">Ask VivaBev Copilot</label>
        <input id="copilot-ask" value={question} onChange={(e) => setQuestion(e.target.value)} placeholder={placeholder}
          className="h-[46px] min-w-0 flex-[1_1_320px] rounded-full border border-input bg-surface px-[18px] text-sm outline-none placeholder:text-muted-foreground focus-visible:ring-2 focus-visible:ring-ring/30" />
        <button type="submit" className="h-[46px] flex-none rounded-full bg-inverse px-5 text-sm font-semibold text-inverse-foreground">Ask</button>
      </form>
      <div className="flex flex-wrap items-center gap-2 text-[13px]">
        {suggestions.map((s) => (
          <Link key={s} to="/copilot" className="rounded-full border border-input px-3.5 py-[7px] hover:text-brand">{s}</Link>
        ))}
        <span className="text-muted-foreground">Answers use only data you can access. Copilot drafts; it never approves.</span>
      </div>
    </Panel>
  )
}

export type DarkItem = {
  key: string
  to: string
  icon: IconSvgElement
  title: string
  meta: string
  amount: number
  flag?: string
}

/** The dark "Waiting for you" card. */
export function DarkList({ title, items, loading, error, empty, action }: {
  title: string
  items: DarkItem[] | undefined
  loading: boolean
  error: boolean
  empty: string
  action: { to: string; label: string }
}) {
  return (
    <section aria-label={title} className="flex min-w-0 flex-[1_1_420px] flex-col gap-[18px] rounded-[28px] bg-inverse p-[26px] text-inverse-foreground">
      <div className="flex items-center justify-between">
        <h2 className="text-xl font-normal">{title}</h2>
        <span className="font-heading text-[32px] leading-none font-light">{items?.length ?? '–'}</span>
      </div>
      {loading && <Skeleton className="h-40 rounded-[18px] bg-inverse-row" />}
      {error && <p className="text-sm text-inverse-muted">Couldn't load this. Try refreshing.</p>}
      {items?.length === 0 && <p className="text-sm text-inverse-muted">{empty}</p>}
      <ul className="flex flex-col gap-1.5">
        {items?.slice(0, 3).map((item) => (
          <li key={item.key}>
            <Link to={item.to} className="flex items-center gap-3.5 rounded-[18px] bg-inverse-row p-2.5 text-inverse-foreground hover:text-inverse-foreground hover:opacity-90">
              <span className="flex size-10 flex-none items-center justify-center rounded-full bg-inverse-chip">
                <HugeiconsIcon icon={item.icon} size={18} strokeWidth={1.5} />
              </span>
              <span className="min-w-0 flex-1">
                <span className="block truncate text-sm font-semibold">{item.title}</span>
                <span className="block truncate text-xs text-inverse-muted">{item.meta}</span>
              </span>
              <span className="text-right">
                <span className="block text-[13px] font-semibold">{formatNaira(item.amount)}</span>
                {item.flag && <span className="mt-1 inline-block rounded-full bg-[#f3c9a6] px-2 py-0.5 text-[11px] font-bold text-[#5a2a08]">{item.flag}</span>}
              </span>
            </Link>
          </li>
        ))}
      </ul>
      {(items?.length ?? 0) > 3 && <p className="-mt-2 text-xs text-inverse-muted">and {items!.length - 3} more</p>}
      <Link to={action.to} className="mt-auto flex items-center gap-2 self-start rounded-full bg-inverse-foreground px-[18px] py-2.5 text-sm font-semibold text-inverse hover:text-inverse hover:opacity-90">
        {action.label} <HugeiconsIcon icon={ArrowUpRight01Icon} size={16} strokeWidth={1.5} />
      </Link>
    </section>
  )
}

/** A frosted card with a title and the round arrow link in its corner. */
export function Card({ title, to, linkLabel, className, children }: {
  title: string
  to?: string
  linkLabel?: string
  className?: string
  children: ReactNode
}) {
  return (
    <Panel className={cn('flex min-w-0 flex-col gap-4 p-[26px]', className)}>
      <div className="flex items-center justify-between gap-3">
        <h2 className="text-xl font-normal">{title}</h2>
        {to && (
          <Link to={to} aria-label={linkLabel ?? `Open ${title}`} className="flex size-11 flex-none items-center justify-center rounded-full bg-surface">
            <HugeiconsIcon icon={ArrowUpRight01Icon} size={18} strokeWidth={1.5} />
          </Link>
        )}
      </div>
      {children}
    </Panel>
  )
}

/** Big number with a caption beside it. */
export function BigFigure({ value, caption, size = 'lg', tone }: { value: ReactNode; caption: string; size?: 'lg' | 'md'; tone?: 'over' }) {
  return (
    <div className="flex items-baseline gap-2.5">
      <span className={cn('font-heading leading-none font-light', size === 'lg' ? 'text-[52px]' : 'text-[32px]', tone === 'over' && 'text-destructive')}>{value}</span>
      <span className={cn('text-muted-foreground', size === 'lg' ? 'text-sm' : 'text-[13px]')}>{caption}</span>
    </div>
  )
}

/** Two-part bar: dark used, hatched remaining. */
export function UsedBar({ used, total }: { used: number; total: number }) {
  const scale = Math.max(total, used, 1)
  const rest = Math.max(total - used, 0)
  return (
    <div className="flex h-3.5 gap-1" aria-hidden>
      {used > 0 && <div className={cn('rounded-full', used > total ? 'bg-over-foreground' : 'bg-inverse')} style={{ flex: `${used / scale} 1 0` }} />}
      {rest > 0 && <div className="hatch rounded-full" style={{ flex: `${rest / scale} 1 0` }} />}
    </div>
  )
}

export function Figures({ rows }: { rows: { label: string; value: ReactNode }[] }) {
  return (
    <dl className="mt-1 grid grid-cols-[1fr_auto] gap-y-2.5 text-sm">
      {rows.map((r) => (
        <div key={r.label} className="contents">
          <dt className="text-muted-foreground">{r.label}</dt>
          <dd className="font-semibold">{r.value}</dd>
        </div>
      ))}
    </dl>
  )
}

export type Bar = { label: string; count: number; to: string; tone: 'dark' | 'green' | 'grey' }

/** Vertical pill bars; the tallest one gets a green callout like "3 pending". */
export function Bars({ bars, highlight }: { bars: Bar[]; highlight?: string }) {
  const max = Math.max(1, ...bars.map((b) => b.count))
  const tone = { dark: 'bg-inverse', green: 'bg-green', grey: 'bg-grey-bar' }
  return (
    <div role="img" aria-label={bars.map((b) => `${b.count} ${b.label.toLowerCase()}`).join(', ')}
      className="flex h-[170px] items-end justify-around gap-3 pt-[18px]">
      {bars.map((b) => (
        <Link key={b.label} to={b.to} className="flex flex-col items-center gap-2 hover:text-brand">
          {b.label === highlight && b.count > 0
            ? <span className="rounded-full bg-brand px-2.5 py-[3px] text-xs font-bold text-brand-foreground">{b.count} {b.label.toLowerCase()}</span>
            : <span className="text-xs font-semibold">{b.count}</span>}
          <span className={cn('w-3.5 rounded-full', tone[b.tone], b.count === 0 && 'opacity-30')}
            style={{ height: b.count === 0 ? 8 : 24 + (b.count / max) * 96 }} />
          <span className="text-xs text-muted-foreground">{b.label}</span>
        </Link>
      ))}
    </div>
  )
}

export type DueItem = { key: string; date: string; to: string; title: string; note: string; needsYou: boolean }

/** Date tile + card; items that need the viewer are green. */
export function DueSoon({ subtitle, items, loading, empty }: { subtitle: string; items: DueItem[]; loading: boolean; empty: string }) {
  return (
    <Panel className="flex min-w-0 flex-[1_1_340px] flex-col gap-3.5 p-[26px]">
      <h2 className="text-xl font-normal">Due soon</h2>
      <p className="text-[13px] text-muted-foreground">{subtitle}</p>
      {loading && <Skeleton className="h-40 rounded-2xl" />}
      {!loading && items.length === 0 && <p className="text-sm text-muted-foreground">{empty}</p>}
      <ul className="flex flex-col gap-2.5">
        {items.map((d) => {
          const date = new Date(`${d.date}T00:00:00`)
          return (
            <li key={d.key}>
              <Link to={d.to} className="group flex items-center gap-3.5">
                <span className="w-14 flex-none rounded-2xl bg-surface py-2 text-center">
                  <span className="block text-[11px] text-muted-foreground uppercase">{date.toLocaleDateString('en-GB', { month: 'short' })}</span>
                  <span className="block font-heading text-[22px] leading-[1.1]">{date.getDate()}</span>
                </span>
                <span className={cn('min-w-0 flex-1 rounded-2xl px-3.5 py-3 group-hover:opacity-90',
                  d.needsYou ? 'bg-green text-green-foreground' : 'bg-surface')}>
                  <span className="block truncate text-sm font-semibold">{d.title}</span>
                  <span className="block truncate text-xs">{d.note}</span>
                </span>
              </Link>
            </li>
          )
        })}
      </ul>
    </Panel>
  )
}
