# ADR-0008: Collapse `experimental` into `main`; `main` is the sole prerelease lane

## Status

Accepted (2026-06-18). **Supersedes the channel-ladder section (§2) of [ADR-0004](0004-calendar-versioning-and-dual-pace-channels.md)** — specifically its 2026-05-30 amendment, which had added a dedicated `experimental` fast lane below `main`. The calendar-versioning scheme (§1), the production line + `[Experimental]` opt-in + review-tier decisions (§3–§6) of ADR-0004, the on-demand release-branch cut from [ADR-0007](0007-cut-release-branch-on-demand.md), and the nuget.org-opt-in policy from [ADR-0002](0002-v11-off-nuget-by-default.md) all remain in force.

## Context

ADR-0004's 2026-05-30 amendment introduced a three-tier channel ladder — `experimental` (`-alpha`) → `main` (`-preview`) → `release/YYYY` (production) — adding a standing `experimental` branch as the fast/AI-assisted lane. It did so to keep `main` stable-by-default (principle of least surprise, per [@dennisdoomen on #302](https://github.com/Fallout-build/Fallout/discussions/302)), explicitly accepting a *bounded* amount of `experimental → main` divergence as the cost, and leaning on forward-only promotion + `[Experimental("FALLOUT0xx")]` to keep that divergence small.

In practice the lane did not earn its keep:

