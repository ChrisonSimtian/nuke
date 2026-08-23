# ADR-0002: v11 publishes to GitHub Packages by default; nuget.org is opt-in

## Status

Accepted (2026-05-29).

## Context

[ADR-0001](0001-release-branch-model.md) established the tag-triggered, multi-channel CD pipeline. The original routing was:

- Tag on `release/v11` → publish Fallout.* to nuget.org (Tier 1) + publish Nuke.* shims to GitHub Packages (Tier 2) + attach all nupkgs to GitHub Release.

Shortly after that landed, Chris flagged a separate direction: **v11 should not go to nuget.org by default.** nuget.org is reserved for v10.x maintenance lines and a future "stabilised" v11. Existing `11.0.x` releases on nuget.org are being unlisted in parallel.

Reasons (not exhaustive — Chris's call):

- The v11 rebrand surface is still settling. Public-API consumers depending on Fallout.* via nuget.org get exposed to churn that hasn't fully stabilised.
- 10.x is still the de-facto stable line for most consumers; the project-controlled nuget.org space should remain v10.x-oriented until v11 has shipped a stable enough surface to assume the position.
- GitHub Packages is a fine release channel for beta-and-stabilising work — opt-in consumers add the feed to their `nuget.config`, the broader consumer base isn't dragged into rebrand churn.

The pipeline shipped in ADR-0001 routed Fallout.* to nuget.org on every tag — exactly the wrong default for the current v11 state.

## Decision

Make nuget.org publish **opt-in via a `workflow_dispatch` input flag**. Specifically:

- Add `publish-to-nugetorg` (boolean, default `false`) to the `release.yml` `workflow_dispatch` inputs.
- `publish-nuget-org` job gains `if: github.event_name == 'workflow_dispatch' && inputs.publish-to-nugetorg == true`.
- Tag pushes alone do NOT trigger nuget.org publish. They publish to GitHub Packages + GitHub Releases only.
- To publish Fallout.* to nuget.org, invoke `workflow_dispatch` with `publish-to-nugetorg=true`. The `nuget-org` env approval gate still fires before the actual push — two layers of intent.

Companion change: **`publish-github-packages` now pushes all `*.nupkg`**, not just `Nuke.*`. GitHub Packages is the v11 release channel; every tag publishes the full set of Fallout.* + Nuke.* packages there.

The Nuke.* shim packages always go to GitHub Packages (per [#47](https://github.com/Fallout-build/Fallout/issues/47) — those IDs are owned by the original NUKE maintainer on nuget.org). This is unchanged.

## Consequences

### Positive

- **Default safety.** A tagged release on `release/v11` can't accidentally publish to nuget.org. The opt-in flag is the explicit "this is stabilised enough" decision.
- **Three layers of nuget.org safety**: tag protection (admin only) + flag opt-in + env approval. Reflects the irreversibility of nuget.org publishes.
- **No CI restructuring beyond the flag.** The existing job shape, environment routing, and approval gates carry over. Small, targeted change.
- **Same pipeline serves v10.x maintenance and stabilised v11.** When `release/v10.x` branches get cut for maintenance lines, the same workflow handles them — just set the flag.

### Negative

- **Maintainer has to remember the flag.** For genuinely-stabilised releases this is a small friction. Mitigation: documented in `docs/branching-and-release.md` step-by-step.
- **GitHub Packages is now load-bearing for v11.** Beta consumers (and us) rely on it as the primary release channel. If GH Packages has an outage, v11 release publishing is blocked until it recovers. The `--skip-duplicate` idempotency means retries are cheap, but the dependency is real.
- **Documentation churn.** Three docs needed updating (`docs/agents/release-and-versioning.md`, `docs/branching-and-release.md`, this ADR) shortly after ADR-0001 landed.

### Neutral

- **ADR-0001 stays accurate** as the description of the release-branch model itself. The Tier 1 / Tier 2 / Tier 3 taxonomy still applies. This ADR documents the *routing policy* on top of that model.
- **`project_release_channels` agent memory** captures the steady-state design. A separate memory entry (`project_v11_off_nuget`) captures the v11-specific carve-out. Future v12/v13 may revisit.

## Alternatives considered

### A. Branch-aware hard-coding in the workflow

Conditional logic: `if ref == 'release/v11' skip nuget.org`. Hard-codes the policy per release branch.

**Rejected because:**

- Every new release branch (v10.x maintenance, v12 stabilisation) would need a YAML edit to register its routing. The flag-based approach is uniform across branches.
- "Stabilised v11" is a fuzzy boundary — at some point v11 starts going to nuget.org. Hard-coded ref checks force a YAML edit at that boundary; the flag lets the boundary be a per-release decision.

### B. version.json field

Per-branch field like `"publishToNugetOrg": true|false`. The workflow reads it.

**Rejected because:**

- Adds tooling (YAML reading version.json, decision logic). For a binary "should I publish or not" decision the input flag is sufficient.
- The decision is per-release, not per-branch (a v11 patch you trust enough for nuget.org vs one you don't). Per-branch config is the wrong granularity.

### C. Two separate workflows

`release.yml` for GitHub Packages only, `release-nugetorg.yml` for nuget.org. Maintainer picks which to invoke.

**Rejected because:**

- Two workflows duplicate test-and-pack work and double the YAML to maintain.
- The shared test-and-pack + artifact upload is what makes the multi-channel fan-out efficient. Splitting would lose that.

## References

- [ADR-0001: Release-branch model & multi-channel CD](0001-release-branch-model.md) — the parent decision.
- [RFC #267](https://github.com/Fallout-build/Fallout/issues/267) — original design discussion.
- [docs/branching-and-release.md](../branching-and-release.md) — maintainer runbook (updated with the routine + stabilised release paths).
- [#47](https://github.com/Fallout-build/Fallout/issues/47) — Nuke.* shims live on GitHub Packages permanently.

## Memory artifacts (AI agent context)

- `project_v11_off_nuget.md` — the v11-off-nuget direction as recorded in agent memory.
- `project_release_channels.md` — the broader channel taxonomy (steady-state model).
