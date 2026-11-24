// Small helpers shared by the dashboards.

/** Short money for the hero: ₦15.75m */
export function shortNaira(n: number) {
  const abs = Math.abs(n)
  const sign = n < 0 ? '-' : ''
  if (abs >= 1_000_000_000) return `${sign}₦${+(abs / 1_000_000_000).toFixed(2)}bn`
  if (abs >= 1_000_000) return `${sign}₦${+(abs / 1_000_000).toFixed(2)}m`
  if (abs >= 1_000) return `${sign}₦${+(abs / 1_000).toFixed(0)}k`
  return `${sign}₦${abs}`
}

export function daysFromToday(isoDate: string, today: string) {
  return Math.round((new Date(`${isoDate}T00:00:00`).getTime() - new Date(`${today}T00:00:00`).getTime()) / 86_400_000)
}
