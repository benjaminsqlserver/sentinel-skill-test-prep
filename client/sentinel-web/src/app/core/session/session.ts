import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, catchError, of, tap } from 'rxjs';
import { CurrentUser, DemoUser, NavigationNode } from '../models/api-models';

const STORAGE_KEY = 'sentinel.demoUser';

/**
 * Who is signed in, and what they may see. This is the one piece of state
 * shared across the whole application, so it lives in a root service and
 * is exposed as read-only signals. Components read it; only this class
 * writes it.
 */
@Injectable({ providedIn: 'root' })
export class Session {
  private readonly http = inject(HttpClient);

  private readonly email = signal<string | null>(readStoredEmail());
  private readonly user = signal<CurrentUser | null>(null);
  private readonly menu = signal<NavigationNode[]>([]);

  /** The email sent in the demo header, or null when signed out. */
  readonly signedInEmail = this.email.asReadonly();
  readonly currentUser = this.user.asReadonly();
  readonly navigation = this.menu.asReadonly();
  readonly isSignedIn = computed(() => this.user() !== null);

  /**
   * The zone to show times in when a screen has no better information.
   * The user's profile zone, not the browser's: a Lagos user on a work
   * trip to London still thinks in Lagos time on their own account page.
   */
  readonly timeZone = computed(() => this.user()?.timeZoneId ?? 'UTC');

  getDemoUsers(): Observable<DemoUser[]> {
    return this.http.get<DemoUser[]>('/api/dev/demo-users');
  }

  /** Switches user (or signs out with null) and reloads everything that depends on identity. */
  signInAs(email: string | null): Observable<unknown> {
    this.email.set(email);
    writeStoredEmail(email);
    return this.refresh();
  }

  /** Reloads the profile and the menu for whoever is signed in now. */
  refresh(): Observable<unknown> {
    const email = this.email();
    this.loadNavigation().subscribe();

    if (!email) {
      this.user.set(null);
      return of(null);
    }

    return this.http.get<CurrentUser>('/api/me').pipe(
      tap((user) => this.user.set(user)),
      catchError(() => {
        // The stored user no longer exists (database reset). Fall back to
        // signed out rather than leaving a half-signed-in shell.
        this.email.set(null);
        writeStoredEmail(null);
        this.user.set(null);
        return of(null);
      }),
    );
  }

  private loadNavigation(): Observable<NavigationNode[]> {
    return this.http.get<NavigationNode[]>('/api/me/navigation').pipe(
      tap((nodes) => this.menu.set(nodes)),
      catchError(() => {
        this.menu.set([]);
        return of([]);
      }),
    );
  }
}

// localStorage can throw (private windows, disabled storage). A failure to
// remember the user must never stop the application from starting.
function readStoredEmail(): string | null {
  try {
    return localStorage.getItem(STORAGE_KEY);
  } catch {
    return null;
  }
}

function writeStoredEmail(email: string | null): void {
  try {
    if (email) {
      localStorage.setItem(STORAGE_KEY, email);
    } else {
      localStorage.removeItem(STORAGE_KEY);
    }
  } catch {
    // Nothing useful to do; the session still works for this tab.
  }
}
