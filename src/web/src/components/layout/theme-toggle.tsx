import { HugeiconsIcon } from '@hugeicons/react'
import { Moon02Icon, Sun03Icon } from '@hugeicons/core-free-icons'
import { Button } from '@/components/ui/button'
import { useTheme } from '@/theme/theme-context'

export function ThemeToggle() {
  const { resolvedTheme, setTheme } = useTheme()
  const isDark = resolvedTheme === 'dark'

  return (
    <Button
      variant="outline"
      size="icon"
      className="size-11 rounded-full bg-card"
      aria-label={isDark ? 'Switch to light mode' : 'Switch to dark mode'}
      onClick={() => setTheme(isDark ? 'light' : 'dark')}
    >
      <HugeiconsIcon icon={isDark ? Sun03Icon : Moon02Icon} size={20} strokeWidth={1.5} />
    </Button>
  )
}
