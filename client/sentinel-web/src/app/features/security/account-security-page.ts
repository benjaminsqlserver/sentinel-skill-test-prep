import { ChangeDetectionStrategy, Component, ElementRef, inject, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { BehaviorSubject, Observable, catchError, combineLatest, map, of, startWith, switchMap } from 'rxjs';
import { describeHttpError } from '../../core/http/api-error';
import { AccountSecurityStatus } from '../../core/models/api-models';
import { Session } from '../../core/session/session';
import { Clock } from '../../core/time/clock';
import { FindingList } from './finding-list';
import { SecurityApi } from './security-api';
import { SecurityBanner } from './security-banner';
import { SignInTable } from './sign-in-table';

/**
 * Every state the page can be in. A union instead of three booleans
 * (isLoading, hasError, data?) means "loading AND error" cannot be
 * represented, so the template cannot accidentally render it.
 */
type PageState =
  | { kind: 'signed-out' }
  | { kind: 'loading' }
  | { kind: 'error'; message: string }
  | { kind: 'loaded'; status: AccountSecurityStatus };

/**
 * The account security page. A container: it owns the data and the side
 * effects, and hands slices of the data to three presentational children.
 *
 *   AccountSecurityPage ── status, now ──▶ SecurityBanner ── (review) ──▶ scroll to findings
 *                       ── findings ────▶ FindingList    ── (acknowledge) ──▶ POST, then reload
 *                       ── signIns ─────▶ SignInTable
 *
 * Data goes down through inputs; intentions come up through outputs; only
 * this component talks to the API.
 */
@Component({
  selector: 'app-account-security-page',
  imports: [SecurityBanner, FindingList, SignInTable],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h1>Account security</h1>

    @switch (state().kind) {
      @case ('signed-out') {
        <p>Sign in to see your account's security.</p>
      }
      @case ('loading') {
        <p aria-busy="true">Checking your recent activity…</p>
      }
      @case ('error') {
        <div class="error" role="alert">
          <p>{{ errorMessage() }}</p>
          <button type="button" (click)="reload()">Try again</button>
        </div>
      }
      @case ('loaded') {
        @if (status(); as status) {
          <app-security-banner
            [riskLevel]="status.riskLevel"
            [findingCount]="status.findings.length"
            [assessedAtUtc]="status.assessedAtUtc"
            [now]="clock.now()"
            (review)="scrollToFindings()" />

          @if (actionError()) {
            <p class="error" role="alert">{{ actionError() }}</p>
          }

          <h2 #findingsHeading tabindex="-1">What we noticed</h2>
          <app-finding-list
            [findings]="status.findings"
            [timeZone]="status.timeZoneId"
            [now]="clock.now()"
            [pendingKey]="pendingKey()"
            (acknowledge)="acknowledge($event)" />

          <h2>Recent sign-ins</h2>
          <app-sign-in-table [signIns]="status.recentSignIns" [timeZone]="status.timeZoneId" />
        }
      }
    }
  `,
  styles: `
    :host { display: grid; gap: 1rem; max-width: 64rem; }
    h2 { margin: .5rem 0 0; font-size: 1.1rem; }
    .error { color: var(--danger); }
  `,
})
export class AccountSecurityPage {
  private readonly api = inject(SecurityApi);
  private readonly session = inject(Session);
  protected readonly clock = inject(Clock);

  private readonly findingsHeading = viewChild<ElementRef<HTMLElement>>('findingsHeading');
  private readonly reload$ = new BehaviorSubject<void>(undefined);

  protected readonly state = signal<PageState>({ kind: 'loading' });
  protected readonly pendingKey = signal<string | null>(null);
  protected readonly actionError = signal<string | null>(null);

  constructor() {
    // Reload whenever the signed-in user changes (the demo switcher) or a
    // reload is requested. switchMap cancels a slow request for the
    // previous user, so a late response can never overwrite a newer one.
    combineLatest([toObservable(this.session.currentUser), this.reload$])
      .pipe(
        switchMap(([user]) => (user ? this.load() : of<PageState>({ kind: 'signed-out' }))),
        takeUntilDestroyed(),
      )
      .subscribe((state) => this.state.set(state));
  }

  protected status(): AccountSecurityStatus | null {
    const state = this.state();
    return state.kind === 'loaded' ? state.status : null;
  }

  protected errorMessage(): string {
    const state = this.state();
    return state.kind === 'error' ? state.message : '';
  }

  protected reload(): void {
    this.reload$.next();
  }

  protected acknowledge(findingKey: string): void {
    if (this.pendingKey() !== null) {
      return; // one at a time; the buttons are disabled too, this is belt and braces
    }

    this.pendingKey.set(findingKey);
    this.actionError.set(null);

    this.api.acknowledge(findingKey).subscribe({
      next: () => {
        this.pendingKey.set(null);
        // Reload rather than removing the card locally: the risk level and
        // the flagged rows depend on every finding, and only the server
        // knows the new answer.
        this.reload();
      },
      error: (error: unknown) => {
        this.pendingKey.set(null);
        this.actionError.set(describeHttpError(error));
      },
    });
  }

  protected scrollToFindings(): void {
    const heading = this.findingsHeading()?.nativeElement;
    heading?.scrollIntoView({ behavior: 'smooth', block: 'start' });
    heading?.focus({ preventScroll: true }); // keyboard and screen-reader users land there too
  }

  private load(): Observable<PageState> {
    return this.api.getStatus().pipe(
      map((status): PageState => ({ kind: 'loaded', status })),
      // Keep showing the old data while refreshing after an acknowledge,
      // instead of flashing "loading" over a page the user is reading.
      startWith<PageState>(this.state().kind === 'loaded' ? this.state() : { kind: 'loading' }),
      catchError((error: unknown) => of<PageState>({ kind: 'error', message: describeHttpError(error) })),
    );
  }
}
