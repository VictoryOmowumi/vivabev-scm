const naira = new Intl.NumberFormat('en-NG', {
  style: 'currency',
  currency: 'NGN',
  maximumFractionDigits: 0,
})

/** 64000000 -> "₦64,000,000" */
export function formatNaira(amount: number): string {
  return naira.format(amount)
}

/** "Funmi Adeleke" -> "FA" */
export function initials(name: string): string {
  return name
    .split(' ')
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0]!.toUpperCase())
    .join('')
}

/** 8600000 -> "₦8.6m", 950000 -> "₦950k" */
export function formatNairaShort(amount: number): string {
  const abs = Math.abs(amount)
  const sign = amount < 0 ? '-' : ''
  if (abs >= 1_000_000) return `${sign}₦${trim(abs / 1_000_000)}m`
  if (abs >= 1_000) return `${sign}₦${trim(abs / 1_000)}k`
  return `${sign}₦${abs}`
}

const trim = (n: number) => (Math.round(n * 10) / 10).toString()

const dateFormat = new Intl.DateTimeFormat('en-GB', { day: 'numeric', month: 'short', year: 'numeric' })
const dateTimeFormat = new Intl.DateTimeFormat('en-GB', {
  day: 'numeric',
  month: 'short',
  year: 'numeric',
  hour: '2-digit',
  minute: '2-digit',
})

/** "2027-01-31" (a C# DateOnly) -> "31 Jan 2027". Built from its parts, so no time zone can move the day. */
export function formatDate(isoDate: string): string {
  const [year, month, day] = isoDate.slice(0, 10).split('-').map(Number)
  return dateFormat.format(new Date(year!, month! - 1, day!))
}

/**
 * "2026-11-03T08:12:00" -> "3 Nov 2026, 09:12" in the viewer's time zone.
 * The API stores UTC but SQL Server drops the "Z", so we add it back before parsing.
 */
export function formatDateTime(isoUtc: string): string {
  const hasZone = /(Z|[+-]\d\d:\d\d)$/.test(isoUtc)
  return dateTimeFormat.format(new Date(hasZone ? isoUtc : `${isoUtc}Z`))
}

/** VivaBev's fiscal year runs July to June and is named after the year it ends: Jul 2026 is FY2027. */
export function fiscalYearOf(date = new Date()): number {
  return date.getMonth() >= 6 ? date.getFullYear() + 1 : date.getFullYear()
}
