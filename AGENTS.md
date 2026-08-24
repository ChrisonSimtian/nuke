# AGENTS.md

Guidance for AI coding tools (Claude Code, GitHub Copilot, Cursor, Aider, Codex, etc.) working in this repo.

This is the **canonical brief**. GitHub Copilot reads this `AGENTS.md` natively; the `CLAUDE.md` tool-specific file points here.

## What this project is

**Fallout** — a build automation system for C#/.NET, hard-fork successor to [NUKE](https://github.com/nuke-build/nuke). The build is itself a C# console app (`build/_build.csproj`), so any change to the framework can be dogfooded by running `./build.ps1` (Windows) or `./build.sh`.

Originally NUKE by [matkoch](https://github.com/matkoch); under new maintenance as of 2026 and being renamed to Fallout. The codebase is mature, large, and has long-standing conventions — prefer matching existing patterns over introducing new ones.

**Rebrand status:** the structural rename has landed — namespaces (`Fallout.*`), package IDs, project filenames, and the global tool name (`dotnet fallout`) are all in place. Legacy `Nuke.*` lives on only as the consumer transition shims under `src/Shims/`. The mapping is a strict 1:1 prefix swap — every `Nuke.X.Y.Z` namespace and assembly is `Fallout.X.Y.Z`, with no consolidation — which `[TypeForwardedTo]` in the shims locks in place. Consumer-facing migration is [docs/Migration/from-nuke.md](docs/Migration/from-nuke.md).

**Versioning & channels (classic GitFlow, staying on semver 10.x — [ADR-0009](docs/adr/0009-gitflow-and-semver-reversion.md), replacing [ADR-0004](docs/adr/0004-calendar-versioning-and-dual-pace-channels.md)).** The project stays on **classic semver `10.x`** — no renumbering, and no jump to v11 until a breaking change is actually needed (deliberately deferred as long as possible; see below). `main` and `develop` are renames of branches that already existed (`release/v10.4` and `main`), not new branches. **GitHub Packages = test/preview/rc; nuget.org = production**:
- **`develop` = the integration trunk + the only preview channel** (renamed from `main`). This is the default branch. Both deliberate work and faster, AI-assisted work land here. Every push publishes a `-preview` prerelease (`10.5.0-preview.<height>.g<commit>`) to **GitHub Packages only — never nuget.org**. Ordinary review applies. There is still no separate `experimental`/`-alpha` branch (ADR-0008's decision stands, just renamed onto `develop`).
- **`release/vX.Y` = the branch that stabilizes a release** (e.g. `release/v10.5`). It's cut from `develop` on demand, at the first release of the next minor — not ahead of time (this keeps [ADR-0007](docs/adr/0007-cut-release-branch-on-demand.md)'s rule). It takes `-rc.N` prereleases. After it's cut, only non-breaking fixes land on it. Review is rigorous.
- **`main` = the production branch** (renamed from `release/v10.4`). It only takes merges from `release/vX.Y` (for GA) or a `hotfix/vX.Y.Z` branch. GA tags fire here, publishing to nuget.org (opt-in) plus GitHub Packages and GitHub Releases.
- **Every `10.x` release is non-breaking** — `10.4`, `10.5`, `10.6`, and so on never break a consumer, so Dependabot/Renovate upgrades within `10.x` are always safe. A breaking change waits for **v11**, with no fixed date — the project puts that decision off for as long as it can. When it happens, the upgrade will go through `fallout-migrate`, the same tool used for the NUKE-to-Fallout move. Until then, any breaking idea lands on `develop` behind `[Experimental("FALLOUT0xx")]` (or a short-lived branch off `develop`), with no fixed release target.
- **The legacy `support/v10` line** (plus `hotfix/v10.x`) is separate from `main` — it takes security and critical fixes for versions older than `10.4` (`10.0.x`–`10.3.x`). It is not affected by anything above.
- Public APIs that aren't stable yet are marked `[Experimental("FALLOUT0xx")]` and can ship on any channel. Making one stable means removing the attribute.

**Active work** — rebrand completion + plugin-architecture internal foundation ([milestone #6](https://github.com/Fallout-build/Fallout/milestone/6)), shipping as non-breaking `10.x` releases. **No public plugin SDK yet** — that ships once the project decides to cut v11 ([milestone #7](https://github.com/Fallout-build/Fallout/milestone/7)). Internal middleware/listener interfaces stay `internal`; do not expose via `InternalsVisibleTo` to non-test assemblies. See [docs/roadmap.md](docs/roadmap.md) and the five open RFCs ([#97](https://github.com/Fallout-build/Fallout/issues/97)–[#101](https://github.com/Fallout-build/Fallout/issues/101)).

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

1. **At PR-creation time, follow the [PR-creation flow](docs/agents/release-and-versioning.md#pr-creation-flow) in `docs/agents/release-and-versioning.md`.** That flow covers working from a fork: branch off `upstream/develop`, push to `origin`, open the PR against `upstream`. Create the PR as a draft by default, and label it. Every PR gets a `target/vCurrent` label (or `target/vNext` for work held for the next major) and a changelog-category label — [`.github/release.yml`](.github/release.yml) lists the label taxonomy, and AI applies the matching one whenever it raises a PR. A breaking change also gets a `breaking-change` label and a `⚠️ Breaking change` callout in the PR description that names the migration path. The label carries it into the generated release notes. Breaking changes wait for the **next major** — they don't ship mid-cycle. A breaking-change PR targets **`develop`**, with the breaking surface behind `[Experimental("FALLOUT0xx")]` (or, if that doesn't fit, on a short-lived branch off `develop` held until the cut). It **never** targets a `release/vX.Y` or `main` production branch. This rule is not optional — review will block a PR that breaks it.
2. **Default to backwards compatibility.** Prefer an additive change over a breaking one. Before changing a public signature, removing an API, renaming a package, or changing an on-disk format, ask: can this be additive instead? Prefer `[Obsolete]` markers, transition shims (see `src/Shims/` + `Fallout.SourceGenerators.TransitionShimGenerator`), the `[Experimental("FALLOUT0xx")]` opt-in for surface that isn't stable yet, feature flags, and extra overloads over a hard break. When a breaking change really can't be avoided, it lands on `develop` behind `[Experimental("FALLOUT0xx")]` (or on a short-lived branch held for the next major), and follows rule #1's flow. The break must be deliberate, named, and have a migration path in the PR description. See [#262](https://github.com/Fallout-build/Fallout/issues/262) for the broader discussion. The `[Experimental]` convention (its diagnostic-ID scheme and registry) is documented in [docs/agents/conventions.md](docs/agents/conventions.md#experimental-for-opt-in-unstable-apis) and [docs/experimental-apis.md](docs/experimental-apis.md). Deprecations use `[Obsolete]` with a `FALLOUTOBS0xx` diagnostic ID, so a consumer using `TreatWarningsAsErrors` can suppress just that one deprecation — see [docs/agents/conventions.md](docs/agents/conventions.md#obsolete-for-deprecating-public-apis) and the [docs/obsolete_apis.md](docs/obsolete_apis.md) registry.
3. **Central package versions only** — add to `Directory.Packages.props`, never `Version=` inline.
4. **Tests next to code** — every `src/Foo` has a `tests/Foo.Tests` sibling. Mirror namespaces.
5. **Stay on xUnit + FluentAssertions + Verify.** Don't introduce new test frameworks.
6. **No per-file license headers.** The MIT notice lives in [`LICENSE`](LICENSE) at the repo root — single source of truth. Don't reintroduce header preambles on new files.
7. **No conventional commits.** Do not use `feat:`, `fix:`, `chore:`, `refactor:`, or any other conventional-commit prefix on commit messages or PR titles. Write functional descriptions that explain what the commit or PR accomplishes — e.g. "Add retry logic to the HTTP tool wrapper" or "Fix null-reference in target dependency resolution". The only exception is the `!` suffix (e.g. `fix(security)!: …`) used as a **detection signal** for breaking changes, not as a general style requirement.
8. **Write issues and PR descriptions terse.** Follow [docs/agents/issue-and-pr-style.md](docs/agents/issue-and-pr-style.md): lead with the point, cut filler, bullets over prose, link don't recap, match length to substance. Issues use the **Problem → Outcome → Acceptance criteria** shape.
9. **Never ping the former NUKE maintainer, Matthias Koch (GitHub handle `matkoch`).** He no longer maintains Fallout and does not want the notifications. Do not `@`-mention him (never write `@` before his handle, in any file or GitHub surface), add him as a reviewer/assignee, request his review, tag him in issue/PR/commit text, or add him as a commit co-author/`Co-authored-by:` trailer — from any AI tool. Credit NUKE's origin by name or a plain profile link — just never with a leading `@` (a bare `@handle` is what fires a mention). Full rule in [docs/agents/conventions.md](docs/agents/conventions.md#what-not-to-do).

Full conventions + what-not-to-do list: [docs/agents/conventions.md](docs/agents/conventions.md).

## Where to look next

- **[docs/agents/repository-layout.md](docs/agents/repository-layout.md)** — full directory structure, project groupings, transition-shim strategy
- **[docs/agents/release-and-versioning.md](docs/agents/release-and-versioning.md)** — branching, semver policy, PR-creation flow, release pipeline, NuGet gotchas
- **[docs/branching-and-release.md](docs/branching-and-release.md)** — maintainer runbook for cutting releases, hotfixing older majors, cutting new `release/vX.Y` branches
- **[docs/adr/](docs/adr/)** — Architecture Decision Records (read `0009-gitflow-and-semver-reversion.md` and `0001-release-branch-model.md` for the release model)
- **[docs/agents/conventions.md](docs/agents/conventions.md)** — conventions, what-not-to-do list, tool-wrapper recipe
- **[docs/agents/issue-and-pr-style.md](docs/agents/issue-and-pr-style.md)** — how to write terse issues, user stories, and PR descriptions
- **[docs/architecture.md](docs/architecture.md)** — high-level architecture overview
- **[docs/Migration/from-nuke.md](docs/Migration/from-nuke.md)** — consumer-facing NUKE → Fallout migration guide
- **[docs/roadmap.md](docs/roadmap.md)** — themed milestones and RFCs
- **[docs/dependencies.md](docs/dependencies.md)** — third-party dependencies (update when adding meaningful libraries)
- **[CONTRIBUTING.md](CONTRIBUTING.md)** — contributor-facing flow (branching, PR review, merging convention)

## Useful pointers

- The `build/Build.*.cs` files are the canonical example of how to consume the framework — read these when reasoning about user-facing APIs.
- `src/Fallout.Common/Tools/<Tool>/<Tool>.json` files are the source of truth for tool wrappers; the `.cs` next to them is generated.
- `fallout-migrate` is a pipeline of `IMigrationStep`s (`src/Fallout.Migrate/Migration.cs`). A step is one operation over one set of files, and it owns the rewrite rules for those files. Add a new rename to the step that already covers that file type — do **not** add a step per rename. Recipe: [docs/agents/conventions.md](docs/agents/conventions.md#migration-step-recipe).
- Source generators (`src/Fallout.SourceGenerators`) produce per-target code at compile time — if a symbol seems missing, check whether it's generated.
- The Verify snapshots (`*.verified.txt`, `*.verified.cs`) under `tests/` are the contract for generator output; review carefully when they change.
