import { HugeiconsIcon } from '@hugeicons/react'
import { Logout03Icon } from '@hugeicons/core-free-icons'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import type { CurrentUser } from '@/features/auth/auth-api'
import { useLogout } from '@/features/auth/use-auth'
import { initials } from '@/lib/format'

export function UserMenu({ user }: { user: CurrentUser }) {
  const logout = useLogout()

  return (
    <DropdownMenu>
      <DropdownMenuTrigger
        aria-label={`Account: ${user.displayName}`}
        className="size-11 cursor-pointer rounded-full bg-inverse text-sm font-bold text-inverse-foreground"
      >
        {initials(user.displayName)}
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="w-60">
        <DropdownMenuLabel className="flex flex-col">
          <span>{user.displayName}</span>
          <span className="text-xs font-normal text-muted-foreground">{user.email}</span>
          <span className="text-xs font-normal text-muted-foreground">
            {user.departmentCode} · {user.siteCode}
          </span>
        </DropdownMenuLabel>
        <DropdownMenuSeparator />
        <DropdownMenuItem onSelect={() => logout.mutate()}>
          <HugeiconsIcon icon={Logout03Icon} size={16} />
          Sign out
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  )
}
