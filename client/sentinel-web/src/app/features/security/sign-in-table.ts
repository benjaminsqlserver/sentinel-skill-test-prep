import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { SignInAttempt } from '../../core/models/api-models';
import { UtcTimePipe, ZonedDatePipe } from '../../core/time/time-pipes';

/** Recent sign-ins. Rows the findings cite as evidence are highlighted. */
@Component({
  selector: 'app-sign-in-table',
  imports: [ZonedDatePipe, UtcTimePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (signIns().length === 0) {
      <p class="empty">No sign-ins yet.</p>
    } @else {
      <table>
        <caption>Times shown in {{ timeZone() }}</caption>
        <thead>
          <tr><th scope="col">When</th><th scope="col">Result</th><th scope="col">Where</th><th scope="col">Device</th><th scope="col">IP address</th></tr>
        </thead>
        <tbody>
          @for (signIn of signIns(); track signIn.id) {
            <tr [class.flagged]="signIn.isFlagged">
              <td>
                <time [attr.datetime]="signIn.occurredAtUtc" [title]="signIn.occurredAtUtc | utcTime">
                  {{ signIn.occurredAtUtc | zonedDate: timeZone() }}
                </time>
                @if (signIn.isFlagged) { <span class="flag" title="Part of a finding above">⚑</span> }
              </td>
              <td>
                @if (signIn.succeeded) { <span class="ok">Signed in</span> }
                @else { <span class="failed">Failed · {{ describeFailure(signIn.failureReason) }}</span> }
              </td>
              <td>{{ signIn.countryCode === 'ZZ' ? 'Unknown' : signIn.city + ', ' + signIn.countryCode }}</td>
              <td>{{ signIn.device }}</td>
              <td><code>{{ signIn.ipAddress }}</code></td>
            </tr>
          }
        </tbody>
      </table>
    }
  `,
  styles: `
    table { width: 100%; border-collapse: collapse; background: var(--surface); }
    caption { text-align: left; color: var(--muted); font-size: .85rem; padding: .25rem 0; }
    th, td { text-align: left; padding: .45rem .6rem; border-bottom: 1px solid var(--border); font-size: .9rem; }
    tr.flagged { background: var(--danger-bg); }
    .flag { color: var(--danger); margin-left: .35rem; }
    .ok { color: var(--ok); }
    .failed { color: var(--danger); }
    .empty { color: var(--muted); }
  `,
})
export class SignInTable {
  readonly signIns = input.required<SignInAttempt[]>();
  readonly timeZone = input.required<string>();

  protected describeFailure(reason: string | null): string {
    switch (reason) {
      case 'WrongPassword': return 'wrong password';
      case 'WrongSecondFactor': return 'wrong code';
      case 'AccountLocked': return 'account locked';
      case 'AccountSuspended': return 'account suspended';
      default: return 'unknown reason';
    }
  }
}
