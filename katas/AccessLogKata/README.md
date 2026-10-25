# Kata 2 — Summarise an access log (25 minutes)

Warm-up practice for the fundamentals the prep guide lists: iterating,
dictionaries, converting strings to numbers and dates, and handling input
that is not what you expected.

## The brief

Each line of the log looks like this:

```
2026-10-24T08:00:00Z,ada,/reports/finance,ALLOW,142
```

timestamp (ISO 8601, must include a zone), user, path, decision (`ALLOW` or
`DENY`, any case), and the response time in milliseconds (a non-negative
integer).

Write `Summarise(IEnumerable<string> lines)` returning:

- per user: allowed count, denied count, and **average** response time (ms, as a `double`);
- the path with the most denials (ties: alphabetical first; none: `null`);
- the 1-based line numbers of every malformed line, which are otherwise ignored.

Blank lines and lines starting with `#` are comments, not errors.
User names are case-insensitive (`Ada` and `ada` are one user, reported in
lower case).

```powershell
dotnet test katas/Katas.slnx --filter "Category=Starter&FullyQualifiedName~AccessLog"
```
