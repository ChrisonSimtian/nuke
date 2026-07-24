# ADR-0011: Reintroduce JetBrains `[PublicAPI]` as an IDE authoring hint

## Status

Proposed (2026-07-24). Tracks [#539](https://github.com/Fallout-build/Fallout/issues/539). Partially reverses [PR #76](https://github.com/Fallout-build/Fallout/pull/76) (the wholesale `JetBrains.Annotations` removal), but only the `[PublicAPI]` concern and via a different mechanism — not a straight revert.

## Context

[PR #76](https://github.com/Fallout-build/Fallout/pull/76) removed `JetBrains.Annotations` wholesale — ~1450 `[PublicAPI]` attributes plus `[CanBeNull]`, `[Pure]`, `[UsedImplicitly]`, `[ContractAnnotation]`, and others. The stated rationale held: most of those attributes carried no weight for *this* repo's own build, and hand-maintaining ~1450 call sites is a genuine cost.

One consequence has proven to hurt day-to-day contribution: **Rider/ReSharper now flags Fallout's public build-authoring surface as unused.** That surface is unused *in-repo by design* — external consumers call `DotNetTasks`, subclass `GitHubActionsAttribute`, implement `IComponent`, and so on; the framework itself does not. Without a signal that this is intentional API, the IDE's unused-symbol inspection lights up exactly the code we ship, drowning real findings in false positives.

`[PublicAPI]` is precisely that signal: it tells Rider/ReSharper "this is intentional API, consumed externally — do not flag it as unused." It is an **IDE authoring hint only** — no runtime behaviour, no semantic change, no effect on compilation beyond an attribute in metadata.

**This is a distinct concern from public-API break detection.** Two things get conflated because both say "public API":

- **This ADR — an IDE authoring hint.** Silences a false "unused" inspection. Cares only that a symbol *is* API, not whether it changed.
- **API-surface break detection** — [#410](https://github.com/Fallout-build/Fallout/issues/410) / [PR #530](https://github.com/Fallout-build/Fallout/pull/530), the Roslyn `PublicApiAnalyzers` (`PublicAPI.Shipped.txt` / `.Unshipped.txt`) track, plus the `PublicApiGenerator` snapshot work. Guards the shipped surface against accidental change and feeds the changelog.

They are complementary and independent. This ADR does not touch, block, or depend on that track.

The prior re-introduction attempt lived on an unmerged fork branch (`feature/jetbrains-publicapi-annotations`) with no issue and no ADR — exactly the "did it without recording why" pattern we want to avoid. This ADR exists so the decision is reviewable in the open.

## Decision

**Reintroduce `JetBrains.Annotations` for the `[PublicAPI]` concern only, applied as a per-assembly blanket on the consumer-facing layer, with a selective per-type carve-out for extensibility surface in inner assemblies (the "hybrid" split).**

1. **Outer (consumer-facing) assemblies get a blanket `[assembly: PublicAPI]`.** Stamping it at assembly scope declares the assembly's *entire* public surface as intentional API — which, for a build framework, is true: outer assemblies exist to be consumed. This avoids reintroducing ~1450 hand-placed attributes and the maintenance tax of keeping them in sync as new API lands.

2. **The outer layer is a single MSBuild allowlist** — `_FalloutOuterApiAssemblies` in `Directory.Build.props` — the one source of truth for "what is consumer-facing." Membership at time of writing:

   `Fallout.Common`, `Fallout.Build`, `Fallout.Build.Shared`, `Fallout.Components`, `Fallout.Core`, `Fallout.Tooling`, `Fallout.ProjectModel`, `Fallout.Utilities`, `Fallout.Utilities.IO.Compression`, `Fallout.Utilities.IO.Globbing`, `Fallout.Utilities.Net`, `Fallout.Utilities.Text.Json`, `Fallout.Utilities.Text.Yaml`, `Fallout.Solution`, and the `Nuke.*` transition shims (`Nuke.Common`, `Nuke.Build`, `Nuke.Components`).

3. **Inner assemblies get no blanket.** `Fallout.Cli`, `Fallout.Migrate`, `Fallout.Migrate.Analyzers`, `Fallout.MSBuildTasks`, `Fallout.SourceGenerators`, `Fallout.Tooling.Generator`, and `Fallout.Persistence.Solution` are build-time tooling, codegen, the CLI host, and a vendored parser — not build-authoring API. Their `public` types are implementation details behind a facade or an exe boundary.

4. **Genuine extensibility surface *inside* an inner assembly is annotated per-type in source** — a `[PublicAPI]` on the specific type, not a blanket on the assembly. This is the "hybrid" half: it keeps the blanket honest (an inner assembly's blanket would wrongly bless its implementation details as API) while still silencing the false-unused inspection on the rare inner type an external consumer really does touch. Each such carve-out is recorded in [docs/api-encapsulation-audit.md](../api-encapsulation-audit.md).

5. **The reference flows to consumers (no `PrivateAssets`).** `[PublicAPI]` is only useful to a consumer's IDE if the attribute is present in the metadata the consumer resolves; that requires `JetBrains.Annotations` to be a normal (non-private) dependency of the outer packages, exactly as upstream NUKE shipped it. A consumer's Rider/ReSharper then sees our surface as API too.

6. **A project opts out** with `<FalloutPublicApi>false</FalloutPublicApi>` set before the import. Central Package Management is off for `build/_build.csproj`, so the wiring is additionally guarded on `'$(ManagePackageVersionsCentrally)' != 'false'`.

Only `[PublicAPI]` is reintroduced. The other removed attributes (`[CanBeNull]`, `[Pure]`, `[ContractAnnotation]`, …) stay gone — they carried the maintenance cost PR #76 objected to without addressing the unused-symbol problem this ADR is about. Reintroducing them, if ever wanted, is a separate decision.

## Consequences

### Positive

- **The false "unused" inspection on shipped API is silenced** — for contributors here and for consumers whose IDE resolves our packages.
- **Low churn, one source of truth.** The consumer-facing layer is one allowlist, not ~1450 attributes drifting out of sync. New API in an outer assembly is covered automatically.
- **The blanket stays honest.** By keeping inner assemblies off the blanket and carving out only real extensibility types, the annotation continues to *mean* "intentional API" rather than "happens to be public."
- **Reversible and inert.** It is an attribute in metadata; removing the allowlist entry or the package reference fully reverts it, and it never affects runtime.

### Negative

- **Outer packages gain a dependency on `JetBrains.Annotations`.** Because the reference is non-private (it must be, to reach consumers), consumers acquire a transitive dependency on the annotations package. It is small and runtime-inert, and it is what NUKE shipped, but it is a real addition to the dependency graph — the main trade-off this decision makes. (An internal, self-defined copy of the attribute avoids the dependency but does not flow to consumers; see Alternatives.)
- **`[assembly: PublicAPI]` is coarse.** It blesses an outer assembly's *entire* public surface, including anything that is `public` by accident. That is the risk the encapsulation audit exists to manage: public-by-accident types in outer assemblies are tracked and narrowed via the normal breaking-change flow, not left silently blessed forever.
- **The allowlist is a curated judgement call**, not mechanically derived — packability is SDK-default-true for non-test projects here, so it cannot stand in for "consumer-facing." Membership needs a human decision at review time and maintenance as the assembly graph evolves (notably the in-progress onion/layering work).

### Neutral

- No versioning, channel, or release-pipeline impact (ADR-0004/0007/0008 untouched). Adding or removing `[PublicAPI]` is not a breaking change.
- Independent of the API-break-detection track (#410 / #530); neither blocks the other.
- The other JetBrains attributes remain removed.

## Alternatives considered

### A. Restore per-type `[PublicAPI]` on every public member (the pre-#76 state)

Reinstate all ~1450 attributes, hand-maintained.

**Rejected** — it re-incurs exactly the maintenance cost PR #76 removed, and every new public API needs a remembered attribute. The assembly-level blanket achieves the same IDE outcome for the outer layer at a fraction of the surface area.

### B. Define an internal `[PublicAPI]` attribute in our own source (no package dependency)

Declare the attribute ourselves under a `JETBRAINS_ANNOTATIONS`-style guard, avoiding the `JetBrains.Annotations` package.

**Rejected** — an internally-defined attribute silences the inspection in *our* IDE but does not flow to *consumers'* IDEs (they would need the same attribute type by full name in resolvable metadata). Since a stated goal is that consumers' Rider sees our surface as API, the real package is required. Worth revisiting only if the dependency proves objectionable and consumer-side hinting is dropped as a goal.

### C. Do nothing; suppress the inspection via `.editorconfig` / DotSettings

Turn down the unused-symbol severity for the relevant assemblies instead of annotating.

**Rejected** — a blunt severity change hides *genuine* unused-symbol findings too (real dead code in the same assemblies), and it is per-checkout tooling config that does nothing for consumers. `[PublicAPI]` is the targeted signal: "unused because it's API," not "stop checking for unused."

### D. Ship annotations as an external XML file alongside the assembly

JetBrains supports `<AssemblyName>.ExternalAnnotations.xml`.

**Rejected** — higher-friction to author and keep in sync than a compiled attribute, and packaging/resolving the XML for consumers is more fragile than the attribute-in-metadata path. The blanket assembly attribute is simpler and travels with the assembly.

## References

- [#539](https://github.com/Fallout-build/Fallout/issues/539) — the tracking issue for this decision.
- [PR #76](https://github.com/Fallout-build/Fallout/pull/76) — the wholesale `JetBrains.Annotations` removal this partially reverses.
- [#410](https://github.com/Fallout-build/Fallout/issues/410) / [PR #530](https://github.com/Fallout-build/Fallout/pull/530) — public-API break detection (Roslyn `PublicApiAnalyzers`); the separate, complementary concern.
- [docs/api-encapsulation-audit.md](../api-encapsulation-audit.md) — the public-by-accident audit + per-type carve-out registry that accompanies this pass.
- `Directory.Build.props` — `_FalloutOuterApiAssemblies` (the allowlist) and the `FalloutPublicApi` wiring.
