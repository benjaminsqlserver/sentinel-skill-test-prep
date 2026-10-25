import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { catchError, filter, of, switchMap } from 'rxjs';
import { AccountSecurityStatus } from '../../core/models/api-models';
import { Session } from '../../core/session/session';
import { Clock } from '../../core/time/clock';
import { SecurityApi } from '../security/security-api';
import { SecurityBanner } from '../security/security-banner';

/**
 * The landing page. It reuses SecurityBanner with a different reaction to
 * (review): here it navigates, on the security page it scrolls. Same child,
 * different parent, different behaviour, and the child never changed.
 */
@Component({
  selector: 'app-dashboard-page',
  imports: [SecurityBanner],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (session.currentUser(); as user) {
      <h1>Good to see you, {{ user.displayName }}</h1>
      @if (status(); as status) {
        <app-security-banner
          [riskLevel]="status.riskLevel"
          [findingCount]="status.findings.length"
          [assessedAtUtc]="status.assessedAtUtc"
          [now]="clock.now()"
          (review)="openSecurityPage()" />
      }
    } @else {
      <h1>Welcome to Sentinel</h1>
      <p>Choose a user from the switcher in the top right to sign in.</p>
    }
  `,
  styles: `:host { display: grid; gap: 1rem; max-width: 64rem; }`,
})
export class DashboardPage {
  protected readonly session = inject(Session);
  protected readonly clock = inject(Clock);
  private readonly api = inject(SecurityApi);
  private readonly router = inject(Router);

  protected readonly status = signal<AccountSecurityStatus | null>(null);

  constructor() {
    toObservable(this.session.currentUser)
      .pipe(
        filter((user) => user !== null),
        // The banner is a nicety here; if it fails, the dashboard still renders.
        switchMap(() => this.api.getStatus().pipe(catchError(() => of(null)))),
        takeUntilDestroyed(),
      )
      .subscribe((status) => this.status.set(status));
  }

  protected openSecurityPage(): void {
    this.router.navigateByUrl('/account/security');
  }
}
