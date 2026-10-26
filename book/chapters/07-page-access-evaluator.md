# Chapter 7 · The Page Access Evaluator

This chapter is the long version of the skill test's second scenario. Read
it twice: once for the code, once for the *process*, because the process is
what you will reproduce in 40 minutes.

## 7.1 The process, before any code

**Step 1 — restate the requirements as a numbered list.** You have seen it
already: §2.4, rules 1 to 8. In the test, write that list as a comment at
the top of your class before writing anything else. It costs two minutes,
and it is the first thing a reviewer reads.

**Step 2 — list the edge cases and decide each one.** For page access:

| Edge case | Decision | Why |
|---|---|---|
| Page's parent does not exist | Deny, reason `OrphanedPage` | Fail closed |
| Parent cycle (`/a` → `/b` → `/a`) | Deny pages on the cycle (`CyclicHierarchy`), and pages below it as `ParentDenied` | Fail closed; no infinite loop |
| A page is its own parent | Impossible to create (`Page.Create`/`MoveUnder` refuse) **and** handled if it arrives anyway | Belt and braces |
| Two pages share an id | Throw `ArgumentException` | A programming error in the caller, not bad business data |
| Public page under a private parent | Allow | Public means public; the menu promotes it |
| Path with different case or a trailing slash | Normalise before comparing | `/Admin/` and `/admin` are one page |
| Unknown path | Return `null` | "No such page" is not "denied" |
| Very deep tree | Must not overflow or go quadratic beyond reason | Memoise decisions |
| Empty page list | Empty result | Not an error |

**Step 3 — pick the shape.** A pure function: subject and pages in,
decisions out. Every decision carries a *reason*, because "denied" with no
reason becomes a support ticket (Chapter 19 builds the screen that shows
them).

**Step 4 — write it, then test each decision from step 2.**

## 7.2 The supporting types

<!-- include: src/Sentinel.Domain/Access/AccessReason.cs -->

<!-- include: src/Sentinel.Domain/Access/PageAccessDecision.cs -->

The page itself, and the path normalisation it relies on:

<!-- include: src/Sentinel.Domain/Pages/PagePath.cs -->

<!-- include: src/Sentinel.Domain/Pages/Page.cs -->

## 7.3 The evaluator

<!-- include: src/Sentinel.Domain/Access/PageAccessEvaluator.cs -->

Read `DecideUncached` from top to bottom and compare it with the rules in
§2.4. They are in the same order. That is not an accident; it is the
cheapest way to make a reviewer trust your code.

### Why memoise?

`Decide` caches each decision in `_decisions`. Without the cache, deciding
every page in a tree means deciding each ancestor again for every
descendant: a ten-level-deep branch decides its root ten times. With it,
each page is decided once. The test
`A_very_deep_tree_does_not_overflow_the_stack` builds a 2,000-level chain to
prove the recursion depth is fine for any realistic menu.

### Why is recursion safe here?

Recursion over user data is a classic way to hang a server: a cycle in the
data becomes infinite recursion. The evaluator checks `IsOnCycle(page)`
*before* recursing into the parent. If the page is on a cycle it returns
immediately. If it is not, walking up must reach either a root or a page
that *is* on a cycle, and that page returns without recursing. Either way
the recursion terminates. The comment in the code says exactly this,
because the next reader will wonder.

### Why does `IsOnCycle` distinguish "on" from "below"?

Given `/a → /b → /a` and `/c → /a`, page `/c` is not *on* the cycle, it
hangs *below* it. Reporting `/c` as `CyclicHierarchy` would send whoever
investigates to the wrong page. Reporting it as `ParentDenied` points at its
parent, which points at the cycle. Good reasons lead you to the cause.

### Why throw for duplicate ids but not for orphans?

An orphan is bad *data*: it came from the database and the evaluator must
cope. Duplicate ids mean the *caller* built a wrong list, which is a bug in
our code. Bugs should be loud; data problems should be safe.

## 7.4 The tests

The test data builders keep each test focused on what is different about it:

<!-- include: tests/Sentinel.Domain.Tests/TestData.cs -->

And the tests themselves. Read the names first: together they are the
specification.

<!-- include: tests/Sentinel.Domain.Tests/Access/PageAccessEvaluatorTests.cs -->

Two of these tests were wrong the first time they were written, and the
evaluator was right. The first draft of
`Wildcard_grant_does_not_cover_the_parent_permission_itself` used the
standard tree, where `/reports` requires `reports.view`, and asserted that
`reports.*` would be refused there. But `reports.*` *does* cover
`reports.view`. The failing test was a misunderstanding of my own rule,
caught in seconds because the rule was written down and the test was
precise. The comment in the test records it. This happens in timed tests
too: when a test you wrote fails, re-read the requirement before you touch
the code.

## 7.5 The same problem in 40 minutes

In the test you will not have `Result`, `Permission` or a page tree class.
You will have whatever input types the scenario gives you. The *shape* of
the answer transfers directly:

1. A comment with the requirements and your edge-case decisions.
2. Index the input once (`Dictionary<string, Page>` by normalised path).
3. One small function per rule (`ParentAllows`, `RolesAllow`).
4. Memoise per user, guard against cycles with a `visiting` set.
5. A test per decision.

Kata 1 (`katas/PageAccessKata`, Chapter 25) is exactly that, with a model
answer of about a hundred lines. Do it against the clock at least twice.
