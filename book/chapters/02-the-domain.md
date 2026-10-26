# Chapter 2 · The Sentinel Domain

Before you can model a domain you must understand it. This chapter has
almost no code. It is the chapter the others depend on, because every
decision in the code is downstream of the vocabulary set here.

## 2.1 What Sentinel is

Sentinel is the internal portal of a mid-sized company. Employees sign in
to it to reach reports, billing, the support desk and administration
screens. It answers two questions, constantly:

1. **May this person open this page?** — the *access* half.
2. **Is this person really this person?** — the *account security* half.

Each half has a rule engine in the domain layer. `PageAccessEvaluator`
answers the first (Chapter 7); `CompromiseDetector` answers the second
(Chapter 8). Everything else in the system exists to feed those two engines
data and to show their verdicts to people.

## 2.2 The actors

| Actor | What they do in Sentinel |
|---|---|
| **Employee** | Signs in, uses the pages their roles allow, checks their own account security |
| **Administrator** | Manages users, roles and pages; investigates "why can't X see Y?" |
| **Auditor** | May look at users and every report; may change nothing |
| **Support agent** | Fields tickets from employees; forwards bugs to engineering |
| **Identity provider** | An external system that performs the actual sign-in and reports each attempt to Sentinel |

The last actor is important and easily forgotten: Sentinel does not check
passwords. It *records* sign-in attempts reported by an identity provider
(Azure AD, Okta, Keycloak...) and analyses them. In development, you play
the identity provider with the `.http` file or `curl`.

## 2.3 The vocabulary

**Permission.** A named capability, written as dot-separated lower-case
segments: `reports.finance`, `admin.users.manage`. A *grant* may end in a
wildcard: `reports.*` covers every permission beneath `reports`, and `*`
covers everything. A page always *requires* a concrete permission, never a
wildcard.

**Role.** A named bundle of permissions: *Finance Manager* grants
`reports.view`, `reports.finance`, `reports.finance.*` and `billing.*`.

**User.** A person, with an email address, a display name, an IANA time zone
such as `Africa/Lagos`, a status (*Active* or *Suspended*), a set of roles,
and optional per-user **overrides**: a direct *Allow* or *Deny* of one
permission.

**Page.** A screen in the portal, identified by its path (`/reports/finance`).
Pages form a tree: every page except the roots has a parent. A page may
require a permission, require only that you are signed in, or be **public**.

**Sign-in attempt.** One attempt to sign in, successful or failed, with its
time (UTC), IP address, approximate location and device.

**Finding.** One reason to think an account may be compromised: a brute-force
burst, impossible travel, a new country, a new device. Each has a
**severity** (Low, Medium, High).

**Assessment.** The findings for one account and the worst of their
severities, the **risk level**.

**Acknowledgement.** The user's "that was me" on a finding. An acknowledged
finding no longer raises the risk level.

## 2.4 The access rules

These are the rules the business agreed. They are written as numbered
statements because that is how you should write rules you are about to
implement in a timed test, too.

1. A **public** page may be opened by anyone, signed in or not.
2. Nobody else may open anything unless signed in **and active**. A
   suspended user sees public pages only.
3. A page may be opened only if its **parent** may be opened. You cannot
   reach a room through a locked door.
4. A page with **no required permission** may be opened by any active user
   (subject to rule 3).
5. A page that requires a permission may be opened if any of the user's
   grants **covers** it (rule 7), and
6. **no deny covers it.** An explicit deny beats every allow, from any role.
7. A grant `x` covers `x`. A grant `x.*` covers anything that starts `x.`,
   at any depth, but not `x` itself. A grant `*` covers everything.
8. Bad data fails **closed**: a page whose parent does not exist, or whose
   ancestry loops back on itself, may not be opened by anyone.

Rule 8 is the one people forget, and the one a reviewer looks for. Data
that "cannot happen" happens: an import goes wrong, a test builds a tree by
hand, a future cache serves a stale parent. The access rules must never
throw and must never let someone in by accident.

## 2.5 The compromise rules

Agreed with the security team, with thresholds that can be tuned:

| Finding | Rule | Severity |
|---|---|---|
| Brute force | At least 5 failed attempts within any 15-minute window | Medium; **High** if a success follows within the window |
| Impossible travel | Two consecutive *successful* sign-ins more than 500 km apart, implying a speed above 900 km/h | High |
| New country | A successful sign-in from a country this user has never signed in from successfully | Medium |
| New device | A successful sign-in from a device this user has never used | Low |

And the conditions around them:

- Findings are reported only for the last **30 days**, but the previous
  **90 days** count as "seen before".
- A user's **first ever** successful sign-in is never "new" anything; there
  is nothing to compare it with.
- A **failed** sign-in from a new country does not make that country
  "known".
- Attempts **after now** (clock skew upstream) are ignored.
- The risk level is the highest severity among **unacknowledged** findings.
  *Medium* and above are "compromise suspected": the UI interrupts the user.

## 2.6 The people in the seed data

Every developer database is seeded with eight people, each of whom tells one
story. You will meet them throughout the book; this table is worth a
bookmark.

| User | Role | Zone | Story | Expected risk |
|---|---|---|---|---|
| Ada Okafor | Administrator | Africa/Lagos | Ordinary; one new phone | Low |
| Bola Adeyemi | Finance Manager | Africa/Lagos | Lagos, then London 40 minutes later | High |
| Chen Wei | Operations | Asia/Singapore | 8 failures from Frankfurt in 9 minutes, none succeeded | Medium |
| Dana Smith | Support Agent | America/New_York | A real trip to London on a new laptop | Medium |
| Emeka Nwosu | Finance Manager + **deny** `reports.finance.export` | Europe/London | A contractor | None |
| Femi Bello | Auditor | Africa/Lagos | Reads users, changes nothing | None |
| Grace Eze | Support Agent, **suspended** | Africa/Lagos | Left the company | None |
| Tunde Bakare | Operations | Africa/Lagos | 500+ scripted sign-ins; then a successful attack from Amsterdam | High |

## 2.7 What we are deliberately not building

Real identity platforms do much more. Sentinel does not authenticate
passwords, send email, lock accounts, or keep an audit log. It uses a
development-only authentication scheme (Chapter 12). The pages behind the
menu (reports, billing) are placeholders. Every one of those omissions is a
place where the book would grow without teaching anything new about the
three scenarios.
