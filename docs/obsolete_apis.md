# Obsolete APIs

Fallout deprecates public APIs with [`System.ObsoleteAttribute`](https://learn.microsoft.com/dotnet/api/system.obsoleteattribute) and gives each deprecation a stable **`DiagnosticId`** so consumers can suppress *that one* deprecation without silencing every `CS0618`. This page is the **canonical registry of allocated `FALLOUTOBS0xx` diagnostic IDs**.

This is the counterpart to the [`[Experimental]` registry](experimental-apis.md): `[Experimental]` gates *not-yet-stable* surface you opt into (error-by-default); `[Obsolete]` marks *on-the-way-out* surface that still works (warning-by-default). The two use **separate ID sequences** — `FALLOUTOBS0xx` here, `FALLOUT0xx` there — so a suppression can never cross the two.

See [the agent conventions](agents/conventions.md#obsolete-for-deprecating-public-apis) for the contributor rules.

## How it works

A deprecated API carries a message, a diagnostic ID, and a help URL:

```csharp
using System;

[Obsolete(
    "Use [GitHubActionsInputAttribute] instead. Removed in 2027.x.x.",
    DiagnosticId = "FALLOUTOBS001",
    UrlFormat = "https://github.com/Fallout-build/Fallout/blob/main/docs/obsolete_apis.md")]
public string[] OnWorkflowDispatchOptionalInputs { get; set; } = new string[0];
```

`ObsoleteAttribute` is **warning-by-default**: consumers keep compiling, but see the deprecation. Without a `DiagnosticId` the compiler reports the generic `CS0618`; setting one makes it report `FALLOUTOBS001` instead, which is what lets a consumer suppress a single deprecation:

```xml
<PropertyGroup>
  <NoWarn>$(NoWarn);FALLOUTOBS001</NoWarn>
</PropertyGroup>
```

This matters for consumers building with `TreatWarningsAsErrors`: without a per-deprecation ID their only options are to fix every usage at once or blanket-disable all of `CS0618`. `DiagnosticId` gives them a targeted, acknowledge-and-move-on escape hatch while they migrate. `UrlFormat` (optional; `{0}` is replaced with the diagnostic ID when present) points at this registry so the warning links to the migration path.

## Diagnostic-ID scheme

- IDs use the form `FALLOUTOBS0xx` and are allocated **sequentially**.
- The `FALLOUTOBS0xx` sequence is **independent** of the `FALLOUT0xx` sequence used by [`[Experimental]`](experimental-apis.md) — allocate from this registry only.
- An ID is **never reused** — once retired it stays retired, so a consumer's `NoWarn` can never silently re-bind to a different deprecation.
- Every allocation is recorded in the registry table below, in the same PR that introduces the attribute.
- **Adding `[Obsolete]` is not a breaking change** — a warning-level deprecation keeps existing code compiling. The break is *removing* the API, which is batched to the next yearly major (see [AGENTS.md rule #2](../AGENTS.md) and the [release/versioning policy](agents/release-and-versioning.md)). Record the removal target in the message and the registry so consumers can plan.
- **When the API is finally removed**, its row moves to **Removed** status and the ID is retired, not recycled.

## Registry

Status values: **Deprecated** (live, warns on use), **Removed** (API deleted — ID retired).

| ID | Surface | Deprecated | Status | Notes |
|----|---------|------------|--------|-------|
| _none yet_ | | | | |

<!--
Allocation example (do not uncomment unless a real API is deprecated):

| `FALLOUTOBS001` | `GitHubActionsAttribute.OnWorkflowDispatch{Optional,Required}Inputs`, `GitHubActionsWorkflowDispatchTrigger.{Optional,Required}Inputs` | 2026.x | Deprecated | Untyped `workflow_dispatch` input arrays. Use `[GitHubActionsInputAttribute]` / `GitHubActionsWorkflowDispatchTrigger.Inputs` instead. Removal target: 2027.0.0. |
-->
