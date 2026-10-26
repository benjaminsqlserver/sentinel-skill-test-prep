# Chapter 3 · The Architecture

## 3.1 The dependency rule

Clean Architecture is usually taught as concentric circles, which is
memorable and nearly useless, because a picture does not tell you what to do
on a Tuesday afternoon. It reduces to one mechanical rule:

> Source code dependencies point only inwards. An inner layer never names a
> type defined in an outer layer.

Everything else (ports, adapters, dependency inversion) is technique for
obeying that rule when control has to flow the other way. Sentinel has four
production layers and a contracts assembly:

```text
          ┌──────────────────────────────┐
          │  Sentinel.Api                │  Minimal API endpoints, auth, composition root
          └──────────────┬───────────────┘
                         │ depends on
          ┌──────────────▼───────────────┐
          │  Sentinel.Infrastructure     │  EF Core, SQL Server, clock, geolocation, seeding
          └──────────────┬───────────────┘
                         │ depends on
          ┌──────────────▼───────────────┐
          │  Sentinel.Application        │  Commands, queries, handlers, ports, behaviours
          └──────────────┬───────────────┘
                         │ depends on
          ┌──────────────▼───────────────┐
          │  Sentinel.Domain             │  Entities, value objects, the two rule engines
          └──────────────────────────────┘

  Sentinel.Contracts ──► referenced by Application and Api (DTOs only)
  client/sentinel-web ──► knows only the HTTP API and its own copies of the DTOs
```

**Sentinel.Domain** references nothing but the base class library. No
EF Core, no ASP.NET, no NuGet packages at all; an architecture test
(Chapter 13) fails the build if one appears.

**Sentinel.Application** references Domain and Contracts. It defines the
*ports*, interfaces such as `IApplicationDbContext`, `IClock` and
`IGeoLocator`, and contains every command, query and handler. It does not
know SQL Server exists.

**Sentinel.Infrastructure** implements the ports: the EF Core context, the
system clock, a geolocation table.

**Sentinel.Api** is the composition root. It references everything and is
referenced by nothing.

**Sentinel.Contracts** is a deliberately anaemic assembly of request and
response records: the wire format. The Angular client mirrors it by hand.

## 3.2 Why the domain has no dependencies

The two rule engines are the most valuable code in the system and the
longest-lived. Every dependency you add to them is a constraint on how they
can evolve and a cost on every test. `PageAccessEvaluator` can be unit
tested with nothing but `new`: no database, no web host, no mocks. That is
not purity for its own sake; it is why Chapter 7 has thirty tests that run
in under a second.

It is also the single most transferable lesson for the second scenario. In
a 40-minute test, write the rules as a pure function of their inputs, then
test that function. Do not let I/O anywhere near it.

## 3.3 CQRS, and what it means here

CQRS (Command Query Responsibility Segregation) is often described as
needing two databases and an event bus. It needs neither. Its essence is:

> The model you use to change state need not be the model you use to read it.

Sentinel's version, sometimes called "CQRS-lite", is the right default for a
system of this size:

- **Commands** load an aggregate with change tracking, call a method on it,
  and save. `AssignRoleCommandHandler` loads a `User`, calls
  `user.AssignRole(roleId, now)`, and lets the aggregate decide whether that
  is allowed. The handler contains almost no logic.
- **Queries** never modify anything. They read with `AsNoTracking()` and
  shape a DTO for the screen. `GetAccountSecurityStatusQueryHandler` loads
  history, runs the detector, and returns exactly what the page needs.

Both kinds are messages (`record` types) dispatched to exactly one handler.
That uniformity is what lets cross-cutting behaviour (validation, logging)
be written once (Chapter 9).

## 3.4 Vertical slices inside the layers

Inside Application, code is organised by **feature, then use case**, never
by technical kind:

```text
src/Sentinel.Application/
├── Abstractions/        Messaging/, Validation/, Ports/
├── Behaviors/           ValidationBehavior, LoggingBehavior
├── Access/HasPermission/
├── Pages/
│   ├── CheckPageAccess/     CheckPageAccessQuery.cs, CheckPageAccessQueryHandler.cs
│   ├── GetNavigation/
│   ├── GetPageAccess/
│   └── ...
├── Security/
│   ├── GetAccountSecurityStatus/
│   ├── AcknowledgeFinding/
│   └── ...
├── Roles/  └── Users/
```

A developer asked to change how findings are acknowledged opens one folder.
The alternative, top-level `Commands/`, `Handlers/` and `Validators/`
folders, scatters each use case across three places and gets worse with
every feature. An architecture test (Chapter 13) enforces the folder
convention.

Vertical slices and Clean Architecture are sometimes presented as rivals.
They are not: the layers decide *which direction dependencies point*; the
slices decide *how code is grouped within a layer*. Sentinel uses both.

## 3.5 SOLID, concretely

SOLID is easy to recite and hard to recognise. Here is where each principle
lives in this codebase, so you can point at it in an interview.

| Principle | Where you can see it |
|---|---|
| **S**ingle responsibility | Each handler does one use case. `PageAccessEvaluator` decides access; it does not load data or build menus (`PageAccessMapping` does that). |
| **O**pen/closed | Adding a use case adds a folder; nothing existing changes, because handlers and validators are discovered by `AddApplication()`. Adding a pipeline behaviour wraps every handler without editing any. |
| **L**iskov substitution | `Result<T>` is usable anywhere a `Result` is expected; `ValidationError` anywhere an `Error` is. The kata tests run one test class against two implementations of the same interface. |
| **I**nterface segregation | `IClock` has one member. `IGeoLocator` has one. The Angular `Session` exposes read-only signals, not its writable state. |
| **D**ependency inversion | Application owns `IApplicationDbContext`; Infrastructure implements it. The arrow of the source dependency points inwards even though control flows outwards. |

## 3.6 The dispatcher, and a note on MediatR

Most CQRS samples in .NET reach for MediatR. Since version 13 MediatR is
commercially licensed above a revenue threshold. We write our own
dispatcher instead: about sixty lines, no licensing question, and writing it
teaches exactly what MediatR does (Chapter 9). Swapping to MediatR or
Wolverine later is mechanical.

## 3.7 A note on repositories

We do not use the repository pattern. `DbContext` already *is* a unit of
work with repositories hanging off it; wrapping it yields either a leaky
abstraction that exposes `IQueryable` (and abstracts nothing) or a growing
interface with a method per query. The Application layer depends on
`IApplicationDbContext`, an interface it owns, which satisfies the
dependency rule while keeping LINQ.

## 3.8 What is not here, and when it would be

**Domain events.** Nothing in Sentinel reacts to a change in another
aggregate yet. When something does (say, "suspending a user acknowledges
nothing and emails security"), raise an event from the aggregate and
dispatch it after `SaveChanges`. Until then, an event bus with no
subscribers is ceremony.

**A read model.** The page access queries load the whole page tree on every
call. With tens of pages that costs microseconds. With tens of thousands,
cache the tree and invalidate it when a page changes. Measure first.
