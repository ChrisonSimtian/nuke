# ADR-0010 — Fallout collects no telemetry

- **Status:** Accepted
- **Date:** 2026-07-24
- **Deciders:** Fallout maintainers
- **Relates to:** [#79](https://github.com/Fallout-build/Fallout/issues/79) (drop the dead `Microsoft.ApplicationInsights` dependency — subsumed by this decision), the removed `docs/01-getting-started/07-telemetry.md`.

## Context

NUKE shipped a telemetry subsystem (added upstream in 2021): a `[Telemetry]` build extension plus global-tool hooks that gathered anonymous usage data — OS/SDK versions, build shape (target/component counts), hashed repo/commit fingerprints — and sent it to an **Azure Application Insights** instance owned personally by the original maintainer, behind a first-run disclosure/consent flow and a `NUKE_TELEMETRY_OPTOUT` opt-out.

Fallout inherited all of it, but it has been **inert since the fork**:

- The Application Insights instrumentation key was matkoch-owned and cannot be reused, so `Microsoft.ApplicationInsights` was dropped from dependencies ([#79](https://github.com/Fallout-build/Fallout/issues/79)) and `TrackEvent` became a stub that discards its inputs.
- The static constructor short-circuits before the consent flow ever runs — no disclosure prompt, no awareness cookie.
- Nothing is collected and nothing is transmitted anywhere.

The scaffolding was nonetheless **kept on purpose**, on the stated intent of wiring up a "Fallout-controlled backend later." That intent never acquired an owner, an endpoint, or a concrete plan — it was a standing *maybe*. Meanwhile the dormant code was a liability: it still assembled usage properties on the hot build path, it carried a re-enable comment that invited someone to flip it back on, and it kept a consumer-facing surface (`FALLOUT_TELEMETRY_OPTOUT`, `FalloutTelemetryVersion`, disclosure copy) alive for a feature that does nothing.

## Decision

**Fallout collects no telemetry — now or as a matter of standing policy for this fork — and the inherited subsystem is removed in full rather than left dormant.**

- No usage data is gathered, and nothing phones home. A build framework people run in CI and on their own machines should not be a data-collection channel; for a hard-fork rebuilding trust, "we collect nothing" is a simpler and stronger promise than "we collect anonymized data you can opt out of."
- Because there is nothing to opt out of, there is no opt-out. The `FALLOUT_TELEMETRY_OPTOUT` / legacy `NUKE_TELEMETRY_OPTOUT` env vars and the `FalloutTelemetryVersion` / `NukeTelemetryVersion` MSBuild property are removed along with the code that read them.
- Dormant scaffolding is not kept "just in case." If Fallout ever wants usage insight, that is a fresh decision requiring its own ADR and an **opt-in**, fully-documented design with a named endpoint and owner — not the revival of this code.

### Removed surface

- The `Telemetry` subsystem (`Telemetry`, `Telemetry.Events`, `Telemetry.Properties`) and the `[Telemetry]` build extension.
- All call sites: the `[Telemetry]` attribute on `FalloutBuild`, the config-generation hook, and the `setup` / `add-package` / `cake-convert` CLI events.
- `FALLOUT_TELEMETRY_OPTOUT` / `NUKE_TELEMETRY_OPTOUT`, `FalloutTelemetryVersion` / `NukeTelemetryVersion`, the `FalloutTelemetryDocsUrl` constant, and the test-run opt-out plumbing.
- The telemetry documentation page.
- The `fallout migrate` tool now **strips** NUKE's telemetry knobs from migrated projects instead of renaming them to dead `Fallout*` equivalents.

## Consequences

### Positive

- **Zero data collection**, and no privacy footgun — the "someone re-enables it incorrectly" path is gone because there is no code to re-enable.
- Smaller surface: dead code, a dead dependency ([#79](https://github.com/Fallout-build/Fallout/issues/79)), a consent UX, and several consumer-facing knobs all removed.
- Migrated NUKE projects come out clean — no telemetry-branded cruft carried across.

### Negative

- We forgo the aggregate usage insight telemetry could (in principle) have provided. Accepted: it was never actually flowing, and prioritization has done fine without it.
- Reintroducing any metrics later means starting from a clean, opt-in design — deliberately more work than flipping a dormant switch. That friction is the point.

### Neutral

- Removing the opt-out env var and the MSBuild property is **not a breaking change**: both only ever gated a no-op, and an unset/unknown env var or MSBuild property is silently ignored — no consumer build errors. The `FALLOUT001` legacy-property warning simply stops firing for `NukeTelemetryVersion`.
- `DOTNET_CLI_TELEMETRY_OPTOUT` (the **.NET SDK's** telemetry, unrelated to Fallout's) is left in place in the bootstrap scripts and generated CI — we still disable that.

## Alternatives considered

### A. Keep the scaffolding dormant, wire up a Fallout-owned endpoint later (the prior stance — rejected)

This is what the code and docs described. **Rejected** because the "later" had no owner, endpoint, or timeline; the dormant code was a live liability (hot-path property assembly, a re-enable invitation, consumer-facing knobs for a no-op); and keeping consent/opt-out machinery for a feature that does nothing is confusing. A future decision to collect data should be made explicitly, not pre-wired.

### B. Ship opt-in telemetry with a Fallout endpoint (rejected for now)

Stand up an opt-in, Fallout-owned analytics pipeline. **Rejected** — no compelling need for a build framework, and it is out of scope for the rebrand/foundation work. Not foreclosed forever: it would require its own ADR superseding this one, and would have to be opt-in by construction.

### C. Local-only anonymized stats (rejected)

Collect stats to a local file for the user's own inspection, never transmitted. **Rejected** as scope creep with no demonstrated demand; nothing stops a consumer from building this themselves via the public build-event hooks.

## References

- [#79](https://github.com/Fallout-build/Fallout/issues/79) — drop the dead `Microsoft.ApplicationInsights` dependency (subsumed here)
- [docs/agents/conventions.md](../agents/conventions.md) — "No telemetry" convention
- [docs/migration/from-nuke.md](../migration/from-nuke.md) — telemetry knobs are stripped on migration
