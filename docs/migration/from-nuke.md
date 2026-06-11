# Migrating from NUKE to Fallout

Fallout is the hard-fork successor to [NUKE](https://github.com/nuke-build/nuke). If you maintain a NUKE-based build, this guide gets you running on Fallout in under five minutes. Why the rename and the relationship to upstream NUKE are explained in [the rebrand plan](https://github.com/Fallout-build/Fallout/blob/main/docs/rebrand-plan.md).

## TL;DR

```sh
dotnet tool install -g Fallout.Migrate
cd path/to/your-nuke-repo
fallout-migrate
./build.ps1   # or ./build.sh
```

Done. Read on if anything looks unusual, or you want to know what the tool did.

## The recommended path: `fallout-migrate`

`fallout-migrate` is a global `dotnet` tool that performs the entire migration in one command. It rewrites `PackageReference`s, `using` directives, MSBuild properties, bootstrap scripts, env vars, and renames `.nuke/` → `.fallout/`. It's idempotent and has a `--dry-run` mode.

### Step 1 — install the tool

```sh
dotnet tool install -g Fallout.Migrate
```

### Step 2 — dry-run first (optional but recommended)

From your NUKE repo's root:

```sh
fallout-migrate --dry-run
```

You'll see the list of files that would change and the counts. Nothing on disk is modified. Skim the output to make sure the tool found the right repo root.

### Step 3 — run for real

```sh
fallout-migrate
```

The tool prints a summary at the end:

```
Files changed:   5
Edits made:      25
Directories:     1 renamed

Migration complete. Verify the build:  ./build.ps1
```

### Step 4 — verify

```sh
./build.ps1   # or ./build.sh on unix
```

If your build had previously worked against NUKE, it should now work against Fallout. The first run will restore the `Fallout.*` packages from nuget.org. If you don't see `Fallout.Common`, `Fallout.Build`, etc. in your `_build.csproj`'s `PackageReference` list, the rewrite failed — file an issue (see below) and use [the shim path](#alternative-keep-using-nuke-via-the-transition-shim) in the meantime.

## What gets changed

| Where | From | To |
|---|---|---|
| `PackageReference` in `*.csproj` | `Nuke.Common`, `Nuke.Build`, ... | `Fallout.Common`, `Fallout.Build`, ... |
| `using` directives in `*.cs` | `using Nuke.X.Y;` | `using Fallout.X.Y;` |
| Fully-qualified type refs | `Nuke.Common.AbsolutePath` | `Fallout.Common.AbsolutePath` |
| Base class name | `: NukeBuild` | `: FalloutBuild` |
| Base interface name | `: INukeBuild` | `: IFalloutBuild` |
| MSBuild properties in `_build.csproj` | `<NukeRootDirectory>`, `<NukeTelemetryVersion>` | `<FalloutRootDirectory>`, `<FalloutTelemetryVersion>` |
| Bootstrap scripts | `dotnet nuke`, `NUKE_TELEMETRY_OPTOUT`, `.nuke/temp` | `dotnet fallout`, `FALLOUT_TELEMETRY_OPTOUT`, `.fallout/temp` |
| Config directory | `.nuke/` | `.fallout/` (contents preserved) |

The 1:1 namespace prefix swap is the only structural change. Type names (other than `NukeBuild` / `INukeBuild`) keep their identifiers — `[Parameter]`, `[Solution]`, `[GitHubActions]`, `Solution`, `GitRepository`, etc. all stay the same.

## Alternative: keep using `Nuke.*` via the transition shim

If you can't run `fallout-migrate` right now (frozen CI, vendored build with local patches, compliance review pending), you can keep `using Nuke.Common;` working by referencing the **transition shim**. The shim re-exports the legacy `Nuke.*` types as subclasses of the canonical `Fallout.*` types.

The shim ships on **GitHub Packages** (not nuget.org — that namespace belongs to NUKE's original maintainer). Add this fork's feed to your `nuget.config`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="fallout-shims" value="https://nuget.pkg.github.com/Fallout-build/index.json" />
  </packageSources>
  <packageSourceCredentials>
    <fallout-shims>
      <add key="Username" value="YOUR_GITHUB_USERNAME" />
      <add key="ClearTextPassword" value="YOUR_GITHUB_PAT_WITH_READ_PACKAGES" />
    </fallout-shims>
  </packageSourceCredentials>
</configuration>
```

You'll need a GitHub Personal Access Token with the `read:packages` scope. Then bump your `Nuke.Common` package version to the latest published on the shim feed and your build keeps compiling unchanged.

**Caveats**:
- The shim covers most public types but skips a few (sealed-class wrappers and static helper classes like `DotNetTasks` are not bridged yet — tracked in [#69](https://github.com/Fallout-build/Fallout/issues/69)). If your build uses any of those, it won't compile against the shim and you'll need `fallout-migrate` after all.
- The shim is a transition aid, not a permanent home. It's removed in 12.0. Plan to migrate before then.

## Manual migration

If you'd rather drive the rewrite by hand (small projects, or you want to learn what the tool does):

1. Edit `build/_build.csproj`. For every `<PackageReference Include="Nuke.X" ...>`, change `Nuke.X` → `Fallout.X`. Same for `<NukeRootDirectory>` / `<NukeTelemetryVersion>` MSBuild properties.
2. In every `.cs` file under `build/`, change `using Nuke.X.Y;` to `using Fallout.X.Y;`. Replace `: NukeBuild` with `: FalloutBuild` and `INukeBuild` with `IFalloutBuild`.
3. In `build.ps1`, `build.sh`, `build.cmd`: `dotnet nuke` → `dotnet fallout`, `NUKE_TELEMETRY_OPTOUT` → `FALLOUT_TELEMETRY_OPTOUT`, `.nuke/temp` → `.fallout/temp`.
4. Rename the `.nuke/` directory to `.fallout/`. Contents are preserved.
5. Run `./build.ps1` to verify.

The [`Fallout.Migrate.Analyzers`](https://www.nuget.org/packages/Fallout.Migrate.Analyzers) NuGet package makes step 2 easier: install it temporarily in `_build.csproj` and every `Nuke.*` reference shows up as a `FALLOUT004` warning with a Roslyn codefix attached. Uninstall once the warnings are gone.

## Common gotchas

- **CI YAML files** are not rewritten by `fallout-migrate`. If your CI uses `dotnet nuke` directly in `.github/workflows/*.yml`, GitLab YAML, etc., change those to `dotnet fallout` by hand.
- **`[GitHubActions]` attribute regeneration**: the framework regenerates your CI YAML from the attribute on next build. The regenerated file will use the new tool name — review the diff and commit it.
- **`.nuke/parameters.json`** moves to `.fallout/parameters.json`. Schemas are unchanged.
- **MSBuild props files** (`Directory.Build.props` / `Directory.Build.targets`) — if you wrote custom NUKE-related properties (`<Nuke*>`), `fallout-migrate` rewrites them too, but only inside `*.csproj`. Custom props files at repo root are not in scope; rewrite by hand.
- **Vendored / forked NUKE plugins** with their own `Nuke.*` namespaces aren't rewritten. The tool only touches your build orchestrator. If you depend on a third-party plugin still on `Nuke.*`, [the shim](#alternative-keep-using-nuke-via-the-transition-shim) covers the surface used by your own code, but the plugin itself stays on the upstream NUKE packages.

## If something breaks

File an issue at [github.com/Fallout-build/Fallout/issues](https://github.com/Fallout-build/Fallout/issues). Include:

- Your build's `_build.csproj` (or relevant excerpt)
- The output of `fallout-migrate --dry-run`
- The error you hit (compiler error, runtime exception, etc.)
- Whether the build worked on the equivalent NUKE version before migration

If `fallout-migrate` itself crashed, also attach the stack trace.

## Staying on NUKE

If you'd prefer not to migrate, that's fine — [`nuke-build/nuke`](https://github.com/nuke-build/nuke) continues under its original maintainer's direction. Fallout doesn't displace it; we hard-forked because we wanted to take the codebase in a different direction (enterprise CI/CD focus, plugin architecture — see [the roadmap](https://github.com/Fallout-build/Fallout/blob/main/docs/roadmap.md)). Bug fixes and improvements may flow between the two projects, but the codebases will diverge over time. Pick the one whose direction matches your needs.
