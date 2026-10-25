import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter, withComponentInputBinding, withRouterConfig } from '@angular/router';
import { routes } from './app.routes';
import { demoUserInterceptor } from './core/http/demo-user.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideHttpClient(withInterceptors([demoUserInterceptor])),
    provideRouter(
      routes,
      // Route data and query parameters arrive as component inputs, so a
      // page declares `readonly reason = input<string>()` instead of
      // injecting ActivatedRoute and subscribing.
      withComponentInputBinding(),
      withRouterConfig({ onSameUrlNavigation: 'reload' }),
    ),
  ],
};
