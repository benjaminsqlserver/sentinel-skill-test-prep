# Chapter 13 · Testing the Back End

Sentinel has four test projects, one per kind of question.

| Project | Question it answers | Speed |
|---|---|---|
| Domain.Tests | Are the rules right? | ~1 s for 127 tests |
| Application.Tests | Do the slices do the right thing with a real database? | ~15 s |
| Api.Tests | Does HTTP behave: status codes, auth, the wire format? | ~15 s |
| Architecture.Tests | Does the code still obey its own structure? | ~3 s |

## 13.1 Domain tests

You have seen the evaluator and detector tests in Chapters 7 and 8. The
remaining domain tests follow the same habits: a `[Theory]` for every rule
that is a table, and a test on each side of every boundary.

<!-- include: tests/Sentinel.Domain.Tests/Access/PermissionTests.cs -->

<!-- include: tests/Sentinel.Domain.Tests/Users/UserTests.cs -->

## 13.2 Handlers against SQLite

<!-- include: tests/Sentinel.Application.Tests/TestHost.cs -->

The host runs the real dispatcher, the real pipeline and the real EF Core
model, over an in-memory SQLite database that lives as long as the
connection is open. Each `Send` gets its own DI scope, as an HTTP request
would, so a test cannot accidentally pass because two operations shared a
change tracker.

<!-- include: tests/Sentinel.Application.Tests/SecuritySliceTests.cs -->

Two tests here are labelled as regression tests for the incidents in
Part IV. On the lab branches they are deleted, because the point of a lab
is that the bug shipped: the test did not exist.

<!-- include: tests/Sentinel.Application.Tests/PageAccessSliceTests.cs -->

## 13.3 The API in process

<!-- include: tests/Sentinel.Api.Tests/SentinelApiFactory.cs -->

`WebApplicationFactory<Program>` hosts the real API in memory. The factory
swaps SQL Server for SQLite (removing both the options and EF Core 9+'s
`IDbContextOptionsConfiguration<T>` registrations; removing only the first
leaves SQL Server configured), turns off start-up migration, and seeds.

<!-- include: tests/Sentinel.Api.Tests/ApiTests.cs -->

`Every_timestamp_on_the_wire_is_utc_with_a_z` is the test that would have
prevented Chapter 22. It reads the raw JSON, not a deserialised object,
because deserialising would hide the very thing it is checking.

## 13.4 Architecture tests

<!-- include: tests/Sentinel.Architecture.Tests/DependencyRuleTests.cs -->

A diagram in a wiki can be ignored. A red build cannot. These tests also
check two conventions: handlers are `internal` (only the dispatcher calls
them) and every request has exactly one handler (a missing handler is
otherwise a run-time exception on first use).

## 13.5 What not to test

Do not test EF Core, ASP.NET or the framework's JSON serialiser. Test *your*
decisions on top of them: that the converter makes dates UTC, not that EF
can save a row. Do not write a test per getter. Do write a test for every
decision you would have to explain to a reviewer.
