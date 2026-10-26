# About this book {.unnumbered}

*Visual Studio 2026 · .NET 10 (LTS) · C# 14 · ASP.NET Core minimal APIs · EF Core 10 · SQL Server · Angular 22 · TypeScript 6 · Vitest*

Copyright © 2026 Benjamin Fadina. All rights reserved. Every source code
listing is released under the MIT licence and may be used freely.

Microsoft, Visual Studio, .NET, Entity Framework Core and SQL Server are
trademarks of the Microsoft group of companies. Angular is a trademark of
Google LLC. Woven, Qualified and Andela are the names of their respective
owners. No endorsement by any of them is implied, and nothing in this book
was supplied by them.

First edition, October 2026.

## A word about the test, and about honesty

This book prepares you for the **Fullstack .NET Engineer / Angular skill
test** described in the Woven prep guide used by Andela. That guide lists
three timed scenarios:

| Scenario | Time | AI | Language |
|---|---|---|---|
| Debugging a problem with limited information | 35 min | **not allowed** | C# |
| Check which pages users are authorized to access | 40 min | **not allowed** | C# |
| Show a user when their account may be compromised | 25 min | allowed (cite it) | Angular |

The guide is explicit that the scenarios are confidential, that using
someone else's solution is unacceptable, and that sharing them is not
allowed. This book respects that completely. **It does not contain the
test's scenarios, does not guess at them, and contains no "answers".** What
it contains is a real application, Sentinel, whose features happen to sit
in the same three areas, and a method for each area that you can rehearse
until it is a habit. When you sit the test, every line you write will be
your own.

Two of the three scenarios forbid AI assistance. That matters for how you
use this book: type the code yourself, run the labs without a chatbot, and
build the reflexes that the test will measure. The third scenario
encourages AI and asks you to cite it; Chapter 26 shows how.

## Who this book is for

A developer who can already write C# and has met Angular, and who wants to
walk into a timed assessment rehearsed rather than hopeful. If you are
newer to programming, Appendices A and B cover the fundamentals the prep
guide lists (iteration, classes and objects, arrays, lists and dictionaries,
converting between types) in the context of this project.

The application is built the way an enterprise system should be built:
Clean Architecture, CQRS, vertical slices and SOLID. You do not need any of
that to pass a 40-minute exercise. You do need what it teaches: small
functions with clear names, decisions written down, edge cases handled on
purpose, and tests that prove it. The architecture is the long version of
the same discipline the test rewards in miniature.

## How to use this book

Every listing in this book is complete and is the file in the repository,
inserted at build time; there are no `// ... rest of the class` elisions in
production code. File paths are given as a header above each listing,
relative to the repository root, like this:

<!-- include: src/Sentinel.Domain/Access/PermissionEffect.cs -->

Type the code. Reading code produces the feeling of understanding; typing
code produces understanding. If you clone the repository instead, at least
pause at the end of each chapter and rebuild from memory the shape of what
you just read.

The book has five parts:

- **Part I (Chapters 1–3)** reads the test like an engineer, introduces the
  Sentinel domain, and lays out the architecture.
- **Part II (Chapters 4–13)** builds the C# back end. Chapter 7 is the
  heart of the second scenario; Chapter 8 is the server side of the third.
- **Part III (Chapters 14–20)** builds the Angular front end. Chapters 17
  and 18 are the heart of the third scenario: passing information between
  components, and displaying dates.
- **Part IV (Chapters 21–24)** is the first scenario: a method for
  debugging with limited information, two reproducible incidents to
  practise it on, and how to write up what you found for engineers and for
  people who are not.
- **Part V (Chapters 25–26)** is rehearsal: timed katas and drills, a
  self-review rubric built from the test's own evaluation criteria, and a
  test-day checklist.

If your test is in three days, read Chapters 1, 7, 17, 18, 21, 22 and 26,
and do the katas in Chapter 25. If it is in three weeks, read everything,
in order.

## Getting the code

```powershell
git clone <your copy of this repository> sentinel
cd sentinel
git branch -a        # main, two lab branches, one drill branch
```

The README at the root explains how to run everything. Chapter 4 does it
step by step.
