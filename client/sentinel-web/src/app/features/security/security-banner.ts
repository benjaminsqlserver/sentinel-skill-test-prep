import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { RiskLevel } from '../../core/models/api-models';
import { RelativeTimePipe, UtcTimePipe } from '../../core/time/time-pipes';
import { presentRisk } from './risk';

/**
 * The verdict at the top of the page. Presentational: everything it shows
 * arrives through inputs, and the one thing it can do (ask to review)
 * leaves through an output. It does not know where the data came from or
 * what "review" will do, which is exactly what makes it reusable on the
 * admin screen and trivial to test.
 */
@Component({
  selector: 'app-security-banner',
  imports: [RelativeTimePipe, UtcTimePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="banner" [class]="'tone-' + presentation().tone" role="status" aria-live="polite">
      <div class="banner-text">
        <h2>{{ presentation().headline }}</h2>
        <p>{{ presentation().advice }}</p>
        <p class="assessed" [title]="assessedAtUtc() | utcTime">
          Checked {{ assessedAtUtc() | relativeTime: now() }}
        </p>
      </div>
      @if (findingCount() > 0) {
        <button type="button" class="primary" (click)="review.emit()">
          Review {{ findingCount() }} {{ findingCount() === 1 ? 'item' : 'items' }}
        </button>
      }
    </section>
  `,
  styles: `
    .banner { display: flex; gap: 1rem; align-items: center; justify-content: space-between;
              padding: 1rem 1.25rem; border-radius: 8px; border-left: 6px solid; }
    .banner h2 { margin: 0 0 .25rem; font-size: 1.15rem; }
    .banner p { margin: .15rem 0; }
    .assessed { font-size: .8rem; opacity: .75; }
    .tone-ok { background: var(--ok-bg); border-color: var(--ok); }
    .tone-info { background: var(--info-bg); border-color: var(--info); }
    .tone-warning { background: var(--warning-bg); border-color: var(--warning); }
    .tone-danger { background: var(--danger-bg); border-color: var(--danger); }
  `,
})
export class SecurityBanner {
  readonly riskLevel = input.required<RiskLevel>();
  readonly findingCount = input(0);
  readonly assessedAtUtc = input.required<string>();
  readonly now = input.required<Date>();

  /** The user wants to see the findings. The parent decides what that means. */
  readonly review = output<void>();

  protected readonly presentation = computed(() => presentRisk(this.riskLevel()));
}
