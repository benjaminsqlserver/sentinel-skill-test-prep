# Chapter 19 · The Dashboard and the Admin Screens

These screens are not on the skill test's critical path, but each one
demonstrates a pattern you are likely to need.

## 19.1 The dashboard: reusing a child with a different parent

<!-- include: client/sentinel-web/src/app/features/pages/dashboard-page.ts -->

The same `SecurityBanner` as on the security page. Here `(review)`
navigates; there it scrolls. The child is unchanged, which is the payoff of
keeping it presentational. Note also the deliberate difference in error
handling: on the dashboard the banner is a nicety, so a failure hides it
rather than showing an error.

## 19.2 The admin API service

<!-- include: client/sentinel-web/src/app/features/admin/admin-api.ts -->

`HttpParams` builds query strings safely, and every id that goes into a path
is passed through `encodeURIComponent`.

## 19.3 Search as you type

<!-- include: client/sentinel-web/src/app/features/admin/users-page.ts -->

The pipeline in the class comment is worth learning as a unit:
`debounceTime` → `distinctUntilChanged` → `switchMap`. Each operator
prevents a specific bug: a request per keystroke, a repeated request when
the user types and deletes a character, and an old slow response
overwriting a newer one.

## 19.4 The access explorer: the second scenario, made visible

<!-- include: client/sentinel-web/src/app/features/admin/access-explorer-page.ts -->

This screen answers the ticket "why can't Ada see the finance report?" in
one place: every page, the verdict, and the reason in words. The selected
user lives in the URL (`?userId=`), so the screen can be bookmarked or
pasted into a ticket. The `effect` reloads whenever that input changes,
including the first time.

`forkJoin` runs the two requests in parallel and continues when both have
finished; if either fails, the error handler runs once.

<!-- include: client/sentinel-web/src/app/features/admin/roles-page.ts -->
