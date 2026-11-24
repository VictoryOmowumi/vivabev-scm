import { useCurrentUser } from '@/features/auth/use-auth'
import { can, Permission } from '@/lib/permissions'
import { ProcurementDashboard } from './procurement-dashboard'
import { TeamDashboard } from './team-dashboard'
import { WarehouseDashboard } from './warehouse-dashboard'

/** Same layout for everyone (the Main design board); what fills it depends on the job. */
export function DashboardPage() {
  const { data: user } = useCurrentUser()
  if (!user) return null
  if (can(user, Permission.SourcingManage)) return <ProcurementDashboard user={user} />
  if (can(user, Permission.InventoryReceive)) return <WarehouseDashboard user={user} />
  return <TeamDashboard user={user} />
}
