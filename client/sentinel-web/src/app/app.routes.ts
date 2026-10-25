import { Route, Routes } from '@angular/router';
import { pageAccessGuard } from './core/access/page-access.guard';
import { DashboardPage } from './features/pages/dashboard-page';
import { ForbiddenPage } from './features/pages/forbidden-page';
import { PlaceholderPage } from './features/pages/placeholder-page';
import { AccountSecurityPage } from './features/security/account-security-page';

/**
 * Every route here has a page with the same path in the server's page tree,
 * and the guard asks the server about that path. Adding a route without
 * adding the page sends users to /forbidden ("Unknown"), which is the safe
 * failure: a page nobody configured is a page nobody may see.
 */
function guarded(path: string, route: Omit<Route, 'path'>): Route {
  return { path, canActivate: [pageAccessGuard], runGuardsAndResolvers: 'always', ...route };
}

function placeholder(path: string, title: string): Route {
  return guarded(path, { component: PlaceholderPage, data: { title } });
}

export const routes: Routes = [
  guarded('', { component: DashboardPage, pathMatch: 'full' }),
  placeholder('account', 'My account'),
  guarded('account/security', { component: AccountSecurityPage }),

  placeholder('reports', 'Reports'),
  placeholder('reports/finance', 'Finance'),
  placeholder('reports/finance/export', 'Ledger export'),
  placeholder('reports/operations', 'Operations'),
  placeholder('billing', 'Billing'),
  placeholder('support', 'Support desk'),

  // Admin screens are lazy: users who cannot open them never download them.
  placeholder('admin', 'Administration'),
  guarded('admin/users', { loadComponent: () => import('./features/admin/users-page').then((m) => m.UsersPage) }),
  guarded('admin/roles', { loadComponent: () => import('./features/admin/roles-page').then((m) => m.RolesPage) }),
  guarded('admin/access-explorer', {
    loadComponent: () => import('./features/admin/access-explorer-page').then((m) => m.AccessExplorerPage),
  }),

  placeholder('help', 'Help centre'),
  placeholder('status', 'System status'),

  { path: 'forbidden', component: ForbiddenPage },
  { path: '**', component: ForbiddenPage, data: { reason: 'Unknown' } },
];
