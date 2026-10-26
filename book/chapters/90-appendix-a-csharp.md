# Appendix A · C# Fundamentals for the Test

The prep guide lists fundamentals to review. Here each one is, in C#, with
an example from Sentinel's domain.

## A.1 Iteration

```csharp
// for: when you need the index
for (var i = 0; i < attempts.Count; i++) { var attempt = attempts[i]; }

// foreach: the default
foreach (var attempt in attempts) { if (!attempt.Succeeded) failures++; }

// LINQ: when the loop computes a value
var failures = attempts.Count(a => !a.Succeeded);
var countries = attempts.Where(a => a.Succeeded).Select(a => a.Location.CountryCode).Distinct().ToList();
var newestFirst = attempts.OrderByDescending(a => a.OccurredAtUtc).ToList();
var byUser = attempts.GroupBy(a => a.UserId).ToDictionary(g => g.Key, g => g.Count());

// while: when the end condition is not a count (see MaxInAnyWindow)
while (burst[end].OccurredAtUtc - burst[start].OccurredAtUtc > window) start++;
```

LINQ is lazy: a query runs when it is enumerated (`ToList`, `foreach`,
`Count`). Enumerating it twice runs it twice. Call `ToList()` once when you
will use the result more than once.

## A.2 Inspecting values

```csharp
Console.WriteLine($"[debug] user={user.Name} roles={string.Join(",", user.Roles ?? [])}");
```

In xUnit, output from tests goes through `ITestOutputHelper`
(constructor-injected) rather than `Console`. Remove debug output before
submitting.

## A.3 Classes and objects

```csharp
public sealed class Role                     // a class with behaviour and identity
{
    private readonly List<string> _permissions = [];
    public Role(string name) => Name = name;  // constructor
    public string Name { get; }               // get-only property
    public IReadOnlyList<string> Permissions => _permissions;
    public void Grant(string p) => _permissions.Add(p);   // a method
}

var role = new Role("Auditor");               // initialise
role.Grant("reports.view");                   // call a method
Console.WriteLine(role.Name);                 // get a property

public sealed record KataPage(string Path, string? ParentPath, IReadOnlyList<string>? RequiredRoles);
var page = new KataPage("/reports", "/", ["analyst"]);   // records: value equality, concise
var moved = page with { ParentPath = "/home" };           // non-destructive copy
```

## A.4 Collections

| Need | Type | Key operations |
|---|---|---|
| Fixed sequence | `T[]` | index, `Length` |
| Growable list | `List<T>` | `Add`, `Remove`, `Count`, index |
| Lookup by key | `Dictionary<TKey, TValue>` | `TryGetValue`, `TryAdd`, `GetValueOrDefault`, `ContainsKey` |
| Membership | `HashSet<T>` | `Add` (returns false if present), `Contains` |
| Read-only view | `IReadOnlyList<T>`, `IReadOnlyDictionary<,>` | expose, don't mutate |

Case-insensitive keys: `new Dictionary<string, Page>(StringComparer.OrdinalIgnoreCase)`.
Counting idiom: `counts[key] = counts.GetValueOrDefault(key) + 1;`

## A.5 Converting between types

```csharp
int.TryParse("142", NumberStyles.None, CultureInfo.InvariantCulture, out var ms);   // never int.Parse on input
DateTimeOffset.TryParse("2026-10-24T08:00:00Z", CultureInfo.InvariantCulture, DateTimeStyles.None, out var at);
Enum.TryParse<PermissionEffect>("deny", ignoreCase: true, out var effect);           // also accepts "1"! check Enum.IsDefined
var asText = ms.ToString(CultureInfo.InvariantCulture);
var utc = at.UtcDateTime;                                                             // DateTimeOffset -> DateTime (Kind=Utc)
```

Always pass `CultureInfo.InvariantCulture` when parsing machine data. A
machine set to French parses `1,5` as one and a half.

## A.6 Null handling

```csharp
var roles = user.Roles ?? [];                        // default for null
var city = attempt?.Location?.City ?? "Unknown";     // null-conditional chain
ArgumentNullException.ThrowIfNull(pages);            // programming errors fail fast
if (page.ParentId is { } parentId) { ... }           // pattern: non-null and bind
```
