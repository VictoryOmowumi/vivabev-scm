import { createBrowserRouter } from 'react-router'
import { AppShell } from '@/components/layout/app-shell'
import { ComingSoonPage } from '@/components/layout/coming-soon-page'
import { LoginPage } from '@/features/auth/login-page'
import { RequireAuth } from '@/features/auth/require-auth'
import { DashboardPage } from '@/features/dashboard/dashboard-page'

export const router = createBrowserRouter([
  { path: '/login', element: <LoginPage /> },
  {
    element: <RequireAuth />, // everything below needs a signed-in user
    children: [
      {
        element: <AppShell />, // header + page
        children: [
          { index: true, element: <DashboardPage /> },
          { path: 'requisitions', element: <ComingSoonPage title="Requisitions" /> },
          { path: 'approvals', element: <ComingSoonPage title="Approvals" /> },
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
