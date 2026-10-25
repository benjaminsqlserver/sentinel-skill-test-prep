# SUP-4172 — "My sign-in history shows the wrong times"

**Lab 1 for Chapter 22.** Everything below is exactly what support forwarded.
Nothing has been investigated yet. Start a copy of
`docs/templates/investigation-log.md` before you touch the code.

---

**From:** Kemi (Customer Support, Tier 1)
**Priority:** Normal
**Customer:** Bola Adeyemi (bola.adeyemi@sentinel.dev), Finance Manager

> Hi team, customer says the times on the Account security page are wrong.
> Quote from her email: *"I always sign in at 9 when I get to the office and
> the page says 8:00. It also says I signed in from London at 8:40 on Friday.
> I have never been to London and at 8:40 I was on the bus. Is someone else
> in my account??"* She is quite worried because of the red banner. I tried it as myself and the times look fine to me?? Not sure
> if related but we deployed the "EF Core housekeeping" change on Tuesday.
> Screenshot attached (not attached).
>
> Thanks, Kemi

---

## What you have

- This branch, which reproduces the problem. Run the API and the Angular app
  as in the README and sign in as Bola.
- `git log` — the history is real; read it like an investigator.
- No access to the customer, but you can write the questions you would send.

## What a good answer contains

1. An investigation log: hypotheses, how each was tested, sources cited.
2. A fix with a regression test that fails before and passes after.
3. A write-up for engineers (`docs/templates/engineering-write-up.md`).
4. A reply Kemi can send to Bola (`docs/templates/customer-update.md`).

Time box: 35 minutes, the length of the real scenario. Then read Chapter 22.
