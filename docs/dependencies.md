# Third-party dependencies

A flat overview of the non-trivial libraries Fallout pulls in, what each is for, and where it's used. Tiny utility packages (single-purpose helpers, transitive-only deps) are omitted. Anything load-bearing or notable is here.

**Keep this current.** When adding or removing a meaningful library, update the table. Reviewers should call this out if a PR introduces a new dep without a row here.

Central package versions are pinned in `Directory.Packages.props`; this page links each entry back to it.

---

## Microsoft / .NET BCL

| Package | Purpose | Used by |
|---|---|---|
| `Microsoft.Build` (+ `.Framework`, `.Tasks.Core`, `.Utilities.Core`) | MSBuild engine — read/evaluate `.csproj`/`.props` files | `Fallout.ProjectModel`, `Fallout.MSBuildTasks` |
| `Microsoft.Build.Locator` | Locate an installed MSBuild at runtime | `Fallout.ProjectModel` |
| `Microsoft.CodeAnalysis.*` (CSharp, Workspaces, MSBuild, Analyzers) | Roslyn — C# parsing/compilation/analysis | `Fallout.SourceGenerators`, `Fallout.Cli` (Cake rewriter) |
| `Microsoft.Extensions.DependencyModel` | Parse `.deps.json` runtime metadata | `Fallout.Build` |
| `Microsoft.SourceLink.GitHub` | Source-link symbols into published nupkgs so debuggers can step into Fallout | All packable libs |
| `Nerdbank.GitVersioning` | Build-time semver derived from git history | All packable libs |
| `System.Text.Json` | Modern JSON parser/serializer | `Fallout.Utilities.Text.Json` |
| `System.Net.Http`, `System.Security.Cryptography.Xml`, `System.ComponentModel.Annotations` | BCL fillers needed for `netstandard2.0` multi-targeting | various |

## Logging

| Package | Purpose |
|---|---|
| `Serilog` + `Sinks.Console` + `Sinks.File` | The logging framework. All `Log.Information/Warning/Error` calls route through this. |
| `Serilog.Formatting.Compact` (+ `.Reader`) | Structured JSON log format for machine-readable logs |

## Azure

