# ADR-0009: Lift the `netstandard2.0` floor off the tooling stack via an out-of-process MSBuild bridge

## Status

Proposed (2026-06-30). Implements the **Lever 2** half of [#441](https://github.com/Fallout-build/Fallout/issues/441) ("move off the ns2.0/net472 floor"); the **Lever 1** half (freeing the `Fallout.SourceGenerators` Roslyn cone) is tracked separately in [#442](https://github.com/Fallout-build/Fallout/pull/442) and is an independent cone — the two do not interact. Unblocks the deferred onion lifts (`AbsolutePath`, `Host`, and lifting `IFalloutBuild` into `Fallout.Core`).

## Context

Two project graphs are pinned to `netstandard2.0`, for unrelated reasons:

- **Cone A — `Fallout.SourceGenerators`** is a Roslyn source generator and is ns2.0 by compiler requirement. Its references (`Build.Shared`, `Solution`, `Persistence.Solution`) do **not** touch the tooling stack. Addressed by Lever 1 (#442); out of scope here.
- **Cone B — `Fallout.MSBuildTasks`** targets `net10.0;net472`. Its `net472` flavour exists because the MSBuild tasks it ships are loaded **into the consumer's MSBuild host**, and the in-IDE build of VS 2019/2022 (and `msbuild.exe`) is **full-framework** MSBuild, which can only load a `net472` task assembly in-process. That `net472` reference is the **sole** force dragging `Fallout.Tooling` and `Fallout.Tooling.Generator` down to a `net10.0;netstandard2.0` multi-target:

  ```
  Fallout.MSBuildTasks (net10.0;net472)  ──►  Fallout.Tooling          (net10.0;netstandard2.0)
                                         └─►  Fallout.Tooling.Generator (netstandard2.0)
  ```

Because `Fallout.Tooling` is ns2.0 and `Fallout.Core` is ns2.1, anything `Tooling` must consume cannot live in Core. That is the direct cause of:

- the tool-execution contract living in the dedicated ns2.0 leaves `Fallout.Application.Tooling.Execution` / `.Requirements` instead of Core;
- `Configure<T>` being un-liftable to Core (Phase 2a deferral);
- `AbsolutePath` (2b) and `Host` (2c) being un-liftable, which in turn blocks lifting `IFalloutBuild` into Core (2e).

The three concrete tasks (`CodeGenerationTask`, `EmbedPackagesForSelfContainedTask`, `PackPackageToolsTask`, all extending `ContextAwareTask : Microsoft.Build.Utilities.Task`) are build-time **file producers** — tool-wrapper codegen, package embedding, tool packing. None needs a chatty in-process MSBuild API; each is expressible as *inputs → work → outputs + diagnostics*. That makes them safe to run out-of-process.

`ContextAwareTask` carries a `CustomAssemblyLoader : AssemblyLoadContext` whose only job is to isolate Fallout's dependency closure from MSBuild's own assemblies inside the host — a known-fragile piece that exists purely because the work runs in-process.

## Decision

Extract the task **logic** into a `net10` engine and front it with two thin adapters, selected by MSBuild runtime:

```
Fallout.MSBuildTasks.Engine   (net10)   ← the real work; references net10 Tooling + Generator
        ├─ Fallout.MSBuildTasks        (net10)   in-proc Task → calls the engine directly
        └─ Fallout.MSBuildTasks.Bridge (net472)  ToolTask → shells out to a standalone net10 worker
```

1. **`Fallout.Tooling` and `Fallout.Tooling.Generator` become `net10`-only.** With the `net472` consumer gone, the floor is lifted. `Fallout.Core` (or a single set of net-modern contracts) can then absorb the tool contracts, `Configure<T>`, and `AbsolutePath` — unblocking 2b/2c/2e.
2. **Core MSBuild hosts (`dotnet` / SDK build) load the in-proc `net10` task** — the hot path, no process spawn. It keeps the `ContextAwareTask`/`AssemblyLoadContext` isolation (loading net10 Tooling + Serilog/STJ into SDK-MSBuild can still clash).
3. **Full-framework MSBuild hosts (VS 2019/2022, `msbuild.exe`) load the `net472` bridge** — a `Microsoft.Build.Utilities.ToolTask` that shells out via `System.Diagnostics.Process` to a **standalone net10 worker console** (`Fallout.MSBuildTasks.Worker`). `ToolTask` is purpose-built for this: process launch, output capture, cancellation, and `LogEventsFromTextOutput` to surface worker diagnostics in the VS error list.
   - The bridge references **only** `Microsoft.Build.*` + the BCL — **zero Fallout dependencies**, no `AssemblyLoadContext` dance.
   - The worker is **standalone**, not a verb on the `fallout` CLI — this is a time-boxed workaround and must add no surface to the long-lived CLI.
4. **`.targets` selects the adapter on `$(MSBuildRuntimeType)`** (`Core` → in-proc task; `Full` → bridge). The legacy `Nuke*`→`Fallout*` property bridging and `FalloutTasksEnabled` flag are untouched.
5. **`dotnet` host discovery** uses `$(DOTNET_HOST_PATH)` (set by the SDK), falling back to PATH, passed to the bridge so it can launch `dotnet Fallout.MSBuildTasks.Worker.dll`.
6. **I/O marshalling stays boring** — `ITaskItem[]`/properties (e.g. `FalloutSpecificationFiles`) serialize to a temp JSON arg file; the worker writes its output files plus a small results JSON the bridge reads back for output item groups.

The net10 runtime being present at the consumer's build is **assumed, not mitigated** — Fallout is a net10 build system; if net10 were absent, nothing would run.

## Consequences

- **Unblocks the de-static "lift to Core" track** (2b `AbsolutePath`, 2c `Host`, 2e `IFalloutBuild`) and lets the ns2.0 tooling leaves (`Application.Tooling.Execution`/`.Requirements`) eventually fold back into Core.
- **Deletes the fragile in-host ALC isolation for VS** — the bridge has no Fallout deps to isolate.
- **Adds a per-invocation process spawn for full-framework builds only.** These are build-time, run-once-per-build operations; the cost is negligible and the hot `dotnet`/CLI path keeps its in-proc speed.
- **The bridge is a fenced, deletable legacy adapter** for VS 2019/2022. VS 2026's move to a 64-bit/.NET MSBuild is expected to load net10 tasks in-process directly, at which point `Full` hosts also get a modern runtime and the bridge is dead weight. **Retirement = delete the `.Bridge` project + the `Full` branch in `.targets`.** Keep it past then only if it is costing nothing.
- **Behaviour parity is the bar:** VS-driven consumer builds (codegen, package embed, tool pack) must produce identical output and identical error-list diagnostics through the bridge as through the in-proc task.

## Alternatives considered

- **Drop `net472` outright** (the original Lever 2 framing). Simplest, but breaks the Fallout MSBuild tasks for in-VS / `msbuild.exe` builds on VS 2019/2022. Rejected: the bridge preserves those at low, time-boxed cost.
- **Unify everyone onto the out-of-process worker** (no in-proc task). One code path, but pays the process-spawn cost on every `dotnet`/CLI build and discards the existing ALC isolation that already works. Rejected in favour of in-proc-for-Core, bridge-for-Full.
- **MSBuild task host (`Runtime="NET"`)** to run a net-core task from full-framework MSBuild. Not a reliable cross-runtime path from net472; the explicit shell-out is more robust and fully decoupled.
