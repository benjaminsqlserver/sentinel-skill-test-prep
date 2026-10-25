import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { explainAccess } from '../../core/access/access-reasons';

/**
 * Where the guard sends people it turns away. `page` and `reason` are query
 * parameters, bound to inputs by withComponentInputBinding().
 */
@Component({
  selector: 'app-forbidden-page',
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h1>You can't open this page</h1>
    @if (page()) {
      <p><code>{{ page() }}</code></p>
    }
    <p>{{ explanation() }}</p>
    <p><a routerLink="/">Back to the dashboard</a></p>
  `,
})
export class ForbiddenPage {
  readonly page = input<string>();
  readonly reason = input<string>();

  protected readonly explanation = computed(() => explainAccess(this.reason()));
}
