import { Navigate, Outlet, useLocation } from 'react-router'
import { FullPageError, FullPageSpinner } from '@/components/layout/page-states'
import { useCurrentUser } from './use-auth'

/** Wraps every page that needs a signed-in user. */
export function RequireAuth() {
  const { data: user, isPending, isError, error } = useCurrentUser()
  const location = useLocation()

  if (isPending) return <FullPageSpinner />
  if (isError) return <FullPageError message={error.message} />
  if (!user) return <Navigate to="/login" replace state={{ from: location.pathname }} />
  return <Outlet />
}
