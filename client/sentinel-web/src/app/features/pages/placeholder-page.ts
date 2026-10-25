import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';
import { Session } from '../../core/session/session';

/**
 * Stands in for the screens this book does not build (reports, billing...).
 * They exist so the page tree and the guard have something real to protect.
 * `title` arrives from the route's `data` through component input binding.
 */
@Component({
  selector: 'app-placeholder-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h1>{{ title() }}</h1>
    <p>
      @if (session.currentUser(); as user) {
        {{ user.displayName }}, you can see this page because the access rules allow it.
      } @else {
        This page is public.
      }
    </p>
    <p class="muted">The real screen is out of scope for this book. Try the <em>Account security</em> page or the <em>Administration</em> section.</p>
  `,
  styles: `.muted { color: var(--muted); }`,
})
export class PlaceholderPage {
  protected readonly session = inject(Session);
  readonly title = input('Sentinel');
}
