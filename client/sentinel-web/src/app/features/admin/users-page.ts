import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { Subject, catchError, debounceTime, distinctUntilChanged, map, of, startWith, switchMap } from 'rxjs';
import { describeHttpError } from '../../core/http/api-error';
import { UserSummary } from '../../core/models/api-models';
import { AdminApi } from './admin-api';

/**
 * The user register with search-as-you-type. The RxJS pipeline is the
 * classic one and worth knowing by heart:
 *
 *   keystrokes -> debounceTime (wait for a pause)
 *              -> distinctUntilChanged (ignore "ab" -> "abc" -> "ab")
 *              -> switchMap (cancel the previous search when a new one starts)
 */
@Component({
  selector: 'app-users-page',
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h1>Users</h1>
    <label class="search">
      Search
      <input type="search" placeholder="Name, or a full email address" (input)="search$.next($any($event.target).value)" />
    </label>

    @if (error()) {
      <p class="error" role="alert">{{ error() }}</p>
    } @else {
      <table>
        <thead><tr><th>Name</th><th>Email</th><th>Roles</th><th>Status</th><th></th></tr></thead>
        <tbody>
          @for (user of users(); track user.id) {
            <tr [class.suspended]="user.status === 'Suspended'">
              <td>{{ user.displayName }}</td>
              <td>{{ user.email }}</td>
              <td>{{ user.roles.join(', ') || '—' }}</td>
              <td>{{ user.status }}</td>
              <td><a routerLink="/admin/access-explorer" [queryParams]="{ userId: user.id }">Explain access</a></td>
            </tr>
          } @empty {
            <tr><td colspan="5" class="muted">No users match.</td></tr>
          }
        </tbody>
      </table>
      <p class="muted">{{ total() }} {{ total() === 1 ? 'user' : 'users' }}</p>
    }
  `,
  styles: `
    .search { display: flex; gap: .5rem; align-items: center; margin-bottom: .75rem; }
    .search input { width: 20rem; }
    table { width: 100%; border-collapse: collapse; background: var(--surface); }
    th, td { text-align: left; padding: .45rem .6rem; border-bottom: 1px solid var(--border); }
    tr.suspended td { color: var(--muted); }
    .muted { color: var(--muted); }
    .error { color: var(--danger); }
  `,
})
export class UsersPage {
  private readonly api = inject(AdminApi);

  protected readonly search$ = new Subject<string>();
  protected readonly users = signal<UserSummary[]>([]);
  protected readonly total = signal(0);
  protected readonly error = signal<string | null>(null);

  constructor() {
    this.search$
      .pipe(
        debounceTime(250),
        map((term) => term.trim()),
        distinctUntilChanged(),
        startWith(''),
        switchMap((term) =>
          this.api.getUsers(term).pipe(
            map((page) => ({ page, error: null as string | null })),
            catchError((error: unknown) => of({ page: null, error: describeHttpError(error) })),
          ),
        ),
        takeUntilDestroyed(),
      )
      .subscribe(({ page, error }) => {
        this.error.set(error);
        this.users.set(page?.items ?? []);
        this.total.set(page?.totalCount ?? 0);
      });
  }
}
