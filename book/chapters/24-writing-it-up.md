# Chapter 24 · Writing It Up for Engineers and for Everyone Else

Three of the five criteria in the debugging scenario are about writing. This
chapter shows what good looks like, using both labs, and gives you the
habits that produce it in ten minutes.

## 24.1 Two audiences, two questions

| | Engineers | Non-engineers |
|---|---|---|
| Their question | *What broke, why, and will it happen again?* | *Am I affected, am I safe, what do I do?* |
| Lead with | The cause, in one sentence | The answer to their worry |
| Vocabulary | Precise: file, line, type, `Kind` | Plain: "times were shown an hour early" |
| Length | As long as the cause needs | As short as possible |
| Must include | Why it was not caught; follow-ups | What to do, if anything; how to report it again |
| Must not include | Blame | Jargon, stack traces, speculation |

## 24.2 Worked example: SUP-4172 for engineers

> **SUP-4172 — Sign-in times from the database serialised without a zone and displayed one UTC offset early**
>
> **Audience:** engineering · **Status:** fixed on `main` · **Severity:** S3 (display; no data corrupted)
>
> **Summary.** Commit "EF Core housekeeping" removed the `ConfigureConventions`
> override that applied `UtcDateTimeConverter` to every `DateTime`. Values read
> from `datetime2` then had `DateTimeKind.Unspecified`, `System.Text.Json`
> serialised them without `Z`, and browsers parsed them as local time. Users
> east of UTC saw times early by their offset (Lagos: one hour); users west of
> UTC saw them late; users in UTC saw no change, which is why support could not
> reproduce it.
>
> **Impact.** Every timestamp read from the database, on every screen, since
> Tuesday's deploy. Stored data is correct; only display was wrong. Values
> created in memory (`assessedAtUtc`) were unaffected, which made the bug look
> intermittent.
>
> **Root cause.** `SentinelDbContext.ConfigureConventions` was deleted as
> apparently redundant. `datetime2` round-trips the value but not the kind.
> The client's `new Date(value)` silently interpreted the zone-less strings.
>
> **Why it was not caught.** No test asserted the wire format of timestamps or
> the `Kind` of values read back; the client accepted zone-less input.
>
> **Fix.** Restored the convention. Added `Every_timestamp_on_the_wire_is_utc_with_a_z`
> (API) and `Sign_in_times_come_back_from_the_database_as_utc` (persistence),
> both red before and green after. Hardened `parseApiTimestamp` to reject
> timestamps without a designator (defence in depth: the next such bug fails
> loudly).
>
> **Separately:** the London sign-in on Bola Adeyemi's account is a genuine
> impossible-travel finding and has been passed to security.
>
> **Follow-ups.** Reject zone-less `occurredAt` on `POST /api/sign-ins` (today
> it is read as server-local time) · add a comment to the convention so it is
> not "tidied" again.
>
> **References.** Microsoft Learn: *Value Conversions*; *DateTime.Kind*;
> *System.Text.Json DateTime and DateTimeOffset support*. MDN: *Date.parse()*.

Notice: the summary is three sentences and contains the whole story. The
impact section says what was *not* affected. "Why it was not caught"
describes the system, not the person who made the commit.

## 24.3 Worked example: SUP-4172 for the customer

> Hi Bola,
>
> **What happened.** A change we made on Tuesday caused the times on your
> Account security page to show one hour early. Your 9:00 sign-ins were
> displayed as 8:00. That is fixed: the times now show correctly in Lagos time.
>
> **Is your account safe?** The time problem did not affect your account's
> security. **However, the sign-in from London last Friday is real**: someone
> signed in to your account from London forty minutes after you signed in from
> Lagos. Please change your password now, and then choose *This wasn't me* on
> that entry so our security team can follow up.
>
> **If you see anything else unusual,** reply to this email with a screenshot
> and the time you saw it.
>
> Thank you for reporting this: the detail that your sign-in "always" happens
> at 9 is what let us find the problem quickly.
>
> Kemi, Customer Support

What makes this good: it answers her actual fear first ("is someone in my
account?"), does not hide the bad news behind the good, gives one clear
action, and thanks her for the specific detail that helped.

## 24.4 Worked example: SUP-4219, both audiences

**For support (sent first, before the fix):**

> Ifeanyi, short answer: **yes, treat Tunde's account as compromised.** Our
> data shows seven failed sign-ins from Amsterdam yesterday at about 21:05 UTC,
> followed by a successful one. Please ask him to change his password and to
> rotate the credentials his integration scripts use. The green status on his
> security page is a bug on our side, which we are fixing now; it affects only
> accounts with very many sign-ins.

**For engineers:**

> **SUP-4219 — Assessment loaded the oldest 500 sign-ins instead of the newest**
>
> **Summary.** `AccountAssessor` ordered history ascending before `Take(500)`,
> so for users with more than 500 attempts in 90 days the most recent attempts
> were never assessed. A real brute-force-then-success on Tunde Bakare's account
> showed as "No suspicious activity".
>
> **Impact.** Users with more than 500 sign-ins in 90 days, i.e. accounts used
> by automation. For them, findings from roughly the last week were invisible.
>
> **Root cause.** Commit "Let the database sort sign-in history chronologically"
> changed `OrderByDescending` to `OrderBy`, reasoning that the detector wants
> oldest-first. The detector sorts its input itself; the query order only
> decides which rows survive `Take`.
>
> **Why it was not caught.** No test exercised the cap; seed users had fewer
> than 500 attempts.
>
> **Fix.** Restored `OrderByDescending` with a comment explaining why the order
> matters. Added `When_history_exceeds_the_cap_the_newest_attempts_are_assessed`
> with a guard assertion that the seed data exceeds the cap. Added a heavy-usage
> user to the seed data.
>
> **Follow-ups.** Compute the baseline (seen countries/devices) in SQL so the
> cap never drops baseline either · flag automated sign-in patterns.

## 24.5 Habits that make this fast

- **Write the log as you go**; the write-up is then mostly copying.
- **Summary first**, in three sentences: what broke, why, what changed.
- **Say what you don't know.** "We have not checked other environments" is
  professional; silence is not.
- **Name the gap, not the person.** "No test asserted..." rather than "X
  forgot...".
- **For customers: answer the fear, then the facts, then the action.**
- **Read the customer reply aloud.** If a sentence needs an engineer to
  explain it, rewrite it.

## 24.6 The templates

<!-- include: docs/templates/engineering-write-up.md -->

<!-- include: docs/templates/customer-update.md -->
