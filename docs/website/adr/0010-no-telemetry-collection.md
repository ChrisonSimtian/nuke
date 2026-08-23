# ADR-0010: Fallout collects no telemetry

## Status

Accepted (2026-07-24). Subsumes [#79](https://github.com/Fallout-build/Fallout/issues/79) (drop the dead `Microsoft.ApplicationInsights` dependency).

## Context

NUKE shipped a telemetry subsystem: a `[Telemetry]` build extension plus global-tool hooks that gathered anonymous usage data (OS/SDK versions, build shape, hashed repo fingerprints) and sent it to an **Azure Application Insights** instance owned personally by the original maintainer, behind a first-run consent flow and a `NUKE_TELEMETRY_OPTOUT` opt-out.

Fallout inherited it, but it has been inert since the fork: the instrumentation key can't be reused, so the client dependency was dropped ([#79](https://github.com/Fallout-build/Fallout/issues/79)) and `TrackEvent` became a stub; the static constructor short-circuits before the consent flow runs. Nothing is collected or transmitted.

The scaffolding was kept on the stated intent of wiring up a "Fallout-controlled backend later" — an intent that never acquired an owner, endpoint, or plan. Meanwhile it assembled usage properties on the hot build path, carried a re-enable comment, and kept consumer-facing knobs alive for a no-op.

## Decision

**Fallout collects no telemetry, and the inherited subsystem is removed in full rather than left dormant.**

1. Nothing phones home. For a hard-fork rebuilding trust, "we collect nothing" is a stronger promise than "anonymized data you can opt out of".
2. With nothing to opt out of, there is no opt-out: the `*_TELEMETRY_OPTOUT` env vars and the `*TelemetryVersion` MSBuild property go too, along with the code that read them.
3. Dormant scaffolding is not kept "just in case". Any future usage insight is a fresh decision needing its own ADR and an **opt-in** design with a named endpoint and owner — not a revival of this code.
4. `fallout migrate` **strips** NUKE's telemetry knobs instead of renaming them to dead `Fallout*` equivalents.

## Consequences

- **Positive** — zero data collection and no re-enable path; dead code, a dead dependency, a consent UX, and several consumer knobs removed; migrated NUKE projects come out clean.
- **Negative** — no aggregate usage insight. Accepted: it was never flowing anyway. Adding metrics later means starting from a clean opt-in design; that friction is the point.
- **Not a breaking change** — the env var and MSBuild property only ever gated a no-op, and an unset/unknown one is silently ignored. The `FALLOUT001` legacy-property warning simply stops firing for `NukeTelemetryVersion`.
- **Unaffected** — `DOTNET_CLI_TELEMETRY_OPTOUT` (the .NET SDK's own telemetry) is still set in bootstrap scripts and generated CI.

## Alternatives considered

- **Keep the scaffolding dormant, wire up a Fallout endpoint later** (the prior stance) — rejected: the "later" had no owner or timeline, and consent machinery for a no-op is a liability, not option value.
- **Ship opt-in telemetry now** — rejected: no compelling need for a build framework, out of scope for the foundation work. Not foreclosed; it would need an ADR superseding this one.
- **Local-only stats, never transmitted** — rejected as scope creep; consumers can build it on the public build-event hooks.

## References

- [#79](https://github.com/Fallout-build/Fallout/issues/79) — dead `Microsoft.ApplicationInsights` dependency, subsumed here.
- [docs/migration/from-nuke.md](../migration/from-nuke.md) — telemetry knobs are stripped on migration.
