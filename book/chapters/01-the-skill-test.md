# Part I · Orientation {.unnumbered}

# Chapter 1 · Reading the Skill Test Like an Engineer

Most candidates read a prep guide once, skim the bold text, and open the
first scenario. That is like reading the acceptance criteria of a ticket
after you have written the code. This chapter reads the guide the way you
would read a specification, and turns each sentence into a habit you can
practise.

## 1.1 What the guide actually says

Strip the guide to its load-bearing sentences and you get this:

1. **Three scenarios, each with its own timer**, inside a 48-hour window.
   The timers are short (35, 40 and 25 minutes) and they are per scenario.
   You can rest between scenarios; you cannot rest inside one.
2. **Research is allowed and should be cited.** Looking things up is "not a
   negative"; using a snippet without a link is.
3. **AI is forbidden in two scenarios and encouraged in one.** Breaking the
   rule disqualifies you.
4. **The environment is Qualified**, an online IDE. The guide recommends
   ten minutes trying it first.
5. **Each scenario lists what reviewers look for.** Those lists are the
   marking scheme. Treat them as such.

The guide also says something about the coding scenario that is easy to
skim past: if a more advanced concept is beyond you in the time, "use what
you know and leave a comment for the reviewer". That advice tells you the reviewer is a person reading your code with
empathy, not a test runner counting green ticks. A working simple solution
with a clear comment beats an ambitious broken one, every time.

## 1.2 The marking scheme, scenario by scenario

### Debugging a problem with limited information (35 minutes, C#, no AI)

You receive a bug report from customer support and "will not have all of
the information you need". Reviewers look at:

| Criterion | The habit that satisfies it | Where this book practises it |
|---|---|---|
| How do you gather information about a problem in code? | Reproduce first; read the code path end to end; use logs and the debugger before you form opinions | Chapter 21, both labs |
| How do you research issues? | Search precisely, read primary documentation, cite every source | §21.6, §22.5 |
| How do you document your learning so others can benefit? | An investigation log written as you go | `docs/templates/investigation-log.md` |
| How do you communicate technical concepts to other engineers? | A blameless write-up: cause, fix, why it was missed, follow-ups | Chapter 24 |
| How do you communicate technical concepts to non-engineers? | A reply that answers the reader's actual worry, first, in their words | Chapter 24 |

Notice that **three of the five criteria are about writing**. Candidates
who find the bug in twenty minutes and spend the last fifteen on a
two-line note leave most of the marks on the table. Chapter 21 gives you a
timetable for the 35 minutes that protects the writing time.

### Check which pages users are authorized to access (40 minutes, C#, no AI)

A coding scenario with provided tests. Reviewers look at:

| Criterion | The habit |
|---|---|
| Does your code address the core requirements? | Restate the requirements as a checklist in a comment before coding |
| Did you handle edge cases? | List them *before* coding; decide each one; write a test per decision |
| Are your variable and function names clear? | Name by intent: `CanOpen(page)`, not `Check(p)` |
| Is the structure good? Functions right-sized? Anything hard-coded? | One public entry point, small private helpers, constants named |
| Did you add helpful tests beyond the ones provided? | Every edge-case decision gets a test |
| Do you leave helpful comments explaining trade-offs? | A short "Decisions" comment at the top of the class |

The domain is authorisation: users, pages, and who may open what. Sentinel
is built around exactly that problem, at production scale (Chapters 6 and 7),
and Kata 1 in Chapter 25 is a timed exercise in the same shape.

### Show a user when their account may be compromised (25 minutes, Angular, AI allowed)

Reviewers look at:

| Criterion | The habit |
|---|---|
| Core requirements | Get something correct on screen in the first ten minutes |
| How do you pass information between components? | Inputs down, outputs up; a container that owns data and side effects |
| Edge cases | Empty, loading, error, unknown values, the boundary of every rule |
| How do you display and format dates? | UTC on the wire; parse strictly; format with `Intl` in the user's zone; relative time with an injectable "now" |
| Names, structure, comments | As above |

Twenty-five minutes is very little. The guide says the scenario "does not
require expert-level knowledge of Angular", and the environment is
preconfigured. The candidates who do well are the ones who do not have to
think about *how* to pass data to a child component or how to format a date
in a zone, because they have done it twenty times. Chapters 17 and 18
exist so that you have.

## 1.3 Ten minutes in Qualified

Do what the guide says: open the Qualified practice environment before the
real thing and spend ten minutes in it. Specifically, find out:

- how to run the tests, and how long a run takes;
- where console output appears (you will want `Console.WriteLine` or
  `console.log` when a test fails mysteriously);
- whether you can add a test file or only edit the provided ones;
- which language version and framework version it runs. **Look for the
  Angular version** in particular: if it predates Angular 17 you will be
  writing `*ngIf` and `@Input()` rather than `@if` and `input()`. Appendix C
  translates everything in this book into the older syntax.

## 1.4 A timetable for each scenario

Timers make people skip the parts that earn marks. Decide your timetable in
advance and keep to it.

**Debugging (35 min):** 3 read the report and write down what you know and
don't · 7 reproduce · 12 investigate and fix · 3 regression test ·
10 write-ups (engineer + non-engineer). If you have not found the cause at
minute 22, stop and write up what you have ruled out: a clear record of
an unfinished investigation is worth more than a silent half-fix.

**Authorisation (40 min):** 5 read, restate requirements, list edge cases
in a comment · 20 implement · 10 add tests for each edge case · 5 tidy
names and write the "Decisions" comment.

**Angular (25 min):** 3 read and sketch the component tree · 10 get the
data on screen, correctly · 7 edge cases (empty, unknown, date formatting) ·
5 names, comments, AI citation.

## 1.5 What this chapter asks you to do

Nothing yet, except decide when you will sit the test and work backwards.
The rest of the book is built so that by test day each of the habits above
is something you have done, not something you have read about.
