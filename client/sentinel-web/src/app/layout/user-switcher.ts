import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { DemoUser } from '../core/models/api-models';

/**
 * A select box of demo users. It supports two-way binding:
 *
 *   <app-user-switcher [users]="users" [(selected)]="email" />
 *
 * `[(selected)]` is sugar for `[selected]="email" (selectedChange)="email = $event"`;
 * Angular wires it up because the output is named after the input plus
 * "Change". (Angular 17.2+ also offers `model()`, which declares both in one
 * line. The explicit pair is shown here because it is what every version of
 * Angular understands, including whatever version the test environment has.)
 */
@Component({
  selector: 'app-user-switcher',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <label>
      <span>Signed in as</span>
      <select [value]="selected() ?? ''" (change)="onChange($event)">
        <option value="">Nobody (signed out)</option>
        @for (user of users(); track user.email) {
          <option [value]="user.email" [selected]="user.email === selected()">
            {{ user.displayName }} · {{ user.description }}
          </option>
        }
      </select>
    </label>
  `,
  styles: `
    label { display: flex; gap: .5rem; align-items: center; font-size: .85rem; }
    select { max-width: 22rem; }
  `,
})
export class UserSwitcher {
  readonly users = input<DemoUser[]>([]);
  readonly selected = input<string | null>(null);
  readonly selectedChange = output<string | null>();

  protected onChange(event: Event): void {
    const value = (event.target as HTMLSelectElement).value;
    this.selectedChange.emit(value === '' ? null : value);
  }
}
