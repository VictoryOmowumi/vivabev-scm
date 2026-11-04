import { createBrowserRouter } from 'react-router'
import { AppShell } from '@/components/layout/app-shell'
import { ComingSoonPage } from '@/components/layout/coming-soon-page'
import { ApprovalsPage } from '@/features/approvals/approvals-page'
import { LoginPage } from '@/features/auth/login-page'
import { RequireAuth } from '@/features/auth/require-auth'
import { DashboardPage } from '@/features/dashboard/dashboard-page'
import { RequisitionDetailPage } from '@/features/requisitions/requisition-detail-page'
import { RequisitionsPage } from '@/features/requisitions/requisitions-page'

export const router = createBrowserRouter([
  { path: '/login', element: <LoginPage /> },
  {
    element: <RequireAuth />, // everything below needs a signed-in user
    children: [
      {
        element: <AppShell />, // header + page
        children: [
          { index: true, element: <DashboardPage /> },
          { path: 'requisitions', element: <RequisitionsPage /> },
          { path: 'requisitions/new', element: <ComingSoonPage title="New requisition" /> },
          { path: 'requisitions/:id', element: <RequisitionDetailPage /> },
          { path: 'approvals', element: <ApprovalsPage /> },
          { path: 'suppliers', element: <ComingSoonPage title="Suppliers" /> },
          { path: 'catalogue', element: <ComingSoonPage title="Catalogue" /> },
          { path: 'budgets', element: <ComingSoonPage title="Budgets" /> },
          { path: 'copilot', element: <ComingSoonPage title="Copilot" /> },
          { path: '*', element: <ComingSoonPage title="Page not found" /> },
        ],
      },
    ],
  },
])
