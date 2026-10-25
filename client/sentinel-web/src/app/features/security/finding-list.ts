import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { SecurityFinding } from '../../core/models/api-models';
import { RelativeTimePipe, UtcTimePipe, ZonedDatePipe } from '../../core/time/time-pipes';
import { findingTitle, severityTone } from './risk';

/**
 * The findings, one card each, with a "This was me" button. Clicking it
 * does not call the API; it emits the finding's key and the page decides.
 * `pendingKey` comes back down as an input so the button can show progress
 * and refuse a double click while the request is in flight.
 */
@Component({
  selector: 'app-finding-list',
  imports: [ZonedDatePipe, RelativeTimePipe, UtcTimePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @for (finding of findings(); track finding.key) {
      <article class="finding" [class]="'tone-' + tone(finding.severity)">
        <header>
          <span class="severity">{{ finding.severity }}</span>
          <h3>{{ title(finding.kind) }}</h3>
        </header>
        <p>{{ finding.summary }}</p>
        <p class="when">
          <time [attr.datetime]="finding.occurredAtUtc" [title]="finding.occurredAtUtc | utcTime">
            {{ finding.occurredAtUtc | zonedDate: timeZone() }}
          </time>
          · {{ finding.occurredAtUtc | relativeTime: now() }}
        </p>
        <div class="actions">
          <button type="button" (click)="acknowledge.emit(finding.key)" [disabled]="pendingKey() !== null">
            {{ pendingKey() === finding.key ? 'Saving…' : 'This was me' }}
          </button>
          <a href="/help">This wasn't me</a>
        </div>
      </article>
    } @empty {
      <p class="empty">Nothing to review.</p>
    }
  `,
  styles: `
    :host { display: grid; gap: .75rem; }
    .finding { border: 1px solid var(--border); border-left: 5px solid; border-radius: 6px; padding: .75rem 1rem; background: var(--surface); }
    .finding header { display: flex; gap: .5rem; align-items: baseline; }
    .finding h3 { margin: 0; font-size: 1rem; }
    .severity { font-size: .7rem; font-weight: 700; text-transform: uppercase; letter-spacing: .05em; }
    .when { font-size: .85rem; color: var(--muted); }
    .actions { display: flex; gap: 1rem; align-items: center; }
    .tone-info { border-left-color: var(--info); }
    .tone-warning { border-left-color: var(--warning); }
    .tone-danger { border-left-color: var(--danger); }
    .empty { color: var(--muted); }
  `,
})
export class FindingList {
  readonly findings = input.required<SecurityFinding[]>();
  readonly timeZone = input.required<string>();
  readonly now = input.required<Date>();
  readonly pendingKey = input<string | null>(null);

  readonly acknowledge = output<string>();

  protected readonly title = findingTitle;
  protected readonly tone = severityTone;
}
