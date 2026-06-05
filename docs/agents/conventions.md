# Conventions

Three groups: conventions to respect, things never to do, and the tool-wrapper recipe.

## Conventions worth respecting

- **Centralized package versions** — add new packages to `Directory.Packages.props`, never inline. Adding a *meaningful* library (not a tiny transitive helper)? Add a row to [docs/dependencies.md](https://github.com/Fallout-build/Fallout/blob/main/docs/dependencies.md) in the same PR — reviewers will ask.
- **Tool wrappers**: copy/paste from neighbours; cover full commands; use `<c>`, `<a>`, `<ul>`/`<ol>`, `<em>`, `<para/>` in `help`; don't write `secret: false` or `default: xxx`. See [Tool wrapper recipe](#tool-wrapper-recipe) below.
- **Tests next to code, separate folder**: every `Foo` project under `src/` has a sibling `Foo.Tests` project under `tests/`. Mirror the namespace.
- **No IDE-specific style files committed.** `.editorconfig` and `*.DotSettings` were removed during the takeover — relying on `dotnet format` defaults and review.
- **Telemetry opt-out is set in test runs** (`FALLOUT_TELEMETRY_OPTOUT=true`). Keep it that way.
- **No per-file license headers.** The MIT notice lives in [`LICENSE`](https://github.com/Fallout-build/Fallout/blob/main/LICENSE) at the repo root, and NuGet packages declare MIT via `PackageLicenseExpression`. Per-file headers were stripped in v11 (one source of truth + the header URL would have rotted on the repo-org transfer). Vendored third-party code keeps its own copyright headers — don't touch those (e.g. files under `src/Persistence/Fallout.Persistence.Solution/` retain Microsoft's MIT notice).
- **`[Experimental]` for opt-in unstable public APIs.** Not-yet-stable public surface is marked with `[Experimental("FALLOUT0xx")]` rather than held back or shipped silently. See [the `[Experimental]` convention](#experimental-for-opt-in-unstable-apis) below and the [diagnostic-ID registry](../experimental-apis.md).

## Writing style for issues, PRs, and commits

Applies to AI tools and humans. Many readers are non-native English speakers — keep it readable.

- Be short and precise. Lead with the point.
- Prefer bullet points over paragraphs.
- Use plain, simple English. Short sentences, common words.
- Cut filler: no preamble, no hedging, no AI-flavored padding.
- Say what changed and why. Drop the rest.

Covers GitHub issues, PR titles/bodies/comments, and commit messages.

## `[Experimental]` for opt-in unstable APIs

Per [ADR-0004 §5](../adr/0004-calendar-versioning-and-dual-pace-channels.md), public APIs that aren't ready to commit to a stability guarantee are marked with [`System.Diagnostics.CodeAnalysis.ExperimentalAttribute`](https://learn.microsoft.com/dotnet/api/system.diagnostics.codeanalysis.experimentalattribute) instead of being held back or shipped silently. The attribute ships in the .NET 8+ BCL — **no package reference needed** (the repo targets .NET 10).

```csharp
using System.Diagnostics.CodeAnalysis;

[Experimental("FALLOUT001")]
public sealed class NewPluginHost
{
    // ...
}
```

**Rules:**

- **Diagnostic-ID scheme: `FALLOUT0xx`.** Each experimental surface gets its own ID (e.g. `FALLOUT001`), allocated **sequentially and never reused** — a retired ID stays retired. Register every allocation in the [diagnostic-ID registry](../experimental-apis.md) in the same PR that introduces it.
- **Consumers must explicitly opt in.** `ExperimentalAttribute` is an *error-by-default* diagnostic: code that touches the API fails to compile until the consumer suppresses the exact ID — `#pragma warning disable FALLOUT001` around the call site, or `<NoWarn>$(NoWarn);FALLOUT001</NoWarn>` in their project. Opting into instability is therefore a conscious, per-API choice — which is right for a *framework* (a product devs build on), not an app.
- **Promoting to stable = removing the attribute.** Because the feature already rode the test lanes (`experimental`/`main`) and was promoted forward, deleting the `[Experimental]` line is the whole promotion — no special cross-branch dance. This is what lets stabilised work feed into the production line without a divergent fork. Adding *or* removing `[Experimental]` is **not** a breaking change.
- **Channel discipline differs.** On the `experimental` (alpha) / `main` (preview) test lanes, churn is expected and the attribute is a courtesy. On a `release/YYYY` **production line**, any risky-but-shipped public surface **must** wear `[Experimental]` — that contract is what keeps the stable line trustworthy while still carrying new work.
- **Don't apply it speculatively.** Because the diagnostic is error-by-default, marking an API that's already used internally breaks the build everywhere it's referenced. Only add `[Experimental]` to a genuinely not-yet-stable API, and suppress every internal usage in the same change so the build stays green.

## CI pipeline & triggers

Shaped by [milestone #18](https://github.com/ChrisonSimtian/Fallout/milestone/18) and the [ADR-0004](../adr/0004-calendar-versioning-and-dual-pace-channels.md) ladder. Invariants:

- **Feature branches run zero CI until a PR is opened.** Push triggers list **only** long-lived branches; nothing fires on `feature/*`, `bugfix/*`, etc. until they're PR'd against `experimental`/`main`/`release/*`/`support/*`. Do **not** add a working-branch pattern to any `OnPush*`/`branches:` trigger.
- **The Linux PR gate (`ubuntu-latest`) is the only required check** — runs on PRs to the four long-lived branches.
- **`experimental` (push) → `-alpha`, `main` (push) → `-preview`** to GitHub Packages (`experimental.yml` / `preview.yml`).
- **Cross-platform `windows`/`macos` are gated to release intent** — PR-to-`release/*`/`support/*` or a `v*` tag push only. They do **not** run on `main`/`experimental` pushes. ("On `main` we've got our edge.")
- **`concurrency: cancel-in-progress` on every build workflow except `release.yml`** — never cancel a publish mid-flight.
- **Canonical CI-ignore paths:** `docs/**`, `.assets/**`, `**/*.md` — applied to every PR/push trigger.
- The `ubuntu-latest` / `windows-latest` / `macos-latest` workflows are **generated** from `build/Build.CI.GitHubActions.cs` — edit the attributes + constants there and regenerate (`./build.sh`), never hand-edit the `.yml`. `experimental.yml` / `preview.yml` / `release.yml` are hand-written.
- **Every publishing lane runs `Test` before it publishes** (#324). `experimental.yml`, `preview.yml`, and `release.yml` all run a single `dotnet fallout Test Pack` invocation — NUKE executes it as discrete internal stages (Restore → Compile → Test → Pack) and fails at the breaking stage, so a test failure stops the job before the push step. Don't split a lane into separate `dotnet fallout Compile`/`Test`/`Pack` steps — each invocation re-runs the dependency graph (double-compile); the single invocation *is* the staged build.
- **Caching** (#328): every workflow caches `~/.nuget/packages` + `.fallout/temp`, keyed on `global.json` + `**/*.csproj` + `Directory.Packages.props` (the dependency-affecting set), with a `restore-keys:` prefix fallback for partial restores. There is no `packages.lock.json` to add to the key, and build outputs (`bin`/`obj`) are deliberately **not** cached (stale-artifact correctness risk).

## What not to do

- Don't reintroduce `source/` — production code lives under `src/`, tests under `tests/`. Same for `images/` (now `.assets/`).
- Don't add `main`/`experimental` (or any working-branch pattern) to the **push** triggers of the cross-platform workflows — they're release-intent-gated on purpose (milestone #18 / #318 / #326).
- Don't add `submodules: recursive` to checkouts — there are no submodules (no `.gitmodules`); it's a dead init step.
- Don't add `secret`/`default` defaults to tool JSON files (see CONTRIBUTING.md).
- Don't introduce a new test framework or assertion library — stay on xUnit + FluentAssertions + Verify.
- Don't commit `output/` or any `bin/`/`obj/` directory.
- Don't commit `nuke-global.sln` or other `nuke-global.*` files — they're generated by `GenerateGlobalSolution`.
- Don't bypass `Directory.Packages.props` or `Directory.Build.targets`.
- Don't reintroduce `.editorconfig` or `*.DotSettings` without a maintainer-level decision — they were intentionally removed.

## Tool wrapper recipe

When asked to add or extend a tool wrapper:

1. Find the closest existing tool under `src/Fallout.Common/Tools/<Tool>/<Tool>.json`.
2. Copy its shape; cover a full command with all arguments.
3. Run `./build.ps1 GenerateTools` to verify it generates cleanly.
4. Do **not** commit the generated `.cs` output.
