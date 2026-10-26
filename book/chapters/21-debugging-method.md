# Part IV · Debugging with Limited Information {.unnumbered}

# Chapter 21 · A Method for Debugging with Limited Information

The first scenario gives you a bug report from customer support and tells
you up front that you will not have everything you need. That is not a
trick; it is every real bug report. This chapter is a method you can run
under a 35-minute timer, without AI, and that leaves a written trail a
reviewer can follow.

## 21.1 The five questions the reviewer is asking

From the prep guide's list, rephrased as what they will look for in your
submission:

1. **Gathering:** did you reproduce it, read the relevant code path, and
   use evidence (logs, data, the debugger) before deciding?
2. **Research:** when you looked something up, was it the right thing, from
   a good source, and did you cite it?
3. **Documenting:** could a colleague pick up your investigation from what
   you wrote?
4. **To engineers:** is the root cause precise, the fix justified, and the
   gap that let it ship named?
5. **To non-engineers:** does the customer-facing note answer *their*
   question, in *their* words?

## 21.2 The loop

```text
  Read ──▶ Separate facts from assumptions ──▶ Reproduce ──▶ Hypothesise
   ▲                                                            │
   │                                                            ▼
 Write up ◀── Verify ◀── Fix + regression test ◀── Test one hypothesis at a time
```

### Read, and separate facts from assumptions

Copy the ticket into your log verbatim. Then make three columns: what you
**know** (and how), what you **assume**, what you **don't know**. Support
tickets mix observation with interpretation: "the times are wrong" is an
observation; "someone is in my account" is an interpretation. Keep them
apart, because the interpretation may be a second, separate problem.

### Write the questions you would ask

You cannot ask the customer during the test. Write the questions anyway,
phrased so support could forward them unchanged:

- What time zone are you in, and what is your computer's clock set to?
- Roughly when did you first notice? Has it always been like this?
- Does it happen for every entry, or only some?
- What exactly did you see? A screenshot, or the text copied?

These show the reviewer you know which facts are missing, and often the
act of writing them tells you where to look.

### Reproduce before you theorise

A bug you can reproduce is half fixed. A bug you cannot reproduce is one
where every fix is a guess. Reproduce at the **lowest level that still
shows the problem**: if the API response is wrong, you do not need the
browser; if one function's output is wrong, you do not need the API.
`curl` and the `.http` file are faster than clicking.

### Hypothesise, then try to kill each hypothesis

Write each hypothesis with the evidence for and against it and the
*cheapest test that could rule it out*. Run the cheapest first. A
hypothesis you cannot test is not yet useful.

### Bisect when you know it used to work

If the ticket hints at a recent change ("since Tuesday's deploy"), the
history is evidence. `git log --oneline -- <path>` lists changes to the
code involved; `git bisect` finds the commit that broke it in a handful of
steps:

```powershell
git bisect start
git bisect bad                 # this commit is broken
git bisect good <older-sha>    # this one was fine
# build/test, then: git bisect good | git bisect bad ... until it names the commit
git bisect reset
```

### Fix the cause, then prove it with a test that fails first

Write the regression test **before** the fix, run it, and watch it fail for
the reason you expect. A test that has never failed has not proved
anything. Then fix, and watch it pass.

## 21.3 Tools for gathering evidence in .NET

| Tool | Use | Example |
|---|---|---|
| The debugger | Inspect values where you suspect the problem | A breakpoint in a handler; watch `occurredAtUtc.Kind` |
| Structured logs | What happened, in what order, how long | `LoggingBehavior`: `GetAccountSecurityStatusQuery succeeded in 12.3 ms` |
| EF Core's SQL log | What query actually ran | `"Microsoft.EntityFrameworkCore.Database.Command": "Information"` in appsettings |
| `ToQueryString()` | The SQL for one LINQ query, without running it | `query.ToQueryString()` in the debugger's Immediate window |
| `curl` / `.http` | The exact bytes on the wire | `curl -s .../api/me/security -H "X-Demo-User: ..."` |
| Browser dev tools | What the client received and did | Network tab → Response; Console for logged errors |
| `git log`, `git blame`, `git bisect` | When and why the code changed | `git log -p -- src/.../SentinelDbContext.cs` |
| A unit test | A tiny, repeatable reproduction | Call the function with the customer's data |

When the scenario's environment has no debugger, `Console.WriteLine` is a
perfectly respectable tool. Print the input and output of the function you
suspect, with labels, and delete the lines before you submit.

## 21.4 Research, and citing it

You may search; you must cite. Good habits:

- Search for the **precise** symptom: `DateTimeKind Unspecified System.Text.Json no Z`,
  not `dates wrong .NET`.
- Prefer **primary sources**: Microsoft Learn, the Angular docs, MDN, the
  library's GitHub issues. A Stack Overflow answer is a pointer to them, not
  a substitute.
- Record each source in your log with one line on what it told you. Copied
  code gets a link in a comment.

```csharp
// EF Core value converter approach, per Microsoft Learn: "Value Conversions",
// https://learn.microsoft.com/ef/core/modeling/value-conversions
```

## 21.5 A 35-minute timetable

| Minutes | Activity | Output |
|---|---|---|
| 0–3 | Read; copy the ticket; facts / assumptions / unknowns; questions for the reporter | Log sections 1–3 |
| 3–10 | Reproduce at the lowest level | Log section 6 |
| 10–22 | Hypotheses, cheapest test first; find the cause | Log sections 4–5, 7 |
| 22–25 | Regression test (red), fix (green) | Code |
| 25–35 | Engineer write-up; customer reply | Two short documents |

**At minute 22, if you have not found the cause, stop investigating.**
Write up what you know, what you ruled out and how, and what you would do
next. That is a complete, professional answer to "the bug is not found
yet", and it scores on four of the five criteria. A late, unexplained fix
scores on one.

## 21.6 The templates

They are in `docs/templates/`. Copy them at the start of each lab.

<!-- include: docs/templates/investigation-log.md -->
