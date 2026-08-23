# ADR-0001: Release-branch model with tag-triggered multi-channel CD

## Status

Accepted (2026-05-29). Implementation shipped under [milestone #13](https://github.com/Fallout-build/Fallout/milestone/13).

> **Amended by [ADR-0004](0004-calendar-versioning-and-dual-pace-channels.md) (2026-05-29, further amended 2026-05-30).** The **versioning** decision below (per-major semver, "any breaking change bumps the major") is superseded by calendar versioning (`YYYY.MINOR.PATCH`, breaking batched to the yearly cut), and the channel model gains published **test lanes** — `experimental` (`-alpha`) and `main` (`-preview`), both GitHub Packages only. The release-branch model, tag-triggered multi-channel CD, GitHub Environments, and cherry-pick/forward-port hotfix flow described here remain in force.
>
> **Further amended by [ADR-0007](0007-cut-release-branch-on-demand.md) (2026-06-08).** The release branch is now **cut on demand at the first release** rather than being long-lived from year start, and ADR-0001's *"branches are cheap, don't delete"* stance is relaxed for dead branches with no unique history (tags are the durable release markers). The trigger, `validate-ref`, environments, and `prepare-release` mechanics are unchanged.

## Context

Pre-decision, Fallout's release pipeline auto-published on every merge to `main`. Nerdbank.GitVersioning bumped the patch via git-height, the tag fired, and `.github/workflows/release.yml` pushed to nuget.org. Two compounding problems surfaced:

1. **Consumer-facing noise.** Every merge — including internal cleanup like the license-header strip ([#260](https://github.com/Fallout-build/Fallout/pull/260)) — produced a Fallout.* release on nuget.org. Within a few days of v11-prep the patch counter climbed from `11.0.x` to `11.0.13+`. Each release fires a Dependabot upgrade PR in every downstream consumer repo. The fan-out cost to the userbase was disproportionate to the change content.

2. **No hotfix path for older majors.** Once `main` advances to v12-prep territory, there's no clean way to ship a v11 patch for downstream consumers. The escape hatch would be reverting `main` to a v11 base, fixing, releasing, then reapplying v12 work — non-starter.

Chris (project maintainer) called out the consumer pain explicitly: *"the pain is real, while OUR pain is just a bunch of tokens and then we're done"* — explicit guidance to favor maintainer cost over downstream cost when the two trade off. This shaped the timing of the cutover (immediate, not "after v11.0.0 ships from main") and the willingness to absorb migration overhead.

## Decision

Adopt a **release-branch model with tag-triggered, multi-channel CD**:

### Branching

- `main` = integration trunk. Every PR lands here. Merges to `main` do **not** publish.
- `release/vN` = release channel per major version. Tags pushed here fire the release pipeline. Cut from `main` at the point major N goes live. Long-lived; old release branches stay supportable.
- `release/vN.M` = optional minor-line branches, cut on demand.
- Hotfixes flow **main → cherry-pick → release/vN**, never direct-to-release except for declared emergencies.

### Release trigger

**Tag push on a `release/vN` branch.** The workflow's `validate-ref` job confirms the tag's commit is reachable from a `release/v*` branch and fails fast otherwise. A `workflow_dispatch` fallback with a `tag` input handles re-runs after transient publish failures (`--skip-duplicate` keeps re-runs idempotent).

### Multi-channel fan-out via GitHub Environments

Three environments keyed by **channel**, not by major (the major is captured in the deployment `ref`):

| Environment | Tier | Audience | Gating |
|---|---|---|---|
| `nuget-org` | 1 — production | All consumers via nuget.org | Required-reviewer approval |
| `github-packages` | 2 — bleeding edge | Opt-in beta testers; Nuke.* transition shims | None |
| `github-releases` | (bundled) | Archival + manual download | None |

Future Tier 3 (Docker local NuGet server for pre-merge testing) tracked in [#279](https://github.com/Fallout-build/Fallout/issues/279).

### Tag protection

Repository ruleset blocks creation/deletion/update of `v*` tags except by repo admins. Combined with the `nuget-org` approval gate, this is two-layer gating: who-can-tag + who-can-approve-publish.

### Versioning

`Nerdbank.GitVersioning` is per-branch via `version.json`. `main` carries the next major in development; each `release/vN` keeps `"version": "N.0"`. Patch heights compute within each branch. `publicReleaseRefSpec` is extended on each release branch to include itself, so versions are git-sha-clean (`11.0.20`, not `11.0.20-g<sha>`).

## Consequences

### Positive

- **Consumer noise gone.** No more 20-patch-per-week nuget.org bombardment from internal cleanup. Releases are explicit, deliberate, batched.
- **Older majors are supportable.** A v11 patch can ship from `release/v11` while `main` works on v12, without revert-then-replay gymnastics.
- **Two-layer release gating** (tag protection + env approval) makes accidental or unauthorized publishes correspondingly less likely.
- **Per-channel deployment records** on the GitHub Environments UI give a clean audit trail of what shipped where, when.
- **Channel taxonomy is encoded in infrastructure**, not just convention — wrong-channel publishes are blocked structurally.

### Negative

- **Cherry-pick overhead for hotfixes.** Maintainer mental model is "fix lands on main, then cherry-pick to release branch" instead of "fix where it belongs and ship." Mitigation: documented in [docs/branching-and-release.md](../branching-and-release.md), and the cherry-pick step is single-command.
- **Two-step merge for some changes.** A v11 hotfix takes two PRs (main + cherry-pick) instead of one. Cost is small per incident.
- **Workflow complexity.** `release.yml` went from one job to five jobs across three environments. More YAML, more triggers, more places a bug can hide. Smoke tests via `workflow_dispatch` mitigate.
- **CI workflow triggers had to grow.** Branch protection on `release/vN` requires `ubuntu-latest`, which forced `ubuntu-latest.yml` and `-docs.yml` to extend their `pull_request.branches` to include `release/v*`. A latent chicken-and-egg (no CI fires on a release-branch PR until those workflow updates land *on the release branch*) surfaced during initial setup and required a one-time admin-bypass merge to resolve.
- **`publicReleaseRefSpec` requires per-branch tuning.** Each new `release/vN` cut needs its `version.json` updated to include itself in the public-release ref spec, otherwise its versions are git-sha-suffixed. Documented as part of the "cutting a new `release/vN`" runbook.

### Neutral

- **CHANGELOG.md cadence unchanged.** PRs still add entries under `[Unreleased] — <next-major>` per the existing flow. The CHANGELOG-vs-GitHub-Releases reconciliation is a separate concern, tracked in [#263](https://github.com/Fallout-build/Fallout/issues/263).
- **`Build.cs`'s `IPublish.Publish` target is now unused by CI** — the workflow calls `dotnet nuget push` and `gh release create` directly. The target stays available for local invocation. Consolidating or removing it is a possible future cleanup, not urgent.

## Alternatives considered

### A. Keep main-triggered, reduce noise via path filters

Stick with the existing model and add aggressive `paths-ignore` rules to skip releases on doc-only or test-only changes.

**Rejected because:**

- Doesn't solve the hotfix problem.
- Path-based skip rules are brittle — easy to miss edge cases, and "internal cleanup" doesn't always map cleanly to file paths.
- A no-publish merge to main still bumps NB.GV's patch height, so the next "real" release jumps numbers, which is also confusing.

### B. Release-per-merge to `release/vN`

Merging to `release/v11` automatically publishes (mirror of the old `main → release` shape but scoped to a release branch).

**Rejected because:**

- Solves the hotfix problem but reintroduces the noise problem (now scoped to the release branch instead of main, but still per-merge).
- Loses the "explicit decision to ship" property that tag-driven gives.

### C. `workflow_dispatch`-only releases as the permanent shape

Stay on the stopgap. All releases are manual button clicks.

**Rejected because:**

- Loses the git-native tag-as-release-marker discoverability. `git tag --list` should tell you what shipped, not "look at the GitHub Actions history."
- Easier to lose track of which commits made it into which release.
- The tag-trigger approach is the same amount of work to maintain and reads more cleanly externally.

### D. Defer this work to milestone #8 (v13 CD vision)

Treat the noise as a v13 problem; ship v11 under the existing model.

**Rejected** based on Chris's consumer-pain-first principle (see Context). The noise was actively hurting consumers *now*; deferring to v13 meant continued daily pain until then.

## References

- [Milestone #13](https://github.com/Fallout-build/Fallout/milestone/13) — work-breakdown.
- [RFC #267](https://github.com/Fallout-build/Fallout/issues/267) — design discussion.
- [docs/branching-and-release.md](../branching-and-release.md) — maintainer runbook for the model.
- [docs/agents/release-and-versioning.md](../agents/release-and-versioning.md) — PR-flow + release-pipeline reference.
- Related: [#262](https://github.com/Fallout-build/Fallout/issues/262) (backwards-compat principle), [#263](https://github.com/Fallout-build/Fallout/issues/263) (CHANGELOG vs GH Releases), [#279](https://github.com/Fallout-build/Fallout/issues/279) (Tier 3 Docker).

## Memory artifacts (AI agent context)

- `project_release_channels.md` — channel taxonomy in agent memory.
- `feedback_consumer_pain_first.md` — the principle that drove the cutover timing.
