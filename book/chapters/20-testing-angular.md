# Chapter 20 · Testing the Angular Application

```powershell
cd client/sentinel-web
npx ng test --watch=false     # 62 tests, about 7 seconds
npx ng test                   # watch mode while you work
```

Angular 22 runs specs with **Vitest** in a jsdom environment. `describe`,
`it`, `expect` and `vi` are globals. If your test environment uses Jasmine
and Karma instead, the only differences you will meet are `vi.fn()` →
`jasmine.createSpy()` and `vi.spyOn` → `spyOn`.

## 20.1 Pure functions first

The cheapest, fastest tests are of functions with no Angular in them:

<!-- include: client/sentinel-web/src/app/core/time/timestamps.spec.ts -->

`it.each` turns a table of examples into a test per row, exactly like
xUnit's `[Theory]`. The relative-time table tests both sides of every unit
boundary.

<!-- include: client/sentinel-web/src/app/features/security/risk.spec.ts -->

## 20.2 Presentational components: set inputs, read the DOM, catch outputs

<!-- include: client/sentinel-web/src/app/features/security/test-data.ts -->

<!-- include: client/sentinel-web/src/app/features/security/security-banner.spec.ts -->

`fixture.componentRef.setInput(name, value)` sets inputs the way a parent
template would, so the test exercises the real `input()` declarations
rather than poking fields. Outputs are observed by subscribing to them.

<!-- include: client/sentinel-web/src/app/features/security/finding-list.spec.ts -->

## 20.3 The container against a fake back end

<!-- include: client/sentinel-web/src/app/features/security/account-security-page.spec.ts -->

`HttpTestingController` lets each test decide what the server answers,
including the failures that are hard to cause on demand: a 404 on
acknowledge, a network error. `http.verify()` in `afterEach` fails the test
if a request was made and not answered, or answered and not made.

The `Session` fake provides only `currentUser`. A fake that small is a
design signal: the page depends on very little of the session, which is
what read-only signals were for.

## 20.4 The guard and the interceptor

<!-- include: client/sentinel-web/src/app/core/access/page-access.guard.spec.ts -->

A functional guard is tested by calling it inside
`TestBed.runInInjectionContext`, so that its `inject()` calls work.

<!-- include: client/sentinel-web/src/app/core/http/demo-user.interceptor.spec.ts -->

## 20.5 What to test in 25 minutes

In the Angular scenario you will not write 62 tests. Write the two or three
that prove the risky parts: the date formatting in a non-UTC zone, the
empty state, and the output your parent depends on. A reviewer reading
`it('shows the time in the user zone, not the browser zone')` learns more
about you than from any amount of coverage.
