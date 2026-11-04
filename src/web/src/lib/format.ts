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
