# Fallout.Core

> **The reactor core.** Pure domain physics — no I/O, no process, no console, no logging, no meltdown.

`Fallout.Core` is the bottom layer of Fallout. It holds the immutable domain shape of the build execution pipeline and the pure graph algorithms that schedule it:

- **`ITargetModel`** — read-only projection of a build target (identity, status, dependency names). The stable surface that higher layers and the future plugin SDK read against.
- **`ExecutionStatus`** — the lifecycle states of a target.
- **`TopoSort` / `PlanResult<T>`** — a pure, side-effect-free topological sort with strongly-connected-component cycle detection. Takes nodes and an edge function in; returns an ordering and any cycles out. It never throws, never mutates its inputs, and never reads ambient state.
- **`Constants`** — Fallout's own fixed values: directory and file names, parameter names, environment variable keys, package ids, project URLs. Values only; anything that derives a path or reads the file system is `FalloutPaths` in `Fallout.Build.Shared`. It sits in the root `Fallout` namespace rather than a layer's, so every call site reaches it unqualified.

## Invariants

Everything in this assembly is held to a strict purity bar, enforced by an architecture-fitness test:

- No reference to any other Fallout project.
- No dependency on `System.IO`, `System.Diagnostics.Process`, `Console`, or Serilog.
- No statics holding mutable state.
- No type declared under an outer layer's namespace — `Fallout.Core.*` or the root `Fallout` only. (`Fallout.Common.Execution` is a frozen exception: `ExecutionStatus` and `ITargetModel` are public, so correcting them waits for the yearly major.)

If you need to touch the filesystem, spawn a process, write to the console, or log — that logic belongs in `Fallout.Build` or higher, not here. The orchestration that *calls* these algorithms (reading parameters, failing the build on a cycle, mutating target state) lives in `Fallout.Build`; the core only computes.
