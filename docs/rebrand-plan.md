# Rebrand plan: Nuke.* → Fallout.*

Canonical reference for how the namespace/assembly rename and consumer migration work. Captures decisions originally made in [#31](https://github.com/Fallout-build/Fallout/issues/31) and [#35](https://github.com/Fallout-build/Fallout/issues/35) so they survive outside the issue tracker.

This document is itself transient — it gets archived once the shim packages are sunset (see [Sunset timeline](#sunset-timeline)).

## 1. Namespace mapping

**Rule: strict 1:1 prefix swap.** Every `Nuke.X.Y.Z` namespace and `Nuke.X.Y.Z` assembly becomes `Fallout.X.Y.Z`. No consolidation, no restructuring, no exceptions.

This is locked by the bridge-package design — `[TypeForwardedTo]` requires the old type's *full* name to exist in the new assembly, so any consolidation would orphan symbols and break source compatibility for consumers using the shim packages.

### Locked mapping table

| Current namespace | Renamed namespace | Notes |
|---|---|---|
| `Nuke.Build` | `Fallout.Build` | |
| `Nuke.Build.Execution.Extensions` | `Fallout.Build.Execution.Extensions` | |
| `Nuke.Build.Shared` | `Fallout.Build.Shared` | |
| `Nuke.CodeGeneration` | `Fallout.CodeGeneration` | Lives in the `Nuke.Tooling.Generator` project |
| `Nuke.CodeGeneration.Generators` | `Fallout.CodeGeneration.Generators` | |
| `Nuke.CodeGeneration.Model` | `Fallout.CodeGeneration.Model` | |
| `Nuke.CodeGeneration.Writers` | `Fallout.CodeGeneration.Writers` | |
| `Nuke.Common` | `Fallout.Common` | The big one — most user-facing types |
| `Nuke.Common.CI` | `Fallout.Common.CI` | |
| `Nuke.Common.CI.AppVeyor` (+ `.Configuration`) | `Fallout.Common.CI.AppVeyor` | |
| `Nuke.Common.CI.AzurePipelines` (+ `.Configuration`) | `Fallout.Common.CI.AzurePipelines` | |
| `Nuke.Common.CI.Bamboo` | `Fallout.Common.CI.Bamboo` | |
| `Nuke.Common.CI.Bitbucket` | `Fallout.Common.CI.Bitbucket` | |
| `Nuke.Common.CI.Bitrise` | `Fallout.Common.CI.Bitrise` | |
| `Nuke.Common.CI.GitHubActions` (+ `.Configuration`) | `Fallout.Common.CI.GitHubActions` | |
| `Nuke.Common.CI.GitLab` | `Fallout.Common.CI.GitLab` | |
| `Nuke.Common.CI.Jenkins` | `Fallout.Common.CI.Jenkins` | |
| `Nuke.Common.CI.SpaceAutomation` (+ `.Configuration`) | `Fallout.Common.CI.SpaceAutomation` | |
| `Nuke.Common.CI.TeamCity` (+ `.Configuration`) | `Fallout.Common.CI.TeamCity` | |
| `Nuke.Common.CI.TravisCI` | `Fallout.Common.CI.TravisCI` | |
| `Nuke.Common.ChangeLog` | `Fallout.Common.ChangeLog` | |
| `Nuke.Common.Execution` (+ `.Theming`) | `Fallout.Common.Execution` | |
| `Nuke.Common.Git` | `Fallout.Common.Git` | |
| `Nuke.Common.Gitter` | `Fallout.Common.Gitter` | Likely dead code — audit before rename |
| `Nuke.Common.IO` | `Fallout.Common.IO` | |
| `Nuke.Common.ProjectModel` | `Fallout.Common.ProjectModel` | |
| `Nuke.Common.Tooling` | `Fallout.Common.Tooling` | |
| `Nuke.Common.Tools.<Tool>` | `Fallout.Common.Tools.<Tool>` | Applies to all ~70 tool wrappers |
| `Nuke.Common.Utilities` (+ `.Collections`, `.Net`) | `Fallout.Common.Utilities` | |
| `Nuke.Common.ValueInjection` | `Fallout.Common.ValueInjection` | |
| `Nuke.Components` | `Fallout.Components` | |
| `Nuke.GlobalTool` (+ `.Rewriting.Cake`) | `Fallout.Cli` | Project/namespace/assembly is `Fallout.Cli`. The **published package id is `Fallout.GlobalTools`** (`dotnet tool install Fallout.GlobalTools`) — decoupled from the assembly name on purpose. Command name stays `fallout`. |
| `Nuke.MSBuildTasks` | `Fallout.MSBuildTasks` | |
| `Nuke.SourceGenerators` | `Fallout.SourceGenerators` | |
| `Nuke.Utilities.Text.Json` | `Fallout.Utilities.Text.Json` | |
| `Nuke.Utilities.Text.Yaml` | `Fallout.Utilities.Text.Yaml` | |

### Present-day project↔namespace mismatches

Heads-up for whoever executes [#32](https://github.com/Fallout-build/Fallout/issues/32): several projects under `src/` declare types under a namespace that doesn't match the project (assembly) name. The rename must update both axes independently.

| Project (under `src/`) | Root namespace(s) it currently declares |
|---|---|
| `Nuke.Tooling` | `Nuke.Common.Tooling` |
| `Nuke.Tooling.Generator` | `Nuke.CodeGeneration.*` |
| `Nuke.ProjectModel` | `Nuke.Common.ProjectModel` |
| `Nuke.SolutionModel` | `Nuke.Common.ProjectModel` (verify during rename) |
| `Nuke.Utilities` | `Nuke.Common.Utilities` (+ `.Collections`, `.Net`) |
| `Nuke.Utilities.IO.Compression` | `Nuke.Common.IO` |
| `Nuke.Utilities.IO.Globbing` | `Nuke.Common.IO` |
| `Nuke.Utilities.Net` | `Nuke.Common.Utilities.Net` |

The rename keeps these mismatches as-is (mapped through the strict 1:1 rule above). Realigning project ↔ namespace is a *separate* concern, deferred to a post-rename cleanup pass — bundling it into #32 would expand the diff and risk the type-forwarding bridge.

### What's deliberately deferred

- **Consolidating `Nuke.Build` / `Nuke.Build.Shared` / `Nuke.Common`.** Possible long-term win, blocked today by type-forwarding bridge.
- **Hoisting utilities to `Fallout.IO.*` / `Fallout.Net.*` (off the `Common` prefix).** Same blocker.
- **Realigning project ↔ namespace.** Same blocker.

All three are candidate work for a future major version *after* the shim packages have sunset.

## 2. Bridge strategy for existing NUKE consumers

**Chosen approach (decided on [#35](https://github.com/Fallout-build/Fallout/issues/35)):** type-forwarding shim packages published as `Nuke.<X>` on our GitHub Packages feed, complemented by a Roslyn codefix and a migration guide. A migrator CLI is optional, demand-driven.

### Why this works without owning the `Nuke.*` IDs on nuget.org

GitHub Packages namespaces are per-org/user. `ChrisonSimtian/Nuke.Common` on GitHub Packages is a different package from `nuget.org/Nuke.Common`. We can publish bridge packages with the original IDs on our own feed without colliding with the upstream IDs that matkoch still owns. Consumers opt in by adding our feed to their `nuget.config`.

### Package families published on the GH Packages feed

| Family | Contents | Audience |
|---|---|---|
| `Fallout.<X>` | Canonical assemblies. Real types in `Fallout.*` namespaces. | New consumers; existing consumers who want to cut over fully. |
| `Nuke.<X>` (bridge) | Thin assemblies of `[TypeForwardedTo]` declarations only. Depends on the matching `Fallout.<X>`. Exposes the `Nuke.*` namespace surface. | Existing NUKE consumers who want a minimal-touch upgrade. |

Both families ship at the same version, in lockstep.

### Consumer migration paths

**Path A — easiest (recommended for most existing users).** Add our GH Packages feed to `nuget.config`, bump `<PackageReference Include="Nuke.Common" Version="11.0.0" />`, done. Source compiles unchanged. Stay on `Nuke.<X>` indefinitely or cut over later.

**Path B — clean cutover.** Replace `Nuke.<X>` package references with `Fallout.<X>`, rewrite `using Nuke.*` directives to `using Fallout.*`. The Roslyn codefix ([#36](https://github.com/Fallout-build/Fallout/issues/36)) handles the in-IDE per-file case; the optional `Fallout.Migrate` CLI ([#48](https://github.com/Fallout-build/Fallout/issues/48)) handles whole-repo bulk migration.

The migration guide ([#37](https://github.com/Fallout-build/Fallout/issues/37)) documents both paths.

### Sunset timeline

Anchored to the first `Fallout.<X>` 11.x release (call it `R0`).

| Window | Shim posture |
|---|---|
| `R0` → `R0 + 12mo` | Full support. `Nuke.<X>` bridge packages published alongside every `Fallout.<X>` release. |
| `R0 + 12mo` → `R0 + 24mo` | Maintenance only. Security fixes only; no new APIs back-ported through the bridge. |
| `R0 + 24mo` onward | Archived. Bridge packages frozen at their last version. Migration tools remain. |

Dates land on the timeline once `R0` ships.

## Implementation issues

- [#32](https://github.com/Fallout-build/Fallout/issues/32) — execute the namespace rename (P3)
- [#33](https://github.com/Fallout-build/Fallout/issues/33) — reserve `Fallout.*` package IDs (P4)
- [#34](https://github.com/Fallout-build/Fallout/issues/34) — rename `.csproj` files and project references (P4)
- [#36](https://github.com/Fallout-build/Fallout/issues/36) — Roslyn codefix for `using Nuke.* → Fallout.*` (P5)
- [#37](https://github.com/Fallout-build/Fallout/issues/37) — migration guide (P5)
- [#47](https://github.com/Fallout-build/Fallout/issues/47) — `Nuke.<X>` type-forwarding shim packages on GH Packages (P5)
- [#48](https://github.com/Fallout-build/Fallout/issues/48) — `Fallout.Migrate` CLI tool (P5, demand-driven)
