# Kata 1 — Which pages may each user open? (40 minutes)

A practice problem in the *shape* of the skill test's second scenario. It is
not the test's problem (that is confidential and unknown to this book); it
exercises the same muscles: modelling, iteration, dictionaries, edge cases,
naming, tests and comments for a reviewer.

## The brief

You are given users and pages.

- A **user** has a name and a list of role names.
- A **page** has a path (`/reports/finance`), an optional parent path, and a
  list of role names that may open it.

Return, for every user, the paths of the pages they may open, sorted
alphabetically.

Rules:

1. A page with **no required roles** may be opened by any user.
2. A page with required roles may be opened by a user holding **any one** of them.
3. A user may only open a page if they may also open its **parent** (and so on, up to the root).
4. The role `admin` may open every page that exists.
5. Role names are **case-insensitive**. Paths are compared case-insensitively and
   without a trailing slash; return them as given in the page list.

Edge cases the reviewer will look for (decide, implement, and comment):

- a page whose parent does not exist;
- two pages with the same path;
- a user with no roles, or a `null` role list;
- a parent cycle (`/a` → `/b` → `/a`);
- an empty list of users or pages.

## Working it

```powershell
# Your attempt: implement Starter/StarterPageAccessChecker.cs, then
dotnet test katas/Katas.slnx --filter "Category=Starter"

# The model answer's tests (always green):
dotnet test katas/Katas.slnx --filter "Category!=Starter"
```

Set a 40-minute timer. Write a comment at the top of your class listing the
decisions you made about each edge case — that comment is worth as much as
the code. Then compare with `Solution/SolutionPageAccessChecker.cs` and
Chapter 25.
