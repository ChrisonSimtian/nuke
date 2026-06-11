# Nuke.Common transition shim

This package is a **partial transition shim** for projects mid-migration from NUKE to Fallout. Published only to GitHub Packages — `Nuke.Common` on nuget.org is owned by the original NUKE maintainer.

## What's covered

| Old (`Nuke.Common`) | New (`Fallout.Common`) | Shim mechanism |
|---|---|---|
| `NukeBuild` | `FalloutBuild` | Subclass |
| `INukeBuild` | `IFalloutBuild` | Sub-interface |
| `[Parameter]` | `Fallout.Common.ParameterAttribute` | Subclass |
| `[Secret]` | `Fallout.Common.SecretAttribute` | Subclass |
| `[Solution]` | `Fallout.Solutions.SolutionAttribute` | Subclass |
| `[GitRepository]` | `Fallout.Common.Git.GitRepositoryAttribute` | Subclass |
| `CI host singletons` (`GitHubActions`, `AzurePipelines`, `TeamCity`, `AppVeyor`, `GitLab`, `Jenkins`, `Bamboo`, `Bitbucket`, `Bitrise`, `TravisCI`) | `Fallout.Common.CI.<Vendor>.<Vendor>` | Hand-written static class re-exposing `.Instance` (returns canonical type) |
| `DelegateDisposable` | `Fallout.Common.Utilities.DelegateDisposable` | Hand-written static class re-exposing `CreateBracket` / `SetAndRestore` |

The MVP unblocks a `class Build : NukeBuild` declaration with parameter/secret/solution/git injection — the entry-point surface of most consumer Build.cs files. CI host shims cover the `GitHubActions.Instance.Workflow`-style access path.

### Hand-bridge limitation (CI hosts, `DelegateDisposable`)

These types are framework-injected or use private/internal constructors. The shim is `static class`, so consumers receiving canonical-typed instances (e.g. `[CI] readonly GitHubActions GitHubActions;`, `Host is GitHubActions`, methods returning `DelegateDisposable`) need to switch the type reference to canonical. Use `fallout-migrate` for those rewrites; the shim covers static factory / `.Instance` access paths only.

## What's NOT covered (and won't be)

These types have private/internal ctors and no public static factories, **and** are mainly used as value types — meaning the hand-bridge would be empty (no public statics to re-expose) and a subclass shim can't carry implicit operators or be assignment-compatible with framework-returned canonical instances:

- **`Fallout.Common.IO.AbsolutePath`** — used via `AbsolutePath path = "/foo"` (implicit `string`/`AbsolutePath` conversion), `parent / "child"` (`operator/`), and field-injection. None of those work through a static-class shim. Use `fallout-migrate` to rewrite `Nuke.Common.IO.AbsolutePath` → `Fallout.Common.IO.AbsolutePath`.
- **`Fallout.Common.Tools.AzureKeyVault.AzureKeyVault`** — zero public statics, framework-constructed only. `fallout-migrate` rename.
- **`Fallout.Common.Tools.MSBuild.MSBuildProject`** — zero public statics, framework-returned only. `fallout-migrate` rename.

## What's NOT covered (yet)

- **CI attributes** (`[GitHubActions]`, `[AzurePipelines]`, `[TeamCity]`, `[AppVeyor]`, ...) — these take enum parameters (`GitHubActionsImage`, etc.) that the shim doesn't re-export. C# can't subclass enums; bridging them needs a source generator. Tracked in the follow-up issue.
- **The `Target` delegate** — delegates can't be implicitly converted across types in C#, so the shim can't bridge `Nuke.Common.Target Compile => _ => _` cleanly. Use `fallout-migrate` for the source-level rename.
- **Static helper classes** — `DotNetTasks`, `MSBuildTasks`, etc. require method-by-method delegation. Tracked for the source-generator follow-up.

## Packaging

The shim packs normally as a build artifact (`Nuke.Common.<version>.nupkg`). The release pipeline's `Publish` target filters `Nuke.*` packages out of the nuget.org push — that ID is owned by the original NUKE maintainer. The produced nupkg can be uploaded to GitHub Packages or attached to a GitHub release.

Until automated dual-publish lands (tracked in [#69](https://github.com/Fallout-build/Fallout/issues/69)), the manual GH Packages push is:

```pwsh
dotnet pack src/Shims/Nuke.Common/Nuke.Common.csproj -c Release
dotnet nuget push **/Nuke.Common.*.nupkg `
    --api-key $env:GITHUB_TOKEN `
    --source https://nuget.pkg.github.com/Fallout-build/index.json
```

## Migration path

For projects that just want their `class Build : NukeBuild { ... }` to compile against our framework:

1. Add `https://nuget.pkg.github.com/Fallout-build/index.json` as a NuGet source (`nuget.config`).
2. Bump the `Nuke.Common` package version to the latest published here.
3. Build. If the shim covers what you use, you're done. Otherwise:
4. Run `fallout-migrate` to rewrite the remaining references.

## What this shim is NOT

- Not a permanent dependency — designed for a transition window.
- Not a replacement for the migration story — most consumers should run `fallout-migrate` and target `Fallout.Common` directly.
- Not on nuget.org — only on this fork's GitHub Packages feed.

## Future work

Source-generator-based expansion to cover the full public API is tracked in the issue tracker. The architectural decision is between:

1. **Per-package shims** with hand-written wrappers (this approach, doesn't scale to all 158 public types in Common alone).
2. **Generated wrappers** from a Roslyn source generator that reflects the canonical assembly metadata — single source of truth, mechanical regeneration.

(2) is the long-term answer. (1) is what shipped first.
