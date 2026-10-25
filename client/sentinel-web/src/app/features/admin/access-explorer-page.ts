import { ChangeDetectionStrategy, Component, computed, effect, inject, input, signal } from '@angular/core';
import { Router } from '@angular/router';
import { forkJoin } from 'rxjs';
import { explainAccess } from '../../core/access/access-reasons';
import { describeHttpError } from '../../core/http/api-error';
import { AccountSecurityStatus, PageAccess, UserSummary } from '../../core/models/api-models';
import { Clock } from '../../core/time/clock';
import { SecurityBanner } from '../security/security-banner';
import { AdminApi } from './admin-api';

/**
 * "Why can't Ada see the finance report?" answered on one screen: every
 * page, the verdict for one user, and the reason in words. Support staff
 * use this instead of reading role definitions and doing the logic in
 * their heads, which is how wrong answers reach customers.
 */
@Component({
  selector: 'app-access-explorer-page',
  imports: [SecurityBanner],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h1>Access explorer</h1>

    <label class="picker">
      User
      <select [value]="userId() ?? ''" (change)="choose($any($event.target).value)">
        <option value="">Choose a user…</option>
        @for (user of users(); track user.id) {
          <option [value]="user.id" [selected]="user.id === userId()">{{ user.displayName }} ({{ user.roles.join(', ') || 'no roles' }})</option>
        }
      </select>
    </label>

    @if (error()) {
      <p class="error" role="alert">{{ error() }}</p>
    }

    @if (security(); as status) {
      <app-security-banner
        [riskLevel]="status.riskLevel"
        [findingCount]="0"
        [assessedAtUtc]="status.assessedAtUtc"
        [now]="clock.now()" />
    }

    @if (rows().length > 0) {
      <p class="summary">{{ allowedCount() }} of {{ rows().length }} pages allowed.</p>
      <table>
        <thead><tr><th>Page</th><th>Requires</th><th>Verdict</th><th>Why</th></tr></thead>
        <tbody>
          @for (row of rows(); track row.path) {
            <tr [class.denied]="!row.isAllowed">
              <td [style.padding-left.rem]="0.6 + depth(row.path) * 1.1"><code>{{ row.path }}</code> {{ row.title }}</td>
              <td>@if (row.requiredPermission) { <code>{{ row.requiredPermission }}</code> } @else { <span class="muted">—</span> }</td>
              <td>{{ row.isAllowed ? 'Allowed' : 'Denied' }}</td>
              <td><span class="reason">{{ row.reason }}</span> {{ explain(row.reason) }}</td>
            </tr>
          }
        </tbody>
      </table>
    }
  `,
  styles: `
    :host { display: grid; gap: 1rem; }
    .picker { display: flex; gap: .5rem; align-items: center; }
    table { width: 100%; border-collapse: collapse; background: var(--surface); }
    th, td { text-align: left; padding: .4rem .6rem; border-bottom: 1px solid var(--border); font-size: .9rem; }
    tr.denied td { color: var(--muted); }
    .reason { font-family: var(--mono); font-size: .75rem; padding: .05rem .3rem; border-radius: 3px; background: var(--hover); }
    .muted { color: var(--muted); }
    .error { color: var(--danger); }
    .summary { margin: 0; }
  `,
})
export class AccessExplorerPage {
  private readonly api = inject(AdminApi);
  private readonly router = inject(Router);
  protected readonly clock = inject(Clock);

  /** From the ?userId= query parameter, so a link from the users page lands here pre-selected. */
  readonly userId = input<string>();

  protected readonly users = signal<UserSummary[]>([]);
  protected readonly rows = signal<PageAccess[]>([]);
  protected readonly security = signal<AccountSecurityStatus | null>(null);
  protected readonly error = signal<string | null>(null);
  protected readonly allowedCount = computed(() => this.rows().filter((r) => r.isAllowed).length);
  protected readonly explain = explainAccess;

  constructor() {
    this.api.getUsers('', 1, 100).subscribe({
      next: (page) => this.users.set(page.items),
      error: (error: unknown) => this.error.set(describeHttpError(error)),
    });

    // Runs whenever the userId input changes, including the first time.
    effect(() => {
      const id = this.userId();
      this.rows.set([]);
      this.security.set(null);
      if (id) {
        this.load(id);
      }
    });
  }

  protected choose(userId: string): void {
    // Put the choice in the URL, so the screen can be bookmarked and shared
    // with a colleague in a ticket. The input above picks it up from there.
    this.router.navigate([], { queryParams: { userId: userId || null } });
  }

  /** "/reports/finance" -> 2: used to indent the table into a tree. */
  protected depth(path: string): number {
    return path === '/' ? 0 : path.split('/').length - 1;
  }

  private load(userId: string): void {
    this.error.set(null);
    forkJoin({ access: this.api.getPageAccess(userId), security: this.api.getUserSecurity(userId) }).subscribe({
      next: ({ access, security }) => {
        this.rows.set(access);
        this.security.set(security);
      },
      error: (error: unknown) => this.error.set(describeHttpError(error)),
    });
  }
}
