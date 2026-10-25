# Drill — the account security page, against the clock (25 minutes)

The container (`account-security-page.ts`), the API service, the time
utilities and every spec are intact. The three presentational components
have been emptied down to their inputs and outputs.

1. Start a 25-minute timer.
2. `cd client/sentinel-web && npx ng test --watch=false` — note what fails.
3. Rebuild `security-banner.ts`, `finding-list.ts`, `sign-in-table.ts`.
4. Run the app and look at Tunde (High), Dana (Medium) and Emeka (None).
5. Compare with `git diff drill/angular-security-start main -- client/`.

Chapter 25 lists what a reviewer looks for. Score yourself honestly.
