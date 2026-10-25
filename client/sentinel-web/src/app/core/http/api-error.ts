import { HttpErrorResponse } from '@angular/common/http';

/**
 * Turns whatever an HTTP call threw into one sentence a user can read.
 *
 * The API answers failures with RFC 9457 problem details
 * (`{ title, detail, status, errors? }`), but a failure can also come from
 * the network (status 0) or from a proxy that knows nothing about problem
 * details. Every component shows errors through this function, so all
 * three cases read the same way everywhere.
 */
export function describeHttpError(error: unknown): string {
  if (!(error instanceof HttpErrorResponse)) {
    return 'Something went wrong. Please try again.';
  }

  if (error.status === 0) {
    return 'Cannot reach the Sentinel API. Is it running on http://localhost:5080?';
  }

  if (error.status === 401) {
    return 'You are not signed in. Choose a user from the switcher at the top of the page.';
  }

  if (error.status === 403) {
    return 'You do not have permission to do that.';
  }

  const problem = error.error as { detail?: string; title?: string; errors?: Record<string, string[]> } | null;
  if (problem?.errors) {
    return Object.values(problem.errors).flat().join(' ');
  }

  return problem?.detail ?? problem?.title ?? `The server answered ${error.status} ${error.statusText}.`;
}
