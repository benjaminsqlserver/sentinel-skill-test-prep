import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { CurrentUser } from '../../core/models/api-models';
import { Session } from '../../core/session/session';
import { Clock } from '../../core/time/clock';
import { AccountSecurityPage } from './account-security-page';
import { NOW, aFinding, aStatus } from './test-data';

/**
 * The container, tested with its real children and a fake back end. These
 * tests read like the user's experience: the page loads, shows a verdict,
 * and reacts to a click. HttpTestingController lets each test decide what
 * the server answers, including failures that are hard to cause for real.
 */
describe('AccountSecurityPage', () => {
  let fixture: ComponentFixture<AccountSecurityPage>;
  let http: HttpTestingController;
  let element: HTMLElement;
  const currentUser = signal<CurrentUser | null>(null);

  const ada: CurrentUser = { id: 'u1', email: 'ada@sentinel.dev', displayName: 'Ada', timeZoneId: 'Africa/Lagos', permissions: [] };

  beforeEach(() => {
    currentUser.set(ada);
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        // Only the parts of Session the page uses. A fake this small is a
        // sign the page depends on very little, which is the point.
        { provide: Session, useValue: { currentUser: currentUser.asReadonly() } },
        { provide: Clock, useValue: { now: signal(NOW).asReadonly() } },
      ],
    });

    http = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(AccountSecurityPage);
    element = fixture.nativeElement as HTMLElement;
  });

  afterEach(() => http.verify()); // no request left unanswered or unexpected

  async function settle(): Promise<void> {
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  }

  it('shows loading, then the verdict, the findings and the sign-ins', async () => {
    await settle();
    expect(element.textContent).toContain('Checking your recent activity');

    http.expectOne('/api/me/security').flush(aStatus({ riskLevel: 'High' }));
    await settle();

    expect(element.querySelector('app-security-banner h2')?.textContent).toContain('Someone may be using your account');
    expect(element.querySelectorAll('app-finding-list article')).toHaveLength(1);
    expect(element.querySelectorAll('app-sign-in-table tbody tr.flagged')).toHaveLength(1);
  });

  it('acknowledges a finding, then reloads the verdict from the server', async () => {
    await settle();
    http.expectOne('/api/me/security').flush(aStatus({ findings: [aFinding({ key: 'new-country:abc' })] }));
    await settle();

    element.querySelector<HTMLButtonElement>('app-finding-list button')!.click();
    await settle();

    const post = http.expectOne('/api/me/security/findings/new-country%3Aabc/acknowledge');
    expect(post.request.method).toBe('POST');
    post.flush(null);
    await settle();

    http.expectOne('/api/me/security').flush(aStatus({ riskLevel: 'None', findings: [] }));
    await settle();

    expect(element.querySelector('app-security-banner h2')?.textContent).toContain('No suspicious activity');
    expect(element.textContent).toContain('Nothing to review.');
  });

  it('keeps the page and shows the reason when an acknowledgement fails', async () => {
    await settle();
    http.expectOne('/api/me/security').flush(aStatus());
    await settle();

    element.querySelector<HTMLButtonElement>('app-finding-list button')!.click();
    await settle();
    http.expectOne((r) => r.url.endsWith('/acknowledge')).flush(
      { title: 'Finding.NotFound', detail: 'That finding does not exist or no longer applies.' },
      { status: 404, statusText: 'Not Found' },
    );
    await settle();

    expect(element.querySelector('[role="alert"]')?.textContent).toContain('no longer applies');
    expect(element.querySelectorAll('app-finding-list article')).toHaveLength(1);
    expect(element.querySelector<HTMLButtonElement>('app-finding-list button')!.disabled).toBe(false);
  });

  it('offers a retry when the API cannot be reached', async () => {
    await settle();
    http.expectOne('/api/me/security').error(new ProgressEvent('error'), { status: 0 });
    await settle();

    expect(element.querySelector('[role="alert"]')?.textContent).toContain('Cannot reach the Sentinel API');

    element.querySelector<HTMLButtonElement>('[role="alert"] button')!.click();
    await settle();
    http.expectOne('/api/me/security').flush(aStatus({ riskLevel: 'None', findings: [] }));
    await settle();

    expect(element.querySelector('app-security-banner')).not.toBeNull();
  });

  it('reloads for the new user when the signed-in user changes, and asks nothing when signed out', async () => {
    await settle();
    http.expectOne('/api/me/security').flush(aStatus());
    await settle();

    currentUser.set(null);
    await settle();

    http.expectNone('/api/me/security');
    expect(element.textContent).toContain("Sign in to see your account's security.");
  });
});
