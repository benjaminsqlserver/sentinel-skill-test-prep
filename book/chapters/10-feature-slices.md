# Chapter 10 · Feature Slices

Each use case is a folder with a request (and its validator) in one file and
its handler in another. This chapter walks through the slices that matter
for the skill test, and lists the rest.

## 10.1 A command slice: CreateUser

<!-- include: src/Sentinel.Application/Users/CreateUser/CreateUserCommand.cs -->

<!-- include: src/Sentinel.Application/Users/CreateUser/CreateUserCommandHandler.cs -->

The shape every command handler follows:

1. Turn primitives into domain values (`EmailAddress.Create`); return the error if that fails.
2. Check what needs the database (duplicate email).
3. Ask the domain to do the work (`User.Create`).
4. Save; return the id.

The comment about the unique index matters. The `AnyAsync` check is a
courtesy that turns the common case into a friendly 409; under concurrency,
two requests can both pass it, and only the database's unique index stops
the second. Never let an application-level check be the only guarantee of
uniqueness.

## 10.2 Commands that change an aggregate

<!-- include: src/Sentinel.Application/Users/RoleAssignment/RoleAssignmentHandlers.cs -->

Load, call one method, save. All the rules are in `User.AssignRole`; the
handler only checks the thing the aggregate cannot know (whether the role
exists).

## 10.3 The access slices

All three share one helper, so that "how a user becomes an `AccessSubject`"
cannot drift between screens:

<!-- include: src/Sentinel.Application/Access/AccessSubjectLoader.cs -->

**Check one page** (used by the Angular route guard on every navigation):

<!-- include: src/Sentinel.Application/Pages/CheckPageAccess/CheckPageAccessQuery.cs -->

<!-- include: src/Sentinel.Application/Pages/CheckPageAccess/CheckPageAccessQueryHandler.cs -->

**The menu** for the current user:

<!-- include: src/Sentinel.Application/Pages/GetNavigation/GetNavigationQueryHandler.cs -->

Note the edge case in the comment: a user id that no longer exists (deleted
since the token was issued) degrades to the public menu rather than failing.

**Every page with its verdict** for one user, for the admin access explorer:

<!-- include: src/Sentinel.Application/Pages/GetPageAccess/GetPageAccessQueryHandler.cs -->

The mapping from decisions to DTOs, including building the menu tree:

<!-- include: src/Sentinel.Application/Pages/PageAccessMapping.cs -->

`ToNavigationTree` handles the edge case from §7.1: a public page under a
hidden parent is promoted to the top level. The evaluator said the user may
open it, so the menu must offer a way to get there.

**Guarding endpoints with the same rules:**

<!-- include: src/Sentinel.Application/Access/HasPermission/HasPermissionQueryHandler.cs -->

## 10.4 The security slices

The status query, the history query and the acknowledge command must agree
on what the findings are, so they share an assessor:

<!-- include: src/Sentinel.Application/Security/AccountAssessor.cs -->

Read the comment above the query twice. It is the most important comment
in the back end, and Chapter 23 explains why.

<!-- include: src/Sentinel.Application/Security/GetAccountSecurityStatus/GetAccountSecurityStatusQueryHandler.cs -->

<!-- include: src/Sentinel.Application/Security/SecurityMapping.cs -->

Acknowledging a finding:

<!-- include: src/Sentinel.Application/Security/AcknowledgeFinding/AcknowledgeFindingCommandHandler.cs -->

Two edge cases are handled on purpose. Acknowledging twice succeeds
(idempotency: a double-click or a retried request must not produce an
error). Acknowledging a key the user cannot currently see fails, so a
compromised session cannot pre-acknowledge findings it is about to cause.

Recording a sign-in, the endpoint the identity provider calls:

<!-- include: src/Sentinel.Application/Security/RecordSignIn/RecordSignInCommand.cs -->

<!-- include: src/Sentinel.Application/Security/RecordSignIn/RecordSignInCommandHandler.cs -->

The request carries a `DateTimeOffset`, not a `DateTime`. A `DateTime` from
the wire cannot say which zone it is in; a `DateTimeOffset` must. (It still
accepts a string with no offset and assumes server-local time, which is a
trap Chapter 22 returns to.)

## 10.5 The remaining slices

They follow the same patterns and are in the repository:

| Feature | Slices |
|---|---|
| Users | `GetUsers` (paged, search), `GetUserById`, `GetCurrentUser`, `GetDemoUsers`, `FindUserByEmail`, `Suspension`, `PermissionOverrides` |
| Roles | `CreateRole`, `RolePermissions` (grant, revoke), `GetRoles` |
| Pages | `CreatePage`, `GetPages` |
| Security | `GetSignInHistory` |

One worth opening is `GetUsersQueryHandler`: its comment explains why the
search matches part of a display name but only a *whole* email address.
Email is stored through a value converter, which EF Core can compare but not
search inside. That is a trade-off, written down, which is what the
reviewer of your skill test wants to see whenever you choose the simpler
path.
