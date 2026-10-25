# Sentinel — a hands-on preparation book for the Fullstack .NET / Angular skill test

This repository is the companion code for the ebook in [`book/`](book/)
(built copies in [`book/dist/`](book/dist/): PDF, EPUB, DOCX, HTML).

**Sentinel** is an account-security and access portal for a mid-sized company:
users, roles and wildcard permissions; a tree of pages that each user may or
may not open; and a sign-in history that is analysed for signs of a
compromised account. It was chosen because one realistic product exercises
all three areas of the skill test:

| Skill-test scenario | Where Sentinel practises it |
|---|---|
| Debugging a problem with limited information (C#) | Two reproducible incidents on branches `lab/SUP-4172-sign-in-times` and `lab/SUP-4219-all-clear`; Chapters 21–24; `docs/templates/` |
| Check which pages users are authorized to access (C#) | `PageAccessEvaluator` and its 30+ tests; Chapters 6, 7, 10; `katas/PageAccessKata` |
| Show a user when their account may be compromised (Angular) | `CompromiseDetector` on the server; the account security page in `client/sentinel-web`; Chapters 8, 14–20; drill branch `drill/angular-security-start` |

The book does not contain, and does not attempt to guess, the test's actual
scenarios. It builds the skills the prep guide names, on a project of its own.

## Architecture

Clean Architecture with CQRS and vertical slices, SOLID throughout:

```
src/
  Sentinel.Domain          entities, value objects, the two rule engines. No dependencies.
  Sentinel.Contracts       DTOs: the wire format. No dependencies.
  Sentinel.Application     CQRS primitives, a hand-written dispatcher, pipeline behaviours,
                           one folder per use case (Users/CreateUser/, Security/GetAccountSecurityStatus/, ...)
  Sentinel.Infrastructure  EF Core 10 + SQL Server, configurations, migrations, seeding
  Sentinel.Api             minimal-API endpoint modules, Result -> ProblemDetails, auth
tests/                     Domain, Application (SQLite), Api (in-process), Architecture
client/sentinel-web/       Angular 22: standalone, zoneless, signals, Vitest
katas/                     timed exercises with starters and model answers
docs/templates/            investigation log, engineering write-up, customer update
book/                      the ebook sources and build script
```

No MediatR, no AutoMapper, no repositories, no UI component library.

## Running it

Prerequisites: .NET 10 SDK, Node 22+ (24 recommended), SQL Server LocalDB (or
any SQL Server; set `ConnectionStrings:Sentinel`).

```powershell
# API on http://localhost:5080 — creates, migrates and seeds the database on first run
dotnet run --project src/Sentinel.Api

# Angular on http://localhost:4200 (proxies /api to the API)
cd client/sentinel-web
npm install
npm start
```

Pick a user from the switcher in the top right. Each seeded user tells one story:

| User | Story |
|---|---|
| Ada Okafor | Administrator; a new phone (Low) |
| Bola Adeyemi | Lagos then London forty minutes later (High) |
| Chen Wei | Eight failed sign-ins from Frankfurt (Medium) |
| Dana Smith | A real trip to London on a new laptop (Medium + Low) |
| Emeka Nwosu | Finance Manager explicitly denied the ledger export |
| Femi Bello | Auditor: may read users, not change them |
| Grace Eze | Suspended: public pages only |
| Tunde Bakare | 500+ scripted sign-ins, then a successful attack (High) |

To start again from a clean database: `dotnet ef database drop --project src/Sentinel.Infrastructure -f`.

## Tests

```powershell
dotnet test                                        # 179 back-end tests
cd client/sentinel-web; npx ng test --watch=false  # 62 Angular tests
dotnet test katas/Katas.slnx --filter "Category!=Starter"   # kata model answers
```

## Building the book

```powershell
node book/tools/build.mjs     # needs pandoc; Chrome or Edge for the PDF
```

Code listings are pulled from the source files at build time, so the book
always shows the code that is in the repository.

## Branches

| Branch | Purpose |
|---|---|
| `main` | The finished application |
| `lab/SUP-4172-sign-in-times` | Lab 1 (Chapter 22): reproduce and fix a time-zone bug |
| `lab/SUP-4219-all-clear` | Lab 2 (Chapter 23): reproduce and fix a missed attack |
| `drill/angular-security-start` | Chapter 25: rebuild the security page components against the clock |

## Licence

Code: MIT. Book text © 2026 Benjamin Fadina.
