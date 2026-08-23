# ADR-0009: Revert to classic GitFlow; stay on semver 10.x until a breaking change is needed

## Status

Accepted (2026-08-23).

This ADR replaces [ADR-0004](0004-calendar-versioning-and-dual-pace-channels.md) (calendar versioning + dual-pace channels). That includes the parts of ADR-0004 that [ADR-0007](0007-cut-release-branch-on-demand.md) and [ADR-0008](0008-collapse-experimental-into-main.md) already changed.

This ADR keeps ADR-0007's decision (cut the release branch on demand, not ahead of time) and ADR-0008's decision (no separate `experimental` branch). Both decisions still apply. Only the branch names change.

This ADR also changes part of [ADR-0001](0001-release-branch-model.md). The release branch itself, the tag-triggered CD pipeline, and the three GitHub Environments stay the same. What changes: GA tags now come from `main` again, instead of a `release/vN` branch. A `develop` branch takes over the role `main` had under ADR-0004/ADR-0008.

[ADR-0002](0002-v11-off-nuget-by-default.md)'s nuget.org opt-in policy does not change.

## Context

ADR-0004 introduced calendar versioning (`YYYY.MINOR.PATCH`). The goal was to serve two kinds of contributors at once: an AI-assisted fast group and a deliberate, slower group. It gave the slower group a stable target for the whole year. In practice, the versioning part of that decision cost more than it gave back:

