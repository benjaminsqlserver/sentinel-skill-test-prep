# SUP-4219 — "Security page says all clear, but I got a lockout email"

**Lab 2 for Chapter 23.** Exactly what support forwarded; nothing has been
investigated. Start a copy of `docs/templates/investigation-log.md` first.

---

**From:** Ifeanyi (Customer Support, Tier 2)
**Priority:** High
**Customer:** Tunde Bakare (tunde.bakare@sentinel.dev), Operations

> Tunde got the "unusual sign-in activity" email from the identity provider
> yesterday evening and someone in his team says they saw a login from
> Amsterdam. But when he opens Account security it's green, "No suspicious
> activity". I checked two other users who were in the same phishing
> campaign (Chen and Bola) and theirs show red/amber properly, so the
> detector seems to work?
>
> He uses the same account for his integration scripts, if that matters.
> Can someone look urgently, we don't know if the account is compromised.

---

## What you have

- This branch, which reproduces the problem (sign in as Tunde).
- The API, the database, and the logs (`LoggingBehavior` writes one line per request).
- `src/Sentinel.Api/Sentinel.http` for poking the API directly.

## What a good answer contains

1. An investigation log with hypotheses ruled in and out, and sources cited.
2. The fix, with a regression test that fails on this branch and passes after.
3. An answer to the question support actually asked: *is Tunde's account compromised?*
4. A write-up for engineers and a message support can send to Tunde.

Time box: 35 minutes. Then read Chapter 23.
