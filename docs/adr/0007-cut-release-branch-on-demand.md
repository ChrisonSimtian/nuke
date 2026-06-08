# ADR-0007: Cut `release/YYYY` on demand, not preemptively

## Status

Accepted (2026-06-08). **Amends [ADR-0004](0004-calendar-versioning-and-dual-pace-channels.md) §3 and the branching section of [ADR-0001](0001-release-branch-model.md).** The tag-triggered multi-channel CD, the three GitHub Environments, `validate-ref`, `publicReleaseRefSpec`, the Nerdbank.GitVersioning prepare-release mechanics, and the dual-pace social model all remain in force — only the *lifecycle* of the production branch changes.

## Context

ADR-0004 §3 decided *"`release/2026` is cut now, even though `main`/`experimental` are still churning, so the slow crowd has something to own from day one."* In practice that branch was created preemptively and sat idle: **18 commits behind `main`**, carrying only its own org-repoint chore (#347, already mirrored onto `main` via #345), with **nothing released from it**.

Releases are already **tag-triggered** — `release.yml` fires on a `v*` tag push and `validate-ref` confirms the tag is reachable from a production branch. The release branch's only job is to be the thing you tag from and harden on. A *preemptive* release branch is therefore all cost, no benefit until a release is actually in flight:

- it drifts behind `main`;
- it implies a production line exists when nothing has shipped;
- it carries branch protection + CI-trigger upkeep for no active work;
- `publicReleaseRefSpec` already matches it, so it can mint stray height-based versions.

Maintainer framing (Chris): *"release/2026 should not really be there unless we actually do a release."*

**nbgv mechanics (verified empirically against `version.json` @ Nerdbank.GitVersioning 3.7.115):** a clean GA `YYYY.MINOR.PATCH` requires **both** (1) the release commit's `version.json` to carry a *stable* version with the prerelease label dropped — which is exactly what `nbgv prepare-release` does — **and** (2) the build to run on a public-release ref. `publicReleaseRefSpec` / public-release status alone only strips the `+g<commit>` build metadata; it never removes the `-alpha`/`-preview` label, which is intrinsic to the `version` field. (Proof: a public-release build of the current `version.json` still emits `2026.1.0-alpha.<height>`; a build whose `version.json` carries a stable `2026.1.0` on a public ref emits a clean `2026.1.0`.) This is **why GA is cut from a release branch** — where `prepare-release` runs — and it is unchanged by this ADR.

## Decision

1. **`release/YYYY` is cut on demand, at the first release of the year** (the `-rc` → GA window) — not preemptively at year start.
2. **Until that first cut, the most-stable consumable line is `main`** (`-preview`, GitHub Packages). A year has no production branch until that year ships something.
3. **The cut flow is unchanged from ADR-0001:** branch from `main` → `nbgv prepare-release` (stable `version.json` on the branch, bump `main`) → `-rc.N` tags → GA tag → publish. Trigger, `validate-ref`, environments, `publicReleaseRefSpec`, and the clean-GA mechanics all stay as-is.
4. **When the year is superseded, the branch becomes `support/YYYY`** (already ADR-0004 §4).
5. **The current preemptive `release/2026` is deleted.** Nothing shipped from it; its one unique commit is already on `main`. A 2026 production branch will be cut fresh from `main` when 2026 actually releases.
6. **Dead branches with no unique reachable history may be deleted** (e.g. the retired `release/v11`, already removed). This relaxes ADR-0001's *"branches are cheap, don't delete"* stance. **Tags remain the durable release markers** — `git tag` tells you what shipped, not the branch list.

## Consequences

### Positive
- **No idle, drifting production branch.** The branch list reflects reality: a `release/YYYY` exists iff that year has an in-flight or shipped release.
- **No protection / CI-trigger upkeep** for a branch doing nothing.
- **"Most-stable line" is unambiguous:** `main` until a release exists, then `release/YYYY`.
- **The dual-pace social model is untouched** — `-rc` staging and the rigorous production review gate still live on `release/YYYY`; it just blinks into existence when needed.

### Negative
- The slow crowd no longer has a standing branch to own *from day one* — they own it *from first-rc*. ADR-0004's day-one-ownership rationale is dropped (there was nothing to own until an rc existed anyway).
- A fresh `release/YYYY` cut still needs its one-time setup (branch protection; `prepare-release`). Same per-cut cost ADR-0001 documented, deferred to release time.
- *"Where do I get stabilised bits before the first GA?"* → `main`'s `-preview` channel. Slightly less obvious than a named branch, but the honest answer.

### Neutral
- `version.json`, `release.yml`, and the GitHub Environments are **unchanged**. `publicReleaseRefSpec` already matches `release/\d{4}`; an on-demand branch matches the moment it's cut.
- Releases were already tag-based; this ADR changes only the branch lifecycle, not the trigger.

## Alternatives considered

### A. Pure trunk-tag — tag rc + GA on `main`, no release branch ever
**Rejected.** Couples in-progress `main` development with release hardening, and forces `main` to absorb the rigorous production review gate — inverting ADR-0004's review-tier separation (`main` = ordinary, production = rigorous). The on-demand branch keeps that separation while still removing the preemptive branch.

### B. Keep the preemptive `release/YYYY` (status quo, ADR-0004 §3)
**Rejected.** The branch sat idle and drifted; "own from day one" delivered no value while costing protection, upkeep, and a misleading branch.

## References
- [ADR-0001](0001-release-branch-model.md) — branching section amended here.
- [ADR-0004](0004-calendar-versioning-and-dual-pace-channels.md) §3 — amended here.
- [docs/branching-and-release.md](../branching-and-release.md), [docs/agents/release-and-versioning.md](../agents/release-and-versioning.md), [AGENTS.md](../../AGENTS.md) — updated for the on-demand lifecycle.
