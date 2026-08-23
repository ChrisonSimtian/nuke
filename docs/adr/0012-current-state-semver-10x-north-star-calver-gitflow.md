# ADR-0012: Current state is semver `10.x`; North Star is CalVer + GitFlow

## Status

Accepted (2026-08-16). **Amends [ADR-0004](0004-calendar-versioning-and-dual-pace-channels.md) §1 (calendar versioning) by deferring it** — CalVer remains the intended destination, but is no longer described as current. Establishes **GitFlow as the branching North Star**, replacing the GitLab Flow the runbook previously claimed to follow.

ADR-0004 §3–§6 (production line, `[Experimental]` opt-in, review tiers), [ADR-0007](0007-cut-release-branch-on-demand.md) (on-demand release-branch cut), and [ADR-0008](0008-collapse-experimental-into-main.md) (`main` as sole prerelease lane) remain in force **for the current state**, re-expressed here in `release/vMAJOR.MINOR` terms. See §Consequences for how the North Star collides with ADR-0008.

## Context

ADR-0004 decided calendar versioning in 2026. Ten months on **it was never implemented**, and the gap between the written decision and the shipping reality had become actively harmful.

What the repository actually looks like:

- **`version.json` on `main` says `10.5.0-preview.{height}`.** It has never carried a CalVer core in anger; the one commit that flipped it (`e05d9af8`) was superseded back to the 10.x line.
- **No `release/2026` branch was ever cut. No `2026.x` tag was ever pushed.** The only production branches in existence are `release/v10.4` and `support/v10`.
- **The most recent GA is `v10.4.0`** (2026-08-07), cut from `release/v10.4` — a `release/vMAJOR.MINOR` branch, not the `release/YYYY` pattern ADR-0004 specified.
- `AGENTS.md`, `docs/agents/release-and-versioning.md`, `docs/branching-and-release.md`, and the header comment in `publish-packages-preview.yml` all describe a `2026.1.0` CalVer world that does not exist.
- The runbook opened with "We aim to follow GitLab Flow", which was never an accurate description of the intent either.

The versioning *practice* is in good shape and self-corrected without the docs' help. The preview lane did strand once — `main` sat on an old core while newer releases shipped, so its previews sorted below everything on the shelf — but that was fixed in the ordinary course of cutting a release: `v10.4.0` was tagged at 10:28:56 on 2026-08-07 and `eeeca700` ("Move main's preview lane onto the 10.5 core") landed twenty seconds later. The prerelease counter went through the same correction: `-rc.1`/`-rc.3` were `{height}`-driven and jumped to `rc.23` on a 19-commit promotion, after which the field was pinned literally and has been ever since.

**None of that learning is written down anywhere.** It lives in commit messages and in the maintainers' heads, while the documents that a contributor or agent would actually read describe a calendar-versioned repository that has never existed. That is the defect this ADR addresses.

The root cause is not the version number. It is that **the documentation carried aspiration and current state in the same voice**, so a reader — human or agent — could not tell which statements were load-bearing. Every doc claim was equally assertive, and the false ones went unnoticed for months. The practice outran the writing, and the writing had no slot to put "where we're going" that wasn't indistinguishable from "where we are".

## Decision

**Separate the two, explicitly and permanently.**

### 1. Current state: Semantic Versioning on the `10.x` line

Ratifying what the repository already does, rather than changing it:

- **`main` is the preview lane at `10.5.0-preview.{height}`** — the next unreleased minor after `10.4.0` GA, so previews sort above everything shipped.
- **Production lines are `release/vMAJOR.MINOR`** (`release/v10.4`, next `release/v10.5`), cut on demand at the first release of the line (ADR-0007 unchanged).
- **A release branch pins the full version literally**, prerelease segment included — `"10.4.0-rc.4"`, then `"10.4.0"` at GA. `{height}` is not used there; it made the `rc` counter track promotion size instead of release intent.
- **`support/vMAJOR` remains the legacy maintenance pattern** (`support/v10`, pinned `"10.3"` with `versionHeightOffset: 24`).
- **Rolling `main`'s core forward is a required step of cutting a release line**, done in the same sitting as the GA tag.
- Breaking changes are batched to *the next major*, on no fixed calendar.

