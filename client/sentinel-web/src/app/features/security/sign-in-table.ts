import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { SignInAttempt } from '../../core/models/api-models';

/**
 * DRILL (Chapter 25): rebuild this component in 5 minutes.
 * The inputs and outputs below are the contract the container and the
 * spec rely on; keep their names. Write the template, the styles and any
 * helpers. Done means: `npx ng test --watch=false` is green and the page
 * looks right for Tunde, Dana and Emeka.
 */
@Component({
  selector: 'app-sign-in-table',
  imports: [],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<!-- TODO: a table of sign-ins, times in the user's zone, flagged rows highlighted, empty state. -->`,
})
export class SignInTable {
  readonly signIns = input.required<SignInAttempt[]>();
  readonly timeZone = input.required<string>();
}
