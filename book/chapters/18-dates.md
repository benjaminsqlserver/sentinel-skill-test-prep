# Chapter 18 · Displaying and Formatting Dates

"How do you display and format dates?" is on the Angular scenario's list
for a reason: it is where competent code is most often confidently wrong.
This chapter gives you three rules, the code that implements them, and the
failure each rule prevents.

## 18.1 Three rules

1. **The wire format is UTC with an explicit designator**:
   `2026-10-24T08:00:00Z`. Never a bare `2026-10-24T08:00:00`.
2. **Convert to a zone exactly once, at display time.** Store, compare,
   sort and transmit instants; turn them into wall-clock time only in the
   template.
3. **Display in the user's chosen zone** (their profile's IANA zone), not
   the browser's. A Lagos employee on a trip to London still thinks about
   their own account in Lagos time; a support agent in London reading a
   Lagos user's history wants to see what the *user* saw.

## 18.2 The implementation

<!-- include: client/sentinel-web/src/app/core/time/timestamps.ts -->

### Strict parsing

`parseApiTimestamp` refuses any string without `Z` or an offset. JavaScript
would happily parse it, *as local time*, and be wrong by the user's offset
with no error anywhere. Refusing makes the UI say "time unavailable" and
logs an error: a loud, visible failure instead of a quiet wrong one.
Chapter 22 is the incident that justified this function.

### Formatting with `Intl`, not `DatePipe`

| | Angular `DatePipe` | `Intl.DateTimeFormat` |
|---|---|---|
| Zone argument | An offset: `'+0100'` | An IANA name: `'Africa/Lagos'` |
| Daylight saving | You must know the offset for each date | Correct for every date, past or future |
| Locale data | Must be registered per locale | Built into the browser |
| Available in templates | Yes | Through a pipe (we write one) |

`DatePipe` with a fixed offset gets London wrong for half of every year:
London is UTC+0 in January and UTC+1 in July. A test pins this:
`applies daylight saving for the date being shown, not for today`.

Two more decisions in `formatInZone`:

- **`hourCycle: 'h23'`**, so `en-GB` shows `09:00`, never `9:00 am`, and the
  format is stable for tests.
- **An unknown zone falls back to UTC** and still renders. One bad profile
  value must not blank the page.

### Relative time with an injectable "now"

`formatRelative(value, now)` takes `now` as a parameter. That makes it a
pure function (same input, same output) and therefore testable with a
pinned clock, which is how `5 minutes ago`, `yesterday` and `3 months ago`
are each tested exactly. Times slightly in the future (the browser's clock
behind the server's) read as "just now" rather than "in 2 minutes".

## 18.3 The pipes

<!-- include: client/sentinel-web/src/app/core/time/time-pipes.ts -->

Pipes are pure by default: Angular re-runs them only when an argument
changes. `relativeTime` takes `now` as an argument, so passing a "now" that
ticks once a minute keeps "3 minutes ago" current without making the pipe
impure (an impure pipe runs on every change detection, for every row).

## 18.4 The clock

<!-- include: client/sentinel-web/src/app/core/time/clock.ts -->

Tests replace this service with a fixed signal. Nothing in the application
calls `new Date()` for "now" except here.

## 18.5 Pitfalls checklist

- [ ] `new Date('...')` on a string without `Z`: local time.
- [ ] `new Date(2026, 9, 24)`: months are zero-based; this is **October**.
- [ ] `date.getHours()`: the **browser's** zone, not the user's.
- [ ] `toLocaleString()` with no options: format varies by browser and machine.
- [ ] A fixed offset for a zone with daylight saving.
- [ ] Comparing formatted strings instead of instants.
- [ ] Sorting ISO strings: fine **only** if they are all UTC with the same format.
- [ ] Zone abbreviations (`WAT`, `GMT+1`) differ between engines: test the time, not the abbreviation.
- [ ] Relative time computed from a `Date.now()` you cannot control in tests.