One actual change: **`nbgv`'s `release.branchName` becomes `release/v{version}`.** It was `release/{version}`, which would have generated `release/10.5` — missing the `v` that every existing branch and the `validate-ref` job's pattern both expect.

### 2. North Star: calendar versioning + GitFlow

Recorded as direction, not as fact. Nothing below is implemented.

- **CalVer `YYYY.MINOR.PATCH`** — production lines become `release/YYYY`, retired years become `support/YYYY`, breaking changes batch to the yearly cut. ADR-0004's rationale stands; only its timing changes.
- **Full GitFlow**, including a long-lived **`develop`** as the integration trunk and preview lane, with `main` holding released code only. `release/v*` for stabilisation, `hotfix/v*` off `main`, `feature/*` off `develop`.
- **Routine `[Experimental("FALLOUT0xx")]` gating** of all breaking surface before it lands — the discipline ADR-0008 assumed when it retired the `experimental` branch. The mechanism is current; the practice is not (one usage, `FALLOUT001` on `IPublish`).

### 3. Documentation rule

**Every document describing process carries a `Current state` section and a `North Star` section, and never blends them.** Current state is kept true — drift from the repository is a bug, not a stale doc. North Star is explicitly unimplemented.

This applies to [docs/versioning.md](../versioning.md), [docs/branching-and-release.md](../branching-and-release.md), [AGENTS.md](../../AGENTS.md), and [docs/agents/release-and-versioning.md](../agents/release-and-versioning.md).

### Channel summary — current state (revising ADR-0008's table)

| Channel | Built from | Cadence | Version shape | Publishes to | Review tier |
|---|---|---|---|---|---|
| **preview** | `main` | per-commit | `10.5.0-preview.<height>.g<commit>` | GitHub Packages (test) | ordinary |
| **rc** | `release/v10.5` pre-GA | per cut | `10.5.0-rc.2` | nuget.org (opt-in) + GH Packages | rigorous |
| **stable** | `release/v10.5` tags | non-breaking minor/patch | `10.5.3` | nuget.org (opt-in) + GH Packages + GH Releases | rigorous |
| **legacy** | `support/v10` | security/critical only | `10.3.x` | nuget.org (opt-in) + GH Packages | rigorous |
| **`[Experimental]` APIs** | any channel | per-feature | rides the package | (the package) | opt-in by consumer |

## Consequences

### Positive

- **The documentation describes the repository again.** One version scheme, matching `version.json`, the branches that exist, and the tags actually pushed.
- **Hard-won operational knowledge is finally written down.** The preview-core ordering rule and the `-rc.N` pinning rule were both learned by shipping the bug and fixing it, and both lived only in commit messages. [docs/versioning.md](../versioning.md) now carries them as named traps with the evidence attached.
- **Continuity with everything shipped.** `10.1.x` → `10.3.x` → `10.4.0` → `10.5.0` reads as one history; a CalVer jump to `2026.1.0` would have been a discontinuity in every consumer's upgrade path.
- **Aspiration is preserved rather than deleted.** ADR-0004's reasoning was never refuted — it was only ever unimplemented. The North Star section keeps it visible and actionable instead of quietly dropping it.
- **The failure mode is now structural rather than a matter of vigilance.** A claim in a Current state section is falsifiable against the repo; the drift that caused this ADR would have been caught by reading one table.

### Negative

