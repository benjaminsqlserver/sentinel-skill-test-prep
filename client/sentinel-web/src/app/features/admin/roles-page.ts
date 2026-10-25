import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { describeHttpError } from '../../core/http/api-error';
import { Role } from '../../core/models/api-models';
import { AdminApi } from './admin-api';

@Component({
  selector: 'app-roles-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h1>Roles</h1>
    @if (error()) {
      <p class="error" role="alert">{{ error() }}</p>
    }
    <table>
      <thead><tr><th>Role</th><th>Grants</th><th>Users</th></tr></thead>
      <tbody>
        @for (role of roles(); track role.id) {
          <tr>
            <td><strong>{{ role.name }}</strong><br /><span class="muted">{{ role.description }}</span></td>
            <td>
              @for (permission of role.permissions; track permission) {
                <code class="permission" [class.wildcard]="permission.endsWith('*')">{{ permission }}</code>
              }
            </td>
            <td>{{ role.userCount }}</td>
          </tr>
        }
      </tbody>
    </table>
  `,
  styles: `
    table { width: 100%; border-collapse: collapse; background: var(--surface); }
    th, td { text-align: left; padding: .5rem .6rem; border-bottom: 1px solid var(--border); vertical-align: top; }
    .permission { display: inline-block; margin: 0 .3rem .3rem 0; padding: .05rem .35rem; border-radius: 3px; background: var(--hover); }
    .permission.wildcard { background: var(--warning-bg); }
    .muted { color: var(--muted); font-size: .85rem; }
    .error { color: var(--danger); }
  `,
})
export class RolesPage {
  protected readonly roles = signal<Role[]>([]);
  protected readonly error = signal<string | null>(null);

  constructor() {
    inject(AdminApi).getRoles().subscribe({
      next: (roles) => this.roles.set(roles),
      error: (error: unknown) => this.error.set(describeHttpError(error)),
    });
  }
}
