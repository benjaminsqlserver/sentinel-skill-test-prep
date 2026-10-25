import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Session } from '../session/session';

/** The header the API's development-only authentication scheme reads. */
export const DEMO_USER_HEADER = 'X-Demo-User';

/**
 * Adds the chosen demo user to every API request. In production this
 * interceptor would attach a bearer token instead; nothing else in the
 * application would change, because nothing else knows how identity travels.
 */
export const demoUserInterceptor: HttpInterceptorFn = (request, next) => {
  const email = inject(Session).signedInEmail();

  if (!email || !request.url.startsWith('/api/')) {
    return next(request);
  }

  return next(request.clone({ setHeaders: { [DEMO_USER_HEADER]: email } }));
};
