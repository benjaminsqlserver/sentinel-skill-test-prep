import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { Router, RouterOutlet } from '@angular/router';
import { DemoUser } from './core/models/api-models';
import { Session } from './core/session/session';
import { NavTree } from './layout/nav-tree';
import { UserSwitcher } from './layout/user-switcher';

/** The shell: header, menu, and the outlet the pages render into. */
@Component({
  selector: 'app-root',
  imports: [RouterOutlet, NavTree, UserSwitcher],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <header class="top">
      <a class="brand" href="/">Sentinel</a>
      <app-user-switcher [users]="demoUsers()" [selected]="session.signedInEmail()" (selectedChange)="switchUser($event)" />
    </header>
    <div class="body">
      <nav aria-label="Main">
        <app-nav-tree [nodes]="session.navigation()" />
      </nav>
      <main>
        <router-outlet />
      </main>
    </div>
  `,
  styles: `
    :host { display: grid; grid-template-rows: auto 1fr; min-height: 100vh; }
    .top { display: flex; justify-content: space-between; align-items: center; padding: .6rem 1.25rem;
           background: var(--header-bg); color: var(--header-fg); }
    .brand { font-weight: 700; letter-spacing: .04em; color: inherit; text-decoration: none; }
    .body { display: grid; grid-template-columns: 15rem 1fr; }
    nav { padding: 1rem .75rem; border-right: 1px solid var(--border); background: var(--surface); }
    main { padding: 1.25rem 1.75rem; }
    @media (max-width: 760px) { .body { grid-template-columns: 1fr; } nav { border-right: none; border-bottom: 1px solid var(--border); } }
  `,
})
export class App {
  protected readonly session = inject(Session);
  private readonly router = inject(Router);
  protected readonly demoUsers = signal<DemoUser[]>([]);

  constructor() {
    this.session.getDemoUsers().subscribe({
      next: (users) => this.demoUsers.set(users),
      error: () => this.demoUsers.set([]), // the API is down; pages will say so
    });
    this.session.refresh().subscribe();
  }

  protected switchUser(email: string | null): void {
    this.session.signInAs(email).subscribe(() => {
      // Re-run the guard for the page we are on: the new user may not be
      // allowed to see it. onSameUrlNavigation: 'reload' (app.config.ts)
      // and runGuardsAndResolvers: 'always' (app.routes.ts) make this work.
      this.router.navigateByUrl(this.router.url);
    });
  }
}
