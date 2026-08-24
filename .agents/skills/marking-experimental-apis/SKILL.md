---
name: marking-experimental-apis
description: How to mark a public API as not-yet-stable with [Experimental("FALLOUT0xx")], or deprecate one with [Obsolete(..., DiagnosticId = "FALLOUTOBS0xx")], including diagnostic-ID allocation. Trigger when adding public API that isn't ready for a stability guarantee, or removing/replacing/deprecating an existing public API.
---

Fallout has two attributes for public-surface churn, each with its own
diagnostic-ID sequence and registry. Never mix the two sequences.

## `[Experimental]` — opt-in unstable surface

Use [`System.Diagnostics.CodeAnalysis.ExperimentalAttribute`](https://learn.microsoft.com/dotnet/api/system.diagnostics.codeanalysis.experimentalattribute)
(ships in the .NET 8+ BCL, no package reference needed) for public API that
isn't ready to commit to a stability guarantee:

```csharp
using System.Diagnostics.CodeAnalysis;

[Experimental("FALLOUT001")]
public sealed class NewPluginHost { /* ... */ }
```

- **Allocate the next `FALLOUT0xx` ID sequentially, never reused.** Register it
  in [docs/experimental-apis.md](../../../docs/experimental-apis.md) in the
  same PR.
- **`ExperimentalAttribute` is error-by-default.** A consumer must explicitly
  suppress the exact ID (`#pragma warning disable FALLOUT001` or `<NoWarn>`) to
  use the API — that's the opt-in.
- **Promoting to stable = deleting the attribute.** No cross-branch dance —
  the feature already rode the `develop` preview lane. Adding or removing
  `[Experimental]` is **not** a breaking change.
- **On a `release/vX.Y` / `main` production line, any risky-but-shipped public
  surface must wear it.** That's what keeps the production line trustworthy
  while it still carries new work — there's no separate `experimental` branch.
- **Don't apply it speculatively.** Marking an API that's already used
  internally breaks the build everywhere it's referenced — suppress every
  internal usage in the same change.

## `[Obsolete]` — deprecating a stable API

Use [`System.ObsoleteAttribute`](https://learn.microsoft.com/dotnet/api/system.obsoleteattribute)
with a `DiagnosticId` (ships in the .NET 5+ BCL) when a stable public API is on
its way out:

```csharp
[Obsolete(
    "Use [GitHubActionsInputAttribute] instead. Removed in v11.",
    DiagnosticId = "FALLOUTOBS001",
    UrlFormat = "https://github.com/Fallout-build/Fallout/blob/main/docs/obsolete_apis.md")]
public string[] OnWorkflowDispatchOptionalInputs { get; set; } = new string[0];
```

- **Allocate the next `FALLOUTOBS0xx` ID sequentially, never reused** — a
  separate sequence from `FALLOUT0xx` above. Register it in
  [docs/obsolete_apis.md](../../../docs/obsolete_apis.md) in the same PR.
- **Always set `DiagnosticId`.** Without one the compiler reports the generic
  `CS0618`, so a `TreatWarningsAsErrors` consumer can only fix every usage at
  once or blanket-suppress every deprecation. A per-deprecation ID lets them
  suppress just this one while they migrate.
- **Adding `[Obsolete]` is not a breaking change** — it's warning-level, so
  existing code keeps compiling. The *removal* is the break, and it waits for
  the next major. State the removal target in the message (e.g. `Removed in v11.`).
- **Keep the deprecated surface functional.** Prefer bridging the old member to
  the new one over leaving it inert, and suppress the internal bridge usage
  with `#pragma warning disable` scoped to the exact ID.

See [AGENTS.md rule 2](../../../AGENTS.md) for how these fit the
backwards-compatibility policy, and the `creating-a-pr` skill for how a
breaking change (as opposed to marking something experimental or obsolete)
needs to be labelled and targeted.
