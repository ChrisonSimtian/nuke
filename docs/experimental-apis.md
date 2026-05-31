# Experimental APIs

Fallout marks not-yet-stable public APIs with [`System.Diagnostics.CodeAnalysis.ExperimentalAttribute`](https://learn.microsoft.com/dotnet/api/system.diagnostics.codeanalysis.experimentalattribute) so consumers opt into instability deliberately. This page is the **canonical registry of allocated `FALLOUT0xx` diagnostic IDs**.

See [ADR-0004 §5](adr/0004-calendar-versioning-and-dual-pace-channels.md#5-experimental-for-opt-in-unstable-apis) for the decision and [the agent conventions](agents/conventions.md#experimental-for-opt-in-unstable-apis) for the contributor rules.

## How it works

An experimental API carries a diagnostic ID:

```csharp
using System.Diagnostics.CodeAnalysis;

[Experimental("FALLOUT001")]
public sealed class NewPluginHost
{
    // ...
}
```

`ExperimentalAttribute` is an **error-by-default** diagnostic. Code that touches the API will not compile until you explicitly suppress the exact ID, which is your conscious opt-in to an API that may change without notice:

```csharp
#pragma warning disable FALLOUT001 // opting into an experimental API
var host = new NewPluginHost();
#pragma warning restore FALLOUT001
```

or, project-wide, in your `.csproj`:

```xml
<PropertyGroup>
  <NoWarn>$(NoWarn);FALLOUT001</NoWarn>
</PropertyGroup>
```

## Diagnostic-ID scheme

- IDs use the form `FALLOUT0xx` and are allocated **sequentially**.
- An ID is **never reused** — once retired it stays retired, so a suppression can never silently re-bind to a different API.
- Every allocation is recorded in the registry table below, in the same PR that introduces the attribute.
- **Promoting an API to stable means deleting the `[Experimental]` attribute** (the feature already rode the trunk — no cross-branch cherry-pick). The ID's row moves to **Promoted** status and the ID is retired, not recycled. Adding or removing `[Experimental]` is not a breaking change.
- **Channel discipline:** on the `experimental` (alpha) / `main` (preview) test lanes the attribute is a courtesy; on a `release/YYYY` production line any risky-but-shipped public surface **must** wear it.

## Registry

Status values: **Experimental** (live, opt-in), **Promoted** (attribute removed, now stable — ID retired), **Withdrawn** (API removed before promotion — ID retired).

| ID | Surface | Introduced | Status | Notes |
|----|---------|------------|--------|-------|
| `FALLOUT001` | `Fallout.Components.IPublish.PublishTargets` / `.PublishTo` (multi-channel publishing) | 2026.1 | Experimental | Fan a single `Pack` output across multiple feeds with per-feed package-ID routing (`PublishTarget`). Shape may change while the CD model firms up (epic #332). Promote by deleting the attribute once `ReleaseChannel`/`DeploymentTarget` (#334) settle. |

<!--
Allocation example (do not uncomment unless a real API is marked):

| `FALLOUT001` | `Fallout.X.NewPluginHost` | 2026.2 | Experimental | Plugin-host entry point; shape may change while the plugin SDK firms up. |
-->
