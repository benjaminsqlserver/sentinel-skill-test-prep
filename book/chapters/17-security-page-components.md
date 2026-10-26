# Chapter 17 · Passing Data Between Components: the Account Security Page

"How do you pass information between components?" is the first
Angular-specific criterion on the scenario's list. This chapter answers it
with a real page, then lists every mechanism Angular offers and when each
is the right one.

## 17.1 The component tree

```text
AccountSecurityPage  (container: owns data, talks to the API, handles events)
 │
 ├── riskLevel, findingCount, assessedAtUtc, now ──▶ SecurityBanner
 │                                     ◀── (review) ─┘   "the user wants to see the findings"
 │
 ├── findings, timeZone, now, pendingKey ─────────▶ FindingList
 │                                ◀── (acknowledge) ─┘   emits the finding's key
 │
 └── signIns, timeZone ────────────────────────────▶ SignInTable
```

**Data flows down through inputs. Intentions flow up through outputs. Only
the container has side effects.** If you remember one sentence from this
part of the book, make it that one. It is what a reviewer is checking when
they ask how you pass information, and it is what makes each child trivial
to test and reuse: `SecurityBanner` appears on the dashboard and the admin
explorer too, with a different reaction to `(review)` each time, and was
never changed to allow it.

## 17.2 The presentational children

<!-- include: client/sentinel-web/src/app/features/security/risk.ts -->

The words are the product. "High" tells a user nothing; "Someone may be
using your account" tells them to act. Note the edge case: an unexpected
level from a newer API is shown as a *warning*, never as all clear. When in
doubt, fail towards telling the user.

<!-- include: client/sentinel-web/src/app/features/security/security-banner.ts -->

- `input.required<RiskLevel>()`: the parent *must* bind it; forgetting is a
  compile error, not a blank banner.
- `input(0)`: optional, with a default.
- `output<void>()`: an event with no payload. The banner does not know what
  "review" will do, and should not.
- `computed(...)`: derived state, recalculated only when `riskLevel`
  changes.
- `ChangeDetectionStrategy.OnPush`: re-render only when an input or a
  signal it reads changes. With signal inputs this is the natural default.
- `role="status"` and `aria-live="polite"`: a screen reader announces the
  verdict when it changes.

<!-- include: client/sentinel-web/src/app/features/security/finding-list.ts -->

The list emits the **key**, not the whole finding and not an index. A key is
stable; an index changes when the list does, which is how "I clicked
*This was me* on one card and another one disappeared" bugs are born.
`track finding.key` in the `@for` tells Angular the same thing about
identity, so cards are not recreated on every reload.

`pendingKey` travels *down* after the event went *up*: the container
decides that a request is in flight, and the child reflects it by disabling
every button. That prevents double submission without the child knowing
anything about HTTP.

<!-- include: client/sentinel-web/src/app/features/security/sign-in-table.ts -->

Edge cases visible here: no sign-ins at all, unknown location (`ZZ`), and
every failure reason including one we do not recognise. `<time datetime>`
keeps the machine-readable UTC value next to the human one, and the
tooltip shows UTC for support conversations.

## 17.3 The container

<!-- include: client/sentinel-web/src/app/features/security/security-api.ts -->

<!-- include: client/sentinel-web/src/app/features/security/account-security-page.ts -->

Four decisions in this class are worth being able to explain in an
interview:

**A union type for page state.** Three booleans (`isLoading`, `hasError`,
`data`) allow eight combinations, of which four are nonsense ("loading and
failed and loaded"). A union with four members allows exactly the four
real states, and `@switch (state().kind)` renders exactly one of them.

**`switchMap` on user changes.** The demo switcher can change user while a
request is in flight. `switchMap` cancels the previous request, so a slow
response for Ada can never overwrite a fast one for Tunde. Without it, the
page could show one user's findings under another user's name.

**Reload after acknowledging, rather than removing the card locally.** The
risk level and the flagged rows depend on all findings together; only the
server knows the new answer. Optimistic updates are a fine technique, but
not for a security verdict.

**`startWith` the current state when refreshing.** After an acknowledgement
the page keeps showing the old data until the new data arrives, instead of
flashing "loading" under the user's cursor.

## 17.4 Every way to pass information, and when to use each

| Mechanism | Direction | Use it when | In Sentinel |
|---|---|---|---|
| `input()` / `@Input()` | parent → child | The child displays or depends on data | Every presentational component |
| `output()` / `@Output()` + `EventEmitter` | child → parent | The child reports an intention | `review`, `acknowledge`, `selectedChange` |
| Two-way `[(x)]` (`x` + `xChange`, or `model()`) | both | A child edits a value the parent owns | `UserSwitcher` |
| A shared service holding signals or a `BehaviorSubject` | any → any | State many unrelated components need | `Session`, `Clock` |
| Router inputs (`withComponentInputBinding`) | URL → page | State that belongs in the address bar | `userId` on the access explorer; `reason` on `/forbidden` |
| `viewChild()` / `@ViewChild` | parent reads child | The parent must call into its own template (focus, scroll) | `findingsHeading` |
| Content projection `<ng-content>` | parent → child markup | A child wraps markup it does not own | Not needed here |

A reasonable rule for the test: inputs and outputs first; a service when
three or more components need the same state; the URL when the state
should survive a refresh or be shareable.

## 17.5 The edge-case checklist for this page

Before calling a page like this done, check each line:

- [ ] Signed out
- [ ] Loading
- [ ] API unreachable (status 0) and a retry
- [ ] Server error with a problem-details message
- [ ] No findings ("Nothing to review", and the banner offers no button)
- [ ] One finding (singular wording) and several (plural)
- [ ] An unknown risk level or finding kind from a newer server
- [ ] An action that fails (the card stays; the error is shown; buttons re-enable)
- [ ] Double-click on an action
- [ ] The user changes while a request is in flight
- [ ] Dates: zone, daylight saving, "just now" vs clock skew (Chapter 18)

Each of these has a test in Chapter 20.
