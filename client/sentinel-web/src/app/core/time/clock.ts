import { DestroyRef, Injectable, inject, signal } from '@angular/core';

/**
 * "Now", as a signal that ticks once a minute. Components that show
 * relative times read it and pass it down; tests replace the whole service
 * with a fixed time.
 */
@Injectable({ providedIn: 'root' })
export class Clock {
  private readonly current = signal(new Date());
  readonly now = this.current.asReadonly();

  constructor() {
    const timer = setInterval(() => this.current.set(new Date()), 60_000);
    inject(DestroyRef).onDestroy(() => clearInterval(timer));
  }
}
