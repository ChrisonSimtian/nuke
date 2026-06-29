# Consumer compatibility tests

Three small build-project consumers that exercise Fallout's public surface from the perspectives of real downstream users. If any of these stop **compiling**, we've broken something consumers depend on.

## Projects

| Project | What it exercises | Reference style |
|---|---|---|
| [`Nuke.Consumer`](Nuke.Consumer/) | A pre-rename NUKE consumer using `class Build : NukeBuild`, `[Solution]`, and `Target` via the `Nuke.Common` / `Nuke.Build` / `Nuke.Components` transition shims. Catches breakage of **shim coverage** — if a NUKE-shape symbol stops resolving, an upgrading NUKE user breaks. | `ProjectReference` to `src/Shims/Nuke.*/` |
| [`Fallout.Consumer.Local`](Fallout.Consumer.Local/) | A Fallout consumer using `class Build : FalloutBuild`, `[Solution]`, `Target` against **this repo's current source**. Catches breakage of the public Fallout surface **in the current PR**. | Direct `ProjectReference` to the in-repo `Fallout.*` projects |
| [`Fallout.Consumer.NuGet`](Fallout.Consumer.NuGet/) | A Fallout consumer against the **last published** `Fallout.*` packages (pinned in the csproj). Catches **packaging issues** (missing assemblies, wrong references) on the most-recent release, and catches upgrade-direction breakage when the pin is bumped after a release. | `PackageReference` to nuget.org with `<ReplacePackageReferences>false</ReplacePackageReferences>` and `<ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>` so the smart-rewrite doesn't kick in |

## How they're validated

**Compile-time validation only.** All three projects are in `fallout.slnx`, so `dotnet build fallout.slnx` (the standard CI gate) compiles them. Any consumer-facing breaking change makes the build fail.

### Why not runtime validation?

A runtime smoke test layer was considered (spawn each consumer via `dotnet run`, assert exit code `0`). That hits a Fallout framework requirement: `FalloutBuild.cctor()` enumerates `Host` subclasses and reflects for a static `IsRunningHost` property — minimal consumers that don't pull in the full Fallout MSBuild props/targets (`Fallout.Common.props`, `Fallout.Common.targets`) trigger `System.ArgumentException: Host type 'Host' defines no property 'IsRunningHost'` at activation. Reproducing the full build-app environment for a smoke consumer is more setup than the test's marginal value justifies — compile-time already catches the bulk of breaking changes.

If runtime validation becomes worth the cost: import `Fallout.Common.props` + `Fallout.Common.targets` from the consumer csprojs (the way `build/_build.csproj` does), provide a `FalloutRootDirectory`, and the framework should activate cleanly.

## Catching breaking changes

The intended flow:

1. These consumer projects live on `main` and reflect the **current public consumer surface**. Any PR proposing a change to consumer-facing types/namespaces/attributes…
2. …will conflict with the consumer code in those projects (rebase or merge will surface the breakage).
3. Resolving the conflict means either: (a) the change isn't really breaking and the conflict is trivial, or (b) the consumer code needs updating to the new shape, **which is the migration path**. Update both, document the migration in `CHANGELOG.md` under `[Unreleased] — <next-major>`, and you've simultaneously detected the breaking change AND demonstrated how consumers migrate.

This works as designed only because the consumer code is **fixed to the current API shape**, not regenerated to match new code. Don't auto-update consumer Build.cs files to match a PR's renamed types — let them break, then fix them deliberately as part of the migration story.

## Bumping the NuGet pin

After a new `Fallout.*` release ships, edit `Fallout.Consumer.NuGet/Fallout.Consumer.NuGet.csproj` to bump the pinned version. If the consumer source still compiles, the upgrade is non-breaking. If it fails, the new release introduces a consumer-facing breaking change — record it in `CHANGELOG.md` and the migration path under `[Unreleased] — <next-major>`.

The `Nuke.Consumer` doesn't pin anything (it references the in-repo shim assemblies directly), so no bump cadence there — it always tracks HEAD's shim coverage.

## What's deliberately tested

- **NUKE-era `class Build : NukeBuild`** — basic class identity through the shim
- **`[Solution] readonly Solution Solution`** — value-injection attribute + facade type via shim
- **`Target Default => _ => _.Executes(...)`** — delegate type, default target lambda
- **`static int Main() => Execute<Build>(x => x.Default)`** — framework entry point

## What's NOT tested

- Actual build *execution* (see above — runtime activation is fragile)
- CI-host integration (GitHubActions, AzurePipelines, etc.) — covered by `tests/Fallout.Common.Specs/CI/`
- Tool wrappers — covered by their own generated tests
- Source generator behaviour — covered by `tests/Fallout.SourceGenerators.Specs/`

These consumer projects are a **sentinel for shape changes**, not a place to demo features. Don't add consumer projects covering specific subsystems — those go in their own focused tests.
