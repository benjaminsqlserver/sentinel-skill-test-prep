# Part III · The Angular Front End {.unnumbered}

# Chapter 14 · Angular and TypeScript for the Test

The prep guide promises no trick questions about JavaScript and says the
Angular scenario needs only basic reading and writing. "Basic" under a
25-minute timer means *automatic*. This chapter is the minimum you should be
able to write without looking anything up.

## 14.1 The ES2015+ you will actually use

```typescript
// const by default; let when it changes; never var.
const findings = status.findings;
let flaggedCount = 0;

// Arrow functions, and the array methods that replace most loops.
const high = findings.filter((f) => f.severity === 'High');
const keys = findings.map((f) => f.key);
const anyHigh = findings.some((f) => f.severity === 'High');
const total = signIns.reduce((sum, s) => sum + (s.succeeded ? 0 : 1), 0);

// Sorting: sort() mutates. Copy first, always.
const newestFirst = [...signIns].sort((a, b) => b.occurredAtUtc.localeCompare(a.occurredAtUtc));

// Destructuring and defaults.
const { riskLevel, findings: items = [] } = status;
function format(value: string, { locale = 'en-GB', showZone = true } = {}) { /* ... */ }

// Spread to copy and override (the builders in the specs do this).
const changed = { ...finding, severity: 'High' };

// Template literals.
const label = `Review ${count} ${count === 1 ? 'item' : 'items'}`;

// Optional chaining and nullish coalescing: different from || for 0 and ''.
const zone = user?.timeZoneId ?? 'UTC';

// Map and Set for lookups and "seen before".
const seenCountries = new Set<string>();
const byId = new Map(signIns.map((s) => [s.id, s]));
```

Two traps worth knowing because they cause real bugs, not because anyone
will quiz you on them:

- `sort()` without a comparator sorts as **strings**: `[10, 9, 1].sort()` is
  `[1, 10, 9]`.
- `new Date('2026-10-24T08:00:00')` with no `Z` is **local time**. Chapter
  18 is about this.

## 14.2 TypeScript in one table

| You want | Write |
|---|---|
| A shape for API data | `interface SignInAttempt { id: string; succeeded: boolean; }` |
| A closed set of strings | `type RiskLevel = 'None' \| 'Low' \| 'Medium' \| 'High';` |
| One of several states | a discriminated union: `{ kind: 'loading' } \| { kind: 'loaded'; status: X }` |
| A lookup keyed by a union | `const titles: Record<FindingKind, string> = { ... }` |
| "May be missing" | `string \| null` (from JSON) or an optional `?:` property |
| Narrowing | `if (state.kind === 'loaded') { state.status }` |

## 14.3 Angular in one page

| Concept | Modern syntax (Angular 17+) | What it is for |
|---|---|---|
| Component | `@Component({ selector, imports, template })` on a class | A piece of UI with its own template |
| Input | `readonly risk = input.required<RiskLevel>()` | Data **into** a child |
| Output | `readonly review = output<void>()` and `this.review.emit()` | Events **out of** a child |
| Interpolation | `{{ user.displayName }}` | Show a value |
| Property binding | `[riskLevel]="status.riskLevel"` | Set a child's input or a DOM property |
| Event binding | `(review)="scrollToFindings()"` | React to an output or DOM event |
| Two-way binding | `[(selected)]="email"` | Input + `selectedChange` output |
| Control flow | `@if`, `@for (x of xs; track x.id)`, `@switch` | Conditionals and loops in templates |
| Pipe | `{{ value \| zonedDate: zone }}` | Format a value for display |
| Service | `@Injectable({ providedIn: 'root' })` | Shared logic or state |
| Injection | `private readonly api = inject(SecurityApi)` | Get a service |
| Signal | `count = signal(0)`, `count()`, `count.set(1)`, `computed(...)` | Reactive state |
| Observable | `http.get<T>(url).subscribe(...)` | Asynchronous streams (HTTP, events) |

Appendix C translates every row into pre-17 syntax (`@Input()`, `*ngIf`,
`NgModule`) in case the test environment is older.

## 14.4 RxJS: the six operators that cover most code

| Operator | Use |
|---|---|
| `map` | Transform each value |
| `switchMap` | Start a new request for each value, cancelling the previous one |
| `catchError` | Turn an error into a value (an error state) |
| `debounceTime` | Wait for typing to pause |
| `distinctUntilChanged` | Ignore repeats |
| `takeUntilDestroyed` | Unsubscribe when the component goes away |

## 14.5 Using AI in the Angular scenario, properly

The third scenario *encourages* AI and asks you to cite it. Use it for what
it is good at (syntax you have forgotten, an `Intl` option name, a test
skeleton) and keep the decisions yours. Cite it the same way as any source:

```typescript
// Relative time formatting adapted from an AI assistant's suggestion
// (ChatGPT, 2026-10-25, prompt: "Intl.RelativeTimeFormat days/hours ago").
// Reviewed and changed: "now" is a parameter so it can be tested.
```

What a reviewer cannot tell from AI-written code is whether *you*
understood it. Your comments are where you show that you did.
