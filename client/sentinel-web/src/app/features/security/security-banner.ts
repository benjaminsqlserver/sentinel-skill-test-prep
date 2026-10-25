import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { RiskLevel } from '../../core/models/api-models';

/**
 * DRILL (Chapter 25): rebuild this component in 10 minutes.
 * The inputs and outputs below are the contract the container and the
 * spec rely on; keep their names. Write the template, the styles and any
 * helpers. Done means: `npx ng test --watch=false` is green and the page
 * looks right for Tunde, Dana and Emeka.
 */
@Component({
  selector: 'app-security-banner',
  imports: [],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<!-- TODO: headline and advice for the risk level (see risk.ts), "Checked <relative time>",
                  a "Review N item(s)" button only when there is something to review. -->`,
})
export class SecurityBanner {
  readonly riskLevel = input.required<RiskLevel>();
  readonly findingCount = input(0);
  readonly assessedAtUtc = input.required<string>();
  readonly now = input.required<Date>();
  readonly review = output<void>();
}