1. **CalVer confused "when" with "what changed."** A major version bump happened every January, whether or not anything actually broke. That makes the version number a weak signal of compatibility — the opposite of what semver is supposed to tell consumers.
2. **`main` as the release-tag source was non-standard.** Most tools, and most contributors' expectations, assume `main` is where a stable release comes from. ADR-0004 kept `main` doing two jobs at once: integration trunk and preview channel. Every maintainer and contributor had to relearn that split.
3. **The project always wanted a literal `develop` branch.** ADR-0004 said as much in its own text: its model "is gitflow with the project's vocabulary," but it stopped short of adding the actual `develop` branch. See [Alternative C](0004-calendar-versioning-and-dual-pace-channels.md#c-gitflow-with-a-permanent-develop) in that ADR — this ADR adopts it fully.
4. **No CalVer release ever shipped.** `2026.x` never reached GA. The only real release activity since ADR-0004 was a `release/v10.4` hotfix, cut from the older `support/v10` line — unrelated to CalVer.

Separately, and more importantly for the shape of this decision: **the project wants to keep upgrades smooth for existing consumers for as long as possible.** Fallout's `10.x` line is what people using Dependabot or Renovate depend on today. Jumping straight to a new major (v11, or a fresh CalVer year) forces those consumers to think about a breaking change even when nothing they use has actually broken. The project would rather keep shipping non-breaking `10.x` releases — `10.4`, `10.5`, `10.6`, and so on — for as long as there's no real reason to break anything. A major version bump (starting with v11) is reserved for the day the project actually needs to make a breaking change, and that decision is deliberately postponed as long as possible.

## Decision

### 1. Classic GitFlow, using renamed branches, still no `experimental`

The `develop` and `main` branches below are **renames of branches that already exist today** — nothing is created from scratch, and no history is lost:

- **`develop`** — this is the current `main` branch, renamed. It keeps its role: the integration trunk and the only prerelease lane. It's the default branch, and every push publishes a `-preview` build to GitHub Packages only, never nuget.org. Both deliberate work and fast/AI-assisted work land here. There is still no separate `experimental`/`-alpha` branch — ADR-0008's decision to drop that lane stands.
- **`release/vX.Y`** (e.g. `release/v10.5`). Cut from `develop` on demand, at the first release of a new minor version — not ahead of time. This keeps ADR-0007's rule, applied the same way to every `10.x` minor, not just to an eventual major. It takes `-rc.N` prereleases, published to GitHub Packages. After the branch is cut, it only takes non-breaking fixes.
- **`main`** — this is the current `release/v10.4` branch, renamed. It becomes the ongoing production trunk: each subsequent `release/vX.Y` (10.5, 10.6, and so on) merges into it and gets tagged there for GA. It is now the default's counterpart, not the default branch itself.
- **`support/v10`** — unchanged. This stays as the older legacy line, for patches to versions before 10.4 (10.0.x–10.3.x), security and critical fixes only.
- **Hotfixes** — classic GitFlow shape: cut a `hotfix/vX.Y.Z` branch from `main`, fix the bug, then merge it back into both `main` (tag it for release) and `develop` (so the fix isn't lost in the next release).

### 2. Stay on semver 10.x; defer a breaking major as long as possible

- The version format goes back to classic semver: `MAJOR.MINOR.PATCH`. There is **no renumbering** — the project stays on `10.x`, continuing from `10.4`.
- **Every `10.x` release is non-breaking.** `10.4`, `10.5`, `10.6`, and every version after that: a minor adds features, a patch fixes bugs, and nothing that already works stops working. A consumer using Dependabot or Renovate can always safely take a newer `10.x` version without checking for breaking changes first.
- **v11 is reserved for the day a breaking change actually becomes necessary — no fixed date.** The project deliberately puts this decision off as long as it can, specifically to avoid forcing an upgrade decision on consumers who don't need one yet.
- **When v11 does become necessary, the upgrade will use the `fallout-migrate` tool** — the same tool that already handles the NUKE-to-Fallout migration — extended to cover whatever the v11 change requires. The goal is the same smooth-upgrade experience consumers get today.
- Until that day, if any breaking idea needs to be worked on ahead of time, it still lands on **`develop`, behind `[Experimental("FALLOUT0xx")]`** (or, if that doesn't fit, on a short-lived branch off `develop`) — same isolation mechanism as before. It just has no fixed release target; it waits until the project actually decides to cut v11.
- The `target/YYYY`-style labels from the old model don't apply anymore — the project already uses simpler `target/vCurrent` / `target/vNext` labels (see AGENTS.md rule #1), and those don't need to change.

### 3. GA tags fire from `main`

Under ADR-0004/ADR-0008, GA tags fired from `main` while `main` was still the integration trunk. Now `main` is the renamed `release/v10.4` branch — the production trunk. A `release/vX.Y` branch can still publish `-rc.N` prereleases to GitHub Packages while it stabilizes, but the GA tag — and the nuget.org-eligible package — always comes from `main`.

## Consequences

### Positive

- **A model most people already recognize.** `main` is production. `develop` is where things land. This matches what most contributors and tools already expect.
- **No unnecessary renumbering.** Consumers keep upgrading within `10.x` exactly as they do today. Nothing changes for them until the project actually needs to ship something breaking.
- **The version number means something again.** When a major bump does happen, it will mean a real breaking change happened — not that a year passed, and not that the project decided to renumber for its own reasons.
- **Closes a gap ADR-0004 itself flagged.** ADR-0004's own alternatives section ([Alternative C](0004-calendar-versioning-and-dual-pace-channels.md#c-gitflow-with-a-permanent-develop)) already said a literal `develop` branch made sense. This ADR does it, without reopening the separate question ADR-0008 already settled (no `experimental` branch).
- **Reusing branches instead of creating new ones keeps history intact.** Renaming `main` to `develop` and `release/v10.4` to `main` means every commit, PR reference, and CI run stays attached to the same branch, rather than starting a fresh branch with a copied snapshot.

### Negative

- **One more long-lived branch to protect.** `main` is now separate from `develop`, on top of `release/vX.Y` and `support/*`.
- **Docs and CI config need updating.** Every doc, workflow comment, and `version.json` written for the CalVer/ADR-0008 model, or written assuming an immediate v11 resumption, needs updating — this is what this PR does.
- **Branch renames and protection changes are a follow-up, not part of this PR.** This ADR, and the doc/CI changes alongside it, describe the target shape. Renaming `main` to `develop`, renaming `release/v10.4` to `main`, making `develop` the GitHub default branch, and re-applying branch protection to the renamed branches are maintainer tasks done in GitHub's settings, not in this PR.
- **Contributors relearn the model a second time.** People who got used to ADR-0004/ADR-0008's shape in the last few months need to learn this one too. This ADR tries to make the change clear by explaining what moved and why, and by keeping ADR-0007's and ADR-0008's decisions rather than re-arguing them.

### Neutral

- **ADR-0001's release branch, tag-triggered CD, and GitHub Environments stay the same.** Only the branch that carries the tag changes.
- **ADR-0002's nuget.org opt-in policy stays the same.** It works the same way no matter which branch carries the tag.
- **ADR-0007's and ADR-0008's decisions carry over unchanged**, just renamed onto the new branches (`main`'s old role becomes `develop`'s role).
- **The `[Experimental]` attribute (`FALLOUT0xx` diagnostic IDs) works the same way.** Only the branch names in its description change: `develop` is the relaxed test lane, `main` is the production line where risky-but-shipped code must carry `[Experimental]`.
- **`support/v10` is not affected**, other than continuing to serve an older set of patch versions than it did before `release/v10.4` existed.

## Alternatives considered

### A. Keep CalVer, just add a literal `develop`

Add `develop` as the new integration trunk, but keep `YYYY.MINOR.PATCH` versioning and tag GA from `main` (ADR-0008's current shape).

**Rejected.** The complaint about CalVer (a major bump doesn't mean a breaking change happened) is a separate problem from the missing-`develop` complaint. Fixing only the branch layer would leave the weaker part of ADR-0004 in place.

### B. Just rename branches, keep `main` doing two jobs

A small, cosmetic change: rename `release/YYYY` to `release/vX.Y`, but don't add `develop` and don't move the tag location.

**Rejected.** This doesn't fix the thing ADR-0004 itself flagged — that the project already wanted a literal `develop` branch. It also keeps `main` doing double duty as both the integration trunk and the release-tag source.

### C. Bring back `experimental` alongside `develop`

Restore a separate `experimental`/`-alpha` fast lane feeding into `develop`, going back to the three-branch model from ADR-0004's first amendment.

**Rejected.** ADR-0008 already retired that lane for a clear, unrelated reason: it ran behind the trunk, carried no unique work, and cost a publisher, a branch, and a forward-port obligation for no real benefit. Changing the versioning scheme doesn't change that reasoning, so this ADR keeps ADR-0008's decision instead of reopening it.

### D. Resume semver immediately at v11

Bump straight to `v11.0.0` now, since the calendar-versioning experiment is being abandoned anyway.

**Rejected.** There is nothing breaking to ship right now, so bumping the major would be a bump for its own sake — the same "major doesn't signal a real break" problem this ADR is trying to fix, just with a different number. Staying on `10.x` keeps every existing consumer's upgrade path exactly as smooth as it is today, and defers the real cost of a major bump (relearning, migration work) until there's an actual reason to pay it.

## References

- [ADR-0001: Release-branch model with tag-triggered multi-channel CD](0001-release-branch-model.md)
- [ADR-0002: v11 publishes to GitHub Packages by default; nuget.org opt-in](0002-v11-off-nuget-by-default.md)
- [ADR-0004: Calendar versioning + dual-pace channels](0004-calendar-versioning-and-dual-pace-channels.md) — replaced by this ADR
- [ADR-0007: Cut `release/YYYY` on demand](0007-cut-release-branch-on-demand.md) — kept, renamed onto `develop`/`release/vX.Y`
- [ADR-0008: Collapse `experimental` into `main`](0008-collapse-experimental-into-main.md) — kept, renamed onto `develop`
- [docs/branching-and-release.md](../branching-and-release.md) — maintainer runbook, updated for this model
- `AGENTS.md` and the `creating-a-pr` / `cutting-a-release` skills — agent-facing PR-flow and release-pipeline procedures, updated for this model
