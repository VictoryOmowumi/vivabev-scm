import { createBrowserRouter } from 'react-router'
import { AppShell } from '@/components/layout/app-shell'
import { ComingSoonPage } from '@/components/layout/coming-soon-page'
import { ApprovalsPage } from '@/features/approvals/approvals-page'
import { LoginPage } from '@/features/auth/login-page'
import { RequireAuth } from '@/features/auth/require-auth'
import { BudgetDetailPage } from '@/features/budgets/budget-detail-page'
import { BudgetsPage } from '@/features/budgets/budgets-page'
import { DashboardPage } from '@/features/dashboard/dashboard-page'
import { RequisitionDetailPage } from '@/features/requisitions/requisition-detail-page'
import { RequisitionEditorPage } from '@/features/requisitions/requisition-editor-page'
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
          { path: 'requisitions/new', element: <RequisitionEditorPage /> },
          { path: 'requisitions/:id/edit', element: <RequisitionEditorPage /> },
          { path: 'requisitions/:id', element: <RequisitionDetailPage /> },
          { path: 'approvals', element: <ApprovalsPage /> },
          { path: 'suppliers', element: <ComingSoonPage title="Suppliers" /> },
          { path: 'catalogue', element: <ComingSoonPage title="Catalogue" /> },
          { path: 'budgets', element: <BudgetsPage /> },
          { path: 'budgets/:id', element: <BudgetDetailPage /> },
          { path: 'copilot', element: <ComingSoonPage title="Copilot" /> },
          { path: '*', element: <ComingSoonPage title="Page not found" /> },
        ],
      },
    ],
  },
])
