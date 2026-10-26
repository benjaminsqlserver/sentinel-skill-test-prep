# Part V · Rehearsal {.unnumbered}

# Chapter 25 · Katas and Timed Drills

Reading about a timed test does not prepare you for one. This chapter is a
practice schedule: four exercises, each against the clock, each scored with
a rubric built from the test's own criteria.

## 25.1 The exercises

| # | Exercise | Time | Where | Skill |
|---|---|---|---|---|
| 1 | Page access kata | 40 min | `katas/PageAccessKata` | Scenario 2 |
| 2 | Access log kata | 25 min | `katas/AccessLogKata` | Fundamentals: parsing, dictionaries, edge cases |
| 3 | Lab 1 and Lab 2 | 35 min each | `lab/*` branches | Scenario 1 |
| 4 | Security page drill | 25 min | `drill/angular-security-start` | Scenario 3 |

Do each at least twice, a few days apart. The second attempt is where the
habits form.

## 25.2 Kata 1: which pages may each user open?

<!-- include: katas/PageAccessKata/README.md -->

<!-- include: katas/PageAccessKata/Model.cs -->

The tests, shared by your starter and the model answer:

<!-- include: katas/PageAccessKata.Tests/PageAccessCheckerTests.cs -->

Running one test class against two implementations is the Liskov
substitution principle used as a teaching device: anything that satisfies
`IPageAccessChecker` must pass the same tests.

**Do the kata now, before reading the model answer.**

<!-- include: katas/PageAccessKata/Solution/SolutionPageAccessChecker.cs -->

What to compare with your attempt:

- Is there a "Decisions" comment, and does it cover every edge case in the brief?
- Is the input indexed once (`Dictionary`) rather than searched in a loop?
- Are the rules in small named functions (`ParentAllows`, `RolesAllow`)?
- Is the cycle handled, and how? (`visiting` set.)
- Is anything hard-coded that should be a named constant? (`AdminRole`.)
- Did you add tests for decisions the provided tests did not cover?

## 25.3 Kata 2: summarise an access log

<!-- include: katas/AccessLogKata/README.md -->

<!-- include: katas/AccessLogKata/Model.cs -->

<!-- include: katas/AccessLogKata.Tests/AccessLogSummariserTests.cs -->

<!-- include: katas/AccessLogKata/Solution/SolutionAccessLogSummariser.cs -->

The patterns to take away: **parse into a typed record or reject**, so the
aggregation never sees a string; `NumberStyles.None` and
`CultureInfo.InvariantCulture` so that `-5`, `1,000` and a French machine's
decimal comma do not surprise you; `long` for sums that can outgrow `int`;
and `GetValueOrDefault(key) + 1` as the idiomatic dictionary counter.

## 25.4 The Angular drill

```powershell
git switch drill/angular-security-start
cd client/sentinel-web
npx ng test --watch=false     # 12 failures: your target
```

<!-- include: docs/labs/DRILL.md -->

## 25.5 The rubric

Score each attempt honestly, 0 to 2 per line (0 absent, 1 partly, 2
clearly). The lines are the prep guide's criteria, made concrete.

**Coding (katas, drill)**

| Criterion | 2 points looks like |
|---|---|
| Core requirements | Every rule in the brief is implemented and visibly traceable to the code |
| Edge cases | Each listed edge case has a decision, a comment and a test |
| Names | A reader could guess each function's body from its name |
| Structure | One entry point; helpers of 5–20 lines; no magic strings or numbers |
| Tests | At least three tests beyond those provided, each for a decision |
| Comments | A "Decisions"/trade-offs comment; no comments that restate the code |
| (Angular) Passing data | Inputs down, outputs up, one container with the side effects |
| (Angular) Dates | Parsed strictly, formatted in the user's zone, relative time testable |

**Debugging (labs)**

| Criterion | 2 points looks like |
|---|---|
| Gathering | Reproduced at the lowest level; evidence before conclusions |
| Research | Precise searches, primary sources, every source cited |
| Documenting | A log a colleague could continue from |
| To engineers | Cause, fix, why missed, follow-ups, in under a page |
| To non-engineers | Answers the reader's worry first; one clear action; no jargon |

Fourteen or more out of sixteen on the coding rubric, and nine or more out
of ten on the debugging one, and you are ready.
