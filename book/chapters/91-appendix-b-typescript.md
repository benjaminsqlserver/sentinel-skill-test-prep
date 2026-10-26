# Appendix B · TypeScript and JavaScript Fundamentals for the Test

## B.1 Iteration

```typescript
for (let i = 0; i < signIns.length; i++) { const s = signIns[i]; }
for (const s of signIns) { if (!s.succeeded) failures++; }     // for...of: values
for (const key in record) { }                                   // for...in: keys of an object (rarely what you want)

const failed = signIns.filter((s) => !s.succeeded);
const ids = signIns.map((s) => s.id);
const firstFlagged = signIns.find((s) => s.isFlagged);          // undefined if none
const count = signIns.reduce((n, s) => n + (s.isFlagged ? 1 : 0), 0);
```

## B.2 Inspecting values

```typescript
console.log('[security] status', status);          // objects are expandable in dev tools
console.table(status.recentSignIns);               // arrays of objects as a table
```

## B.3 Classes and objects

```typescript
class FindingCounter {
  private readonly counts = new Map<string, number>();
  constructor(private readonly label: string) {}      // parameter property
  add(kind: string): void { this.counts.set(kind, (this.counts.get(kind) ?? 0) + 1); }
  get total(): number { return [...this.counts.values()].reduce((a, b) => a + b, 0); }
}

const counter = new FindingCounter('today');
counter.add('NewDevice');
console.log(counter.total);

const finding = { key: 'x', severity: 'High' };       // object literal
finding.severity = 'Low';                              // set a property
const copy = { ...finding, key: 'y' };                 // copy with a change
```

## B.4 Arrays, maps, objects as dictionaries

| Need | Use |
|---|---|
| Ordered list | `T[]` (`push`, `length`, `includes`, `indexOf`) |
| Lookup by key | `Map<K, V>` (`get`, `set`, `has`), or `Record<string, V>` for fixed keys |
| Membership | `Set<T>` (`add`, `has`) |

## B.5 Converting between types

```typescript
Number('142');                 // 142; Number('') is 0, Number('abc') is NaN
parseInt('142px', 10);         // 142 (stops at the first non-digit)
Number.isNaN(value);           // the only reliable NaN check
String(42); `${42}`;           // to string
JSON.parse(text);              // string -> object (throws on bad JSON)
new Date('2026-10-24T08:00:00Z');   // ALWAYS with a Z or an offset
date.toISOString();            // Date -> UTC string with Z
```

## B.6 Equality and truthiness

Use `===` and `!==`. `0`, `''`, `null`, `undefined`, `NaN` and `false` are
falsy: `count || 10` replaces a real `0`; `count ?? 10` does not.
