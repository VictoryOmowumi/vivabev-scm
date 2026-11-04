import { Link, NavLink } from 'react-router'
import { HugeiconsIcon } from '@hugeicons/react'
import { DropletIcon, SparklesIcon } from '@hugeicons/core-free-icons'
import type { CurrentUser } from '@/features/auth/auth-api'
import { cn } from '@/lib/utils'
import { ThemeToggle } from './theme-toggle'
import { UserMenu } from './user-menu'

const navItems = [
  { to: '/', label: 'Dashboard' },
  { to: '/requisitions', label: 'Requisitions' },
  { to: '/approvals', label: 'Approvals' },
  { to: '/suppliers', label: 'Suppliers' },
  { to: '/catalogue', label: 'Catalogue' },
  { to: '/budgets', label: 'Budgets' },
]

export function AppHeader({ user }: { user: CurrentUser }) {
  return (
    <header className="flex flex-wrap items-center justify-between gap-4">
      <Link
        to="/"
        className="flex items-center gap-2.5 rounded-full border-[1.5px] border-foreground px-5 py-2 font-heading text-[22px]"
      >
        <HugeiconsIcon icon={DropletIcon} size={20} strokeWidth={1.5} className="text-brand" />
        VivaBev
      </Link>

      <nav aria-label="Main" className="flex flex-wrap gap-0.5 rounded-full border border-white/90 bg-card p-1.5 backdrop-blur-sm dark:border-white/10">
        {navItems.map((item) => (
          <NavLink
            key={item.to}
            to={item.to}
            end={item.to === '/'}
            className={({ isActive }) =>
              cn(
                'rounded-full px-4 py-2.5 text-sm font-medium transition-colors',
                isActive ? 'bg-inverse font-semibold text-inverse-foreground' : 'hover:bg-accent',
              )
            }
          >
            {item.label}
          </NavLink>
        ))}
      </nav>

      <div className="flex items-center gap-2">
        <NavLink
          to="/copilot"
          className="flex h-11 items-center gap-2 rounded-full bg-brand px-4 text-sm font-semibold text-brand-foreground"
        >
          <HugeiconsIcon icon={SparklesIcon} size={18} strokeWidth={1.5} />
          Ask Copilot
        </NavLink>
        <ThemeToggle />
        <UserMenu user={user} />
      </div>
    </header>
  )
}
