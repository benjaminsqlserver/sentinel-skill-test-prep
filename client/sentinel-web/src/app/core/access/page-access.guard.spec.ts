import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, Router, RouterStateSnapshot, UrlTree, provideRouter } from '@angular/router';
import { Observable, firstValueFrom } from 'rxjs';
import { PageAccess } from '../models/api-models';
import { pageAccessGuard, pathOnly } from './page-access.guard';

describe('pageAccessGuard', () => {
  let http: HttpTestingController;
  let router: Router;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
  });

  afterEach(() => http.verify());

  function runGuard(url: string): Promise<boolean | UrlTree> {
    const result = TestBed.runInInjectionContext(() =>
      pageAccessGuard({} as ActivatedRouteSnapshot, { url } as RouterStateSnapshot),
    ) as Observable<boolean | UrlTree>;
    return firstValueFrom(result);
  }

  function verdict(overrides: Partial<PageAccess>): PageAccess {
    return { path: '/reports', title: 'Reports', parentPath: '/', requiredPermission: 'reports.view', isAllowed: true, reason: 'PermissionGranted', ...overrides };
  }

  it('lets the navigation through when the server allows it', async () => {
    const result = runGuard('/reports');

    http.expectOne((r) => r.url === '/api/me/page-access' && r.params.get('path') === '/reports').flush(verdict({}));

    expect(await result).toBe(true);
  });

  it('redirects to /forbidden with the reason when the server denies it', async () => {
    const result = runGuard('/admin/users');

    http.expectOne((r) => r.url === '/api/me/page-access').flush(verdict({ path: '/admin/users', isAllowed: false, reason: 'ParentDenied' }));

    expect(router.serializeUrl((await result) as UrlTree)).toBe('/forbidden?page=%2Fadmin%2Fusers&reason=ParentDenied');
  });

  it('fails closed when the page is unknown or the API is down', async () => {
    const result = runGuard('/nowhere');

    http.expectOne((r) => r.url === '/api/me/page-access').flush({ title: 'Page.NotFound' }, { status: 404, statusText: 'Not Found' });

    expect(router.serializeUrl((await result) as UrlTree)).toBe('/forbidden?page=%2Fnowhere&reason=Unknown');
  });

  it('judges the path, not the query string', async () => {
    const result = runGuard('/reports?tab=2#top');

    http.expectOne((r) => r.params.get('path') === '/reports').flush(verdict({}));

    expect(await result).toBe(true);
  });
});

describe('pathOnly', () => {
  it.each([
    ['/reports/finance?tab=2', '/reports/finance'],
    ['/reports#top', '/reports'],
    ['/', '/'],
    ['', '/'],
  ])('%s -> %s', (url, expected) => {
    expect(pathOnly(url)).toBe(expected);
  });
});
