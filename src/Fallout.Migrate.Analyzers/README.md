# Fallout.Migrate.Analyzers

Roslyn analyzer + codefix that rewrites legacy `Nuke.*` references to `Fallout.*` in C# source. Install it temporarily while migrating; uninstall once you're done.

## What it does

Emits a single diagnostic, **`FALLOUT004`** (severity: **Warning**), at each of:

- `using Nuke.<X>;` directives
- `using static Nuke.<X>.<Y>.<Type>;` directives
- Fully-qualified type references like `Nuke.Common.AbsolutePath`
- Bare type names `NukeBuild` and `INukeBuild`

The codefix rewrites all of those to their `Fallout.*` equivalents in a single edit (`Migrate to Fallout.*`), including the type renames (`NukeBuild → FalloutBuild`, `INukeBuild → IFalloutBuild`). `Fix all in Document / Project / Solution` is supported.

## How to use

```sh
dotnet add build/_build.csproj package Fallout.Migrate.Analyzers
```

Open your build project in an IDE that supports Roslyn analyzers (Visual Studio, Rider, VS Code with C# Dev Kit). Each `FALLOUT004` site becomes a lightbulb / quick-fix; apply individually or `Fix all in Solution`.

When the diagnostic count drops to zero:

```sh
dotnet remove build/_build.csproj package Fallout.Migrate.Analyzers
```

## What it does **not** do

- It does **not** update `PackageReference` items in your `_build.csproj` — that's outside Roslyn's domain. Once the source is migrated, swap `Nuke.Common` for `Fallout.Common` (and friends) in your project file by hand.
- It does **not** touch bootstrap scripts (`build.ps1`, `build.sh`, `build.cmd`). Those keep their names.
- It does **not** fire on pure-NUKE projects (no `Fallout.*` reference present). The diagnostic is guarded by `Compilation.ReferencedAssemblyNames` — you have to be partway into the migration before warnings appear.

## Alternative: bulk rewrite via CLI

If you want to migrate an entire repo in one shot from the command line, use the [`fallout-migrate`](../Fallout.Migrate/README.md) global tool instead. It does the same rewrites plus updates `PackageReference`s and bootstrap scripts.

## Deprecation

This package will be removed alongside the `Nuke.Common` transition shim. That removal is a breaking change, so it lands at a yearly major cut — no date is set yet. (The previous plan, "ship through 11.x, remove in 12.0", was written before the `11.x` line was skipped in favour of calendar versioning; see [ADR-0004](../../docs/adr/0004-calendar-versioning-and-dual-pace-channels.md).)
