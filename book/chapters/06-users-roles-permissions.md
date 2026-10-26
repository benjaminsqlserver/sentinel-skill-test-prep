# Chapter 6 · Users, Roles and Permissions

## 6.1 Permission: a value object with one interesting method

<!-- include: src/Sentinel.Domain/Access/Permission.cs -->

`Create` normalises (trim, lower-case) and validates against one regular
expression. The pattern reads: one or more segments of `[a-z0-9_-]`,
separated by dots, optionally ending in `.*`; or the single wildcard `*`.
`[GeneratedRegex]` compiles it at build time, so there is no start-up cost
and no risk of a typo surfacing only at run time.

`Covers` is the rule from §2.4, rule 7, and nothing else:

| Grant | Required | Covers? | Why |
|---|---|---|---|
| `reports.finance` | `reports.finance` | yes | exact match |
| `reports.*` | `reports.finance.export` | yes | any depth |
| `reports.*` | `reports` | **no** | "everything under" ≠ "the thing itself" |
| `reports.*` | `reportsx.finance` | **no** | the prefix includes the dot |
| `reports.finance` | `reports.finance.export` | **no** | a concrete grant has no children |
| `*` | anything | yes | superuser |

The fourth row is the one that catches people. Implementing the wildcard as
`required.StartsWith("reports")` would let `reports.*` cover `reportsx`.
Keeping the trailing dot in the prefix (`Value[..^1]` drops only the `*`)
makes the comparison segment-aware.

::: tip
When a rule has a table like this, put the table in your tests as a
`[Theory]`. `PermissionTests.Covers` in Chapter 13 is exactly this table.
:::

## 6.2 Role

<!-- include: src/Sentinel.Domain/Roles/Role.cs -->

A role grants each permission at most once; granting twice is a `Conflict`,
revoking something not granted is `NotFound`. The field `_permissions` is
not `readonly` because EF Core assigns a new list when it reads the column
(Chapter 11). The public `Permissions` property is a read-only view, so
nothing outside the aggregate can add to the list behind its back.

## 6.3 User

<!-- include: src/Sentinel.Domain/Users/User.cs -->

Points worth studying:

- **Time zones are validated at creation** with
  `TimeZoneInfo.TryFindSystemTimeZoneById`, which on .NET 6+ accepts IANA
  names (`Africa/Lagos`) on every operating system. A user with a bad zone
  would break every date on their security page; reject it at the door.
- **Every timestamp must be UTC.** `EnsureUtc` throws for any other `Kind`,
  because a local time slipping into the domain is a bug, not a user error.
  You will see what happens when that promise breaks on the way *out* of
  the database in Chapter 22.
- **One override per permission.** `SetOverride` replaces rather than adds:
  allowing and denying the same thing at once has no meaning.
- **Suspension carries a reason.** Suspending twice is a conflict, not a
  silent success, so an administrator who thinks they are suspending an
  active user learns that someone else already did.

## 6.4 AccessSubject: flattening a user for the rules

The access rules do not need a `User`. They need three facts: is this
person active, what do they hold, what is denied to them. `AccessSubject`
is that, and nothing more:

<!-- include: src/Sentinel.Domain/Access/AccessSubject.cs -->

`For(user, roles)` accepts *every* role and keeps only the user's own. That
makes the caller simpler (pass the list you have) and puts the filtering
rule, the one thing that must not be got wrong, in one tested place.

`Has` encodes "deny wins": it is `!IsDenied && IsGranted`, in that order,
and it is false for anyone who is not active.

<!-- include: src/Sentinel.Domain/Access/PermissionEffect.cs -->
