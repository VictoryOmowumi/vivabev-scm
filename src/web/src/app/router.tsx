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
import { PurchaseOrderPage } from '@/features/purchase-orders/purchase-order-page'
import { PurchaseOrdersPage } from '@/features/purchase-orders/purchase-orders-page'
import { NewRfqPage } from '@/features/sourcing/new-rfq-page'
import { RfqPage } from '@/features/sourcing/rfq-page'
import { SourcingPage } from '@/features/sourcing/sourcing-page'

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
          { path: 'sourcing', element: <SourcingPage /> },
          { path: 'sourcing/new', element: <NewRfqPage /> },
          { path: 'sourcing/:id', element: <RfqPage /> },
          { path: 'purchase-orders', element: <PurchaseOrdersPage /> },
          { path: 'purchase-orders/:id', element: <PurchaseOrderPage /> },
          { path: 'copilot', element: <ComingSoonPage title="Copilot" /> },
          { path: '*', element: <ComingSoonPage title="Page not found" /> },
        ],
      },
    ],
  },
])
