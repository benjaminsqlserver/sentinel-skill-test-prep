import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { SecurityFinding } from '../../core/models/api-models';

/**
 * DRILL (Chapter 25): rebuild this component in 10 minutes.
 * The inputs and outputs below are the contract the container and the
 * spec rely on; keep their names. Write the template, the styles and any
 * helpers. Done means: `npx ng test --watch=false` is green and the page
 * looks right for Tunde, Dana and Emeka.
 */
@Component({
  selector: 'app-finding-list',
  imports: [],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<!-- TODO: one <article> per finding: title, summary, time in the user's zone, relative time,
                  a "This was me" button that emits the key. Empty state. Disable while pendingKey is set. -->`,
})
export class FindingList {
  readonly findings = input.required<SecurityFinding[]>();
  readonly timeZone = input.required<string>();
  readonly now = input.required<Date>();
  readonly pendingKey = input<string | null>(null);
  readonly acknowledge = output<string>();
}
