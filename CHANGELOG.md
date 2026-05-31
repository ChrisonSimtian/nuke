# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [Unreleased] — 2026.0

### Breaking changes

- **Adopted calendar versioning (`YYYY.MINOR.PATCH`) + dual-pace channel model; retired the v11 numbering** ([ADR-0004](docs/adr/0004-calendar-versioning-and-dual-pace-channels.md)). Fallout now ships on calendar versions (`2026.0.0`, `2026.1.0`, …) — mechanically valid SemVer with the major equal to the calendar year. **Breaking changes are batched to the yearly major cut**; mid-year stable releases (`release/YYYY`) are strictly non-breaking. `main` becomes the published **edge** channel (date-stamped prereleases to GitHub Packages); the slow/stable track lives on `release/YYYY`. Opt-in unstable APIs are marked `[Experimental("FALLOUT0xx")]`.
  - **Migration / impact**: the `11.0.x` packages never shipped a clean stable release (all unlisted), so this strands no stable consumers. The headline content previously slated for "v11" now ships as **`2026.0.0`**. Any tooling pinned to a `[11.0,12.0)`-style range should retarget the `2026.x` line.
  - **Legacy unaffected**: the `release/v10` line stays on semver `10.x` and continues to receive security/critical fixes — v10 consumers do nothing.
