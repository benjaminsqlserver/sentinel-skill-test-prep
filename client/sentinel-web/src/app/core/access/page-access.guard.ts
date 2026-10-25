import { HttpClient } from '@angular/common/http';
import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { Observable, catchError, map, of } from 'rxjs';
import { PageAccess } from '../models/api-models';

/**
 * Asks the API whether the signed-in user may open the page they are
 * navigating to, and sends them to /forbidden (with the reason) if not.
 *
 * Why ask the server on every navigation instead of checking the menu we
 * already have? Because the menu is only a *view* of the rules. A typed URL,
 * a bookmark or a stale tab must be judged by the rules themselves, and
 * the rules live in one place: PageAccessEvaluator, on the server.
 *
 * The guard protects the user experience, not the data. The API checks
 * permissions again on every data request, so a user who bypasses the
 * guard gets a blank page, never someone else's data.
 */
export const pageAccessGuard: CanActivateFn = (_route, state) => {
  const http = inject(HttpClient);
  const router = inject(Router);

  // The router's URL includes query string and fragment; pages are judged by path.
  const path = pathOnly(state.url);

  return http.get<PageAccess>('/api/me/page-access', { params: { path } }).pipe(
    map((verdict) =>
      verdict.isAllowed
        ? true
        : router.createUrlTree(['/forbidden'], { queryParams: { page: verdict.path, reason: verdict.reason } }),
    ),
    catchError((): Observable<boolean | ReturnType<Router['createUrlTree']>> =>
      // 404 (no such page) or the API is down. Fail closed: an unknown
      // page is not a reason to let someone in.
      of(router.createUrlTree(['/forbidden'], { queryParams: { page: path, reason: 'Unknown' } })),
    ),
  );
};

/** "/reports/finance?tab=2#top" -> "/reports/finance" */
export function pathOnly(url: string): string {
  const end = url.search(/[?#]/);
  const path = end === -1 ? url : url.slice(0, end);
  return path === '' ? '/' : path;
}
