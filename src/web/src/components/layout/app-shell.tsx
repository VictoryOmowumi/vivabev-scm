import { Outlet } from 'react-router'
import { useCurrentUser } from '@/features/auth/use-auth'
import { AppHeader } from './app-header'

/** Header + page. Only rendered inside <RequireAuth>, so the user is always loaded. */
export function AppShell() {
  const { data: user } = useCurrentUser()
  if (!user) return null

  return (
    <div className="mx-auto flex w-full max-w-[1440px] flex-col gap-7 px-[clamp(16px,3vw,40px)] pt-6 pb-12">
      <AppHeader user={user} />
      <main>
        <Outlet />
      </main>
    </div>
  )
}