| Package | Purpose | Notes |
|---|---|---|
| `Azure.Identity`, `Azure.Security.KeyVault.{Certificates,Keys,Secrets}` | Power the `[AzureKeyVaultSecret]` value-injection attribute | Pure consumer-tool surface — currently in `Fallout.Common` but will split out under [#73](https://github.com/Fallout-build/Fallout/issues/73). Consumers who don't use Azure today still pay for these. |

## GitHub

| Package | Purpose | Used by |
|---|---|---|
| `Octokit` | GitHub REST API client | `ICreateGitHubRelease`, `Build.Contributors`, `Build.Stargazers` |

## JSON / templating

| Package | Purpose | Notes |
|---|---|---|
| `NJsonSchema` + `.NewtonsoftJson` | Generate `build.schema.json` so IDEs can auto-complete `--params` | Drags `Newtonsoft.Json` transitively |
| `Newtonsoft.Json` | Legacy JSON — direct usage in older codepaths | Long-term consolidation to `System.Text.Json` tracked in [#83](https://github.com/Fallout-build/Fallout/issues/83) |
| `Scriban` | Templating engine | Used by `Fallout.Cli` Cake rewriter only. **Open CVE NU1903 tracked in [#84](https://github.com/Fallout-build/Fallout/issues/84).** |

## Text, IO, archives

| Package | Purpose | Used by |
|---|---|---|
| `YamlDotNet` | YAML parse + serialize | CI config generation, `Fallout.Utilities.Text.Yaml` |
| `Glob` | Glob pattern matching | `Fallout.Utilities.IO.Globbing` |
| `SharpZipLib` | Zip and tar archives | `Fallout.Utilities.IO.Compression` |
| `HtmlAgilityPack` | HTML parsing + XPath | Only used by `ReferenceUpdater` for the upstream CLI doc snapshots in `docs/cli-tools/`. Goes away if we retire that target. |
| `Humanizer` | Pluralize / camelize / titleize string helpers | A handful of call sites — could be inlined if we wanted one fewer dep. |

## NuGet

| Package | Purpose |
|---|---|
| `NuGet.Packaging` | Read .nupkg metadata. Used by `NuGetVersionResolver`, `ProjectUpdater`. |

## Telemetry — currently inactive

| Package | Purpose | Status |
|---|---|---|
| `Microsoft.ApplicationInsights` | Telemetry client | **Dead weight today.** `Telemetry.cs` short-circuits because `InstrumentationKey = ""` (the original NUKE key was matkoch-owned, we don't reuse). Removal tracked in [#79](https://github.com/Fallout-build/Fallout/issues/79). Re-introduce when we stand up a Fallout-owned endpoint. |

## Vendored source

| Package | Source | Why vendored |
|---|---|---|
| `Fallout.VisualStudio.SolutionPersistence` (assembly name remains `Microsoft.VisualStudio.SolutionPersistence` for drop-in type identity) — published to nuget.org alongside the rest of `Fallout.*`. | Submodule at `vendor/vs-solutionpersistence/` tracking [`ChrisonSimtian/vs-solutionpersistence`](https://github.com/ChrisonSimtian/vs-solutionpersistence) — our fork of [`matkoch/vs-solutionpersistence`](https://github.com/matkoch/vs-solutionpersistence), which itself forked from [`microsoft/vs-solutionpersistence`](https://github.com/microsoft/vs-solutionpersistence). MIT-licensed; full attribution chain preserved. | Upstream Microsoft package ships only `net472` + `net8.0`, no `netstandard2.0`. Our source generator must target `netstandard2.0` (Roslyn requirement). Matt added netstandard2.0 patches that we now own forward. Compiled into the wrapper project `src/Fallout.VisualStudio.SolutionPersistence/` so we control the build infra without touching the submodule. Packs as `Fallout.VisualStudio.SolutionPersistence` so `Fallout.SolutionModel` consumers get a valid transitive dep on nuget.org. |

## ⚠️ Matt-era personal forks — to replace

Still on Matt's personal NuGet account; supply-chain SPOF, high-priority to replace.

| Package | Upstream equivalent | Tracked in |
|---|---|---|
| `matkoch.spectre.console` | `Spectre.Console` | [#78](https://github.com/Fallout-build/Fallout/issues/78) — confirmed clean swap, just needs the PR |

## Testing

| Package | Purpose |
|---|---|
| `xunit` (+ `runner.visualstudio`) | Test framework |
| `Microsoft.NET.Test.Sdk` | Test host wiring |
| `FluentAssertions` | Readable assertion DSL |
| `Verify.Xunit` (+ `.DiffPlex`, `.SourceGenerators`) | Snapshot-based testing (the `*.verified.txt` / `*.received.txt` pattern) |
| `coverlet.msbuild` | Code coverage |
| `GitHubActionsTestLogger` | Format test output for GitHub Actions annotations |
| `Basic.Reference.Assemblies.NetStandard20` | Reference assemblies for source-generator compile tests |
| `NetArchTest.Rules` | Architecture-fitness tests (e.g. `Fallout.Core` purity); broader suite tracked in #95 |

## Build-time CLI tools (`PackageDownload`)

These are downloaded by `build/_build.csproj` for use during this repo's own build — not shipped to consumers.

| Tool | Purpose | Status |
|---|---|---|
| `ReportGenerator` | HTML coverage reports | Active. |
| `JetBrains.ReSharper.GlobalTools` | InspectCode (static analysis) | **Decision pending** ([#75](https://github.com/Fallout-build/Fallout/issues/75)) — keep, drop, or just drop from this repo's build. |
| `Codecov.Tool` | Upload coverage to codecov.io | **Likely dead** — `IReportCoverage.ReportToCodecov` is `false`. Removal tracked in [#80](https://github.com/Fallout-build/Fallout/issues/80). |
| `GitVersion.Tool` | Version computation (legacy) | **Transitional** — fully replace with `Nerdbank.GitVersioning` per [#81](https://github.com/Fallout-build/Fallout/issues/81). |
| `xunit.runner.console` | Standalone xunit runner | **Likely redundant** with `Microsoft.NET.Test.Sdk`. Removal tracked in [#82](https://github.com/Fallout-build/Fallout/issues/82). |

---

## How to keep this current

- Adding a dependency? Add a row here in the same PR. Reviewers will ask.
- Removing a dependency? Remove the row. No half-states.
- Bumping a major version of something load-bearing (Roslyn, MSBuild, Newtonsoft, Spectre, …)? Mention it in the PR body but leave the table alone unless the purpose changes.
- Transitive-only packages don't need rows — only what we declare in `Directory.Packages.props` or directly in a `.csproj`.

If you're auditing the live dependency graph, run:

```pwsh
dotnet list fallout.slnx package --include-transitive
```

The text above is curated — that command is authoritative.
