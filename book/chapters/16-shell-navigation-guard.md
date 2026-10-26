# Chapter 16 · The Shell, Navigation and the Route Guard

This chapter connects the second scenario (page authorisation) to the
front end: the menu shows only what the server says you may open, and the
router refuses everything else.

## 16.1 The shell

<!-- include: client/sentinel-web/src/app/app.ts -->

The shell owns two things: the list of demo users and the reaction to
switching. It passes data down to two children and listens to one output.
After a switch it re-navigates to the current URL, which, together with
`runGuardsAndResolvers: 'always'` on the routes, re-runs the guard: the new
user may not be allowed to stay where the old one was.

## 16.2 The user switcher: two-way binding by hand

<!-- include: client/sentinel-web/src/app/layout/user-switcher.ts -->

The input `selected` and the output `selectedChange` form a pair that
Angular recognises: a parent may write `[(selected)]="email"`. The shell
uses the long form, `[selected]` and `(selectedChange)`, because it wants
to *do* something on change, not just store the value.

## 16.3 The menu: a recursive component

<!-- include: client/sentinel-web/src/app/layout/nav-tree.ts -->

The component renders itself for children. A standalone component can
always use its own selector, so it does not list itself in `imports`. The `depth`
input is passed down so the nested lists can be indented.

## 16.4 The routes

<!-- include: client/sentinel-web/src/app/app.routes.ts -->

Every route except `/forbidden` and the wildcard is guarded. The two helper
functions are worth the lines: without them, twelve routes would repeat
`canActivate: [pageAccessGuard], runGuardsAndResolvers: 'always'`, and the
thirteenth would forget it. The admin screens are lazy-loaded, so users who
cannot open them never download them.

## 16.5 The guard

<!-- include: client/sentinel-web/src/app/core/access/page-access.guard.ts -->

Why ask the server on every navigation instead of checking the menu the
client already holds? Because the menu is a *view* of the rules, not the
rules. A typed URL, a bookmark or a tab left open while an administrator
revoked a role must all be judged by the rules themselves, and the rules
live in one place.

And the guard is user experience, not security. The comment says so: the
API checks permissions again on every data request, so a user who bypasses
the guard (it is JavaScript running on their machine) gets an empty page,
never someone else's data.

Three edge cases are handled and tested:

1. Denied → redirect to `/forbidden` with the page and the reason.
2. Unknown page or API down → **fail closed**, reason `Unknown`.
3. The router's URL includes `?query` and `#fragment`; access is decided by
   the path alone (`pathOnly`).

## 16.6 Explaining a denial

<!-- include: client/sentinel-web/src/app/core/access/access-reasons.ts -->

<!-- include: client/sentinel-web/src/app/features/pages/forbidden-page.ts -->

These sentences are written for a non-engineer: no "permission strings", no
"parent nodes". Writing them is the same skill as the customer reply in
Chapter 24, and the same reviewer instinct applies: *say what happened and
what to do next.*

<!-- include: client/sentinel-web/src/app/features/pages/placeholder-page.ts -->
