# Chapter 26 · Test Day

## 26.1 The day before

- [ ] Open the Qualified practice environment for ten minutes (§1.3). Note the Angular version.
- [ ] Do Kata 1 once more, fast. Do not learn anything new.
- [ ] Re-read §21.5 (the debugging timetable) and the rubric in §25.5.
- [ ] Decide which scenario you will do first. Many people prefer the one they fear most, while fresh.
- [ ] Set the 47-hour alarm the prep guide recommends when you start the 48-hour window.

## 26.2 Before each scenario

- [ ] Note whether **AI is allowed**. Two scenarios: no, and it disqualifies you. One: yes, with citation.
- [ ] Start your own timer alongside the platform's, with your timetable's checkpoints.
- [ ] Open a scratch file for notes; you will paste from it into your submission.

## 26.3 Inside the authorisation scenario (40 min)

1. Requirements as a numbered comment.
2. Edge cases as a table in the comment, each with a decision.
3. Index the input; one function per rule; a cycle guard if there is a hierarchy.
4. Run the provided tests early and often.
5. A test per edge-case decision.
6. Names, constants, the "Decisions" comment. If something advanced is beyond the time, use what you know and **say so in a comment**.

## 26.4 Inside the debugging scenario (35 min)

1. Copy the report; facts, assumptions, unknowns; questions for the reporter.
2. Reproduce at the lowest level.
3. Hypotheses, cheapest test first. Cite every source.
4. At minute 22: found it → test, fix; not found → write up what you ruled out.
5. Ten minutes for two write-ups. Protect this time.

## 26.5 Inside the Angular scenario (25 min)

1. Sketch the component tree in a comment: container, children, inputs, outputs.
2. Get correct data on screen first.
3. Dates: strict parsing, the user's zone, `Intl`, relative time with a passed-in "now".
4. Edge cases: empty, loading, error, unknown values, singular/plural.
5. If you use AI, cite it in a comment and say what you changed.

## 26.6 Citing, in practice

```csharp
// Two-pointer sliding window adapted from:
// https://learn.microsoft.com/... (or the Stack Overflow answer's permalink)
```

```typescript
// Suggested by an AI assistant (2026-10-26) for Intl.RelativeTimeFormat units;
// changed to take `now` as a parameter for testability.
```

## 26.7 After

Woven promises detailed feedback whatever the outcome. Read it against the
rubric in §25.5, find the two lowest lines, and practise those. Whatever the
result, you will have built, tested and debugged a real application, and
that stays with you.
