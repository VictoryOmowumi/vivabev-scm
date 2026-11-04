import { Outlet } from 'react-router'
import { useCurrentUser } from '@/features/auth/use-auth'
import { AppHeader } from './app-header'

/** Header + page. Only rendered inside <RequireAuth>, so the user is always loaded. */
export function AppShell() {
  const { data: user } = useCurrentUser()
  if (!user) return null

  return (
    <div className="mx-auto flex container flex-col gap-7 px-4 pt-6 pb-12 sm:px-6 lg:px-10">
      <AppHeader user={user} />
      <main>
        <Outlet />
      </main>
    </div>
  )
}