- **The North Star collides with [ADR-0008](0008-collapse-experimental-into-main.md), which will need superseding when we adopt GitFlow.** ADR-0008 decided `main` is the sole prerelease lane and steady state is `main` + `support/*`. Full GitFlow reintroduces a long-lived `develop` carrying exactly the preview lane ADR-0008 consolidated onto `main` — and reintroduces the forward-port obligation ADR-0008 removed after `experimental` drifted ~17 commits behind. **This is the same failure mode that killed `experimental`**, and adopting GitFlow means answering for it explicitly, not by omission.
- **CalVer and GitFlow have to land together or not at all.** Under GitFlow the `{height}` core and `publicReleaseRefSpec`'s exclusion both move from `main` to `develop`. Sequencing them separately means two migrations of the same fields.
- **Cadence is not legible from the version number** until CalVer lands, which was one of ADR-0004's stated goals. Accepted in the interim: the `-preview`/`-rc`/GA ladder and release notes carry that signal.
- **ADR-0004 is now qualified by three ADRs** — §2 by ADR-0008, §1 by this one — leaving only §3–§6 in force. It is past the point where a consolidated replacement would be clearer than the amendment chain.

### Neutral

- ADR-0007 (on-demand cut) is unchanged in substance; only the branch-name pattern it references changes from `release/YYYY` to `release/vMAJOR.MINOR`, and reverts under the North Star.
- `publicReleaseRefSpec` keeps its `\d{4}` CalVer patterns. They match nothing today and cost nothing; a future CalVer adoption needs no change to that field.
- **The burned `11.0.x` range is a footnote, not an open decision.** `11.0.1`–`11.0.18` were published then unlisted, and NuGet never frees a version that has existed. Under the CalVer North Star the next major is the yearly cut (e.g. `2027.0.0`), which never touches the `11.x` space.
- Labels `target/vCurrent` / `target/vNext` are already evergreen and need no change. `target/2026` exists but is unused.

## Alternatives considered

### A. Adopt calendar versioning now — `main` → `2026.1.0-preview.{height}`

Finally implement ADR-0004: move `main` to a `2026.1.0` core, cut `release/2026` on demand, leave `10.x` to `support/v10`.

**Rejected for now, retained as North Star.** It would have made ADR-0004 true and neatly sidestepped the burned `11.0.x` space. But it buys those with a version discontinuity for every consumer, a second branch-naming migration in one year, and — critically — it pairs with a GitFlow restructure that is a far larger change than a version-core bump. Bundling a live migration into a documentation-correction PR is how the original drift happened: ADR-0004 was written and the implementation never followed. Correct the record first; adopt the North Star as its own deliberate piece of work.

### B. Change `version.json` as part of this ADR

An earlier draft of this ADR treated the preview core as an open defect and proposed moving it. That was based on a **stale fork checkout** reading `10.0.0-preview.{height}`; `upstream/main` had already moved to `10.5.0-preview.{height}` at the 10.4.0 GA.

**Withdrawn — there was nothing to fix.** Recorded because the mistake is instructive: the version core was correct and the *documentation* was wrong, which is the exact inversion this ADR exists to prevent. A reader who trusted the docs over the repo would have "fixed" a working lane. (The `release.branchName` typo in §1 is a genuine, separate change.)

### C. Delete the CalVer aspiration entirely

Retire ADR-0004 §1 outright and commit to semver indefinitely.

**Rejected.** Nothing about ADR-0004's reasoning was shown to be wrong; it was shown to be unscheduled. Deleting it would discard a considered decision because of an implementation gap, and the burned `11.0.x` range makes a future semver major genuinely awkward in a way CalVer avoids. The North Star section exists precisely so aspiration can be kept without being mistaken for fact.

## References

- [ADR-0004: Calendar versioning + dual-pace channels](0004-calendar-versioning-and-dual-pace-channels.md) — §1 deferred here, retained as North Star; §2 previously superseded by ADR-0008; §3–§6 in force.
- [ADR-0008: Collapse `experimental` into `main`](0008-collapse-experimental-into-main.md) — in force for current state; **will need superseding** if the GitFlow North Star is adopted.
- [ADR-0007: Cut the release branch on demand](0007-cut-release-branch-on-demand.md) — reaffirmed, re-expressed for `release/vMAJOR.MINOR`.
- [docs/versioning.md](../versioning.md) — living document: nbgv mechanics, height, the two production traps.
- [docs/branching-and-release.md](../branching-and-release.md) — maintainer runbook.
