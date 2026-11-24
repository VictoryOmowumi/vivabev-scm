import { Outlet } from 'react-router'
import { useCurrentUser } from '@/features/auth/use-auth'
import { AppHeader } from './app-header'

/** Header + page. Only rendered inside <RequireAuth>, so the user is always loaded. */
export function AppShell() {
  const { data: user } = useCurrentUser()
  if (!user) return null

  return (
    <div className="mx-auto flex w-full max-w-container flex-col gap-10 px-[clamp(16px,3vw,40px)] pt-4 pb-12 print:max-w-none print:p-0">
      <div className="print:hidden"><AppHeader user={user} /></div>
      <main>
        <Outlet />
      </main>
    </div>
  )
}
