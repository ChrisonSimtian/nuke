# API encapsulation audit

Companion to the `[PublicAPI]` annotation pass — [issue #539](https://github.com/Fallout-build/Fallout/issues/539), [ADR-0011](adr/0011-reintroduce-publicapi-ide-hint.md). Stamping `[assembly: PublicAPI]` on an outer assembly declares its **entire** public surface as intentional API, so this doc records the outer/inner split, the hybrid per-type carve-outs, and any `public`-by-accident surface the pass exposes.

> **Caveat that drives every call here:** repo-only grep is insufficient for a framework. A type with zero in-repo references can be internal plumbing **or** an extensibility surface only external consumers touch. Verify intent (NUKE history, `public`/`protected virtual` reachability, whether it appears in an outer assembly's public signatures) before flipping anything.

## Outer layer — blanket `[assembly: PublicAPI]`

Consumer-facing assemblies, listed in `_FalloutOuterApiAssemblies` in `Directory.Build.props` (single source of truth). Each gets a blanket assembly attribute:

`Fallout.Common`, `Fallout.Build`, `Fallout.Build.Shared`, `Fallout.Components`, `Fallout.Core`, `Fallout.Tooling`, `Fallout.ProjectModel`, `Fallout.Utilities(.IO.Compression|.IO.Globbing|.Net|.Text.Json|.Text.Yaml)`, `Fallout.Solution`, and the `Nuke.*` transition shims (`Nuke.Common`, `Nuke.Build`, `Nuke.Components`).

The blanket is coarse — it blesses anything `public` in these assemblies, including public-by-accident types. See **Public-by-accident (outer) — follow-up** below.

## Inner layer — no blanket

Build-time tooling, codegen, the CLI host, the migration tool, and the vendored parser. Their `public` types are implementation details behind a facade, an exe boundary, or a Roslyn/MSBuild plugin contract — not build-authoring API. Audited (2026-07-24), genuine consumer-facing surface per assembly:

| Assembly | Genuine consumer extensibility? | Note |
|---|---|---|
| `Fallout.Cli` | None | Almost entirely `internal`; `Program` is an entry point; the one `public` `Configuration` is a non-compiled scaffolding template. |
| `Fallout.Migrate` | None | Only `public static class Program` (tool entry point). |
| `Fallout.Migrate.Analyzers` | None | `DiagnosticAnalyzer` / `CodeFixProvider` — Roslyn plugin endpoints, loaded by reflection, not consumer-referenced. |
| `Fallout.MSBuildTasks` | None (1 borderline) | All public types are MSBuild `Task` endpoints consumed via `<UsingTask>`. `ContextAwareTask` is an abstract base but is subclassed **only in-assembly** — see borderline note. |
| `Fallout.SourceGenerators` | None | `ISourceGenerator` / `IIncrementalGenerator` plugin endpoints + one static helper. |
| `Fallout.Tooling.Generator` | None | Referenced only by `Fallout.MSBuildTasks` (inner→inner codegen). Its interfaces/models read like an API but no outer assembly or consumer reaches them. |
| `Fallout.Persistence.Solution` | **Yes** — see hybrid carve-out | Vendored parser; already encapsulated (`internal`-dominated). A small model whitelist leaks through `Fallout.Solution`'s public API. |

## Hybrid carve-out — per-type `[PublicAPI]` in `Fallout.Persistence.Solution`

`Fallout.Persistence.Solution` stays **off** the outer allowlist deliberately: its csproj keeps most types `internal`, exposing only a small whitelist. A blanket would be fragile — a future accidental `public` in vendored code would be silently blessed. Instead, the model types that appear directly in `Fallout.Solution`'s public constructors / `GetModel()` returns carry a per-type `[PublicAPI]` in source:

| Type | File | Surfaces via |
|---|---|---|
| `SolutionModel` | `Model/SolutionModel.cs` | `Solution(SolutionModel, …)` ctor + `Solution.GetModel()` |
| `SolutionItemModel` | `Model/SolutionItemModel.cs` | `SolutionItem(SolutionItemModel, …)` ctor (abstract base of the two below) |
| `SolutionProjectModel` | `Model/SolutionProjectModel.cs` | `Project(SolutionProjectModel, …)` ctor + `Project.GetModel()` |
| `SolutionFolderModel` | `Model/SolutionFolderModel.cs` | `SolutionFolder(SolutionFolderModel, …)` ctor + `SolutionFolder.GetModel()` |

The project takes a non-private `JetBrains.Annotations` reference so the annotation flows to consumers of the (packed) `Fallout.Persistence.Solution` package.

**Line drawn at "appears in an outer assembly's public signature."** Types reachable only by *navigation* from the four above — `PropertyContainerModel` (their base), `SolutionPropertyBag` + `PropertiesScope`, `ProjectType`, `ConfigurationRule` + `BuildDimension`, `StringTable` — are **not** annotated. They're candidates if reviewers want the deeper model graph covered, or if the facade later exposes them directly. The serializer surface (`ISolutionSerializer*`, `ISerializerModelExtension*`, `SolutionSerializers`, the `Solution*Exception` types) is a vendored-library extension point Fallout does not expose through its facade — left unannotated.

### Borderline

- `ContextAwareTask` (`Fallout.MSBuildTasks`) — an abstract MSBuild-task base with `protected abstract`/`virtual` members, but subclassed only within its own assembly. Not annotated; revisit if it becomes a supported consumer base.

## Public-by-accident (outer) — follow-up

The blanket on outer assemblies blesses their *entire* public surface. A full sweep for types that are `public` by accident (implementation detail that should be `internal`) is **tracked follow-up**, not done in this pass — narrowing any of them is a breaking change and follows the breaking-change flow (gate behind `[Experimental("FALLOUT0xx")]` or hold for the year cut; `CHANGELOG` + `breaking-change` label). Do not silently internalize an outer public type on the strength of in-repo grep alone.
