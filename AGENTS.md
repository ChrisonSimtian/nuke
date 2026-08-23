# AGENTS.md

Guidance for AI coding tools (Claude Code, GitHub Copilot, Cursor, Aider, Codex, etc.) working in this repo.

This is the **canonical brief**. GitHub Copilot reads this `AGENTS.md` natively; the `CLAUDE.md` tool-specific file points here.

## What this project is

**Fallout** — a build automation system for C#/.NET, hard-fork successor to [NUKE](https://github.com/nuke-build/nuke). The build is itself a C# console app (`build/_build.csproj`), so any change to the framework can be dogfooded by running `./build.ps1` (Windows) or `./build.sh`.

Originally NUKE by [matkoch](https://github.com/matkoch); under new maintenance as of 2026 and being renamed to Fallout. The codebase is mature, large, and has long-standing conventions — prefer matching existing patterns over introducing new ones.

**Rebrand status:** the structural rename has landed — namespaces (`Fallout.*`), package IDs, project filenames, and the global tool name (`dotnet fallout`) are all in place. Legacy `Nuke.*` lives on only as the consumer transition shims under `src/Shims/`. The mapping is a strict 1:1 prefix swap — every `Nuke.X.Y.Z` namespace and assembly is `Fallout.X.Y.Z`, with no consolidation — which `[TypeForwardedTo]` in the shims locks in place. Consumer-facing migration is [docs/website/migration/from-nuke.md](docs/website/migration/from-nuke.md).

**Versioning & channels (calendar versioning, two-tier ladder — [ADR-0004](docs/website/adr/0004-calendar-versioning-and-dual-pace-channels.md), channel ladder superseded by [ADR-0008](docs/website/adr/0008-collapse-experimental-into-main.md)).** The project ships on **calendar versions `YYYY.MINOR.PATCH`** (mechanically valid semver; major = year). A maturity ladder feeds the production line — **GitHub Packages = test/preview; nuget.org = production**:
- **`main` = the integration trunk + sole `-preview` channel** — default branch; both deliberate improvements/bug fixes *and* faster AI-assisted work land here. Per-commit `-preview` prereleases (`2026.1.0-preview.<height>.g<commit>`) to **GitHub Packages only — never nuget.org**. Ordinary review. (The dedicated `experimental` `-alpha` lane was removed by ADR-0008 — it ran behind `main` and carried no unique work.)
- **`release/YYYY` = the production line** — **cut from `main` on demand at the first release of the year, not preemptively** ([ADR-0007](docs/website/adr/0007-cut-release-branch-on-demand.md)); until then `main` (`-preview`) is the most-stable line. Hardened deliberately, `-rc.N` → GA, non-breaking minors/patches only after the cut, rigorous review. Tags publish to nuget.org (opt-in) + GitHub Packages + GitHub Releases.
- **Breaking changes are batched to the yearly major cut** — they accumulate on `main` gated behind `[Experimental("FALLOUT0xx")]` (or a short-lived topic branch off `main` when they can't be gated) and ship as next year's `YYYY+1.0.0`. Mid-year `main`/production is strictly non-breaking; the production-cut review is the backstop. Version ladder: `-preview` < `-rc` < GA.
- **Legacy `support/v10`** (renamed from `release/v10`; + `hotfix/v10.x`) stays on semver `10.x`, security/critical fixes only; retired year lines become **`support/YYYY`**. **`release/v11` is retired and its branch removed** (nothing clean shipped; its work re-homed onto the 2026 line) — dead branches with no unique history are now deletable, tags are the durable release markers ([ADR-0007](docs/website/adr/0007-cut-release-branch-on-demand.md) §6).
- Opt-in unstable public APIs are marked `[Experimental("FALLOUT0xx")]` and can ride any channel; promoting to stable = removing the attribute.

**Active work** — rebrand completion + plugin-architecture internal foundation ([milestone #6](https://github.com/Fallout-build/Fallout/milestone/6)), now shipping on the `2026` line. **No public plugin SDK yet** — that's a later major ([milestone #7](https://github.com/Fallout-build/Fallout/milestone/7)). Internal middleware/listener interfaces stay `internal`; do not expose via `InternalsVisibleTo` to non-test assemblies. See [docs/roadmap.md](docs/roadmap.md) and the five open RFCs ([#97](https://github.com/Fallout-build/Fallout/issues/97)–[#101](https://github.com/Fallout-build/Fallout/issues/101)).

## Stack

- .NET SDK pinned in `global.json` (currently `10.0.100`, `rollForward: latestMinor`).
- Central package versions in `Directory.Packages.props` — never add a `Version=` to an individual `PackageReference`.
- xUnit + FluentAssertions + Verify.Xunit for tests.
- Solution file is `fallout.slnx` (new XML solution format, not `.sln`).
- Dependency updates: Handled by Dependabot (weekly grouped PRs).

## Common commands

```powershell
./build.ps1                          # default target = Pack
./build.ps1 Compile
./build.ps1 Test
./build.ps1 GenerateTools            # regenerate tool wrappers from JSON
./build.ps1 --help                   # list all targets and parameters

# Or via dotnet directly when iterating on a single project
dotnet build fallout.slnx
dotnet test tests/Fallout.Common.Tests/Fallout.Common.Tests.csproj
```

Do commit code generated by `GenerateTools` — the `.Generated.cs` files are checked in, and `VerifyGeneratedTools` fails CI if a `.json` spec edit isn't accompanied by regenerating and committing its wrapper.

To restructure an existing PR's commit history into focused commits, use the `/restructure-pr-commits` skill.

## Critical rules (read this every session)

1. **At PR-creation time, follow the [PR-creation flow](docs/website/agents/release-and-versioning.md#pr-creation-flow) in `docs/website/agents/release-and-versioning.md`.** That flow covers working from a fork (branch off `upstream/main`, push to `origin`, PR against `upstream`), creating the PR as a draft by default, and labelling. Every PR gets a `target/vCurrent` label (or `target/vNext` for work held to next year's major) and a changelog-category label — [`.github/release.yml`](.github/release.yml) is the source of truth for the label taxonomy and AI applies the matching one whenever it raises a PR. Breaking changes additionally get a `breaking-change` label and a `⚠️ Breaking change` callout in the PR description naming the migration path — the label is what carries them into the generated release notes, and they are held for the **next yearly major** (breaking changes are batched to the year cut, not shipped mid-year). A breaking-change PR targets **`main`** with the breaking surface gated behind `[Experimental("FALLOUT0xx")]` (or, when it can't be gated, on a short-lived topic branch off `main` held for the year cut) — **never** a `release/YYYY` production train. This is non-negotiable — review will block. (Before [ADR-0008](docs/website/adr/0008-collapse-experimental-into-main.md) breaking work targeted the `experimental` branch; that branch is gone.)
2. **Default to backwards compatibility.** Prefer additive over breaking changes. Before changing a public signature, removing an API, renaming a package, or altering an on-disk format, ask: can this be additive instead? `[Obsolete]` markers, transition shims (see `src/Shims/` + `Fallout.SourceGenerators.TransitionShimGenerator`), the `[Experimental("FALLOUT0xx")]` opt-in escape hatch for not-yet-stable surface, feature flags, and overload-based extension are all preferred to a hard break. When a breaking change is genuinely unavoidable, it lands on `main` (gated behind `[Experimental("FALLOUT0xx")]`, or held on a short-lived topic branch for the next yearly major), and follows rule #1's flow — the break must be deliberate, named, and migration-pathed in the PR description. See [#262](https://github.com/Fallout-build/Fallout/issues/262) for the broader discussion. The `[Experimental]` convention (diagnostic-ID scheme + registry) is documented in [docs/website/agents/conventions.md](docs/website/agents/conventions.md#experimental-for-opt-in-unstable-apis) and [docs/website/experimental-apis.md](docs/website/experimental-apis.md). Deprecations use `[Obsolete]` with a `FALLOUTOBS0xx` `DiagnosticId` so `TreatWarningsAsErrors` consumers can suppress a single deprecation — see [docs/website/agents/conventions.md](docs/website/agents/conventions.md#obsolete-for-deprecating-public-apis) and the [docs/website/obsolete_apis.md](docs/website/obsolete_apis.md) registry.
3. **Central package versions only** — add to `Directory.Packages.props`, never `Version=` inline.
4. **Tests next to code** — every `src/Foo` has a `tests/Foo.Tests` sibling. Mirror namespaces.
5. **Stay on xUnit + FluentAssertions + Verify.** Don't introduce new test frameworks.
6. **No per-file license headers.** The MIT notice lives in [`LICENSE`](LICENSE) at the repo root — single source of truth. Don't reintroduce header preambles on new files.
7. **No conventional commits.** Do not use `feat:`, `fix:`, `chore:`, `refactor:`, or any other conventional-commit prefix on commit messages or PR titles. Write functional descriptions that explain what the commit or PR accomplishes — e.g. "Add retry logic to the HTTP tool wrapper" or "Fix null-reference in target dependency resolution". The only exception is the `!` suffix (e.g. `fix(security)!: …`) used as a **detection signal** for breaking changes, not as a general style requirement.
8. **Write issues and PR descriptions terse.** Follow [docs/website/agents/issue-and-pr-style.md](docs/website/agents/issue-and-pr-style.md): lead with the point, cut filler, bullets over prose, link don't recap, match length to substance. Issues use the **Problem → Outcome → Acceptance criteria** shape.
9. **Never ping the former NUKE maintainer, Matthias Koch (GitHub handle `matkoch`).** He no longer maintains Fallout and does not want the notifications. Do not `@`-mention him (never write `@` before his handle, in any file or GitHub surface), add him as a reviewer/assignee, request his review, tag him in issue/PR/commit text, or add him as a commit co-author/`Co-authored-by:` trailer — from any AI tool. Credit NUKE's origin by name or a plain profile link — just never with a leading `@` (a bare `@handle` is what fires a mention). Full rule in [docs/website/agents/conventions.md](docs/website/agents/conventions.md#what-not-to-do).

Full conventions + what-not-to-do list: [docs/website/agents/conventions.md](docs/website/agents/conventions.md).

## Where to look next

- **[docs/website/agents/repository-layout.md](docs/website/agents/repository-layout.md)** — full directory structure, project groupings, transition-shim strategy
- **[docs/website/agents/release-and-versioning.md](docs/website/agents/release-and-versioning.md)** — branching, semver policy, PR-creation flow, release pipeline, NuGet gotchas
- **[docs/website/branching-and-release.md](docs/website/branching-and-release.md)** — maintainer runbook for cutting releases, hotfixing older majors, cutting new `release/vN` branches
- **[docs/website/adr/](docs/website/adr/)** — Architecture Decision Records (read `0004-calendar-versioning-and-dual-pace-channels.md` and `0001-release-branch-model.md` for the release model)
- **[docs/website/agents/conventions.md](docs/website/agents/conventions.md)** — conventions, what-not-to-do list, tool-wrapper recipe
- **[docs/website/agents/issue-and-pr-style.md](docs/website/agents/issue-and-pr-style.md)** — how to write terse issues, user stories, and PR descriptions
- **[docs/website/architecture.md](docs/website/architecture.md)** — high-level architecture overview
- **[docs/website/migration/from-nuke.md](docs/website/migration/from-nuke.md)** — consumer-facing NUKE → Fallout migration guide
- **[docs/roadmap.md](docs/roadmap.md)** — v11/v12/v13 milestones and RFCs
- **[docs/dependencies.md](docs/dependencies.md)** — third-party dependencies (update when adding meaningful libraries)
- **[CONTRIBUTING.md](CONTRIBUTING.md)** — contributor-facing flow (branching, PR review, merging convention)

## Useful pointers

- The `build/Build.*.cs` files are the canonical example of how to consume the framework — read these when reasoning about user-facing APIs.
- `src/Fallout.Common/Tools/<Tool>/<Tool>.json` files are the source of truth for tool wrappers; the `.cs` next to them is generated.
- `fallout-migrate` is a pipeline of `IMigrationStep`s (`src/Fallout.Migrate/Migration.cs`). A step is one operation over one set of files, and it owns the rewrite rules for those files. Add a new rename to the step that already covers that file type — do **not** add a step per rename. Recipe: [docs/website/agents/conventions.md](docs/website/agents/conventions.md#migration-step-recipe).
- Source generators (`src/Fallout.SourceGenerators`) produce per-target code at compile time — if a symbol seems missing, check whether it's generated.
- The Verify snapshots (`*.verified.txt`, `*.verified.cs`) under `tests/` are the contract for generator output; review carefully when they change.
