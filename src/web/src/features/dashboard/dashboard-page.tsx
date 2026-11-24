import { useCurrentUser } from '@/features/auth/use-auth'
import { can, Permission } from '@/lib/permissions'
import { ProcurementDashboard } from './procurement-dashboard'
import { TeamDashboard } from './team-dashboard'

/** Same layout for everyone (the Main design board); what fills it depends on the job. */
export function DashboardPage() {
  const { data: user } = useCurrentUser()
  if (!user) return null
  return can(user, Permission.SourcingManage) ? <ProcurementDashboard user={user} /> : <TeamDashboard user={user} />
}
