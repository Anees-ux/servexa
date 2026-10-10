import {
  createRouter,
  createRoute,
  createRootRoute,
  Navigate,
} from '@tanstack/react-router';
import { Shell } from '../../workspaces/Shell';
import { AccountsView } from '../../features/customers/AccountsView';
import { SitesView } from '../../features/sites/SitesView';
import { AssetsView } from '../../features/assets/AssetsView';
import { WorkOrdersView } from '../../features/work-orders/WorkOrdersView';
import { SchedulingView } from '../../features/scheduling/SchedulingView';
import { DispatchView } from '../../features/dispatch/DispatchView';
import { TechnicianView } from '../../features/technician/TechnicianView';

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

const assetsRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/assets',
  component: AssetsView,
});

const workOrdersRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/work-orders',
  component: WorkOrdersView,
});

const schedulingRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/scheduling',
  component: SchedulingView,
});

const dispatchRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/dispatch',
  component: DispatchView,
});

const technicianRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/technician',
  component: TechnicianView,
});

const routeTree = rootRoute.addChildren([
  indexRoute,
  customersRoute,
  sitesRoute,
  assetsRoute,
  workOrdersRoute,
  schedulingRoute,
  dispatchRoute,
  technicianRoute,
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
