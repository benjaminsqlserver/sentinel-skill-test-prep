# Appendix D · Troubleshooting

Every entry below happened while this book was written.

| Symptom | Cause | Fix |
|---|---|---|
| `NU1903: Package 'System.Security.Cryptography.Pkcs' 9.0.11 has a known high severity vulnerability` | A transitive dependency of the SQL Server provider, with warnings as errors | Pin it centrally with `CentralPackageTransitivePinningEnabled` (§4.4) |
| `NU1301: ... No such host is known (api.nuget.org)` | A transient network failure | Retry; check proxy settings |
| `Invalid object name 'Users'` on start-up | The API was run with `--no-build` before the migration was added, so the old assembly had no migrations | Build, then run |
| `CS8188: An expression tree may not contain a throw-expression` | `throw` inside a value converter lambda | Move the check into a static method (§11.1) |
| `CS8620` nullability mismatch on `HasConversion` for a nullable value object | The generic overload's nullability | Cast to the non-generic `ValueConverter` (`PageConfiguration`) |
| `npm error notarget No matching version found` during `ng new` | A package published moments earlier, not yet on the mirror | Run `npm install` again in the workspace |
| `proxy.conf.json contains parse errors: [1, 1] InvalidSymbol` | A UTF-8 byte-order mark from PowerShell 5's `Set-Content -Encoding utf8` | Save without BOM |
| A path argument becomes `C:/Program Files/Git/account/security` | Git Bash converts arguments that look like POSIX paths | `export MSYS_NO_PATHCONV=1` |
| Demo data differs between runs | `string.GetHashCode()` is randomised per process | Seed `Random` from stable input (§11.4) |
| A role's new permission is not saved | No `ValueComparer` on the converted list | Add one (`PermissionListComparer`) |
| Times on screen are off by the user's UTC offset | A `DateTime` with `Kind = Unspecified` serialised without `Z` | Chapter 22 |
| A heavy user's security page is green during an attack | `OrderBy` + `Take` kept the oldest rows | Chapter 23 |
| Tests in Api.Tests still hit SQL Server | Only `DbContextOptions<T>` removed; EF Core 9+ also registers `IDbContextOptionsConfiguration<T>` | Remove both (§13.3) |

## Resetting everything

```powershell
dotnet ef database drop --project src/Sentinel.Infrastructure -f
git clean -xdf -e node_modules     # removes bin/obj and build output
dotnet build; dotnet test
```
