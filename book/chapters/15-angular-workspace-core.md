# Chapter 15 · The Angular Workspace and the Core Layer

## 15.1 Creating the workspace

```powershell
mkdir client; cd client
npx @angular/cli@22 new sentinel-web --style=css --ssr=false --skip-git --routing `
  --package-manager=npm --ai-config=none --interactive=false
cd sentinel-web
```

Angular 22 creates a *standalone*, *zoneless* application tested with
*Vitest*. Standalone means no `NgModule`: each component lists what it uses
in its own `imports`. Zoneless means change detection is driven by signals
and events rather than by Zone.js patching every timer; in practice it
means "keep state in signals and the screen stays in step".

File names have lost their suffixes in recent versions (`app.ts` rather
than `app.component.ts`, class `App` rather than `AppComponent`). This book
follows the current style guide.

## 15.2 Talking to the API through a proxy

The API runs on port 5080 and the dev server on 4200. Rather than enable
CORS on the API, the dev server forwards `/api` to it:

<!-- include: client/sentinel-web/proxy.conf.json -->

and `angular.json` points `serve` at that file (`"options": { "proxyConfig":
"proxy.conf.json" }`). The browser only ever sees one origin, exactly as
it will in production behind a reverse proxy.

::: warning
If you write JSON files from Windows PowerShell 5 with `Set-Content
-Encoding utf8`, they get a byte-order mark, and the Angular CLI then
reports `proxy.conf.json contains parse errors: [1, 1] InvalidSymbol`.
That happened while writing this chapter. The cure is to save without a
BOM. Error positions like `[1, 1]` are a strong hint that the problem is
invisible.
:::

## 15.3 The models

<!-- include: client/sentinel-web/src/app/core/models/api-models.ts -->

Timestamps are typed `string`, on purpose. JSON has no date type, and
declaring `occurredAtUtc: Date` would be a lie that the compiler believes:
the value at run time would still be a string, and the moment it is parsed
would be hidden. Here it is visible, and done in exactly one function
(Chapter 18).

## 15.4 Errors, once

<!-- include: client/sentinel-web/src/app/core/http/api-error.ts -->

A failed HTTP call can be a network failure (status 0), an authentication
problem, a validation problem with one message per field, or a proxy's HTML
error page. Every component shows errors through this function, so all of
them read the same way.

## 15.5 Identity on every request

<!-- include: client/sentinel-web/src/app/core/http/demo-user.interceptor.ts -->

A functional interceptor (`HttpInterceptorFn`) adds the header to calls to
our own API only. The `startsWith('/api/')` check is the edge case: the
identity must never leak to another origin, and there is a test for it.

## 15.6 The session

<!-- include: client/sentinel-web/src/app/core/session/session.ts -->

The one piece of application-wide state lives in a root service, exposed as
**read-only** signals (`asReadonly()`). Components read; only `Session`
writes. That is the interface-segregation principle in a front end, and it
makes the fakes in tests tiny (Chapter 20).

`localStorage` access is wrapped in `try/catch`: it can throw in private
windows and with storage disabled, and a failure to *remember* a user must
never stop the application from *starting*.

## 15.7 Wiring it together

<!-- include: client/sentinel-web/src/app/app.config.ts -->

`withComponentInputBinding()` lets route data and query parameters arrive as
component inputs. `onSameUrlNavigation: 'reload'` lets the shell re-run the
guard for the current page when you switch user (Chapter 16).

## 15.8 Global styles

<!-- include: client/sentinel-web/src/styles.css -->

No component library: plain CSS with design tokens. Components refer to
`var(--danger)` rather than `#b42318`, so the palette lives in one place.
