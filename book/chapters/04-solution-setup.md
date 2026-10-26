# Part II · The Back End {.unnumbered}

# Chapter 4 · Environment and Solution Setup

## 4.1 What to install

- **.NET 10 SDK** (10.0.3xx or later). Check with `dotnet --list-sdks`.
- **Visual Studio 2026** with the *ASP.NET and web development* workload,
  or VS Code with C# Dev Kit, or Rider. Nothing in this book needs a
  particular IDE.
- **SQL Server**: LocalDB (installed with Visual Studio) is enough. Any
  edition from 2019 works; set the connection string in
  `src/Sentinel.Api/appsettings.json` or with user secrets.
- **Node.js 22 or later** (24 LTS recommended) and npm 10+, for Angular.
- **Git**.
- Optionally **pandoc** and Chrome or Edge, to build this book.

Verify:

```powershell
dotnet --list-sdks
node --version
sqllocaldb info
```

## 4.2 Creating the projects

```powershell
mkdir sentinel; cd sentinel
git init -b main
dotnet new sln -n Sentinel           # creates Sentinel.slnx (the XML solution format)

foreach ($p in 'Domain','Contracts','Application','Infrastructure') {
    dotnet new classlib -n "Sentinel.$p" -o "src/Sentinel.$p" -f net10.0
}
dotnet new web -n Sentinel.Api -o src/Sentinel.Api -f net10.0

foreach ($t in 'Domain','Application','Api','Architecture') {
    dotnet new xunit -n "Sentinel.$t.Tests" -o "tests/Sentinel.$t.Tests" -f net10.0
}

dotnet sln Sentinel.slnx add (Get-ChildItem src,tests -Recurse -Filter *.csproj).FullName
```

Delete the `Class1.cs` and `UnitTest1.cs` files the templates create. The
solution file is short enough to read:

<!-- include: Sentinel.slnx -->

## 4.3 Central build configuration

Settings every project shares go in one file at the root. MSBuild finds
`Directory.Build.props` by walking up from each project:

<!-- include: Directory.Build.props -->

`TreatWarningsAsErrors` is worth its occasional annoyance. A nullable
warning ignored today is a `NullReferenceException` reported by a customer
next month, and a debugging lab in this book.

Pin the SDK so that everyone builds with the same compiler:

<!-- include: global.json -->

## 4.4 Central package management

Versions are declared once, in `Directory.Packages.props`; project files
name packages without versions.

<!-- include: Directory.Packages.props -->

Note the transitive pin. `Microsoft.Data.SqlClient` (pulled in by the EF
Core SQL Server provider) depended on a version of
`System.Security.Cryptography.Pkcs` with published advisories, and with
warnings as errors the build failed with `NU1903`. Transitive pinning lets
us raise that one package without referencing it from any project. This is
a real incident from writing this book: the fix took two minutes because
the error message named the package and the advisory.

## 4.5 The project files

Each project's file states its references and nothing else. The Domain:

<!-- include: src/Sentinel.Domain/Sentinel.Domain.csproj -->

The Application layer references EF Core for `DbSet<T>` and LINQ, but not
a provider:

<!-- include: src/Sentinel.Application/Sentinel.Application.csproj -->

Infrastructure brings the provider and the design-time tools:

<!-- include: src/Sentinel.Infrastructure/Sentinel.Infrastructure.csproj -->

The API composes them:

<!-- include: src/Sentinel.Api/Sentinel.Api.csproj -->

## 4.6 The EF Core tool

The `dotnet ef` commands come from a local tool, recorded in the repository
so every clone uses the same version:

```powershell
dotnet new tool-manifest
dotnet tool install dotnet-ef --version 10.0.13
```

<!-- include: dotnet-tools.json -->

## 4.7 Verifying the build

```powershell
dotnet build
dotnet test
```

Both must be clean before you go on. From here on, every chapter ends in
the same state: it builds, and the tests pass.
