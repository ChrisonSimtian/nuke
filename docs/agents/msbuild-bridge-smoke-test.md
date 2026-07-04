# MSBuild bridge smoke test (PR #447 / ADR-0009)

Runbook for verifying the **full-framework MSBuild bridge** added by [ADR-0009](../adr/0009-lift-ns20-floor-via-msbuild-bridge.md)
(branch `tooling/lift-ns20-floor`). CI does **not** cover this path — the repo dogfood runs
`FalloutTasksEnabled=False`, and the bridge only runs under full-framework `MSBuild.exe`, which is
**Windows-only**. So it needs a manual, Windows + VS smoke test.

## What exercises which path

The `.targets` select by `$(MSBuildRuntimeType)`:

| Driver | RuntimeType | Path | Covered by |
|---|---|---|---|
| `dotnet` / SDK MSBuild | `Core` | in-proc net10 task (`build/netcore`) | `dotnet pack`, CI |
| VS 2019 / 2022 `MSBuild.exe` | `Full` | **net472 bridge → net10 worker** (`build/netfx`) | **this runbook only** |
| VS 2026 (v18) `MSBuild.exe` | `Core` | in-proc net10 task | same as `dotnet` |

**VS 2026 does not exercise the bridge** — its MSBuild is .NET-based, so it takes the in-proc path.
The bridge is reachable *only* via full-framework MSBuild (VS 2019/2022 or Build Tools). Per the ADR
the bridge is **time-boxed**: delete it once consumers are off full-framework MSBuild.

## VS version currency (as of 2026-07)

| Version | Status | Bridge? |
|---|---|---|
| VS 2026 (v18) | current | no (Core) |
| VS 2022 (v17) | prior gen, widely used | **yes** |
| VS 2019 (v16) | legacy, near EOL; its MSBuild can't resolve the net10 SDK | **yes** |
| VS for Mac | **retired 2024-08-31**; never had full-framework MSBuild | n/a |

## Homelab VM spec

A parked Windows image, spun up only when touching the bridge / MSBuild-task code:

- Windows 10/11 or Windows Server.
- **VS Build Tools 2022** with the `Microsoft.Component.MSBuild` workload — the BuildTools SKU is
  enough; full VS is not needed. (Optionally Build Tools 2019 too, for the oldest supported path.)
- The pinned **.NET SDK** (`global.json`, currently `10.0.100`) — provides `dotnet` for the worker.
- `git`.

Does **not** need VS 2026 — that only re-tests the `Core` path the dev box's `dotnet` already covers.

## How to run

From a worktree on the PR branch (`MSBuild.exe` runs must use PowerShell, not Git Bash — Bash
mangles `/switch` args and `/c/...` paths):

```powershell
# 1. Publish the three task projects (Release)
dotnet publish src/Fallout.MSBuildTasks/Fallout.MSBuildTasks.csproj -c Release
dotnet publish src/Fallout.MSBuildTasks.Bridge/Fallout.MSBuildTasks.Bridge.csproj -c Release
dotnet publish src/Fallout.MSBuildTasks.Worker/Fallout.MSBuildTasks.Worker.csproj -c Release
dotnet build  src/Fallout.SourceGenerators/Fallout.SourceGenerators.csproj -c Release

# 2. Pack Fallout.Common (gate on _IsPacking) and extract its build/ assets
dotnet pack src/Fallout.Common/Fallout.Common.csproj -c Release -p:_IsPacking=true -o <feed>
#   unzip <feed>\Fallout.Common.*.nupkg -> <pkg>, giving <pkg>\build\{netcore,netfx}

# 3. Consumer that imports <pkg>\build\Fallout.Common.{props,targets}, sets a tool spec in
#    @(FalloutSpecificationFiles), and runs the FalloutCodeGeneration target. A non-SDK .proj
#    lets BOTH VS2019 and VS2022 evaluate it (VS2019's MSBuild can't resolve the net10 SDK).

$env:DOTNET_HOST_PATH = "C:\Program Files\dotnet\dotnet.exe"   # so the bridge can launch the worker
& "<VS>\MSBuild\Current\Bin\MSBuild.exe" Consumer.proj -t:FalloutCodeGeneration -p:PkgBuild=<pkg>\build
```

**Pass criteria:** the `Full`-path runs produce the *same* generated output as `dotnet` (`Core`),
with the worker process actually spawned and no load/protocol errors.

## Result — 2026-07-01 (commit 7220ab5a)

| Driver | Outcome |
|---|---|
| `dotnet` (Core) | ✅ generated valid `Git.Generated.cs` (in-proc net10 task) |
| VS 2022 `MSBuild.exe` (Full) | ❌ bridge crashes before spawning the worker |
| VS 2019 `MSBuild.exe` (Full) | ❌ identical crash |

**Defect — the net472 bridge can't load `System.Text.Json`.**

```
error MSB6003: System.IO.FileNotFoundException: Could not load file or assembly
'System.Text.Json, Version=10.0.0.0, ... PublicKeyToken=cc7b13ffcd2ddd51'
  at Fallout.MSBuildTasks.Protocol.WorkerJson.Serialize[T]
  at CodeGenerationTask.BuildRequestJson()  ->  WorkerBridgeTask.GenerateCommandLineCommands()
```

Root cause:
- `Fallout.MSBuildTasks.Protocol` (`WorkerProtocol.cs`) serializes with **`System.Text.Json`**.
- The packaged STJ is assembly version **`10.0.0.8`**; the code references **`10.0.0.0`**.
- The bridge runs **in-process inside .NET-Framework `MSBuild.exe`**, where fusion needs an exact
  version or a binding redirect. There is none (and a `Bridge.dll.config` redirect would not help —
  fusion only reads the host `MSBuild.exe.config` for in-proc assemblies). On .NET Core both the
  in-proc task and the worker bind fine via roll-forward, which is why only `Full` fails.
- This contradicts ADR-0009 §3 ("the bridge references **only** `Microsoft.Build.*` + the BCL —
  zero Fallout dependencies"). The Protocol's STJ dependency reintroduces exactly the version-
  sensitive in-proc dependency the design set out to avoid.

**Fix direction:** make `WorkerJson` framework-safe so the net472 bridge carries no redirect-
sensitive dependency — e.g. serialize the (trivial) DTOs with the in-box `DataContractJsonSerializer`,
or hand-roll, keeping STJ (if at all) on the net10 worker side only. The blocker is the bridge's
*outbound* serialization (`BuildRequestJson`); the worker (net10) is fine.

### Fixed — 2026-07-01

`WorkerJson` (`Fallout.MSBuildTasks.Protocol`) now uses the in-box `DataContractJsonSerializer`
(`System.Runtime.Serialization`) instead of `System.Text.Json`, and the STJ `PackageReference` is
removed from the Protocol project. STJ no longer ships in `build/netfx`, so the net472 bridge loads
with no redirect-sensitive dependency. Re-run:

| Driver | Outcome |
|---|---|
| `dotnet` (Core) | ✅ `Git.Generated.cs` |
| VS 2022 `MSBuild.exe` (Full) | ✅ bridge → worker, output **byte-identical** to Core |
| VS 2019 `MSBuild.exe` (Full) | ✅ identical |

All three generated files share one SHA256 — behaviour parity (ADR-0009 §"Consequences") holds.

> No automated tests cover the bridge/worker serialization — this smoke test is the only coverage.
> A `WorkerJson` round-trip unit test in a `Fallout.MSBuildTasks.Protocol.Specs` sibling would catch
> a regression without the manual VS run.
