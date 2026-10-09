import {
  createRouter,
  createRoute,
  createRootRoute,
  Navigate,
} from '@tanstack/react-router';
import { Shell } from '../../workspaces/Shell';
import { AccountsView } from '../../features/customers/AccountsView';
import { SitesView } from '../../features/sites/SitesView';
import { Card } from '../../shared/design-system/Card';

const rootRoute = createRootRoute({
  component: Shell,
});

const indexRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/',
  component: () => <Navigate to="/customers" />,
});

const customersRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/customers',
  component: AccountsView,
});

const sitesRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/sites',
  component: SitesView,
});

const WorkOrdersView = () => (
  <Card title="Work Order Operations Spine">
    <div style={{ padding: '20px 0', color: 'var(--text-secondary)' }}>
      <p style={{ marginBottom: '12px' }}>
        Work Order aggregate root is tied directly to Customer Accounts and Operational Sites configured in the previous tabs.
      </p>
      <div style={{ padding: '16px', backgroundColor: 'var(--bg-subtle)', borderRadius: 'var(--radius-md)', border: '1px solid var(--border-subtle)' }}>
        <strong>Next Batch:</strong> Work Order lifecycle (Draft → Approved → Scheduled → InProgress → OperationallyComplete), service scope items, and dispatch commitment.
      </div>
    </div>
  </Card>
);

const DispatchView = () => (
  <Card title="Dispatch & Scheduling Grid">
    <div style={{ padding: '20px 0', color: 'var(--text-secondary)' }}>
      <p style={{ marginBottom: '12px' }}>
        Resource scheduling concurrency guard, booking windows, and technician assignments.
      </p>
      <div style={{ padding: '16px', backgroundColor: 'var(--bg-subtle)', borderRadius: 'var(--radius-md)', border: '1px solid var(--border-subtle)' }}>
        <strong>Next Batch:</strong> Resource scheduling, territory filtering, and shift commitment.
      </div>
    </div>
  </Card>
);

const workOrdersRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/work-orders',
  component: WorkOrdersView,
});

const dispatchRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/dispatch',
  component: DispatchView,
});

const routeTree = rootRoute.addChildren([
  indexRoute,
  customersRoute,
  sitesRoute,
  workOrdersRoute,
  dispatchRoute,
]);

export const router = createRouter({
  routeTree,
  defaultPreload: 'intent',
});

declare module '@tanstack/react-router' {
  interface Register {
    router: typeof router;
  }
}
