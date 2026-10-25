import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { NavigationNode } from '../core/models/api-models';

/**
 * The menu, drawn from the server's navigation tree. Recursive: a node's
 * children are another <app-nav-tree>. The component has no idea who is
 * signed in or what they may see; the server already filtered the tree.
 */
@Component({
  selector: 'app-nav-tree',
  // No need to import NavTree into itself: a standalone component can
  // always use its own selector, which is what makes recursion work.
  imports: [RouterLink, RouterLinkActive],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ul [class.nested]="depth() > 0">
      @for (node of nodes(); track node.path) {
        <li>
          <a [routerLink]="node.path"
             routerLinkActive="active"
             [routerLinkActiveOptions]="{ exact: true }">{{ node.title }}</a>
          @if (node.children.length > 0) {
            <app-nav-tree [nodes]="node.children" [depth]="depth() + 1" />
          }
        </li>
      }
    </ul>
  `,
  styles: `
    ul { list-style: none; margin: 0; padding: 0; }
    ul.nested { padding-left: .9rem; border-left: 1px solid var(--border); margin-left: .4rem; }
    a { display: block; padding: .3rem .5rem; border-radius: 4px; color: inherit; text-decoration: none; }
    a:hover { background: var(--hover); }
    a.active { background: var(--accent-bg); color: var(--accent); font-weight: 600; }
  `,
})
export class NavTree {
  readonly nodes = input.required<NavigationNode[]>();
  readonly depth = input(0);
}
