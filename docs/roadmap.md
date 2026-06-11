# Fallout roadmap

This document explains where Fallout is going and why. It's the canonical reference for the next two themed milestones. If you want to influence the direction, the RFCs linked below are open for comment now.

> **Versioning note.** Fallout moved to **calendar versioning** ([ADR-0004](adr/0004-calendar-versioning-and-dual-pace-channels.md)): versions are `YYYY.MINOR.PATCH` with the major set to the year. The work below was originally framed as "v11" and "v12" majors; under CalVer, the **rebrand + plugin-foundation** milestone ships on the **2026** line and the **public plugin SDK** lands in a **later calendar major**. The retired `v11` numbering carried no clean release — its work re-homed onto 2026. Milestones are theme-based and carry across releases; the version a piece of work ships under is its calendar year.

## Where we are

Fallout is the hard-fork successor to [NUKE](https://github.com/nuke-build/nuke). The rebrand from NUKE to Fallout is in flight — most of the renaming has landed; consumer-facing migration tooling, the documentation site port, and the coordinated announcement are still in progress. Active development ships on the **2026** line (`main` → preview is the sole prerelease lane, production = `release/2026`); the NUKE-era `10.x` line (`support/v10`) is now a legacy maintenance line (security/critical fixes only).

## The next two themed milestones

### Rebrand completion + plugin-architecture foundation (2026 line)

**Milestone:** [Plugin architecture foundation & rebrand completion](https://github.com/Fallout-build/Fallout/milestone/6)

This milestone finishes the rebrand and lays the internal groundwork for a plugin model. It bundles two workstreams so consumers face a single migration wave instead of two:

**Workstream A — Rebrand completion.** Ship the `Nuke.*` → `Fallout.*` transition shims with full public-API coverage, the migration CLI (`Fallout.Migrate`), the Roslyn codefix for `using` directives, the migration guide, the documentation site port, and the coordinated announcement.

**Workstream B — Plugin-architecture foundation (internal).** Restructure the build orchestrator so a plugin SDK can sit on top later:
- Extract a `Fallout.Core` domain project — pure, immutable, dependency-free.
- Introduce dependency injection inside the orchestrator (consumer-facing `Execute<T>()` stays unchanged).
- Wrap IO, process, and console statics behind injectable interfaces (statics on the surface, DI underneath).
- Split the `FalloutBuild` god class into its three distinct responsibilities.
- Replace the 23 init-pipeline attributes with an internal `IBuildMiddleware` pipeline.
- Add architectural-fitness tests so the new layers stay clean.
- Lift orchestration-path test coverage to ~80%.

**What this milestone is not:** it ships **no public plugin SDK**. The internal interfaces (`IBuildMiddleware`, `ITargetLifecycleListener`, infrastructure interfaces) stay `internal`. Their shape needs to bake through dogfooding before becoming part of a public contract — see the SDK milestone below.

The full ticket list is in [milestone #6](https://github.com/Fallout-build/Fallout/milestone/6). Workstream A is the release-blocker; Workstream B slips to a later 2026 minor if it threatens the rebrand completion.

### Public plugin SDK (later calendar major)

**Milestone:** [Public plugin SDK](https://github.com/Fallout-build/Fallout/milestone/7)

This milestone commits the public contract that third-party contributors compile their plugins against. After the foundation has been dogfooded, the shapes that prove themselves become public, additive-only API in `Fallout.Plugin.Sdk` 1.0:

- A plugin will be a NuGet package + a single `[assembly: FalloutPlugin(typeof(...))]` attribute + an `IFalloutPlugin.Configure(builder)` method. Same convention as Roslyn analyzers, MSBuild SDKs, source generators.
- The SDK exposes a closed catalogue of extension points (build middleware, CI host adapters, parameter sources, tool-wrapper contributions, lifecycle listeners, output sinks).
- The SDK versions independently of the Fallout host. Strict additive-only minors; breaking changes only in majors (batched to the yearly cut); major bumps are rare.
- First-party CI host adapters migrate to the SDK as dogfood proof.
- A canonical sample plugin + plugin author's guide ship with the SDK.

The exact shape isn't pinned yet — that's what the RFCs are for.

### Beyond the plugin SDK — Continuous Delivery vision

**Milestone:** [Continuous Delivery vision](https://github.com/Fallout-build/Fallout/milestone/8)

Long-horizon direction: extend Fallout beyond CI into release management and deployment orchestration — the space currently owned by TeamCity, Octopus Deploy, and the release stages of Azure DevOps Pipelines. The wedge hypothesis is the same one that powers Fallout-on-CI: C#-native, code-first, single binary, leveraging the plugin SDK.

Shape is genuinely TBD. The milestone collects RFCs and scoping work; no release-date commitment. Comment on [#106](https://github.com/Fallout-build/Fallout/issues/106) with deployment shapes you'd want to use Fallout for.

## How to engage — RFCs open now

Five RFC issues are open to shape the public plugin SDK. These are **self-RFCs**: each one ships an opinionated proposal and asks "tell me where I'm wrong" rather than "what should we do." Strong opinions backed by use cases are the most useful kind of comment.

- [RFC #1 — Plugin contract shape](https://github.com/Fallout-build/Fallout/issues/97): how a plugin declares itself.
- [RFC #2 — Extension-point catalogue](https://github.com/Fallout-build/Fallout/issues/98): what plugins can contribute.
- [RFC #3 — SDK versioning policy](https://github.com/Fallout-build/Fallout/issues/99): how the SDK evolves without breaking plugins.
- [RFC #4 — Plugin discovery and load model](https://github.com/Fallout-build/Fallout/issues/100): runtime mechanics, trust model.
- [RFC #5 — Conflict resolution semantics](https://github.com/Fallout-build/Fallout/issues/101): what happens when two plugins step on each other.

**Comment by 2026-08-31.** After that date, each proposed shape locks, rolls into the implementing SDK-* issue named on the RFC, and the RFC closes as decided. The proposals are the working baseline; silence means assent.

The most useful contribution you can make right now is naming a plugin you'd want to build and showing where the proposals don't fit it. "I want to ship a plugin that does X — your model breaks because of Y" is gold.

## Timeline

Honest version: the rebrand-plus-foundation milestone is ambitious. The rebrand workstream alone is non-trivial; pairing it with a substantial internal restructure means timelines will slip if both are kept tight. Workstream A (rebrand) is gated; Workstream B (foundation) ships when it's ready or moves to a later 2026 minor.

The rebrand + plugin foundation ships on the **2026** line; the public plugin SDK follows in a **later calendar major**, after the foundation has been dogfooded for some weeks. We'll update this section when the dates firm up.

## What this roadmap doesn't cover

- The full backlog of bug fixes, polish, and incremental improvements outside the plugin-architecture theme. See [open issues](https://github.com/Fallout-build/Fallout/issues) for those.
- CI provider revival (Azure Pipelines, GitLab, TeamCity, etc.) — tracked as demand-driven in the [CI roadmap milestone](https://github.com/Fallout-build/Fallout/milestone/4).
- Continuous Delivery (deployment orchestration, environment promotion, release tracking) — see the [Continuous Delivery vision milestone](https://github.com/Fallout-build/Fallout/milestone/8) for the placeholder RFC.
- Long-tail design discussion without a release target — see the [Backlog milestone](https://github.com/Fallout-build/Fallout/milestone/3).

## Versioning and stability promises

A note on what we are and aren't committing to:

- **The 2026 foundation milestone is "foundation laid," not "stable plugin contract."** The internal interfaces shaped during it may change without notice. Do not take a dependency on them via reflection or `InternalsVisibleTo` — there will be no compatibility support for that path. (Not-yet-stable public surface that does ship is marked `[Experimental("FALLOUT0xx")]` — opt-in, and free to change.)
- **The plugin SDK's `Fallout.Plugin.Sdk` is the stable contract.** Once 1.0 ships, the additive-only minors / breaking-changes-only-in-majors policy in RFC #3 governs every change.
- **The consumer-facing build DSL** (`class Build : FalloutBuild`, `[Parameter]`, `[Solution]`, `Target X => _ => _.DependsOn(...).Executes(...)`, static tool facades like `DotNetBuild(...)`) stays stable across the foundation and SDK milestones. The internal restructure does not change how you author a `_build.csproj`.
