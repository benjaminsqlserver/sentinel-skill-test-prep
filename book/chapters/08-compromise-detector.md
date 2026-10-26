# Chapter 8 · Sign-ins and the Compromise Detector

The skill test's third scenario is an Angular one: *show* a user when
their account may be compromised. Showing is only honest if the verdict is
right, so Sentinel computes it on the server, in the domain, where it can be
tested exhaustively. This chapter builds that verdict. Chapters 17 and 18
show it.

## 8.1 Where a sign-in came from

<!-- include: src/Sentinel.Domain/Security/GeoLocation.cs -->

Three decisions are encoded here:

- **Unknown is a value**, not `null`. `GeoLocation.Unknown` (country `ZZ`,
  the ISO "user-assigned" code) can be passed around, displayed and
  compared without null checks, and it has no coordinates, so distance
  calculations involving it return `null` instead of measuring from the
  Gulf of Guinea at latitude 0, longitude 0.
- **Distance is the haversine formula.** Accurate to about half a percent,
  which is far better than IP geolocation itself.
- **The summary comment says what the data is worth**: evidence, never proof.
  A VPN puts a Lagos user in Amsterdam.

The device is identified by hashing the user agent:

<!-- include: src/Sentinel.Domain/Security/DeviceFingerprint.cs -->

## 8.2 The sign-in attempt

<!-- include: src/Sentinel.Domain/Security/SignInAttempt.cs -->

An attempt is an immutable fact: there are no methods that change it. Its
invariants are checked once, in `Record`: the timestamp is UTC, the IP
parses, and a success has no failure reason while a failure must have one.
The location is stored as four flat columns (Chapter 11 explains why) and
rebuilt as a value object on read.

## 8.3 Findings, severity and the assessment

<!-- include: src/Sentinel.Domain/Security/SecurityFinding.cs -->

`Severity` has explicit numeric values so that `Max()` means "worst". A
finding's `Key` is built from the id of the attempt that triggered it, so
recomputing the assessment tomorrow produces the same key, and the user's
"that was me" sticks.

<!-- include: src/Sentinel.Domain/Security/DetectionPolicy.cs -->

Thresholds are data. The security team's first question after launch will
be "can it be less noisy?", and the answer should be a configuration change,
not a code review.

## 8.4 The detector

<!-- include: src/Sentinel.Domain/Security/CompromiseDetector.cs -->

### The brute-force rule, carefully

"At least five failures within fifteen minutes" sounds simple and hides two
traps.

*Trap one: what counts as "within"?* Five failures ten minutes apart are
within forty minutes of each other but never more than two inside any
fifteen-minute window. The detector first groups failures into bursts (a
gap longer than the window ends a burst), then uses a two-pointer sweep,
`MaxInAnyWindow`, to find the most failures inside any window-sized slice.
`Failures_spread_more_thinly_than_the_window_allows_are_not_a_burst` pins
that behaviour.

*Trap two: did the attacker get in?* Failures alone mean someone is trying.
A success shortly after them means someone may have succeeded. The detector
looks for a success after the first failure and within a window of the last,
raises the severity to High, and adds the success to the evidence, so the
table highlights it.

### The impossible-travel rule

Only successful attempts with coordinates are compared, each with the one
before it. Two guards matter:

- **Short hops are ignored** (below 500 km). IP geolocation cannot tell
  Lagos from Ikeja, so "travelling" 9 km in a minute is noise.
- **Zero elapsed time is the clearest case, not a division by zero.** The
  speed is set to infinity explicitly, rather than letting `km / 0.0`
  produce it by accident.

### New country and new device

Both walk successes in order with a `HashSet` of what has been seen. The
first success sets the baseline and is never reported: flagging every new
account would teach users to ignore the banner, and a security warning that
is ignored is worse than none.

### What the detector refuses to trust

It sorts its input itself (oldest first, ties by id, so the output is
deterministic), ignores attempts dated after `now`, and requires `now` to be
UTC. What it *cannot* do is notice that it has been handed the wrong subset
of history. Remember that sentence; Chapter 23 is about it.

## 8.5 The tests

<!-- include: tests/Sentinel.Domain.Tests/Security/CompromiseDetectorTests.cs -->

These tests are organised by rule, and each rule has tests for both sides of
its boundary: four failures and five, ten minutes apart and two, 9 km and
5,000 km. That habit (test just inside and just outside every threshold) is
the single most reliable way to "handle edge cases" in a timed test.

## 8.6 Doing it in the browser instead

In the test, you may be given raw login events in Angular and asked to
decide what looks suspicious there. The same rules translate directly to
TypeScript: sort a copy (`[...events].sort(...)`, never sort the input in
place), walk it once, keep `Set`s of seen countries and devices, and return
findings rather than booleans so the template can explain itself. Chapter
17 shows where such a function belongs in a component tree (a pure function
in its own file, called by the container) and Chapter 20 how to test it.