- **Inlined `vs-solutionpersistence` parser + renamed `Fallout.SolutionModel` → `Fallout.Solution`; namespace `Fallout.Common.ProjectModel` → `Fallout.Solutions`** (closes #248). The vendored Microsoft fork (`vendor/vs-solutionpersistence/`, submodule, fork-of-fork-of-Microsoft) is gone — sources inlined into a new `Fallout.Persistence.Solution` project under `src/Persistence/`. The facade was renamed and rehoused alongside it (`src/Persistence/Fallout.Solution/`), with the long-standing rebrand-era namespace mismatch (`Fallout.Common.ProjectModel`) fixed to match the assembly name (`Fallout.Solutions`, plural per BCL convention to avoid `Fallout.Solution.Solution` awkwardness).
  - **Package IDs**: `Fallout.SolutionModel` → `Fallout.Solution`, `Fallout.VisualStudio.SolutionPersistence` → `Fallout.Persistence.Solution`. Consumers that explicitly referenced either need to update their `PackageReference`/`ProjectReference`.
  - **Namespaces**: `Fallout.Common.ProjectModel` → `Fallout.Solutions`; `Microsoft.VisualStudio.SolutionPersistence.{Model,Serializer,Utilities,...}` → `Fallout.Persistence.Solution.{Model,Serializer,Utilities,...}`. Replace `using` statements accordingly. The `Nuke.Common.ProjectModel.*` transition-shim path is preserved (`ShimMarker.cs` now mirrors both `Fallout.Common.*` → `Nuke.Common.*` and `Fallout.Solutions.*` → `Nuke.Common.ProjectModel.*`), so NUKE-era consumer code using `using Nuke.Common.ProjectModel; [Solution] readonly Solution Solution;` keeps compiling.
  - **Onion layering**: this PR establishes `src/Persistence/` as the layered home for persistence-ring code. Future persistence-related projects go under the same directory.
  - **Visibility narrowing deferred**: parser types remain `public` for this PR; the IVT decorations in `Fallout.Persistence.Solution.csproj` are future-intent. Per-type internal narrowing will follow in a later PR (cascading `CS0050` analysis needed).
  - **Codefix follow-up**: `Fallout.Migrate.Analyzers`'s `Nuke`→`Fallout` rewriter still produces `Fallout.Common.ProjectModel.*` rather than `Fallout.Solutions.*`. Tracked separately.
  - **Consumer-compat sentinel updated**: `tests/Consumers/Fallout.Consumer.Local/` (added in the prior PR to detect exactly this kind of rename) had its `ProjectReference` path and `using` statement updated in this PR — that update IS the documented consumer migration recipe. `Nuke.Consumer` and `Fallout.Consumer.NuGet` (pinned to 11.0.8) were unaffected, confirming the shim coverage and prior-release stability hold.

- **AES-GCM v2 secret format for `EncryptionUtility` — `parameters.json` encrypted values** (#214, closes #212). The secret-encryption scheme used by `fallout :secrets` is now AES-GCM with per-secret random salt + nonce and 600,000 PBKDF2-SHA256 iterations (OWASP 2023). Previous `v1:` (AES-CBC, static salt, 10,000 iterations, unauthenticated) values **continue to decrypt** — the `Decrypt` path dispatches on `v1:`/`v2:`/unprefixed-legacy. New `Encrypt` calls always emit `v2:`.
  - **On-disk format**: `v2:base64(salt[16] || nonce[12] || tag[16] || ciphertext)`. `v1:` was `v1:base64(salt-as-iv || ciphertext)` with a static `"Ivan Medvedev"` salt.
  - **Migration path**: existing `.fallout/parameters.json` files with `v1:` values stay readable. Re-running `fallout :secrets` to add or update **any** secret naturally re-encrypts that entry under `v2:` (existing `SaveSecrets` flow already calls `Encrypt` per value). A whole-file rekey command can be added if there's demand.
  - **Mixed-version repos**: developers on Fallout < 11.0 pulling a `parameters.json` that contains `v2:` values from a teammate on 11.0+ get `Could not decrypt 'X' with provided password`. Upgrade direction is fine (`v1:` reads keep working); downgrade is not.
  - **`Rfc2898DeriveBytes` constructor obsoletion (`SYSLIB0060`)** cleared — both legacy and current paths now use the static `Rfc2898DeriveBytes.Pbkdf2(...)`. No public-API impact; just stops the warning.

- **`Fallout.GlobalTool` package renamed to `Fallout.Cli`** (#206). The dotnet-tool install command is now `dotnet tool install -g Fallout.Cli`. The CLI **command name stays `fallout`** — no script or invocation changes needed.
  - **NuGet package ID changes**: `Fallout.GlobalTool` (frozen at 10.3.40, then unlisted) → `Fallout.Cli` (10.3.41 onward, then 11.0.x).
  - **Project / assembly / namespace rename**: `src/Fallout.GlobalTool/` → `src/Fallout.Cli/`, `tests/Fallout.GlobalTool.Tests/` → `tests/Fallout.Cli.Tests/`, `namespace Fallout.GlobalTool[.*]` → `namespace Fallout.Cli[.*]` across 48 files. Internal types only — no public-API consumer breakage from the namespace rename itself.
  - **Migration for consumers**: see [`docs/migration/from-globaltool-to-cli.md`](docs/migration/from-globaltool-to-cli.md). Short form: `dotnet tool uninstall -g Fallout.GlobalTool && dotnet tool install -g Fallout.Cli`, and in repos with a local manifest, edit `.config/dotnet-tools.json` to replace `fallout.globaltool` with `fallout.cli`.
  - **Update-notification text** in `UpdateNotificationAttribute` already points at the new name, so existing `Fallout.GlobalTool` installs will prompt users toward `Fallout.Cli` on next run.

- **Thin bootstrappers + `.config/dotnet-tools.json` manifest; `build.cmd` dropped** (#204, PR-B of #203). The per-repo `build.cmd`/`build.sh`/`build.ps1` shape changes substantially — and the `[GitHubActions]` generator now emits a different workflow shape downstream consumers will see on the next regen.
  - **`build.cmd` is gone** from the canonical scaffold. `fallout :setup` no longer emits it. New repos get only `build.sh` + `build.ps1`.
  - **`build.sh` / `build.ps1` are ~60-line thin shims**: provision dotnet (kept verbatim from the pre-existing block), `dotnet tool restore`, `exec dotnet fallout "$@"`. The `BUILD_PROJECT_FILE` / `BUILD_DIRECTORY` config + explicit `dotnet build` + `dotnet run --project` lines are gone — `Fallout.Cli`'s in-tool runner (added in #201) does that work now.
  - **`.config/dotnet-tools.json`** is the new home of the pin. `fallout :setup` writes one with the tool pinned at the running CLI's own version. Skipped if one already exists (consumer may have other local tools pinned).
  - **`[GitHubActions]` generator** (`Fallout.Common/CI/GitHubActions/Configuration/GitHubActionsRunStep.cs`): the single `run: ./{BuildCmdPath} {targets}` step is replaced with three steps — `actions/setup-dotnet@v4` (reads `global.json`), `dotnet tool restore`, `dotnet fallout {targets}`. Downstream consumers using `[GitHubActions(InvokedTargets = ...)]` see the new shape on next workflow regen.
  - **`BuildCmdPath` property** on `GitHubActionsAttribute` is no longer consumed (the run-step no longer references it). The Azure Pipelines / AppVeyor / TeamCity / SpaceAutomation generators still fall back to a literal `"build.cmd"` if `BuildCmdPath` is unset — keeps those legacy providers functional for the demand-driven revival (#8) without forcing this repo to keep a `build.cmd`.
  - **Migration**: existing repos can keep their fat bootstrappers and they'll work indefinitely (the bootstrapper does its own `dotnet build` + `dotnet run --project`, doesn't depend on the global tool). To adopt the new shape, re-run `fallout :setup --force` (or hand-edit the shims to match the new template).

- **Last-mile Newtonsoft removal — closes the #83 migration tree** (#119 STJ-6, #115 STJ-2 tail). Every Fallout source file is STJ-native; `Newtonsoft.Json.dll` survives in the closure only as a transitive of `NuGet.Packaging` and `Serilog.Formatting.Compact.Reader`. The `Newtonsoft.Json` `PackageVersion` is gone from `Directory.Packages.props` entirely.
  - **`GitHubActions.GitHubEvent` is now `JsonObject`** (was Newtonsoft `JObject`). Consumers that introspect the event payload need to swap `using Newtonsoft.Json.Linq` → `using System.Text.Json.Nodes` and update accessors (`["key"].Value<T>()` → `["key"].GetValue<T>()`, `Property(name)` → indexer, etc.).
  - **`Fallout.Utilities.Net` `HttpRequestExtensions.WithJsonContent` / `HttpResponseExtensions.GetBodyAsJson` defaults are STJ.** The `[Obsolete]`-marked Newtonsoft overloads (`JsonSerializerSettings` parameter, `JObject` return) are gone. `JsonSerializerOptions` overloads remain for explicit configuration; the parameterless default uses STJ with default options. `GetBodyAsJsonObject` returns `System.Text.Json.Nodes.JsonObject`.
  - **`SlackMessageActionButton.Type` / `TeamsMessage.Type` / `TeamsMessage.Context`**: hand-written `[JsonProperty(...)]` attributes swapped to `[JsonPropertyName(...)]`. Consumer impact only if you subclass these types and added `[JsonProperty]`-style attributes — switch to `[JsonPropertyName]`.
  - **`TwitterTasks`**: internal error-parsing path swapped from `JObject.Parse` to `JsonNode.Parse`. No public-API change.
  - **`ArgumentsFromParametersFileAttribute`** (#115 STJ-2 tail): `parameters.json` reading swapped from `JObject` to `JsonObject`. Behaviour identical for valid input; STJ may be slightly stricter about malformed JSON.

- **`SchemaUtility` rewritten on `System.Text.Json` — NJsonSchema gone** (#114, STJ-1 of #83). The build-parameter schema generator (which emits `.fallout/build.schema.json` for editor autocomplete) no longer goes through NJsonSchema. It now hand-rolls the draft-04 schema using `JsonNode`/`JsonObject` directly — same output shape, no Rico-entanglement.
  - **Dropped packages**: `NJsonSchema`, `NJsonSchema.NewtonsoftJson`, `NJsonSchema.Annotations`, `Namotion.Reflection` — four packages and ~12 transitive dependencies gone from `Fallout.Build`. `Newtonsoft.Json` stays in the closure only via `Fallout.Common`'s `*Tasks.cs` (Slack/Twitter/Teams) until #119 lands.
  - **API**: `SchemaUtility.GetJsonString(IFalloutBuild)` and `GetJsonDocument(IFalloutBuild)` unchanged for consumers.
  - **Behavior preserved**: well-known definitions in canonical order (Host, ExecutableTarget, Verbosity, FalloutBuild), `allOf:[user, base]` envelope, `[CustomParameter]` subclasses render as plain string (not recursive), value-type `Nullable<T>` → `[T, "null"]`, reference-type nullables → `["null", T]` for primitives / `oneOf [null, $ref]` for refs, `[TypeConverter]`-attributed types (AbsolutePath, Solution, etc.) render as `"string"`.

- **`Fallout.Tooling` engine migrated to System.Text.Json** (#117 STJ-4, #118 STJ-5). The tool-options ↔ JSON layer that powers every tool wrapper is now STJ-native:
  - `Options.InternalOptions` is now `System.Text.Json.Nodes.JsonObject` (was Newtonsoft `JObject`). `Options.JsonSerializer` and `Options.JsonSerializerSettings` are gone — use `Options.SerializerOptions` (`JsonSerializerOptions`) instead.
  - The Options→InternalOptions converter is now a `JsonConverterFactory` registered on `SerializerOptions.Converters` (STJ doesn't inherit class-level `[JsonConverter]` attributes onto subclasses). `LookupTable<,>` round-trips through a parallel `ObjectFromFieldConverter` factory; `Enumeration` subclasses go through a new `EnumerationJsonConverterFactory` that bridges `[TypeConverter]`-attributed types (which STJ doesn't honour natively).
  - All 62 generated tool wrappers regenerated with `[JsonPropertyName]` instead of `[JsonProperty]` (#118). The generator's own JSON model (`Fallout.Tooling.Generator`) is migrated too — `Tool` / `Property` / `DataClass` / `Enumeration` / `Task` use `[JsonRequired]` / `[JsonPropertyName]` / `[JsonIgnore]` from `System.Text.Json.Serialization`.
  - `IReadOnlyDictionary<string, object>` round-trips deserialize values as `JsonElement` (not `string` as Newtonsoft did); `DelegateHelper.ParseCollection` coerces both shapes back to string.
  - **`Newtonsoft.Json` `PackageReference` removed** from `Fallout.Tooling`, `Fallout.Tooling.Generator`, and `Fallout.Utilities.Text.Json`. `Object.ToJObject` deleted (closes the residual #116 tail). `Fallout.Common` still references Newtonsoft for `*Tasks.cs` files until #119 (STJ-6) lands.
  - Migration for consumers: replace `Options.JsonSerializerSettings` → `Options.SerializerOptions`; replace `obj.ToJObject(serializer)` → `JsonSerializer.SerializeToNode(obj, Options.SerializerOptions).AsObject()`; tool wrappers using hand-written `[JsonProperty]` (e.g. `SlackMessage.Type`) need to switch to `[JsonPropertyName]`.

- **`Fallout.Utilities.Text.Json` Newtonsoft surface removed** (#116, STJ-3 of #83). The `[Obsolete]`-marked Newtonsoft.Json overloads added in 10.3.x are gone:
  - **Deleted types:** `JObjectExtensions` (`GetChildren`/`GetPropertyValue`/`GetPropertyStringValue`/`GetPropertyValueOrNull` on `JObject`), `AllWritableContractResolver`, `Base64JsonConverter<T>`, `Object.ToJObject`.
  - **`JsonExtensions` stripped to System.Text.Json only**: `DefaultSerializerSettings` removed (use `DefaultSerializerOptions`); the `JsonSerializerSettings`-taking overloads of `ToJson` / `GetJson` / `ReadJson` / `WriteJson` / `UpdateJson` and the `JObject`-returning `GetJson`/`ReadJson`/`UpdateJson(Action<JObject>)` are gone. Equivalent STJ surface: same method names taking `JsonSerializerOptions` (now defaulted to `DefaultSerializerOptions` when omitted), plus `GetJsonObject` / `ReadJsonObject` / `UpdateJsonObject(Action<JsonObject>)` for `JsonNode`-based access.
  - **Migration**: replace `obj.ToJson(settings)` with `obj.ToJson(JsonExtensions.DefaultSerializerOptions)` (or pass your own `JsonSerializerOptions`); replace `content.GetJson<JObject>()` with `content.GetJsonObject()`; replace `JObject.GetPropertyValue<T>(name)` with `JsonObject.GetPropertyValue<T>(name)` (same method name, different receiver type via `using System.Text.Json.Nodes`).


- **Fixed `Fallout.SolutionModel` 10.2.24–10.2.34 unrestorable** (#107): the vendored SolutionPersistence wrapper was `IsPackable=false`, so `dotnet pack` fell back to emitting the dependency under the *assembly* name (`Microsoft.VisualStudio.SolutionPersistence`) at the Fallout version — which doesn't exist on nuget.org. Wrapper now packs as `Fallout.VisualStudio.SolutionPersistence` (PackageId set explicitly; `AssemblyName` preserved for drop-in type identity), so `Fallout.SolutionModel.nuspec` declares the correct transitive dep. Also affected `Fallout.Common`, `Fallout.Build`, `Fallout.ProjectModel`, `Fallout.Components` — all fixed.
- **`--skip-duplicate` on the publish push** (#108): release.yml's `IPublish.PushSettings` now enables skip-duplicate, so a partial publish failure (e.g. one package's API key permission gap) no longer makes reruns error on the already-uploaded versions. Pipeline is idempotent.
- **Fixed `build.ps1` bootstrap on PowerShell 7.5+** (#15): the script now uses `Join-Path` for SDK resolution so newer PowerShell's stricter path handling doesn't break the launcher.
- **Fixed solution folder names starting with digits** (#16): `StronglyTypedSolutionGenerator` prefixes a leading underscore when the folder name would produce an invalid C# identifier.
- **Retired `GitVersion.Tool`** (#81) — version is now sourced exclusively from `Nerdbank.GitVersioning` via `version.json`. `MajorMinorPatchVersion` and friends in `Build.cs` read NB.GV's `$(Version)` MSBuild output.
- **Trimmed unused / replaceable dependencies** (#75, #78, #79, #80, #82): dropped `JetBrains.ReSharper.GlobalTools`, the matkoch Spectre.Console fork (swapped for upstream `Spectre.Console`), `Microsoft.ApplicationInsights`, `Codecov.Tool`, and `xunit.runner.console`. See `docs/dependencies.md` for the current graph.
- **Bumped Scriban 7.1.0 → 7.2.0** (#84) to clear NU1903.
- **Fixed `[GitHubActions]` `CheckoutRef` breaking cross-repo / fork PRs**. The generator emitted `ref: ${{ github.head_ref }}` but didn't set `repository:`, so the default `${{ github.repository }}` was used. For PRs from a fork, the source branch only exists on the fork; checkout tried to resolve it on origin and failed with "branch or tag could not be found" (regressed in #175). Generator now also emits `repository: ${{ github.event.pull_request.head.repo.full_name || github.repository }}` whenever `CheckoutRef` is set — works for fork PRs, same-repo PRs, AND push events (the `||` fallback). Regenerate via any `dotnet fallout <target>` to pick up the fix.
- **Fixed `fallout-migrate` producing unrestorable `_build.csproj`** (closes #217). The Nuke → Fallout namespace rewrite carried the original NUKE `Version="10.1.0"` pins onto `Fallout.*` packages — but `Fallout.*` was never published at `10.1.0`, so NuGet hit NU1603 ("not found, falling back to next-higher") and `WarningsAsErrors` in the migrated project escalated to fatal. Migrate now bumps inline `Version="..."` attributes on Nuke → Fallout PackageReferences to the running migrate tool's own version in the same regex pass, and strips the stale `System.Security.Cryptography.Xml` `<PackageReference>` (NUKE-era projects often pinned an older major that conflicts with `Fallout.Common`'s transitive ≥ 10.0.6 — NU1605 downgrade). CPM-managed references (no inline Version) keep namespace-only rewrite — version stays in Directory.Packages.props.
- **Fixed Build.GitVersion injection on GitVersion 6.x** (closes #218). GitVersion 6.x emits BuildMetaData, CommitsSinceVersionSource, PreReleaseNumber, and WeightedPreReleaseNumber as JSON numbers instead of quoted strings. New NumberToStringJsonConverter in Fallout.Utilities.Text.Json handles both shapes so consumers continue to see string for all four. Backwards-compatible with GitVersion 5.x output.

### Added

- **Extracted `Fallout.Core` — the pure domain + graph foundation** (#88, v11 plugin-architecture foundation). New bottom-layer project (`netstandard2.1;net10.0`) holding the side-effect-free types of the build execution pipeline: `ITargetModel` (read-only target projection — the seed for the v12 plugin SDK), `ExecutionStatus`, and `TopoSort` / `PlanResult<T>` (pure topological sort + strongly-connected-component cycle detection). `Fallout.Core` references nothing else in the repo and is held to a strict purity bar — no `System.IO`, `Process`, `Console`, or Serilog — enforced by a `NetArchTest` architecture-fitness test (broader suite tracked in #95).
  - **Fully backward-compatible.** `ExecutionStatus` moved assemblies (`Fallout.Build` → `Fallout.Core`) but is `[TypeForwardedTo]`, and namespaces are unchanged, so existing consumer `using Fallout.Common.Execution;` code compiles untouched. The live `ExecutableTarget` stays in `Fallout.Build` and now implements `ITargetModel`; `ExecutionPlanner` is reduced to a thin orchestration wrapper over `Fallout.Core`'s pure `TopoSort` (it still owns reading the `strict` parameter, failing the build on a cycle, and mutating target state).
  - The graph primitives (`Vertex` / `StronglyConnectedComponent*`) moved from `Fallout.Utilities` into `Fallout.Core` as an internal implementation detail of `TopoSort`; they had no external consumers.

### Process

- **Added "Default to backwards compatibility" rule to the AGENTS.md AI brief** (#262). Critical rules now state the principle explicitly — prefer additive over breaking changes; `[Obsolete]`, transition shims, and overload-based extension before a hard break. The existing breaking-change PR-creation flow (label + `⚠️` callout + `version.json` bump + CHANGELOG entry with migration path) remains the mechanics for when a break is unavoidable. Issue [#262](https://github.com/ChrisonSimtian/Fallout/issues/262) stays open for the broader policy discussion (LTS stance for the `Nuke.*` shims, what qualifies as "backwards compatible" for on-disk formats and CI workflow generators, when transition-shim generators are the right answer, etc.).

- **Disabled auto-release on every push to `main`** (stopgap for milestone [#13](https://github.com/ChrisonSimtian/Fallout/milestone/13) / RFC [#267](https://github.com/ChrisonSimtian/Fallout/issues/267)). The `release` workflow now triggers on `workflow_dispatch` only — releases are explicit, run manually from Actions UI. Auto-publishing was firing a Fallout.* release on every merge (≈20 patch versions accumulated in recent days for what was mostly internal cleanup), generating Dependabot upgrade PRs across every downstream consumer. The proper restructure — tag-triggered publishes on `release/vN` branches with three GitHub Environments (`nuget-org` / `github-packages` / `github-releases`) — is being implemented under milestone #13. This stopgap stops the consumer-facing noise immediately while that lands.

- **Release-branch model + multi-channel CD (umbrella entry for milestone [#13](https://github.com/ChrisonSimtian/Fallout/milestone/13)).** Fallout now ships from `release/vN` branches via tag-triggered, multi-channel publish — not from `main`. Captures the full scope of the milestone in one entry to keep CHANGELOG history readable. Documented in [`docs/branching-and-release.md`](docs/branching-and-release.md) (maintainer runbook) and [`docs/adr/0001-release-branch-model.md`](docs/adr/0001-release-branch-model.md) (decision record).
  - **Branching model.** `main` is the integration trunk — merges land here but no longer auto-publish. `release/vN` (e.g. `release/v11`, cut in [#269](https://github.com/ChrisonSimtian/Fallout/issues/269)) is the long-lived release channel per major version. Hotfixes flow `main → cherry-pick → release/vN → tag`. `develop`, `master`, `hotfix/*` not used.
  - **Tag-triggered releases.** Pushing a `v*` tag to a `release/vN` branch fires `release.yml`. A `validate-ref` job confirms the tag is reachable from a `release/v*` branch. `workflow_dispatch` is preserved as a fallback for re-runs after transient publish failures ([#274](https://github.com/ChrisonSimtian/Fallout/issues/274)).
  - **Multi-channel publish.** Three parallel jobs fan out from a shared Test+Pack: `publish-nuget-org` (Tier 1, approval-gated via the `nuget-org` GitHub Environment, Fallout.* only) → `publish-github-packages` (Tier 2, Nuke.* transition shims) → `publish-github-releases` (bundled nupkg artifacts on the tag's GitHub Release). Each produces a per-environment deployment record. `--skip-duplicate` on every push keeps re-runs idempotent.
  - **GitHub Environments** ([#272](https://github.com/ChrisonSimtian/Fallout/issues/272)). Three env shells keyed by channel — `nuget-org` (required-reviewer: `@ChrisonSimtian`), `github-packages` (auto), `github-releases` (auto). Branch deployment policies restrict deploys to `release/v*` (`main` permitted transitionally during the cut).
  - **`NUGET_API_KEY` env-scoped** ([#273](https://github.com/ChrisonSimtian/Fallout/issues/273)). Migrated from repo secret to environment secret on `nuget-org`. The release job declares `environment: nuget-org`, so the secret resolves only inside the gated job.
  - **Branch protection on `release/v11`** ([#270](https://github.com/ChrisonSimtian/Fallout/issues/270)). Mirrors `main`'s profile — required `ubuntu-latest` check, linear history, CODEOWNER review, no force-push/delete, conversation resolution required.
  - **Tag protection ruleset.** Repository ruleset blocks creation/deletion/update of `v*` tags except for repo admins. Two-layer gating with the `nuget-org` env approval: who-can-tag + who-can-approve-publish.
  - **PR-validation triggers extended to `release/v*`** ([#291](https://github.com/ChrisonSimtian/Fallout/pull/291), [#292](https://github.com/ChrisonSimtian/Fallout/pull/292)). `ubuntu-latest.yml` and `ubuntu-latest-docs.yml` now fire on PRs targeting `release/v*` so branch protection's required check actually gets a result. Cross-platform post-merge triggers (`windows-latest`, `macos-latest`) tracked separately in [#293](https://github.com/ChrisonSimtian/Fallout/issues/293).
  - **NB.GV `publicReleaseRefSpec`** ([#275](https://github.com/ChrisonSimtian/Fallout/issues/275)). Extended on `release/v11` to include the branch itself, so versions are clean `11.0.X` instead of `11.0.X-g<sha>`.
  - **`main`'s `version.json` major** stays at 11 for now ([#271](https://github.com/ChrisonSimtian/Fallout/issues/271) deferred until v12 work begins).
  - **Tier 3 Docker local NuGet server** ([#279](https://github.com/ChrisonSimtian/Fallout/issues/279)) — pre-merge testing channel — landed as a separate work item; initial setup in PR [#287](https://github.com/ChrisonSimtian/Fallout/pull/287).

- **nuget.org publish is now opt-in for v11; GitHub Packages is the default release channel.** Tag pushes on `release/v11` publish to GitHub Packages + GitHub Releases only; the `publish-nuget-org` job is skipped unless `workflow_dispatch` is invoked with `publish-to-nugetorg=true` (and the existing `nuget-org` env approval gate still fires). `publish-github-packages` now pushes **all** `*.nupkg` (Fallout.* + Nuke.*), not just Nuke.* shims — GitHub Packages is the de-facto v11 release channel during stabilisation. nuget.org is reserved for v10.x maintenance lines and a future stabilised v11. Decision recorded in [`docs/adr/0002-v11-off-nuget-by-default.md`](docs/adr/0002-v11-off-nuget-by-default.md); maintainer runbook updated in [`docs/branching-and-release.md`](docs/branching-and-release.md). This *modifies* the routing originally documented in the milestone #13 umbrella entry above — the branching model and CD shape are unchanged, only the routing policy.

## [10.2.0] / 2026-05-21
The NUKE → Fallout hard fork. Originally NUKE by [@matkoch](https://github.com/matkoch) and contributors; under new maintenance and rebranded to Fallout.

### Rebrand — names, namespaces, packages
- **Renamed `Nuke.*` → `Fallout.*`** across namespaces, csproj filenames, assembly names, and package IDs (#54). Top-level packages are now `Fallout.Common`, `Fallout.Build`, `Fallout.Components`, `Fallout.Tooling`, `Fallout.ProjectModel`, `Fallout.SolutionModel`, `Fallout.Utilities*`, `Fallout.GlobalTool`, `Fallout.MSBuildTasks`, `Fallout.SourceGenerators`, `Fallout.Tooling.Generator`, `Fallout.VisualStudio.SolutionPersistence`. Pre-reserved on nuget.org under the verified `Fallout.*` prefix owned by Chrison.dev.
- **Renamed `NukeBuild` → `FalloutBuild`** and `INukeBuild` → `IFalloutBuild` (#59). All build classes inherit the renamed base.
- **Renamed `Constants` internals** (`NukeFileName`, `NukeDirectoryName`, etc.) → `Fallout*` (#60), with read-only legacy fallbacks where consumer projects still emit `.nuke/`.
- **Renamed the global tool command**: `dotnet nuke` → `dotnet fallout` (#66). Regenerated all tool wrappers (#67).
- **Finished phase 3.5 rename** (#65): MSBuild props, env vars (`NUKE_*` → `FALLOUT_*`, with legacy reads), the credential store, and telemetry keys.
- **Solution file**: `nuke-common.slnx` → `fallout.slnx` (#51).
- **Runtime state dir**: `.nuke/` → `.fallout/` (#51). Framework const `NukeDirectoryName` now `.fallout`; new `LegacyNukeDirectoryName = ".nuke"` provides a read-only fallback so pre-migration consumer repos keep building. New consumer projects scaffolded by `dotnet fallout :setup` get `.fallout/`. Bulk consumer migration handled by the Fallout.Migrate CLI.

### Consumer migration tooling
- **`Fallout.Migrate` CLI** (#68): one-shot rewrite of a consumer repo from NUKE to Fallout — namespace replacement, `.nuke/` → `.fallout/` move, build-script bootstrap update.
- **`Fallout.Migrate.Analyzers` with `FALLOUT004` codefix** (#36): in-IDE diagnostic for code still referencing `Nuke.*` symbols, with a one-click fix to the matching `Fallout.*` symbol.
- **Migration guide** (#37): `docs/migration.md` walks consumers through the rename end-to-end.

### Backwards-compat: Nuke.* transition shims
- **`Nuke.Common` MVP shim package** (#70): thin wrapper assembly in the `Nuke.*` namespace whose types inherit from the corresponding `Fallout.*` types — lets pre-rename consumers compile against the new packages without source changes. Lives at `src/Shims/Nuke.Common/`.
- **`TransitionShimGenerator` source generator** (#69): emits shim type wrappers per-target, covering the Nuke.Common "Easy tier" surface (plain types, interfaces, enums) and static-class method delegation. Hard-tier types (sealed structs, delegates, etc.) raise `SHIM001` warnings with a pointer to `fallout-migrate`.
- **Shim packages publish to GitHub Packages** (#47): `Nuke.*` package IDs belong to matkoch on nuget.org, so the transition shim feed lives at GH Packages (`https://nuget.pkg.github.com/Fallout-build/index.json`). Production-build feed is nuget.org with `Fallout.*` only.

### Vendored fork of `Microsoft.VisualStudio.SolutionPersistence`
- **Replaced `matkoch.Microsoft.VisualStudio.SolutionPersistence` with a vendored fork** (#86). The upstream Microsoft package only ships `net472` + `net8.0`; our source generators need `netstandard2.0`. Matt's fork had the netstandard2.0 patches — we forked his repo at [`ChrisonSimtian/vs-solutionpersistence`](https://github.com/ChrisonSimtian/vs-solutionpersistence) (preserves the MIT license + upstream Microsoft history + attribution chain). Sources are a submodule at `vendor/vs-solutionpersistence/`; wrapper csproj at `src/Fallout.VisualStudio.SolutionPersistence/` compiles them with the TFMs we need. Assembly name stays `Microsoft.VisualStudio.SolutionPersistence` so type identity is preserved. (Note: in 10.2 the wrapper packed as `IsPackable=false` and caused the restore bug fixed in 10.3.0 above — known issue, fix-forward.)

### Visual identity
- **Logo + banner** (#50): `.assets/fallout-logo.svg` — radioactive trefoil + wordmark, yellow `#F5C800` on black. `Host.WriteLogo` paints the FALLOUT wordmark yellow with a "☢ survived the fallout" footer via true-color ANSI (modern terminals render; legacy sinks ignore the escapes).
- **Dropped Matt-era visual assets**: partner/sponsor logos (JetBrains, AWS, Datadog, Octopus) and NUKE-branded icons/logos/screenshots removed. `PackageIcon` temporarily off until a 256×256 PNG export of the SVG lands.

### Repo restructure
- **`source/` → `src/`, tests moved to `tests/`, `images/` + root `icon.png` → `.assets/`** (#49). `Directory.Build.props` and `AssemblyInfo.cs` hoisted to root. See `docs/architecture.md` for the canonical layout.
- **Removed Matt-era IDE files**: `.editorconfig`, all `*.DotSettings` (ReSharper), `.run/` (Rider run configs), `GitVersion.yml`, `external/` (orphan submodule placeholder), `Dockerfile` (referenced .NET 8, not CI-wired). Also legacy CLI reference files moved to `docs/cli-tools/` (#71).
- **TFS leftover stripped** from `nuget.config` files — the `<solution><add key="disableSourceControlIntegration"/></solution>` block is only relevant under TFVC.

### Telemetry
- **Neutralized telemetry** — the previous Azure Application Insights `InstrumentationKey` routed data to the original NUKE maintainer's account, which we don't own. Set to empty string; static constructor short-circuits before any `TelemetryClient` work. Plumbing stays in place for when a Fallout-controlled endpoint stands up. `IsCommonType` repository check now recognizes both the Fallout and upstream NUKE `RepositoryUrl`.

### CI / release
- **Primary feed is nuget.org** (#58, #54): `Fallout.*` packages publish to https://api.nuget.org/v3/index.json under the reserved prefix.
- **GitHub Actions–only CI** at this point — AppVeyor / TeamCity / GitLab generation removed (#22 dropped the AppVeyor-era orphan code). Re-introduction is demand-driven (CI roadmap milestone).
- **Validation matrix split**: ubuntu-latest runs on every PR; windows-latest / macos-latest run only on push to main, as cost-vs-coverage trade-off.
- **Skip release on docs-only pushes** to main: `paths-ignore` covers `docs/**`, `.assets/**`, `**/*.md`.
- **Rich release notes** from milestone + auto PR list (#21): release body links to the matching `vX.Y.Z` milestone and inlines the PR list.

### String-reference audit
- **Canonical URLs centralized** (#30) in `Constants.FalloutWebsite` / `FalloutRepository` / `FalloutRawRepository` / `FalloutDocsUrl` / `FalloutTelemetryDocsUrl` / `FalloutNotificationsUrl`. Tool wrapper JSON `$schema` URLs (63 files) and `*.Generated.cs` "Generated from" comments updated from `nuke-build/nuke/master/source/` to `ChrisonSimtian/Fallout/main/src/`. IDE-plugin URLs removed from `build/Build.cs` and `templates/Build.cs` (re-add when Fallout plugins ship). NUKE attribution in README/CONTRIBUTING/CLAUDE.md/CHANGELOG history and pedagogical test fixtures intentionally kept.

### Misc
- **Stripped per-file license headers; `LICENSE` is now the single source of truth.** The 4-line `// Copyright … Maintainers of Fallout. // Originally based on NUKE …` block has been removed from all 614 first-party `.cs` files under `src/`, `tests/`, `build/`, and the repo root. MIT compliance is satisfied by `LICENSE` at the repo root (which preserves matkoch's "Maintainers of NUKE 2017-2025" copyright), README acknowledgement of the NUKE origin, and `PackageLicenseExpression="MIT"` on every NuGet package. Vendored third-party code (`src/Persistence/Fallout.Persistence.Solution/`, Microsoft MIT) retains its own copyright headers untouched. Convention docs (`AGENTS.md`, `docs/agents/conventions.md`, `docs/architecture.md`, `CONTRIBUTING.md`) updated to match.
- **Fixed `GetNuGetReleaseNotes` escaping** — now URL-encodes `;` in addition to `,`. Without this, semicolons in changelog bullets split the MSBuild `-p:PackageReleaseNotes=` value and crashed Pack.
- **Dropped `JetBrains.Annotations`** (#76).
- **Docs**: dependency overview (#85, #87), architecture overview, v11/v12 plugin-architecture roadmap, costs/sponsorship transparency (#56), README badge row (#62) + Repobeats activity image (#63), rebrand namespace mapping (#53).

## [10.1.0] / 2025-12-02
- Fixed solution folders in `StronglyTypedSolutionGenerator`
- Fixed MSBuild target packaging for .NET 10
- Fixed GitHub Actions versions

## [10.0.0] / 2025-11-20
- Added support for .NET 10
- Added support for slnx solution files
- Updated dependencies
- Removed automatic PowerShell/Pwsh argument positioning

## [9.0.4] / 2025-01-15
- Security: Fixed output filter from `ArgumentStringHandler`
- Removed obsolete members
- Fixed `PreProcess` of tasks requires exact options type
- Fixed missing `position` and `secret` properties
- Fixed preparation of shadow directory in `ReSharperTasks`
- Fixed base class in `ReSharperTasks`
- Fixed missing arguments in `DotNetTasks`
- Fixed missing commands in `DotNetTasks` and `NuGetTasks`
- Fixed package executable in `OctoVersionTasks`

## [9.0.3] / 2024-12-05
- Fixed nullable options for `ToolTasks.Run`
- Fixed static tool path resolution
- Fixed tool requirements initialization
- Fixed documentation link in tool wrapper remarks
- Fixed documentation on task methods and settings classes
- Fixed missing `NuGetKeyVaultSignTool`
- Fixed package executable in `OctopusTasks`

## [9.0.2] / 2024-12-03
- Fixed MSBuild tasks to use `net8.0` target framework
- Fixed error handling in `:update` command
- Fixed resolving tool path from options
- Fixed nullable options for `ToolTasks.Run`
- Fixed nullable underlying dictionary for delegate properties
- Fixed skipping null and whitespace arguments
- Fixed tool requirements
- Fixed NPM tool path resolution
- Fixed logging errors as standard in `GitTasks` and `DockerTasks`
- Fixed argument format in `DotNetTasks`
- Fixed nullable `Plugins` dictionary in `ReSharperTasks` 

## [9.0.1] / 2024-11-21
- Fixed `Options` serialization to JSON
- Fixed `Options` for default members in interfaces
- Fixed missing `ProcessExitHandler` setters

## [9.0.0] / 2024-11-21
- Removed usages of `BinaryFormatter`
- Changed minimum framework from `net6.0` to `net8.0`

## [8.1.4] / 2024-11-06
- Fixed `build.schema.json` generation to use `allOf` for user and base type properties

## [8.1.3] / 2024-11-05
- Fixed naming from `NukeBuild.IsSucessful` to `IsSucceeding`
- Fixed `NukeBuild.IsSucceeding` to negate `IsFailing`
- Fixed NJsonSchema reference version
- Fixed `:secrets` command to find secret parameters
- Fixed argument format in `DotNetTasks`
- Fixed definite argument in `EntityFrameworkTasks`
- Fixed deprecated argument in `MinVerTasks`

## [8.1.2] / 2024-10-13
- Fixed exclusion of skipped target from lookup for skippable dependencies
- Fixed resolution of empty environment variables to false
- Fixed parallel execution to prefer logger from settings

## [8.1.1] / 2024-10-05
- Fixed nested solution folders in `StronglyTypedSolutionGenerator`
- Fixed whitespace arguments in `ArgumentStringHandler`
- Fixed output logging in parallel execution
- Fixed exclusion of invoked targets from skipping
- Fixed definite argument in `EntityFrameworkTasks`

## [8.1.0] / 2024-09-10
- Added schema generation with references for `build.schema.json`
- Added deserialization of full objects from `parameters.json`
- Added `AbsolutePath` extension methods for `AddUnixSymlink`, `Copy*`, `Move*`, `Rename*`
- Added support for preprocessor directives in solution parsing
- Added `Pattern` in favor of property in `LatestGitHubReleaseAttribute`
- Added `ConcurrencyGroup`, `ConcurrencyCancelInProgress`, `EnvironmentName`, `EnvironmentUrl` in `GitHubActionsAttribute`
- Added `DotnetPackagingTasks`
- Fixed invoked targets to not be excluded from skipping
- Fixed stripping of hyphens in skipped target names
- Fixed empty environment variables to be resolved as empty arrays
- Fixed `EnableUnsafeBinaryFormatterSerialization` to be set through `AppContext`
- Fixed unquoting of multiple quoted arguments in `ArgumentStringHandler`
- Fixed using logger from settings in parallel execution
- Fixed handling of duplicated NuGet package files
- Fixed inclusion of original NuGet packages in requirements
- Fixed GitHubActions to use latest action versions
- Fixed `DotCoverTasks` and `EntityFrameworkTasks` tool path resolution
- Fixed missing members in `GitHubActionsImage`
- Fixed missing properties in `GitLab`
- Fixed missing parameters in `AzurePipelines.SetVariables`
- Fixed missing arguments in `DotNetTasks`
- Fixed tool path in `CodecovTasks`

## [8.0.0] / 2024-01-18
- Changed string parameters to violate requirement when empty or whitespace
- Added on-demand value injection using `OnDemandAttribute` and `OnDemandValueInjectionAttribute`
- Added `AbsolutePath` division operator for `..` range expression
- Added `DOTNET_NOLOGO` to bootstrapping files
- Fixed `BinaryFormatterSerialization` warning by suppression
- Fixed .NET SDK discovery in bootstrapping files
- Fixed quotation for bootstrapping script invocation
- Fixed filtering on `FileAttributes`
- Fixed quoting in `AppVeyor` generation
- Fixed members in `AzurePipelinesImage`
- Fixed members in `GitHubActionsImage`
- Fixed lower-case naming in `DotNetVerbosity` members
- Fixed missing `DotNetTasks` commands
- Fixed missing `EntityFrameworkTasks` command
- Fixed logging in `NpmTasks`
- Fixed argument type in `OctopusTasks`
- Fixed missing argument in `SonarScannerTasks`
- Fixed value formatting in `SonarScannerTasks`
- Fixed members in `NUnitLabelType`
- Fixed deprecated argument in `NUnitTasks`
- Fixed members in `ReportGeneratorReportTypes`

## [7.0.6] / 2023-09-24
- Fixed logging of Docker target execution to fall back to debug messages

## [7.0.5] / 2023-09-05
- Fixed filtering environment variables with newlines in Docker target execution
- Fixed logging in Docker target execution
- Fixed update of version summary in `ChangelogTasks`
- Fixed missing `DockerTasks` command

## [7.0.4] / 2023-08-31
- Fixed check on nullable parameter type
- Fixed telemetry check on home repository
- Fixed missing environment variables for AppVeyor
- Fixed `ICreateGitHubRelease` to work with existing releases
- Fixed `ICreateGitHubRelease` to set `GitHubToken` unconditionally
- Fixed `SetBuildTarget` and `SetTestPlatform` overloads in `UnityTasks`
- Fixed `UnityRunTestsSettings` base type

## [7.0.3] / 2023-08-21
- Fixed enumeration value sets to exclude non-public fields
- Fixed check for `NUKE_ENTERPRISE_TOKEN` in `build.sh` bootstrapping script
- Fixed default warnings with suppression
- Fixed telemetry to treat types as _common_ when their assembly points to home repository
- Fixed filtering of secrets in CLT `Output` collection
- Fixed handling of `AbsolutePath` collections in `ArgumentStringHandler`
- Fixed handling of `IAbsolutePathHolder` in `ArgumentStringHandler`
- Fixed handling of `relativePath` for `SolutionAttribute` in `StronglyTypedSolutionGenerator`
- Fixed error reporting in `StronglyTypedSolutionGenerator`
- Fixed TeamCity `pom.xml` template to use HTTPS
- Fixed duplicated payload serialization in `TeamsTasks`
- Fixed missing arguments in `OctopusTasks`
- Fixed missing command in `UnityTasks`
- Fixed missing members in `UnitBuildTarget`
- Fixed argument formatting in `MSpecTasks`
- Fixed assertion in `UnityTasks`

## [7.0.2] / 2023-05-19
- Fixed string-based command-line tool tasks to not require interpolated strings
- Fixed secret filtering

## [7.0.1] / 2023-05-15
- Fixed system console colors to fall back to current colors
- Fixed trimming of `OnlyWhen` conditions
- Fixed lightweight tool API to expose exit handler
- Fixed `Tool` delegate with `ArgumentStringHandler`
- Fixed AzureKeyVault attributes to print shorter warning and fall back to parameters
- Fixed resolution of absolute paths from `parameters.json`
- Fixed `Solution.GetProject` and `GetSolutionFolder` to only consider root children
- Fixed `ChangelogTasks` for empty lines
- Fixed serialization for `HelmTasks`, `KubernetesTasks`, `NSwagTasks`, and `ReSharperTasks`
- Fixed `DockerTasks.DockerStackDeploy`
- Fixed `CoverallsNetSettings.Job`

## [7.0.0] / 2023-05-06
- Refactored out multiple projects
- Renamed `ProcessCustomLogger` to `ProcessLogger`
- Renamed `LocalExecutableAttribute` to `LocalPathAttribute`
- Renamed `NpmExecutableAttribute` to `NpmPackageAttribute`
- Renamed `PackageExecutableAttribute` to `NuGetPackageAttribute`
- Renamed `PathExecutableAttribute` to `PathVariableAttribute`
- Changed bootstrapping scripts to use `STS` instead of `Current` channel
- Changed `Target` conditions to use regular delegates captured using `CallerArgumentExpressionAttribute`
- Changed `AbsolutePath` to implicit cast to `string`
- Changed `HandleSIngleFileExecutionAttribute` to be opt-in
- Changed string-based command-line tool tasks to use `ArgumentStringHandler`
- Changed `LatestMavenVersionAttribute` to exclude previously hardcoded `m2` suffix
- Changed `OctoVersionTasks` to use replacement package
- Removed legacy project setup
- Removed YAML shell completion
- Removed `ExternalFilesTask`
- Removed `CheckBuildProjectConfigurationsAttribute`
- Removed obsolete members in `OctoVersionAttribute`
- Removed `Nuke.MSBuildLocator` package
- Updated package dependencies
- Updated AzureKeyVault integration
- Added assertion against `Target` self-dependence
- Added support for tool requirements and automatic installation
- Added `ProcessExitHandler` for CLT tasks
- Added auto-resolution of appropriate framework in `NuGetToolPathResolver`
- Added `windowsPath` and `unixPath` to `LocalPathAttribute`
- Added `LatestMavenVersionAttribute.IncludePrerelease`
- Added `DelegateDisposable.SetAndRestore`
- Added `Solution` implicit cast to `AbsolutePath`
- Added `AbsolutePath` extension methods for `TextTasks`, `FileSystemTasks`, `CompressionTasks`, `SerializationTasks`
- Added `AbsolutePath` plus operator
- Added `EnvironmentInfo.Paths`
- Added `IFormattable` to `AbsolutePath`
- Added properties for permissions in GitHubActions generation
- Added support for job timeout and concurrency configuration in GitHubActions generation
- Added `PublishCondition` and `LFS` property in GitHubActions generation
- Added `Directory.Packages.props` to default cache key files in GitHubActions generation
- Added names for actions in GitHubActions generation
- Added display names for tasks in AzurePipelines generation
- Added resolution of GitHub token in `GitHubTasks` through `GITHUB_TOKEN` environment variable
- Added `StaticWebAppsTasks`
- Added `PwshTasks`
- Fixed linking of `Directory.Build` files in build project view
- Fixed skipping of trigger dependencies when original target is skipped
- Fixed `continue` parameter to retry previously skipped targets
- Fixed missing `Log.CloseAndFlush()` for logging
- Fixed newlines in bootstrapping scripts
- Fixed log-level check for `ProcessException`
- Fixed case-sensitivity in `nuget.config` discovery
- Fixed `ProcessException` to retain exit code
- Fixed `StronglyTypedSolutionGenerator` to add auto-generated XML header
- Fixed NPM integration
- Fixed `Repository.IsGitHubRepository` to consider nullable `Endpoint`
- Fixed casing for `PublishBuildArtifacts` in AzurePipelines generation
- Fixed missing environment variables for SpaceAutomation
- Fixed missing environment variables for GitHubActions
- Fixed escaping of GitHubActions workflow values
- Fixed missing arguments in `KubernetesTasks`

## [6.3.0] / 2022-12-12
- Added new version of `Octokit`
- Added `OptionalAttribute` to suppress auto-injection warnings
- Added ability to override `ProcessCustomLogger` in `ToolSettings`
- Added ability to exclude auto-linked files in build project
- Added `DiscordTasks`
- Added `MastodonTasks`
- Added `JavaScriptProject` project type
- Added `MakeNSISTasks`
- Fixed wording for static and dynamic conditions in build summary
- Fixed waiting for confirmation when input is redirected
- Fixed recursion into symlink directories
- Fixed `ProcessException` to output standard output
- Fixed `MinimalOutput` in `UnityTasks`
- Fixed missing `AzurePipelinesBuildReason`
- Fixed missing arguments in `DotNetTasks`
- Fixed argument formatting in `HelmTasks`
- Fixed missing command in `DotNetTasks`

## [6.2.1] / 2022-08-19
- Fixed logging configuration

## [6.2.0] / 2022-08-19
- Added support for intercepted targets
- Added target interception for Docker
- Added support for context components
- Added `DisableDefaultOutputAttribute`
- Added `InstallNpmToolsAttribute` and `NpmExecutableAttribute`
- Added `EnvironmentInfo.IsArm64`
- Added `SetProcessExecutionTimeout` overload for `TimeSpan`
- Added `DotNetRuntimeIdentifiers`
- Fixed telemetry
- Fixed `GetPathExecutable` to manually search `PATH` environment variable if locator executable is not available
- Fixed resolution of surrogate arguments in Visual Studio
- Fixed performance of `NuGetPackageResolver`
- Fixed `GitTasks.GitIsDetached`
- Fixed missing members in `GitHubActionsImage`
- Fixed missing members in `AzurePipelinesRepositoryType`
- Fixed detection for Bamboo
- Fixed missing arguments in `KubernetesTasks`
- Fixed missing arguments in `DockerTasks`

## [6.1.2] / 2022-07-02
- Removed `Newtonsoft.Json.Schema` dependency
- Fixed `Nuke.GlobalTool` to target `net6.0`
- Fixed telemetry to calculate properties only on demand
- Fixed missing `Framework` in `MinVerTasks` and `MinVerAttribute`
- Fixed missing arguments in `DotNetTasks`

## [6.1.1] / 2022-06-21
- Fixed output encoding in `Nuke.GlobalTool` to be UTF-8
- Fixed telemetry to handle Git repositories without remote
- Fixed `GitRepository.HttpsUrl` and `SshUrl` when `Endpoint` is null
- Fixed `ShutdownDotNetServerBuildAttribute` to timeout after 15 seconds

## [6.1.0] / 2022-06-14
- Removed extended setup wizard
- Changed `Nuke.GlobalTool` to use `Spectre.Console`
- Deprecated `CheckBuildProjectConfigurationsAttribute`
- Added single-file packaging and execution
- Added output customization via `NukeBuild.WriteLogo`, `WriteTarget`, and `WriteSummary`
- Added second-chance registration for MSBuild from .NET CLI
- Added submodule support in GitHub Actions, Space Automation, and AppVeyor
- Added `NukeBuild.BuildAssemblyFile` property
- Added generic `EnvironmentInfo.SetVariable`
- Added support for Bitbucket
- Added GitHub Actions support for `fetch-depth`
- Fixed `default_target` replacement in help text
- Fixed parameter padding and line breaks in help text
- Fixed `Assert` methods to accept `IReadOnlyCollection`
- Fixed fatal failure of `MSBuildLocator`
- Fixed missing MSBuild registration when using `ProjectExtensions`
- Fixed `NuGetPackageResolver` performance by reading metadata from `.nuspec` files
- Fixed `GitRepository` initialization when remote is not set
- Fixed exception for duplicated keys in TeamCity property files
- Fixed missing arguments for `DotNetTasks`

## [6.0.3] / 2022-05-02
- Fixed exception handling in various places
- Fixed shell-completion file to be written after parameter resolution
- Fixed `SpecialFolders.UserProfile` on Docker
- Fixed `SolutionSerializer` to work on sanitized content
- Fixed `GitRepository` branch extensions to consider plurals
- Fixed handling of spaces in GitHub Actions and Azure Pipeline generation
- Fixed resolution of MSBuild for Visual Studio Build Tools edition

## [6.0.2] / 2022-04-13
- Fixed `Update` command to use `net6.0`
- Fixed handling of common errors
- Fixed assertion messages to only include argument expression when message is `null`
- Fixed log file pattern to use `-` instead of `:` for time
- Fixed padding of target names in logging
- Fixed logging to use `ExecutingTarget` instead of `Target` to reduce clashing
- Fixed concurrent writing of shell-completion files
- Fixed telemetry to check for interactive console
- Fixed passing build instance for value injection in components
- Fixed `ValueInjectionAttributeBase.GetMemberValue` to consider members from components
- Fixed resolution of members from parameter files
- Fixed reporting of exceptions in summary when not thrown from targets
- Fixed naming for .NET SDK in bootstrapping scripts
- Fixed GitHub Actions to use `GITHUB_TOKEN` instead of `GITHUB_CONTEXT`
- Fixed GitHub Actions assertion messages
- Fixed missing output types for `ReportGeneratorTasks`
- Fixed missing arguments for `HelmTasks`
- Fixed missing arguments for `SonarScannerTasks`
- Fixed missing command for `KubernetesTasks`
- Fixed path resolution in `PowerShellTasks` to use PowerShell Core on non-Windows systems
- Fixed missing runtime types in `NSwagTasks`

## [6.0.1] / 2022-01-10
- Fixed invisible output for `SystemConsoleHostTheme`
- Fixed `GetRelativePath` for same parts in different places

## [6.0.0] / 2022-01-07
- Removed `ToolSettings.ProcessLogFile` and `ProcessLogTimestamp`
- Removed `GitHub` prefix for `GitHubActions` environment variables
- Deprecated `Logger` in favor of `Serilog.Log`
- Deprecated `ControlFlow` asserts in favor of `Assert` class
- Changed `Nuke.GlobalTool` to enable `RollForward` with `LatestMajor`
- Changed default serialization settings for JSON and YAML in `SerializationTasks`
- Changed GitHub Actions generation to use default `GitHubActions.Token` through `EnableGitHubContext`
- Changed Azure Pipelines generation to use default `AzurePipelines.AccessToken` through `EnableAccessToken`
- Added shell-completion support for global tool builds
- Added `NukeBuild.ExecutionPlan` to public API
- Added `Partition.Part` and `Total` to public API
- Added `MSBuildToolPathResolver` support for Visual Studio 2022
- Added `XmlTasks` variants for `string` objects
- Added `AbsolutePath.Name` and `NameWithoutExtension` properties
- Added `AbsolutePath.Exists`, `FileExists`, and `DirectoryExists` extension methods
- Added `Project.HasPackageReference` and `GetPackageReferenceVersion`
- Added `UpdateFile` variants in `SerializationTasks`
- Added `StdToText` and `StdToJson` extension methods for `IEnumerable<Output>`
- Added newest worker images for Azure Pipelines, GitHub Actions, and AppVeyor generation
- Added Azure Pipelines generation for pull-request triggers, fetch depth, and clean checkout
- Added Space Automation support for secrets
- Added TeamCity support for GUID tokens
- Added `AzurePipelinesCachePaths` for common cache paths
- Added `AzurePipelines.PhaseName` property
- Added `GitHub.CreateComment` for issue and pull-request comments
- Added `TeamCity.AuthUserId` and `AuthPassword` properties
- Added `AppVeyorSecretAttribute` for generation of secret value entries
- Added `HttpClient`, `HttpRequest`, and `HttpResponse` extensions
- Added `XNode` extensions
- Added `LatestMavenVersionAttribute`
- Added `MauiCheckTasks`
- Added `MinVerTasks` and `MinVerAttribute`
- Added `PowerShellTasks`
- Added `BootsTasks`
- Added `NetlifyTasks`
- Fixed check for executables compiled with `PublishSingleFile`
- Fixed `MSBuild` localization using `MSBuildLocator`
- Fixed missing assertion for successful status code in `HttpTasks`
- Fixed Azure Pipelines caching
- Fixed `IBuildServer.Branch` for `AzurePipelines`
- Fixed `OctoVersionTasks` and `OctoVersionAttribute` for latest version
- Fixed `AzureSignToolTasks` to invoke `sign` command
- Fixed missing `Files` property in `AzureSignTool`
- Fixed missing `Blame*` properties in `DotNetTasks`
- Fixed property types in `ILRepackTasks`
- Fixed `UnityTasks` to auto-detect version
- Fixed quoting for `UnityTasks.LogFile`

## [5.3.0] / 2021-08-04
- Added LFS and Submodule settings in AzurePipelines configuration
- Added `OctoVersionTasks` and `OctoVersionAttribute`
- Added `AzureSignToolTasks`
- Added `ChocolateyTasks`
- Fixed invocations for PowerShell bootstrapping script
- Fixed retrieval of `version_dotnet_sdk` in telemetry
- Fixed solution serialization to show information about duplicated entries
- Fixed path construction to be lazy for in-memory solutions that get saved
- Fixed `GitHubTasks.GetGitHubBrowseUrl` to trim trailing slash
- Fixed `GitVersionAttribute.Framework` default value to `net5.0`
- Fixed URLs in `ChangeLogTasks`
- Fixed `DotNetTestSettings.Loggers` property to accept multiple values
- Fixed default value emission for `DotCoverTasks`
- Fixed missing properties for `GitVersionTasks`
- Fixed missing secret attributes in `SonarScannerTasks`
- Fixed `NerdbankGitVersioningFormat` enumeration to use lower-case

## [5.2.1] / 2021-06-18
- Fixed telemetry
- Fixed humanized string concatenation

## [5.2.0] / 2021-06-18
- Added telemetry data collection
- Added unified `NukeBuild.Partition` property
- Added `Rider`, `VisualStudio`, `VSCode` as `Host` implementations
- Added `GitRepository.IsOnMainBranch` and `IsOnMainOrMasterBranch`
- Added `AbsolutePath` equality operators
- Fixed SpaceAutomation to generate default `refSpec`
- Changed `Microsoft.CodeAnalysis.CSharp` package version to `3.9.0`
- Removed `Refit` reference and `ITeamCityRestClient` interface
- Removed `Colorful.Console` reference and embedded figlet fonts

## [5.1.4] / 2021-06-01
- Fixed `StronglyTypedSolutionGenerator` to resolve root directory only on demand
- Fixed `JetBrains.Annotations` to be packed with source generators
- Fixed missing SpaceAutomation configuration link

## [5.1.3] / 2021-05-31
- Fixed filtering explicitly overridden targets in build components

## [5.1.2] / 2021-05-18
- Fixed target duration to be measured immediately after execution
- Fixed build script invocation from global tool
- Fixed `AddPackage` command to allow explicit version parameter
- Fixed navigation methods to not be included in command list
- Fixed `StronglyTypedSolutionGenerator` to resolve root directory only on demand
- Fixed `EnvironmentInfo.Framework` to use entry assembly
- Fixed parsing of `GitRepository` remote
- Fixed missing pull-request properties in TeamCity
- Fixed `RunNumber` and `RunId` in `GitHubActions` to be of type `long`
- Fixed `GitVersionAttribute` to automatically populate `Git_Branch` on TeamCity

## [5.1.1] / 2021-04-23
- Fixed parameter loading with missing default parameters file
- Fixed visibility of `Directory.Build` files
- Fixed `ArgumentsFromCommitMessageAttribute` to require manual application
- Fixed summary reporting for exceptions to only include first line of message
- Fixed update notification
- Fixed PowerShell invocation from `build.cmd`
- Fixed `Update` and `Setup` command to not stage parameters file
- Fixed `Update` command for absent bootstrapping scripts
- Fixed skipping unhandled syntax fragments in Cake conversion
- Fixed missing `Instance` properties for `IBuildServer` implementations
- Fixed GitHubActions default cache path
- Fixed missing property for GitHubActions workflow inputs
- Fixed quoting in GitHubActions for included/excluded paths
- Fixed `XmlPoke` to allow specifying encoding
- Fixed `ExternalFilesTask` for single file browse-URL
- Fixed `ICompile`, `IPack`, `ITest` components to check against `SucceededTargets`
- Fixed setting `RepositoryUrl` in `IPack` component

## [5.1.0] / 2021-04-07
- Removed `:Fix` command from global tool (superseded by `:AddPackage`)
- Changed `.nuke` configuration file to `.nuke` directory
- Changed shell-completion to rely on `build.schema.json` file
- Changed default `DependencyBehavior` to `Skip`
- Changed `HostType` to `Host` base class
- Changed `ExecutionStatus` members `Executed` to `Succeeded`, and `Executing` to `Running`
- Changed `IBuildExtension` instances to be cached
- Changed `IOnBeforeLogo` and `IOnAfterLogo` extensions to `IOnBuildCreated` and `IOnBuildInitialized`
- Changed `IsSuccessful` to check for succeeded, skipped and collective targets
- Changed `ParameterAttribute` to allow external value providers with `ValueProviderType` and `ValueProviderMember`
- Changed GitHubActions secret names to split on camel-humps
- Added support for parameter files (profiles)
- Added source generator for strong-typed solutions
- Added shorthand dependencies for build components
- Added `ReportSummary` for summary extension to `NukeBuild` and `INukeBuild`
- Added exception reporting in summary
- Added `ParameterPrefixAttribute` for build components
- Added build extensions for `OnTargetSkipped`, `OnTargetRunning`, `OnTargetSucceeded`, and `OnTargetFailed`
- Added styling for unlisted targets in execution plan HTML
- Added `:Secrets` command to global tool and `SecretAttribute` for encryption in parameters files
- Added `:AddPackage` command to global tool
- Added `:GetConfiguration` command to global tool
- Added `:Update` command to global tool
- Added `:CakeConvert` and `:CakeClean` commands to global tool
- Added generation of `Directory.Build.props` and `Directory.Build.targets` files
- Added parameter resolution for root directory in global tool
- Added shell-navigation aliases `nuke/` and `nuke-`
- Added `ScheduledTargets`, `RunningTargets`, `AbortedTargets`, `FailedTargets`, `SucceededTargets` collections to `NukeBuild` and `INukeBuild`
- Added `ArgumentsFromCommitMessageAttribute` and `:Trigger` command to global tool
- Added `ExitCode` to `INukeBuild`
- Added `IsFinished` and `IsFailing` to `NukeBuild` and `INukeBuild`
- Added caching for `ValueInjectionUtility.TryGetValue`
- Added equality operators and implicit conversion to string for Enumeration
- Added `GetProject`, `GetSolutionFolder`, `Projects`, and `SolutionFolders` to `SolutionFolder`
- Added `GetRuntimeIdentifers` to `ProjectExtensions`
- Added string-escape extension for MSBuild in `DotNetTasks` and `MSBuildTasks`
- Added `EnsureExistingDirectory` and `EnsureExistingParentDirectory` overloads for `AbsolutePath`
- Added `XmlPeekElements` to `XmlTasks`
- Added `GitRepository` properties `RemoteName` and `RemoteBranch`
- Added support for `NerdbankGitVersioning`
- Added `NoCache` property to `GitVersionAttribute` with default value `true`
- Added `SendOrUpdateSlackMessage` to `SlackTasks`
- Added support for JetBrains SpaceAutomation
- Added support for GitHub Actions dispatch workflows
- Added support for GitLab CI
- Added support for multiple AppVeyor configurations
- Added support for AppVeyor secrets
- Added support for platform-independent TeamCity configurations
- Added TeamCity parameter to replace run-button caption
- Added `AddTeamCityLogger` for `DotNetTest` task
- Added `IsPersonalBuild` property to `TeamCity`
- Added caching for `AzurePipelinesAttribute` and `GitHubActionsAttribute`
- Added `SetVariable` to `AzurePipelines`
- Added `CodeMetricsTasks`
- Added `PulumiTasks`
- Added `CodecovTasks`
- Added `CorFlagsTasks`
- Added `FixieTasks`
- Added `ILRepackTasks`
- Fixed skip reason for targets skipped from `--skip` parameter
- Fixed execution plan legend
- Fixed execution plan highlighting for multiple default targets
- Fixed `ConsoleUtility` to allow full deletion of secret
- Fixed exception reporting for `ExecutableTargetFactory`
- Fixed handling of value types in `ValueInjectionUtility.TryGetValue`
- Fixed calculation of value sets for collection parameters
- Fixed compilation of legacy template
- Fixed `IsDescendantPath` to split path parts
- Fixed `MoveDirectory` with additional `deleteRemainingFiles` parameter
- Fixed `SwitchWorkingDirectory` to respect `allowCreate` parameter
- Fixed `ResponseArchive` in `ISignPackages` build component
- Fixed MSBuild path resolution to fallback to using `dotnet --list-sdks`
- Fixed client usage in `GitHubTasks`
- Fixed token ordering in `TemplateUtility`
- Fixed default value for collections in TeamCity configurations
- Fixed TeamCity composite configurations to propagate failures
- Fixed `TeamCity` and `AzurePipelines` to update build numbers in environment variables
- Fixed `TriggerBatch` in AzurePipelines generation
- Fixed condition in AppVeyor generation
- Fixed `FileFilters` property in `ReportGeneratorTasks`
- Fixed logger for `DocFXTasks`
- Fixed `Severity` property in `ReSharperTasks`
- Fixed missing `MSBuild` and `ToolRestore` task in `DotNetTasks`
- Fixed missing `Buildx` task in `DockerTasks`
- Fixed missing `CoverDotNet` task in `DotCoverTasks`
- Fixed missing `GenericCoveragePaths` property in `SonarScannerTasks`
- Fixed missing properties in `ReSharperTasks`
- Fixed missing properties in `TeamCity`, `GitHubActions`, and `AzurePipelines`
- Fixed missing `SignToolDigestAlgorithm` enumeration in `SignToolTasks`

## [5.0.2] / 2020-12-07
- Fixed `ChangelogTasks` to use HTTPS links in history
- Fixed `DotNetRun` and `DotNetTest` run settings
- Fixed conditions for informational text

## [5.0.1] / 2020-12-06
- Fixed configuration generation to wait for user input after file changes
- Fixed build summary for durations smaller than 1 second
- Fixed build summary and `IBuildExtension` instances to be skipped if no targets were started
- Fixed build summary to hide irrelevant durations
- Fixed setting of `EmbeddedPackagesDirectory` for global tools
- Fixed `PackPackageToolsTask` to use lower-case package ids
- Fixed `ParameterAttribute.ValueProvider` to allow members of type `IEnumerable<string>`
- Fixed `Logger` to remove `ControlFlow` from stacktrace
- Fixed assertion messages for warnings
- Fixed path and quoting in `build.cmd`
- Fixed `GitVersion.Tool` version in project templates
- Fixed `LatestMyGetVersionAttribute` to handle new RSS feed format
- Fixed missing arguments `PublishReadyToRun`, `PublishSingleFile`, `PublishTrimmed`, `PublishProfile`, `NoLogo` for `DotNetPublish`
- Fixed parameter name `Verbosity` in `DotNetPack`
- Fixed enumeration value `lcov` in `CoverletTasks`
- Fixed `ReSharperTasks` to use correct tool path
- Fixed `ChangelogTasks` to respect additional markdown-linting rules
- Fixed TeamCity generator to consider artifact products from all relevant targets
- Fixed condition for skipping lines in TeamCity parameter files

## [5.0.0] / 2020-11-12
- Fixed version number

## [0.25.0] / 2020-10-26
- Removed `Configuration` from `Nuke.Common` and moved it to template
- Changed `InjectionAttribute` to catch exceptions and report as warnings
- Changed `ToolPathResolver` to ignore casing
- Changed `ToolSettings` to prefix common properties with `Process`
- Changed property names in `Nuke.Common.targets`
- Changed `GitRepository` to trim `refs/heads/` and `origin/` from branch names
- Changed `ShutdownDotNetBuildServerOnFinish` to not log by default
- Changed `ShutdownDotNetBuildServerOnFinish` to only shutdown on server build
- Added support for interface default implementations
- Added `NukeBuild.ExitCode` for custom exit codes
- Added `ProcessTasks.StartShell` to invoke shell commands
- Added `ToolSettings.Apply` for fluent configurator invocation
- Added `ToolSettings.LogFile` and `LogTimestamp`
- Added `nuke :fix` command to `Nuke.GlobalTool` for adding missing package downloads
- Added `nuke :GetRootDirectory` and `nuke :GetParentRootDirectory` in `Nuke.GlobalTool`
- Added `LatestNuGetVersionAttribute`, `LatestGitHubReleaseAttribute`, `LatestMyGetVersionAttribute`
- Added `GitRepository.Protocol`, `Commit`, and `Tags` properties
- Added logger delegate to `ControlFlow.ExecuteWithRetry`
- Added `BuildExtensionAttributeBase` with `Priority` property
- Added `UnsetVisualStudioEnvironmentVariables` by default
- Added `TeamCity.BuildVcsNumber` property
- Added AzurePipelines variable groups, secret and access token import
- Added `AppVeyor.Url` and `PushArtifact` members
- Added warning when `GitVersion` is used with SSH endpoint and `NoFetch` is disabled
- Added consolidated `ReSharperTasks` for `CleanupCode`, `InspectCode`, and `DupFinder`
- Added `TeamsTasks`
- Added `SignPathTasks`
- Added `SignClientTasks`
- Added `BenchmarkDotNetTasks`
- Added `CleanupCodeTasks`
- Added `DotNetTasks.DotNetNuGetAddSource` task
- Added `OctopusTasks.OctopusBuildInformation` task
- Added missing properties in `SonarScannerTasks`
- Added verbosity mapping attributes for `NUnit`, `OpenCover`, and `ReportGenerator`
- Fixed version check in bootstrapping scripts to rely on dotnet CLI exit code
- Fixed deactivation of multi-level lookup in bootstrapping scripts
- Fixed deactivation of shared compilation in bootstrapping scripts
- Fixed `ToolPathResolver` to consider all package executable names
- Fixed `ToolPathResolver` to choose executable based on operating system
- Fixed output/input encoding to use `UTF-8`
- Fixed `NukeBuild.BuildProjectFile` property
- Fixed AppVeyor generation for Unix images
- Fixed `AzurePipelinensAttribute` to allow multiple use
- Fixed AzurePipelines to replace dots in stage name with underscore
- Fixed `AppVeyor.UpdateBuildVersion` to set environment variable
- Fixed `DupFinderTasks.DiscardCost` property
- Fixed `DotCoverTasks` to use double-dashes instead of slashes
- Fixed `NpmTasks.CustomLogger` to detect warnings in error output

## [0.24.11] / 2020-05-18
- Fixed transitive artifacts in configuration generation
- Fixed `StackOverflowException` in configuration generation
- Fixed `IsPackable` property default
- Fixed missing colon in GitHubActions triggers
- Fixed assertion message for finding Git directory
- Fixed assertion message for `teamcity.dotCover.home`

## [0.24.10] / 2020-04-24
- Fixed MSBuild targets to switch on `MSBuildRuntimeType` again
- Fixed default includes for `NukeSpecificationFiles` and `NukeExternalFiles`
- Fixed indentation for GitHubActions scheduled triggers
- Fixed assertion message for GitHubActions trigger definitions
- Fixed default `RootNamespace`

## [0.24.9] / 2020-04-16
- Fixed MSBuild targets directories

## [0.24.8] / 2020-04-12
- Fixed publishing of global tool for `netcoreapp3.1`
- Fixed .NET Core SDK install script URL
- Fixed trap error output in PowerShell bootstrapping
- Fixed AzurePipelines push triggers
- Fixed AzurePipelines configuration to allow overriding configuration directory
- Fixed previous constructor usages for `AzurePipelinesAttribute`
- Fixed PowerShell downloads to use TLS 1.2 security protocol
- Fixed unrecognized `Visible` attribute for `PackageDownload` item group

## [0.24.7] / 2020-03-26
- Fixed MSBuild targets for .NET Core
- Fixed `GitRepository.GetGitHubMilestone` to retrieve milestone independent of state

## [0.24.6] / 2020-03-25
- Fixed NuGet package resolution performance
- Fixed MSBuild integration
- Fixed TeamCity trace output to be dark gray
- Fixed missing using statement for `Nuke.Common.IO`

## [0.24.5] / 2020-03-24
- Fixed TeamCity configuration to use Bash script on Unix

## [0.24.4] / 2020-03-05
- Fixed Refit version
- Fixed conversion of `GitHubActionsTrigger`
- Fixed project default includes to check existence of files
- Fixed projects to target `netcoreapp2.1`
- Fixed configuration generation to allow multiple configurations per host type
- Fixed `AzurePipelinesAttribute` to allow setting a configuration suffix
- Fixed CI server detection to ignore empty environment variables
- Fixed `TeamCityOutputSink` to not report errors as build problems
- Fixed custom logger for `NpmTasks`
- Fixed custom logger for `DockerTasks`
- Fixed missing NuGet install task
- Fixed missing `Framework` property in `OctopusTasks`
- Fixed missing `ReportType` in `DotCoverTasks`
- Fixed missing properties in `DotNetTasks`
- Fixed Glob version
- Fixed `GitVersionAttribute` to avoid duplicated version numbers
- Fixed `System.ValueTuple` version

## [0.24.2] / 2020-02-15
- Fixed extension methods for settings with base type
- Fixed `SonarScannerTasks` to have `Framework` property
- Fixed generation of `shell-completion.yml` to exclude unlisted targets for invocation

## [0.24.1] / 2020-02-07
- Fixed `NuGetPackageResolver` to include dependencies during tool path resolution
- Fixed parsing of TeamCity environment variables
- Fixed execution flags for `build.sh` and `build.cmd` scripts during setup
- Fixed assertion message in `UnityTasks`
- Fixed `build.cmd` to have newline at end-of-file
- Fixed logo spacing

## [0.24.0] / 2020-02-02
- Removed `NuGetPackage` tasks and AutoMapper package reference
- Removed TeamCity definitions for `VcsRoot` and trigger timezones
- Changed `AbsolutePath`, `RelativePath`, `WinRelativePath` and `UnixRelativePath` to outer scope
- Changed default package for `DotCoverTasks` to `JetBrains.dotCover.DotNetCliTool`
- Changed default includes to be provided via `Nuke.Common.targets`
- Changed `ConfigurationGenerationAttributeBase` to `ConfigurationAttributeBase`
- Changed manually invoked targets to be TeamCity deployment configurations
- Changed `AzurePipelines` interface to use enumerations for test result type and code coverage tool type
- Changed package version for `Glob`, `Microsoft.IdentityModel.Clients.ActiveDirectory`, `Newtonsoft.Json`, `NuGet.Packaging`, `Refit`, `YamlDotNet`
- Added cross-platform `build.cmd` bootstrapping script
- Added build emotions
- Added update of build number for TeamCity, AppVeyor, and Azure Pipelines from `GitVersionAttribute`
- Added `AzureKeyVault` – previously available as addon
- Added `DocFXTasks`, `DockerTasks`, `HelmTasks`, `KubernetesTasks`, and `NSwagTasks` – previously available as addons
- Added TeamCity logger extension method for `DotNetBuildSettings`
- Added support for checkboxes in TeamCity configuration
- Added `GitHubTasks`
- Added `ProjectModelTasks.CreateSolution`
- Added `Solution.GetProject` and `GetSolutionFolder` overloads via `Guid`
- Added `TeamCity.NightlyBuildAlways` property
- Added detailed null-check for `teamcity.build.branch` configuration property
- Added Coverlet extension methods for `DotNetTest` task
- Added `AzurePipelines.PublishCodeCoverage`
- Added setters for `Project` properties
- Added `Solution.AddSolution` and `ProjectModelTask.CreateSolution` overload for creating global solutions
- Added path extension methods for `Get(Win|Unix)RelativePathTo`, `Contains`, and `To(Win|Unix)RelativePath`
- Added `NoFetch`, `Framework`, and `UpdateBuildNumber` properties to `GitVersionAttribute`
- Fixed directory creation in bootstrapping scripts
- Fixed artifact paths for TeamCity and Azure Pipelines
- Fixed path separators for AppVeyor and GitHubActions configurations
- Fixed `NSwag` to quote tool path
- Fixed `SolutionSerializer` to handle inconsistent whitespaces
- Fixed `NpmCi` task to include definite argument
- Fixed `VSTestSettings.TestCaseFilters` to be list of strings
- Fixed `EnvironmentInfo.FrameworkName`
- Fixed `cleanCheckoutDirectory` to be set for all TeamCity build types
- Fixed `AddTeamCityLogger` extension method
- Fixed `buildType` reference in TeamCity build-finished triggers
- Fixed `ReportGenerator` task to resolve `ReportGenerator.dll`
- Fixed sharing of artifacts between agents
- Fixed `GitVersionSettings.UpdateAssemblyInfoFileNames` to be an array

## [0.23.7] / 2020-01-28
- Fixed summary alignment for hosts that trim whitespaces

## [0.23.6] / 2020-01-12
- Fixed `InspectCodeTasks` to use new plugin endpoint for downloading
- Fixed `AppVeyorOutputSink` to issue a warning when exceeding the default limit of 500 messages

## [0.23.5] / 2020-01-10
- Fixed CI integrations to use correct warning/error reporting infrastructure
- Fixed TeamCity configuration to use `UTF-8` encoding
- Fixed process encoding by setting `StandardOutputEncoding` and `StandardErrorEncoding` to `UTF-8`
- Fixed solution deserialization for missing configuration section
- Fixed logo spacing

## [0.23.4] / 2019-11-16
- Fixed assignment for `NuGetAssetsConfigFile` when `BuildProjectDirectory` is null
- Fixed `ToolPathResolver` to not require framework when only one file matches

## [0.23.3] / 2019-11-02
- Fixed separator in Azure Pipelines service messages

## [0.23.2] / 2019-11-02
- Fixed ensuring of existing directory for generation of configuration files
- Fixed packaging of `MSBuildTaskRunner` in `Nuke.Common`

## [0.23.1] / 2019-11-02
- Fixed checking hashes for non-existing configuration files
- Fixed null-reference exception for commands without message

## [0.23.0] / 2019-10-31
- Changed target frameworks to `netcoreapp3.0` and `net472`
- Changed `AzureDevOps` to `AzurePipelines`
- Changed `CheckBuildProjectConfigurationsAttribute` to skip dot-prefixed directories
- Removed `ProjectFromAttribute`
- Removed `MSBuildTasks.MSBuildParseProject`
- Removed `GitVersion.GetNormalizedAssemblyVersion` and `GetNormalizedFileVersion`
- Added NuGet package resolution from `project.assets.json` file
- Added CI interface resolution via `CIAttribute`
- Added `Bamboo` interface
- Added `TeamCityImportDotCoverPathAttribute` to address version mismatch
- Added `GitHubActionsAttribute` for configuration generation
- Added `AzurePipelinesAttribute` for configuration generation
- Added `AppVeyorAttribute` for configuration generation
- Added execution of `dotnet build-server shutdown` when build has finished
- Added `NpmCi` task
- Fixed `TeamCity` parameter dictionaries to use original keys
- Fixed NuGet package resolution for project files without `PackageReference` items
- Fixed code inspections in PowerShell script
- Fixed resolution for legacy package directories
- Fixed generation of `Partition` parameter and script paths
- Fixed `ToolPathResolver` to support global tool packages
- Fixed `ReportGeneratorTasks` and `GitVersionTasks` by providing `Framework` property

## [0.22.2] / 2019-09-29
- Fixed SourceLink integration

## [0.22.1] / 2019-09-21
- Fixed assertion message for missing packages

## [0.22.0] / 2019-09-17
- Changed `UnlistedAttribute` to `List` property on `ParameterAttribute`
- Changed summary to show aborted and not-run targets as warning
- Changed `TeamServices` to `AzureDevOps`
- Changed namespace `Nuke.Common.BuildServers` to `Nuke.Common.CI.*`
- Added support for multiple default targets
- Added support for `PackageDownload` item group
- Added support for hyphens in target names
- Added support for absolute paths in `LocalExecutableAttribute`
- Added support for `GitHubActions`
- Added TeamCity configuration generation via `TeamCityAttribute`
- Added XML serialization for .NET Core
- Added reporting of TeamCity statistical values
- Added additional methods for `CloudFoundryTasks`
- Added `ProjectType` for Docker and SQL projects
- Added implicit cast operator for generated enumerations
- Added `InnoSetupTasks`
- Added `TwitterTasks`
- Added `IOnBuildFinished` build extension
- Added missing arguments for `CoverletTasks`
- Fixed `--boot` in setup for .NET Framework/Mono support
- Fixed XML documentation for generated CLI tasks
- Fixed `MSBuildToolPathResolver` to consider preview editions
- Fixed `NuGetPackageResolver` to allow multiple versions of the same package
- Fixed `TeamCity.SetParameter` and `TeamCity.ImportData`
- Fixed `SolutionSerializer` to fall back to `ProjectConfiguration` section
- Fixed `MSBuildLocator` package to have `vswhere.exe` embedded

## [0.21.2] / 2019-07-28
- Fixed validation to exclude requirements of skipped targets
- Fixed solution serialization to include malicious project GUID in error message

## [0.21.1] / 2019-07-19
- Fixed logging of warnings

## [0.21.0] / 2019-07-15
- Changed `ProjectModelTasks.ParseProject` to revert `MSBUILD_EXE_PATH` environment variable
- Added `CloudFoundryTasks`
- Added missing arguments for `SonarScannerTasks`
- Added missing arguments for `OctopusTasks`
- Added `Jenkins.BranchName` and `Jenkins.ChangeId`

## [0.20.1] / 2019-06-02
- Fixed MSBuild evaluation issues by updating NuGet.Packaging to v4.9.2

## [0.20.0] / 2019-05-29
- Changed `Solution.GetProject` to allow resolution from full path
- Changed HTML execution plan to be shown left-to-right
- Added `When` overload for combinatorial settings
- Added `Project.GetOutputType` method for convenience
- Added `GlobbingOptionsAttribute` for configuration of case-sensitivity
- Fixed casing of `NuGet.exe`
- Fixed `TeamServices` to resolve `BuildNumber` as `string`

## [0.19.2] / 2019-05-10
- Fixed `ProjectModelTasks` to use existing `MSBUILD_EXE_PATH` value
- Fixed `ParameterService` to consider nullable enum types in value set calculation
- Fixed compile errors in build template

## [0.19.1] / 2019-05-03
- Fixed `RequirementService` to check for `InjectionAttributeBase`

## [0.19.0] / 2019-05-03
- Changed MSBuild targets to be invoked with `Exec` task
- Changed `ProcessTasks` to avoid Mono when using WSL
- Added output for non-default working directories
- Added `GitVersion.VersionSourceSha`
- Added `ReportTypes.TeamCitySummary`
- Fixed parameter resolution to handle hyphens
- Fixed MSBuild resolution for Visual Studio 2019
- Fixed issues when build has no default target defined

## [0.18.0] / 2019-03-24
- Changed `ParameterService` to strip dashes when resolving value
- Changed formatting of skip reason
- Added `CompressionTasks`
- Added `EntityFrameworkTasks`
- Fixed `UnityTasks.UnityPath` for Windows
- Fixed `SystemColorOutputSink` to print warning and error details
- Fixed `SonarScannerTasks` to also resolve from netstandard package

## [0.17.7] / 2019-03-12
- Fixed `SystemColorOutputSink` to set `ForegroundColor`

## [0.17.6] / 2019-03-04
- Fixed `RequirementService` to check for `ParameterAttribute` when injecting values interactively

## [0.17.5] / 2019-03-03
- Fixed `GlobDirectories` and `GlobFiles` to not collect items lazily

## [0.17.4] / 2019-03-02
- Fixed bootstrapping script to not set `NUGET_XMLDOC_MODE`

## [0.17.3] / 2019-02-27
- Fixed documentation file generation
- Fixed `CheckBuildProjectConfigurationsAttribute.Timeout` to be settable

## [0.17.2] / 2019-02-24
- Fixed parsing of changelog

## [0.17.1] / 2019-02-23
- Fixed attributes in build tasks

## [0.17.0] / 2019-02-23
- Removed collection-based tasks in `FileSystemTasks`
- Changed `ContinueOnFailure` to `ProceedAfterFailure`
- Changed summary output to not include collective targets
- Added `logInvocation` parameter and `ToolSettings.LogInvocation` property
- Added interactive parameter resolution
- Added `RequiredAttribute` for globally required parameters
- Added skip reason to summary
- Added `FileGlobbingAttribute` and `DirectoryGlobbingAttribute`
- Added `GetProperty<T>`, `GetItems<T>`, and `GetItemMetadata<T>` as `ProjectExtensions`
- Added `Unlisted` for target declarations
- Added `ToolResolver` for custom delegate resolution
- Added `DotNetToolInstall`, `DotNetToolUninstall`, and `DotNetToolUpdate`
- Added `UnsetVisualStudioEnvironmentVariablesAttribute`
- Added universal log methods with severity as parameter
- Fixed parameter resolution for value types
- Fixed `AbsolutePath` to be serializable
- Fixed output for parallel task execution
- Fixed exit code for failing targets using `ProceedAfterFailure`
- Fixed exception message for circular dependencies

## [0.16.0] / 2019-01-30
- Changed setting of default working directory for process invocations
- Changed `Logger.Log` to `Logger.Normal`
- Added `NukeBuild.Execute` overload without default target
- Added `ContinueOnFailure` and `AssuredAfterFailure` as target definition methods
- Added `AbsolutePath` extensions for `GlobDirectories/Files`
- Added `AggregateException` handling to show number as prefix when flattening
- Added `AnsiColorOutputSink` for Bitrise, TeamCity, Travis, TeamServices
- Added `ProjectModelTasks.ParseProject` based on `Microsoft.Build` packages
- Added `LocalExecutableAttribute`
- Added `degreeOfParallelism` and `completeOnFailure` for combinatorial invocations
- Added `[Tool]Tasks.[Tool]Logger` as settable field for custom logging
- Added `VerbosityMappingAttribute`
- Added format-property map for CLI tasks
- Fixed `EnsureCleanDirectory` to only clean instead of delete and recreate
- Fixed `TeamCityOutputSink` to not report errors as build problems
- Fixed `SolutionAttribute` to resolve first by constructor argument
- Fixed `Xunit2ParallelOption` to use lower-case text

## [0.15.0] / 2019-01-16
- Changed `OnlyWhen` to `OnlyWhenStatic` and `OnlyWhenDynamic`
- Changed `graph` parameter to `plan`
- Added `DependentFor`, `Triggers` and `TriggeredBy` for target declarations
- Added `ToolSettings.CombineWith` for combinatorial invocations
- Added several `FileSystemTasks` methods
- Added `TemplateUtility.FillTemplateDirectory` and `FillTemplateFile`
- Added highlighting of execution plans in HTML representation
- Added process cancellation handler to always show summary
- Added `NuGetTasks` to add, remove, update, enable, disable and list sources
- Added `TravisOutputSink`
- Added path resolution for `VSTestTasks`
- Added caching of `MSBuild` path in `GetMSBuidProject`
- Fixed `GitRepository.IsOnDevelopBranch` to recognize `develop` and `development`
- Fixed shell-completion for PowerShell

## [0.14.1] / 2018-12-31
- Fixed package reference versions
- Fixed `SolutionSerializer` to handle empty lines

## [0.14.0] / 2018-12-31
- Removed named target dependencies
- Removed choice of target framework in setup
- Changed setup to write solution file reference to configuration file again
- Added extended solution parsing with integration for `Microsoft.Build`
- Added `Configuration` type
- Added `continue` parameter
- Added checking for active build project configurations in solution files
- Added highlighting for default target in HTML graph
- Added `SonarScannerTasks`
- Added `EnvironmentInfo.SwitchWorkingDirectory`
- Added `SymbolPackageFormat` property for `DotNetTasks`, `MSBuildTasks`, and `NuGetTasks`
- Fixed bootstrapping scripts not to leave DotNet processes behind
- Fixed bootstrapping scripts to correctly quote arguments
- Fixed overload of tool path for .NET Core executables
- Fixed default value not to be hidden by cursor
- Fixed `ToolSettingsExtensions.When` to have generic constraint on `ToolSettings`
- Fixed `InspectCodeTasks` to use deterministic hashing
- Fixed `ChangelogTasks` to correctly parse empty sections at end of file
- Fixed `InjectionAttributeBase` to express implicit assignment only
- Fixed `ExternalFilesTask` to be executed before `Restore` target

## [0.13.0] / 2018-12-10
- Changed verification of PATH environment variable to be executed only with `Trace` log level
- Added `ToolSettings.When` for conditional fluent modifications
- Added `.editorconfig` file in setup to avoid formatting issues
- Added `DotMemoryUnitTasks`
- Added missing properties in `DotNetCleanSettings`, `DotNetRestoreSettings` and `MSBuildSettings.Restore`

## [0.12.4] / 2018-12-02
- Fixed `SolutionAttribute` to handle empty configuration file

## [0.12.3] / 2018-11-29
- Fixed `EnvironmentInfo.Variables` not to be cached
- Fixed `Glob` package reference in legacy template
- Fixed error message for unresolvable root directory
- Fixed global tool setup to hide choice of bootstrapping by default
- Fixed `NuGetPackageResolver` assertion for dependency resolution

## [0.12.2] / 2018-11-27
- Fixed globbing related issues
- Fixed shell-completion to not split common names
- Fixed bootstrapping scripts to guard extraction of SDK version
- Fixed help text to be printed before value injection

## [0.12.1] / 2018-11-24
- Fixed bootstrapping scripts to exit without closing PowerShell
- Fixed expansion for Unix environment variables
- Fixed separator for target parameters
- Fixed `ToolPathResolver` to resolve decidedly
- Fixed `GitVersionTasks` to resolve based on `GitVersion.CommandLine.DotNetCore` package
- Fixed `InjectionAttributeBase` to pass build instance
- Fixed `ReflectionService` to be public to allow usage in addons
- Fixed `DotNetTasks` to expose `restore` related parameters for `test`, `build`, `publish`, `pack`, `run`

## [0.12.0] / 2018-11-15
- Changed `NukeBuild` properties to be static
- Changed `NukeBuild.RootDirectory` to allow resolution from parameter
- Added package resolution for Paket
- Added shell-completion for global tool
- Added parameter resolution for `Enumeration` subclasses
- Added `PathExecutableAttribute` and `PackageExecutableAttribute` for `Tool` delegate resolution
- Added `PackPackageToolsTask` for global tool packaging
- Added `MSpecTasks`
- Fixed bootstrapping scripts to install by channel instead of latest version
- Fixed Glob package version to 0.3.2
- Fixed `Arguments` passing of `secret` parameter

## [0.11.1] / 2018-10-17
- Security: Updated YamlDotNet version

## [0.11.0] / 2018-10-11
- Removed deprecated APIs
- Added event methods `OnBuildCreated`, `OnBuildInitialized`, `OnBuildFinished`, `OnTargetStart`, `OnTargetAbsent`, `OnTargetSkipped`, `OnTargetExecuted` and `OnTargetFailed`

## [0.10.5] / 2018-10-10
- Fixed `--source` parameter for `DotNetRestore` to be collection

## [0.10.4] / 2018-10-10
- Fixed `GitRepository` when origin URL uses SSH without username

## [0.10.3] / 2018-10-05
- Fixed `WinRelativePath` and `UnixRelativePath` to use correct separator

## [0.10.2] / 2018-10-04
- Fixed `RequirementService` to also support shorthand for properties

## [0.10.1] / 2018-10-02
- Fixed wizard to pass definitions for project file template
- Fixed wizard to create source and tests directory if selected
- Fixed wizard to be disabled for legacy format

## [0.10.0] / 2018-10-02
- Removed `PathConstruction.GetRootRelativePath`
- Removed `License` from specification files
- Deprecated `NukeBuild.Configuration` which should belong to user-code
- Deprecated plus operator in `PathConstruction.AbsolutePath` and `RelativePath`
- Changed `SolutionAttribute` to resolve solution file via parameter
- Changed CLI wrapper tasks to attempt to resolve tool paths from `[TOOL]_EXE` environment variable
- Added `AbsolutePath.Parent` and equality members
- Added `TypeConverter` for `AbsolutePath` which allows passing paths as parameter
- Fixed detection of value types in specification files
- Fixed path variable check to split by specific separator

## [0.9.1] / 2018-09-26
- Fixed wrong assertions in global tool

## [0.9.0] / 2018-09-22
- Deprecated properties in `NukeBuild` which should belong to user-code
- Deprecated default settings which should belong to user-code
- Deprecated `DocFxTasks` which is moved to own package
- Added `SpecFlowTasks`
- Added `NukeBuild.OutputSink` property for custom logger implementation
- Fixed `MSBuildLocator` and `MSBuildToolPathResolver` to also consider `/usr/local/bin/msbuild`

## [0.8.0] / 2018-09-07
- Changed `ProcessTasks` to automatically invoke .NET Core DLLs with `dotnet.exe`
- Added `CoverletTask`
- Fixed exception in `ChangelogTasks.ReadChangelog` when `vNext` section was empty
- Fixed console output to use ASCII instead of Unicode
- Fixed `MSBuildLocator` to use fallbacks when no VS instance with .NET Core is installed

## [0.7.0] / 2018-08-29
- Changed assertion of `DataClass` properties print out value on failure
- Added `SquirrelTasks`
- Added `UnityTasks`
- Added tasks to update the changelog and get the latest version to `ChangeLogTasks`
- Fixed global tool to order solutions descending
- Fixed global tool setup to use correct definitions and error about broken solution
- Fixed validation of requirements of skipped targets
- Fixed double evaluation of conditions with `DependencyBehavior.Skip`

## [0.6.2] / 2018-08-18
- Fixed `MSBuildLocator` to not use `System.ValueTuple`
- Fixed typo in `OctopusCreateReleaseSettings`
- Fixed adaptation of solution file in global tool
- Fixed output of global tool on Windows

## [0.6.1] / 2018-08-09
- Fixed global tool to have 'same as global tool' as fallback version
- Fixed PowerShell invocation to use `-ExecutionPolicy ByPass` and `-NoProfile`

## [0.6.0] / 2018-08-05
- Removed setup scripts in favor of `:setup` command in global tool
- Removed `ProcessSettings` in favor of integrating related properties into `ToolSettings`
- Removed deprecated APIs
- Changed tasks with return type to return value tuple
- Changed tasks to redirect output by default
- Added `ITargetDefinition.WhenSkipped` to specify dependency behavior for skipped targets
- Added `SlackTasks` and `VSWhereTasks`
- Added namespace support in `XmlTasks`
- Added `FileSystemTasks` for deleting, moving, copying and hash calculation
- Added support for loading external files
- Fixed various build server properties
- Fixed output color for `Logger.Info` to be `Console.ForegroundColor`
- Fixed naming of `VSTestTasks`
- Fixed build script to use VSWhere for locating MSBuild
- Fixed `NuGetPackageResolver` to determine `globalPackagesFolder` from config files
- Fixed `Xunit2Settings` to specify framework of console executable
- Fixed `DotNetRunSettings` to not quote `ApplicationArguments`

## [0.5.3] / 2018-06-12
- Fixed global tool to search build scripts also in current directory
- Fixed generic tasks to not redirect output by default

## [0.5.2] / 2018-06-11
- Changed build summary to log skipped and absent targets unconditionally
- Added `HttpTasks` and `FtpTasks` for `netstandard` target framework
- Fixed global tool to simply exit if script execution returns non-zero exit codes
- Fixed global tool to search build scripts only within 2-level non-system sub-directories
- Fixed build summary to treat `NotRun` as a failure

## [0.5.0] / 2018-06-05
- Changed build scripts to download .NET Core SDK only if local installation is missing or doesn't match expected version
- Added global tool for setup and build invocation
- Added version logging for PowerShell, Bash, NuGet and DotNet
- Added error output for CLT tasks when redirect output is enabled
- Added `[Tool]Tasks.[Tool](string arguments)` for all CLTs
- Added support for double-dashed arguments
- Fixed resolution of `Skip` parameter when using separators
- Fixed font resource resolution for deprecated namespace
- Fixed saving location of build scripts

## [0.4.0] / 2018-05-02
- Deprecated `Nuke.Core` namespace. All types have been moved to `Nuke.Common`
- Changed parameter binding to allow lisp-cased arguments (dashes for camel-humps)
- Changed build execution to automatically unwrap `AggregateException` and `TargetInvocationException`
- Changed build server instances to access variables in non-ensured way
- Changed `GitRepository.FromLocalDirectory` to not return null but fail instead
- Changed reference from `NuGet.Client` to `NuGet.Packaging`
- Changed summary output to use `Trace`, `Error`, `Success` methods of `Logger`
- Added integration infrastructure for ReSharper plugin
- Added typo-checking for arguments that should be bound via `ParameterAttribute`
- Added automatic retrieval of `GitRepositoryAttribute.BranchName` from build server instances
- Added `Logger.Success` method
- Fixed `GitRepository.ParseUrl` to strip username and password
- Fixed nullable properties in `TeamServices` and `Bitrise`
- Fixed host simulation
- Fixed environment variable parsing when case-insensitive duplicates are found

## [0.3.1] / 2018-03-26
- Deprecated `Action` usages in `DotCoverTasks` and `OpenCoverTasks` in favor of `SetTargetSettings`
- Added `ProjectModelTasks` with matching `SolutionAttribute` for auto-injection
- Added `[Tool]Tasks.[Tool]Path` property for better accessibility
- Added `DotCoverTasks` aliases for `cover`, `delete`, `merge`, `report` and `zip`
- Added `ArrayExtensions` for deconstruction
- Changed `NukeBuild.Configuration` to be overridable but still injectable
- Fixed `ProcessManager` to resolve `toolPath` from environment
- Fixed `ProcessManager` to filter executable based on operating system and file extensions
- Fixed `DeleteDirectory` for non-existent sub-directories at time of deletion
- Fixed line-endings in setup scripts

## [0.2.10] / 2018-03-05
- Fixed handling of `Graph` switch
- Fixed logging in Nuke.CodeGenerator
- Fixed temporary directory path in setup and bootstrapping scripts
- Fixed `NuGetSettings` to resolve tool path from `NuGet.CommandLine` package
- Fixed `Invoke-WebRequest` when InternetExplorer's first-launch configuration was not completed
- Fixed resolution of relative paths to be minimal
- Fixed `PathConstruction.GetRelativePath` to work with Unix paths
- Fixed argument formatting for boolean values
- Fixed enumeration of modified collection

## [0.2.0] / 2018-02-26
- Deprecated `Target` parameter in favor of passing targets as first argument to the bootstrapping scripts
- Deprecated `NoDeps` parameter in favor of new `Skip` parameter that takes a separated list
- Deprecated `DefaultSettings` which are now exposed in each task class individually
- Changed default values for `AssemblyVersion` to `{major}.{minor}.0` and `FileVersion` to `{major}.{minor}.{patch}`
- Added possibility to use `ParameterAttribute` in other injection attributes
- Added `GitVersionAttribute.Bump` parameter for bumping major/minor versions
- Added `GitVersionAttribute.DisableOnUnix` property since GitVersion is not working consistently
- Added `ChangelogTasks.FinalizeChangelog` for finalizing unpublished changes to a certain version
- Added `ChangelogTasks.ExtractChangelogSectionNotes` for extracting release data for a specified tag
- Added `NukeBuild.InvokedTargets` which exposes targets passed from command-line
- Added `NukeBuild.ExecutingTargets` which exposes targets that will be executed
- Added `NukeBuild.SkippedTargets` which exposes targets that will be skipped
- Added `GitRepository.IsGitHubRepository` extension method
- Added `GitRepositoryAttribute.Branch` and `GitRepositoryAttribute.Remote` properties for pass-through
- Added `build.cmd` in setup for easier invocation on Windows
- Added CLT tasks for Git
- Fixed background color in console output

[vNext]: https://github.com/nuke-build/nuke/compare/10.1.0...HEAD
[10.1.0]: https://github.com/nuke-build/nuke/compare/10.0.0...10.1.0
[10.0.0]: https://github.com/nuke-build/nuke/compare/9.0.4...10.0.0
[9.0.4]: https://github.com/nuke-build/nuke/compare/9.0.3...9.0.4
[9.0.3]: https://github.com/nuke-build/nuke/compare/9.0.2...9.0.3
[9.0.2]: https://github.com/nuke-build/nuke/compare/9.0.1...9.0.2
[9.0.1]: https://github.com/nuke-build/nuke/compare/9.0.0...9.0.1
[9.0.0]: https://github.com/nuke-build/nuke/compare/8.1.4...9.0.0
[8.1.4]: https://github.com/nuke-build/nuke/compare/8.1.3...8.1.4
[8.1.3]: https://github.com/nuke-build/nuke/compare/8.1.2...8.1.3
[8.1.2]: https://github.com/nuke-build/nuke/compare/8.1.1...8.1.2
[8.1.1]: https://github.com/nuke-build/nuke/compare/8.1.0...8.1.1
[8.1.0]: https://github.com/nuke-build/nuke/compare/8.0.0...8.1.0
[8.0.0]: https://github.com/nuke-build/nuke/compare/7.0.6...8.0.0
[7.0.6]: https://github.com/nuke-build/nuke/compare/7.0.5...7.0.6
[7.0.5]: https://github.com/nuke-build/nuke/compare/7.0.4...7.0.5
[7.0.4]: https://github.com/nuke-build/nuke/compare/7.0.3...7.0.4
[7.0.3]: https://github.com/nuke-build/nuke/compare/7.0.2...7.0.3
[7.0.2]: https://github.com/nuke-build/nuke/compare/7.0.1...7.0.2
[7.0.1]: https://github.com/nuke-build/nuke/compare/7.0.0...7.0.1
[7.0.0]: https://github.com/nuke-build/nuke/compare/6.3.0...7.0.0
[6.3.0]: https://github.com/nuke-build/nuke/compare/6.2.1...6.3.0
[6.2.1]: https://github.com/nuke-build/nuke/compare/6.2.0...6.2.1
[6.2.0]: https://github.com/nuke-build/nuke/compare/6.1.2...6.2.0
[6.1.2]: https://github.com/nuke-build/nuke/compare/6.1.1...6.1.2
[6.1.1]: https://github.com/nuke-build/nuke/compare/6.1.0...6.1.1
[6.1.0]: https://github.com/nuke-build/nuke/compare/6.0.3...6.1.0
[6.0.3]: https://github.com/nuke-build/nuke/compare/6.0.2...6.0.3
[6.0.2]: https://github.com/nuke-build/nuke/compare/6.0.1...6.0.2
[6.0.1]: https://github.com/nuke-build/nuke/compare/6.0.0...6.0.1
[6.0.0]: https://github.com/nuke-build/nuke/compare/5.3.0...6.0.0
[5.3.0]: https://github.com/nuke-build/nuke/compare/5.2.1...5.3.0
[5.2.1]: https://github.com/nuke-build/nuke/compare/5.2.0...5.2.1
[5.2.0]: https://github.com/nuke-build/nuke/compare/5.1.4...5.2.0
[5.1.4]: https://github.com/nuke-build/nuke/compare/5.1.3...5.1.4
[5.1.3]: https://github.com/nuke-build/nuke/compare/5.1.2...5.1.3
[5.1.2]: https://github.com/nuke-build/nuke/compare/5.1.1...5.1.2
[5.1.1]: https://github.com/nuke-build/nuke/compare/5.1.0...5.1.1
[5.1.0]: https://github.com/nuke-build/nuke/compare/5.0.2...5.1.0
[5.0.2]: https://github.com/nuke-build/nuke/compare/5.0.1...5.0.2
[5.0.1]: https://github.com/nuke-build/nuke/compare/5.0.0...5.0.1
[5.0.0]: https://github.com/nuke-build/nuke/compare/0.25.0...5.0.0
[0.25.0]: https://github.com/nuke-build/nuke/compare/0.24.11...0.25.0
[0.24.11]: https://github.com/nuke-build/nuke/compare/0.24.10...0.24.11
[0.24.10]: https://github.com/nuke-build/nuke/compare/0.24.9...0.24.10
[0.24.9]: https://github.com/nuke-build/nuke/compare/0.24.8...0.24.9
[0.24.8]: https://github.com/nuke-build/nuke/compare/0.24.7...0.24.8
[0.24.7]: https://github.com/nuke-build/nuke/compare/0.24.6...0.24.7
[0.24.6]: https://github.com/nuke-build/nuke/compare/0.24.5...0.24.6
[0.24.5]: https://github.com/nuke-build/nuke/compare/0.24.4...0.24.5
[0.24.4]: https://github.com/nuke-build/nuke/compare/0.24.2...0.24.4
[0.24.2]: https://github.com/nuke-build/nuke/compare/0.24.1...0.24.2
[0.24.1]: https://github.com/nuke-build/nuke/compare/0.24.0...0.24.1
[0.24.0]: https://github.com/nuke-build/nuke/compare/0.23.7...0.24.0
[0.23.7]: https://github.com/nuke-build/nuke/compare/0.23.6...0.23.7
[0.23.6]: https://github.com/nuke-build/nuke/compare/0.23.5...0.23.6
[0.23.5]: https://github.com/nuke-build/nuke/compare/0.23.4...0.23.5
[0.23.4]: https://github.com/nuke-build/nuke/compare/0.23.3...0.23.4
[0.23.3]: https://github.com/nuke-build/nuke/compare/0.23.2...0.23.3
[0.23.2]: https://github.com/nuke-build/nuke/compare/0.23.1...0.23.2
[0.23.1]: https://github.com/nuke-build/nuke/compare/0.23.0...0.23.1
[0.23.0]: https://github.com/nuke-build/nuke/compare/0.22.2...0.23.0
[0.22.2]: https://github.com/nuke-build/nuke/compare/0.22.1...0.22.2
[0.22.1]: https://github.com/nuke-build/nuke/compare/0.22.0...0.22.1
[0.22.0]: https://github.com/nuke-build/nuke/compare/0.21.2...0.22.0
[0.21.2]: https://github.com/nuke-build/nuke/compare/0.21.1...0.21.2
[0.21.1]: https://github.com/nuke-build/nuke/compare/0.21.0...0.21.1
[0.21.0]: https://github.com/nuke-build/nuke/compare/0.20.1...0.21.0
[0.20.1]: https://github.com/nuke-build/nuke/compare/0.20.0...0.20.1
[0.20.0]: https://github.com/nuke-build/nuke/compare/0.19.2...0.20.0
[0.19.2]: https://github.com/nuke-build/nuke/compare/0.19.1...0.19.2
[0.19.1]: https://github.com/nuke-build/nuke/compare/0.19.0...0.19.1
[0.19.0]: https://github.com/nuke-build/nuke/compare/0.18.0...0.19.0
[0.18.0]: https://github.com/nuke-build/nuke/compare/0.17.7...0.18.0
[0.17.7]: https://github.com/nuke-build/nuke/compare/0.17.6...0.17.7
[0.17.6]: https://github.com/nuke-build/nuke/compare/0.17.5...0.17.6
[0.17.5]: https://github.com/nuke-build/nuke/compare/0.17.4...0.17.5
[0.17.4]: https://github.com/nuke-build/nuke/compare/0.17.3...0.17.4
[0.17.3]: https://github.com/nuke-build/nuke/compare/0.17.2...0.17.3
[0.17.2]: https://github.com/nuke-build/nuke/compare/0.17.1...0.17.2
[0.17.1]: https://github.com/nuke-build/nuke/compare/0.17.0...0.17.1
[0.17.0]: https://github.com/nuke-build/nuke/compare/0.16.0...0.17.0
[0.16.0]: https://github.com/nuke-build/nuke/compare/0.15.0...0.16.0
[0.15.0]: https://github.com/nuke-build/nuke/compare/0.14.1...0.15.0
[0.14.1]: https://github.com/nuke-build/nuke/compare/0.14.0...0.14.1
[0.14.0]: https://github.com/nuke-build/nuke/compare/0.13.0...0.14.0
[0.13.0]: https://github.com/nuke-build/nuke/compare/0.12.4...0.13.0
[0.12.4]: https://github.com/nuke-build/nuke/compare/0.12.3...0.12.4
[0.12.3]: https://github.com/nuke-build/nuke/compare/0.12.2...0.12.3
[0.12.2]: https://github.com/nuke-build/nuke/compare/0.12.1...0.12.2
[0.12.1]: https://github.com/nuke-build/nuke/compare/0.12.0...0.12.1
[0.12.0]: https://github.com/nuke-build/nuke/compare/0.11.1...0.12.0
[0.11.1]: https://github.com/nuke-build/nuke/compare/0.11.0...0.11.1
[0.11.0]: https://github.com/nuke-build/nuke/compare/0.10.5...0.11.0
[0.10.5]: https://github.com/nuke-build/nuke/compare/0.10.4...0.10.5
[0.10.4]: https://github.com/nuke-build/nuke/compare/0.10.3...0.10.4
[0.10.3]: https://github.com/nuke-build/nuke/compare/0.10.2...0.10.3
[0.10.2]: https://github.com/nuke-build/nuke/compare/0.10.1...0.10.2
[0.10.1]: https://github.com/nuke-build/nuke/compare/0.10.0...0.10.1
[0.10.0]: https://github.com/nuke-build/nuke/compare/0.9.1...0.10.0
[0.9.1]: https://github.com/nuke-build/nuke/compare/0.9.0...0.9.1
[0.9.0]: https://github.com/nuke-build/nuke/compare/0.8.0...0.9.0
[0.8.0]: https://github.com/nuke-build/nuke/compare/0.7.0...0.8.0
[0.7.0]: https://github.com/nuke-build/nuke/compare/0.6.2...0.7.0
[0.6.2]: https://github.com/nuke-build/nuke/compare/0.6.1...0.6.2
[0.6.1]: https://github.com/nuke-build/nuke/compare/0.6.0...0.6.1
[0.6.0]: https://github.com/nuke-build/nuke/compare/0.5.3...0.6.0
[0.5.3]: https://github.com/nuke-build/nuke/compare/0.5.2...0.5.3
[0.5.2]: https://github.com/nuke-build/nuke/compare/0.5.0...0.5.2
[0.5.0]: https://github.com/nuke-build/nuke/compare/0.4.0...0.5.0
[0.4.0]: https://github.com/nuke-build/nuke/compare/0.3.1...0.4.0
[0.3.1]: https://github.com/nuke-build/nuke/compare/0.2.10...0.3.1
[0.2.10]: https://github.com/nuke-build/nuke/compare/0.2.0...0.2.10
[0.2.0]: https://github.com/nuke-build/nuke/tree/0.2.0