- **The divergence was not bounded in the intended direction.** `experimental` ran ~17 commits *behind* `main`, not ahead — the forward-port discipline the amendment relied on was not happening. Deliberate and fast work was landing on `main` regardless.
- **`experimental` carried no unique production work.** At collapse time its entire source/build delta versus `main` was empty; the multi-channel publish feature it nominally introduced (#333/#339) already existed on `main` by content, and `experimental` was *missing* newer `main` work (ADR-0007, doc updates). It had degenerated into a stale snapshot.
- **It cost real overhead** — a second GitHub Packages publisher, a separate protected branch, an extra workflow (`experimental.yml`), and a forward-port obligation — for no realised benefit.

The maintainer's call: remove the lane and lean entirely on the tools that were always doing the actual isolation work — tags for production, and `[Experimental]` for unstable surface.

## Decision

**Collapse `experimental` into `main`.** Steady-state long-lived branches become **`main` + `support/*`** (plus `gh-pages` for the site). Everything else is a tag or a short-lived topic branch.

1. **`main` is the integration trunk *and* the sole prerelease lane.** Per-commit `-preview` prereleases to **GitHub Packages only — never nuget.org** (unchanged from ADR-0004 §2). Both deliberate improvements and faster/AI-assisted work land here. It remains the default branch.
2. **The `experimental` branch and its `-alpha` channel are retired.** `experimental.yml` is deleted; `preview.yml` (`main` → `-preview`) is the only continuous publisher.
3. **Breaking changes still batch to the yearly major cut** (ADR-0004 §1, unchanged) — but they now accumulate on `main` gated behind **`[Experimental("FALLOUT0xx")]`**, not on a separate branch. The attribute, always the per-API isolation tool, now carries the load the branch used to. Surface that cannot be safely gated waits for the year cut on a short-lived topic branch off `main`.
4. **Production is unchanged and tag-driven.** `release/YYYY` is still cut on demand ([ADR-0007](0007-cut-release-branch-on-demand.md)), hardened to `-rc.N` → GA, and shipped by tag push (`release.yml`) to nuget.org (opt-in) + GitHub Packages + GitHub Releases. `support/*` (legacy `support/v10`, retired `support/YYYY`) is unchanged.
5. **The version ladder loses its bottom rung:** `…-preview.N` < `…-rc.N` < `…` (GA). `version.json` on `main` stays `2026.1.0-preview.{height}`; `publicReleaseRefSpec` already excludes `main`, so no versioning change is required.

### Channel summary (revising ADR-0004's table)

| Channel | Built from | Cadence | Version shape | Publishes to | Review tier |
|---|---|---|---|---|---|
| **preview** | `main` | per-commit | `2026.1.0-preview.<height>.g<commit>` | GitHub Packages (test) | ordinary |
| **rc** | `release/YYYY` pre-GA | per cut | `2026.1.0-rc.2` | nuget.org (opt-in) + GH Packages | rigorous |
| **stable** | `release/YYYY` tags | yearly major + non-breaking minor/patch | `2026.1.3` | nuget.org (opt-in) + GH Packages + GH Releases | rigorous |
| **legacy** | `support/v10` | security/critical only | `10.x` (semver) | nuget.org (opt-in) + GH Packages | rigorous |
| **retired year** | `support/YYYY` | security/critical only | `YYYY.x` (CalVer) | nuget.org (opt-in) + GH Packages | rigorous |
| **`[Experimental]` APIs** | any channel | per-feature | rides the package | (the package) | opt-in by consumer |

## Consequences

### Positive

- **Fewer long-lived branches; a simpler mental model.** Steady state is `main` + `support/*`. No forward-port obligation, no `experimental → main` divergence to manage, one prerelease publisher instead of two.
- **The isolation that was actually working is what remains.** Production has always been gated by the deliberate `release/YYYY` cut + tags, and unstable surface by `[Experimental]`. Neither depended on the `experimental` branch; removing it loses no real safety.
- **Tag-based production is reaffirmed, not reinvented** — `release.yml` is unchanged.

### Negative

- **`main` carries faster, occasionally-unstable churn — the principle-of-least-surprise concern from [#302](https://github.com/Fallout-build/Fallout/discussions/302) that *added* `experimental` is partially reintroduced.** Mitigation: `main` is GitHub-Packages-only `-preview`, **never** a nuget.org/production feed; a consumer who wants stability uses the `release/YYYY` tags / nuget.org, not a `main` checkout. The default branch carrying churn is acceptable precisely because it never auto-publishes to production. Risky public surface still wears `[Experimental]`. The trade — "newcomer cloning `main` finds a stable-ish trunk" for "one fewer long-lived branch and no divergence" — favours collapse now that the lane has proven underused.
- **Breaking work has no dedicated branch; discipline shifts entirely to `[Experimental]` + reviewer vigilance + the yearly-cut batching rule.** A breaking change merged to `main` without `[Experimental]` gating could otherwise leak into a `release/YYYY` cut — the **production-cut review is the backstop** (ADR-0004 §6: rigorous, unhurried review of the cut). This must be enforced at cut time.

### Neutral

- ADR-0004 §1 (CalVer), §3–§6 (production line, `[Experimental]`, review tiers), ADR-0007 (on-demand cut), and ADR-0002 (nuget.org opt-in) are unchanged. This ADR removes one rung of the ladder; it does not touch versioning, production routing, or the opt-in policy.
- Labels: `channel/edge` (the alpha lane) is retired; `channel/preview` is the sole prerelease-channel label.

## Alternatives considered

### A. Keep `experimental`, enforce forward-port hygiene

Fix the drift rather than remove the lane — automate `main → experimental` forward-ports so `experimental` stops lagging.

**Rejected** because it spends tooling and process on preserving a lane that carried no unique value, to buy back a least-surprise property whose cost (an unstable default branch) was the very thing #302 worried about. The divergence was a symptom; the lane being unused was the disease.

### B. Make `release/YYYY` a standing branch again

Cut `release/YYYY` from day one (the pre-ADR-0007 model) so there's always a stable line distinct from `main`.

**Rejected** — ADR-0007 already decided on-demand cutting; `main` (`-preview`, GitHub Packages only) is the most-stable line until the first release of the year, and that is sufficient. Re-introducing a standing release branch trades one long-lived branch for another and re-opens ADR-0007.

## References

- [ADR-0004: Calendar versioning + dual-pace channels](0004-calendar-versioning-and-dual-pace-channels.md) — channel ladder (§2) superseded here; everything else retained.
- [ADR-0007: Cut `release/YYYY` on demand](0007-cut-release-branch-on-demand.md) — reaffirmed.
- [ADR-0002: v11 off nuget.org by default](0002-v11-off-nuget-by-default.md) — nuget.org-opt-in policy, retained.
- [docs/branching-and-release.md](../branching-and-release.md) — maintainer runbook (updated for this model).
- [docs/agents/release-and-versioning.md](../agents/release-and-versioning.md) — agent-facing reference (updated for this model).
- Discussion thread: [#302 — Calendar versioning + dual-pace channels](https://github.com/Fallout-build/Fallout/discussions/302).
