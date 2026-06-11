# ADR-0004: Calendar versioning + dual-pace channels (edge / stable) + experimental APIs

## Status

Accepted (2026-05-29). **Amended (2026-05-30)** — see the amendment below; the channel model changed from "`main` is the edge channel" to a three-tier maturity ladder (`experimental` → `main` → `release/YYYY`). **Supersedes the versioning section of [ADR-0001](0001-release-branch-model.md) and extends its channel model**; the release-branch + tag-triggered multi-channel CD machinery from ADR-0001 and the nuget.org-opt-in policy from [ADR-0002](0002-v11-off-nuget-by-default.md) remain in force. Discussion thread: [#302](https://github.com/Fallout-build/Fallout/discussions/302).

> **⚠️ §2 (channel ladder) superseded by [ADR-0008](0008-collapse-experimental-into-main.md) (2026-06-18).** The dedicated `experimental` (`-alpha`) lane added by the 2026-05-30 amendment below has been **removed** — it ran behind `main`, carried no unique work, and cost a publisher + a branch + a forward-port obligation for no realised benefit. **`main` (`-preview`, GitHub Packages only) is now the sole prerelease lane**; breaking work batches on `main` behind `[Experimental("FALLOUT0xx")]` rather than on a separate branch. Everything else in this ADR — calendar versioning (§1), the production line + `[Experimental]` + review tiers (§3–§6) — is unchanged. The amendment text below is preserved as history; read it through the lens of ADR-0008.

> **§3 amended by [ADR-0007](0007-cut-release-branch-on-demand.md) (2026-06-08).** `release/YYYY` is now **cut on demand at the first release of the year, not preemptively from day one** — until then `main` (`-preview`) is the most-stable line. Everything else in §3 (the production tier, rc → GA, nuget.org-opt-in routing, rigorous review) is unchanged.

## Amendment (2026-05-30): three-tier channel ladder — `experimental` / `main` / `release`

The originally-accepted decision made **`main` itself the unstable edge channel**. Feedback from [@dennisdoomen on #302](https://github.com/Fallout-build/Fallout/discussions/302#discussioncomment) pushed back on principle-of-least-surprise grounds: a newcomer (or a consumer cloning the repo) expects `main` to be the *stable-ish* line that lands deliberate improvements and bug fixes — not the bleeding edge. This amendment adopts that, by **adding a dedicated fast lane below `main`** rather than making `main` the fast lane (i.e. it adopts a form of [Alternative A](#a-a-separate-long-lived-experimental--edge-branch), which the original decision rejected — see the revised rationale there).

What changed:

- **A new `experimental` branch** is the fast/AI lane (was: `main`). Per-commit `-alpha` prereleases to GitHub Packages only.
- **`main` becomes the integration trunk / `-preview` channel** — where work stabilises. Still GitHub Packages only, **never nuget.org**. It is no longer "intentionally unstable"; it is the deliberate trunk.
- **`release/YYYY` is unchanged in role** but now explicitly the *production* tier: it (and `support/*`) are the **only** lines that drive nuget.org.
- **Environment framing made explicit:** GitHub Packages = the **test/preview** channels (`experimental` `-alpha`, `main` `-preview`); nuget.org = **production** (`release/YYYY`, `support/*`).
- **Legacy + retired lines adopt gitflow `support/*` naming:** `release/v10` → `support/v10`; retired year trains become `support/YYYY` (also Dennis's suggestion).
- **A four-rung version ladder**, ordering cleanly under SemVer prerelease rules: `…-alpha.N` (experimental) < `…-preview.N` (main) < `…-rc.N` (release pre-GA) < `…` (release GA). `experimental` and `main` stay on the **same version core**; genuinely-breaking surface rides `[Experimental("FALLOUT0xx")]` and is batched to the yearly cut rather than bumping the core early (a next-year core on `experimental` would sort *above* `main`'s preview and invert the ladder).

The sections below are revised to this model; the original wording is preserved in git history. The trade this accepts (a bounded amount of `experimental → main` divergence) is discussed in [Consequences](#negative) and [Alternative A](#a-a-separate-long-lived-experimental--edge-branch).

**Rollout status (2026-05-30):** the *decision + documentation* landed first; the *CI + versioning mechanics* followed — `main`'s channel relabelled `-edge` → `-preview` (`edge.yml` → `preview.yml`), the `experimental` `-alpha` workflow added, `validate-ref` + the generated CI branch triggers extended to `experimental`/`support/*`, and `version.json` set to `2026.1.0-preview.{height}` with `support/*` in `publicReleaseRefSpec`. **Remaining (maintainer branch ops):** create the `experimental` branch from `main` and set its `version.json` to `…-alpha.{height}`; rename `release/v10` → `support/v10` (and update its `version.json` `publicReleaseRefSpec`); apply branch protection to `experimental` + `support/v10`. Until `experimental` exists, `experimental.yml` simply doesn't fire.

## Context

Fallout's contributor velocity is **bimodal**. A subset of maintainers work AI-assisted and ship a large volume of change per week. Other contributors hand-write code and move deliberately. Left unstructured, the two paces collide: either the fast lane destabilises the surface everyone else depends on, or the slow lane becomes a gate the fast lane has to wait behind. Neither is acceptable, and the project would rather not force one tempo on everyone.

The maintainer's framing (paraphrased): *yearly breaking majors are fine — a year just ships more; the AI crowd should be able to push hard on the next version; and we want a deliberately-unstable channel to release experimental work from, feeding the stabilised parts back into the regular channels. The slower crowd stabilises the released version and takes their time on reviews.*

What we already have (and keep):

- **[ADR-0001](0001-release-branch-model.md)** — `main` integration trunk (merges don't publish), `release/vN` channels, **tag-triggered** multi-channel CD via three GitHub Environments (`nuget-org`, `github-packages`, `github-releases`), cherry-pick-to-release hotfix flow, per-branch Nerdbank.GitVersioning.
- **[ADR-0002](0002-v11-off-nuget-by-default.md)** — nuget.org publish is opt-in (`workflow_dispatch` flag + env approval); GitHub Packages is the faster default channel; the noise that drove this was specifically **nuget.org Dependabot fan-out** into consumer repos.

What's missing for the dual-pace model:

1. A **published fast channel** for the AI crowd to release intentionally-unstable work from.
2. A **versioning scheme** that matches a yearly-breaking cadence and reconciles with a contributor (Dennis) who advocated gitflow + strict semver.
3. A **per-feature opt-in** for unstable public APIs that can ride *any* channel — so "experimental" need not mean a divergent fork.
4. **Explicit review tiers** so the fast lane isn't gated and the slow lane isn't steamrolled.

Crucial timing fact: **v11 never cleanly shipped.** The `11.0.x` packages were contaminated auto-publishes, since unlisted from nuget.org (the ADR-0002 era + the #268/#294/#298 unlist batches). There is no stable v11 consumer base to renumber, so adopting calendar versioning *now* is near-zero-cost.

## Decision

### 1. Calendar versioning: `YYYY.MINOR.PATCH`

Adopt CalVer immediately, retiring the v11 numbering. `main` becomes `2026.x`.

- The version is **mechanically valid SemVer 2.0** — all three components are numeric, so Nerdbank.GitVersioning, NuGet, and ordering all keep working unchanged. It *is* semver; the major simply happens to be the year. This is the reconciliation with the semver camp.
- **`MAJOR` = the calendar year.** **`MINOR`** = a feature drop within the year. **`PATCH`** = fixes (git-height, as today).
- **Breaking changes are allowed only at the yearly major cut.** Mid-year stable releases are strictly non-breaking: minor adds features, patch fixes bugs. Breaking work accumulates on `experimental` through the year and ships together as next year's `YYYY+1.0.0`. This is what gives the slow crowd a **stable API target for a whole year**, and it keeps the semver guarantee honest — a major bump always coincides with real breakage.

This replaces the old "any breaking change bumps the major in the same PR, any time" rule from ADR-0001 / `release-and-versioning.md`.

### 2. Three-tier channel ladder: `experimental` → `main` → `release/YYYY`

*(Amended 2026-05-30 — originally "`main` is the edge channel". See the amendment above.)*

Two **test/preview** lanes (GitHub Packages only, never nuget.org), feeding the **production** line:

- **`experimental`** — the fast / AI-assisted lane. Per-commit `-alpha` prereleases. **Intentionally unstable**; breaking work accumulates here for the yearly major. This is the dedicated fast lane (it replaces the original "`main` is edge" idea).
- **`main`** — the integration trunk where work stabilises. Per-commit `-preview` prereleases. GitHub Packages only — **never nuget.org**. Deliberate improvements and bug fixes land here; non-breaking work is promoted up from `experimental`.

Both are **non-public NB.GV refs**, so the build identifier (height + commit) lives in the **prerelease segment**, never the version core. As built: `experimental` → `2026.1.0-alpha.<height>.g<commit>`, `main` → `2026.1.0-preview.<height>.g<commit>`. A core like `2026.05.29` would parse as a *stable* `MAJOR.MINOR.PATCH` release, not a nightly — so the build id stays in the prerelease segment.

`experimental` and `main` share the **same version core** (e.g. both `2026.1.0`). Risky surface that isn't core-breaking ships behind `[Experimental("FALLOUT0xx")]`; genuinely-breaking work is batched to the yearly cut rather than bumping `experimental` to a next-year core (which would sort *above* `main`'s preview and invert the alpha < preview ladder).

GitHub Packages is opt-in for consumers (they add the feed), so neither lane causes the nuget.org Dependabot fan-out that ADR-0001 / ADR-0002 were avoiding.

### 3. `release/YYYY` = the production line

- Cut from `main`. The **slow crowd owns it**: hardening, `-rc.N` previews, then GA. After the cut it receives **non-breaking minors + patches only** — never a breaking change (those wait for next year's cut).
- This is the **production tier**: `release/YYYY` (and `support/*`) are the **only** lines that publish to nuget.org — still **opt-in** via the ADR-0002 flag + env approval — plus GitHub Packages + GitHub Releases. `main` and `experimental` never reach nuget.org.
- `release/2026` is cut now, even though `main`/`experimental` are still churning, so the slow crowd has something to own from day one.

### 4. Legacy + retired lines use gitflow `support/*` naming

*(Amended 2026-05-30 — `release/v10` → `support/v10`; retired year trains → `support/YYYY`.)*

- **`support/v10`** (renamed from `release/v10`; + `hotfix/v10.1`, `hotfix/v10.2`) stays on **semver `10.x`** as a **legacy maintenance line — security and critical fixes only, no new features.** It is not renumbered into CalVer. This is a hard requirement: existing v10 consumers keep their line. Adopting gitflow `support/*` (Dennis's suggestion on #302) makes the vocabulary consistent now that `experimental` ≈ gitflow's `develop`.
- **Retired year trains** (e.g. `release/2026` once 2027 supersedes it) become **`support/YYYY`** — same security/critical-only posture.
- **`release/v11`** is **retired** — nothing clean shipped under it, and the in-flight rebrand/plugin work it carried re-homes onto the `2026` line. It is *not* a maintained line, so it keeps its `release/v11` name (renaming to `support/` would wrongly imply active maintenance); kept for archaeology, marked EoL (per ADR-0001's "branches are cheap, don't delete" rule).
- `publicReleaseRefSpec` covers the production patterns: current-year CalVer (`^refs/heads/release/\d{4}$`), retired-year CalVer (`^refs/heads/support/\d{4}$`), and legacy semver (`^refs/heads/support/v\d+$`). `main` and `experimental` are deliberately **not** public refs (they carry `-preview` / `-alpha`).

### 5. `[Experimental]` for opt-in unstable APIs

Use `System.Diagnostics.CodeAnalysis.ExperimentalAttribute` with a `FALLOUT0xx` diagnostic-ID scheme to mark public APIs that may change without notice — including APIs shipped in a **stable** release.

- The C# compiler forces consumers to explicitly suppress the diagnostic to use an experimental API, so opting in is a conscious choice. Fallout is a *framework* (a product devs build on), not an app — letting devs decide per-API whether to take the risk is exactly right.
- **Promoting an experimental feature to stable = deleting the attribute.** Because the feature already rode the trunk, there is no cross-branch cherry-pick — this is what lets us "feed stabilised work back into the regular channels" without a divergent fork.
- **Discipline differs by channel:** on the `experimental`/`main` test lanes, churn is expected and the attribute is a courtesy. On the **production line** (`release/YYYY`), any risky-but-shipped surface **must** wear `[Experimental]` — that is the contract that lets the stable line stay trustworthy while still carrying new work.

### 6. Review tiers rise with the ladder

- **`experimental`:** light, fast review. Breakage is acceptable and cheap because no production consumer tracks the alpha lane.
- **`main`:** ordinary review — this is the deliberate trunk, so changes get a real look, but it isn't the production gate.
- **Promotion to `release/YYYY` + the GA cut:** rigorous, unhurried, human review — **the slow crowd's domain and the project's quality gate.** There is no clock on it, because `experimental` already served the impatient.

The net property: **the fast lane never blocks on slow review, and the slow lane is never steamrolled** — the thing they guard (production) is theirs to pace. This is as much the social fix as the technical one.

> Even on the fast lane, *requesting* review/feedback is encouraged (Dennis's point on #302): the faster the churn, the more a second reader keeps shared understanding from eroding. Light review ≠ no review.

### Channel summary

| Channel | Built from | Cadence | Version shape | Publishes to | Review tier |
|---|---|---|---|---|---|
| **alpha** | `experimental` | per-commit | `2026.1.0-alpha.<height>.g<commit>` | GitHub Packages (test) | light/fast |
| **preview** | `main` | per-commit | `2026.1.0-preview.<height>.g<commit>` | GitHub Packages (test) | ordinary |
| **rc** | `release/YYYY` pre-GA | per cut | `2026.1.0-rc.2` | nuget.org (opt-in) + GH Packages | rigorous |
| **stable** | `release/YYYY` tags | yearly major + non-breaking minor/patch | `2026.1.3` | nuget.org (opt-in) + GH Packages + GH Releases | rigorous |
| **legacy** | `support/v10` (+ `hotfix/v10.x`) | security/critical only | `10.x` (semver) | nuget.org (opt-in) + GH Packages | rigorous |
| **retired year** | `support/YYYY` | security/critical only | `YYYY.x` (CalVer) | nuget.org (opt-in) + GH Packages | rigorous |
| **`[Experimental]` APIs** | any channel | per-feature | rides the package | (the package) | opt-in by consumer |

Version ladder (SemVer prerelease ordering): `…-alpha.N` < `…-preview.N` < `…-rc.N` < `…` (GA).

## Consequences

### Positive

- **`main` is stable-by-default (principle of least surprise).** A newcomer or consumer landing on `main` finds the deliberate trunk, not the bleeding edge — the fast churn lives one rung down on `experimental`. This is the change the 2026-05-30 amendment makes.
- **A maturity ladder, not just two paces.** `experimental` (alpha) → `main` (preview) → `release/YYYY` (rc → GA) gives consumers a clear, SemVer-ordered choice of how much risk to take, with `[Experimental]` as the per-API opt-in on top.
- **Social fix is structural.** Velocity mismatch stops being a source of friction: fast contributors ship to `experimental` unblocked; deliberate contributors own `main` stabilisation and the production cut at their own tempo.
- **CalVer reconciles both camps.** It is real semver mechanically (Dennis keeps ordering + discipline), and breaking-batched-to-year-boundary keeps the major-signals-breaking guarantee — while delivering the maintainer's yearly-breaking cadence.
- **Near-zero adoption cost, taken now.** No clean v11 shipped, so renumbering to `2026` strands no one.
- **Legacy stays supportable.** v10 consumers keep their line; the existing release-branch CD handles it with no new machinery.

### Negative

- **A bounded amount of `experimental → main` divergence.** This is the cost the original decision avoided by making `main` itself the edge (see [Alternative A](#a-a-separate-long-lived-experimental--edge-branch)). It is accepted here, and kept *bounded* because: most work is non-breaking and is promoted to `main` promptly (so `main` tracks `experimental` closely); only genuinely-breaking work is held on `experimental` until the yearly cut; and `[Experimental("FALLOUT0xx")]` lets risky-but-shippable surface ride as non-breaking, shrinking the held-back set. Promotion is forward-only (`experimental → main → release/YYYY`); a stable-urgent fix lands on `main` and is forward-ported to `experimental` so the fast lane never regresses.
- **Two GitHub Packages publishers now** (`experimental` alpha + `main` preview) rather than one. A GH Packages outage blocks both test lanes; idempotent `--skip-duplicate` retries keep this cheap, but the dependency is real.
- **Mid-year, a year bump with no breaking changes would "waste" a semver major signal.** Mitigated by the rule that breaking changes *are* batched to the year boundary, so a major bump always means real breakage. An emergency mid-year break is the rare exception and would be documented loudly.
- **`publicReleaseRefSpec` now matches three production patterns** (`release/\d{4}`, `support/\d{4}`, `support/v\d+`) and still needs per-branch tuning on each new `release/YYYY` cut. Documented in the runbook.
- **Contributors must learn `[Experimental]`.** A new convention to internalise; analyzer enforcement + docs mitigate.
- **Docs churn.** `branching-and-release.md`, `release-and-versioning.md`, and `AGENTS.md` all change in the wake of this ADR.

### Neutral

- **ADR-0001's branch/channel/CD model and ADR-0002's nuget.org-opt-in policy are unchanged.** This ADR changes the *versioning scheme*, adds the *test lanes* (`experimental` alpha + `main` preview, both GitHub Packages only), and adds the *`[Experimental]` + review-tier* conventions. The three-environment fan-out, tag-triggered trigger, and cherry-pick hotfix flow all carry over.
- **`target/vN` labels become `target/YYYY`** (`target/2026`, …). Legacy work keeps `target/v10`. Milestones remain theme-based.

## Alternatives considered

### A. A separate long-lived `experimental` / `edge` branch

A standing branch where the AI crowd works fast, promoting stabilised work to `main`.

**Originally rejected, then adopted (2026-05-30 amendment).** The original decision rejected this to avoid continuous `experimental → main` divergence + cherry-pick-back merge-hell, instead making `main` *itself* the edge channel. The [#302](https://github.com/Fallout-build/Fallout/discussions/302) feedback showed that cure was worse than the disease: an unstable `main` violates principle-of-least-surprise for the whole community, every day, to avoid a divergence cost that turns out to be **bounded** in practice (most work is non-breaking and promotes promptly; `[Experimental]` carries risky surface as non-breaking; promotion is forward-only). The amendment therefore adopts the dedicated `experimental` lane — but keeps the divergence-minimising tools (`[Experimental]`, same-core, forward-only promotion) that made the original objection answerable. See the [amendment](#amendment-2026-05-30-three-tier-channel-ladder--experimental--main--release) and [Consequences → Negative](#negative).

### B. Keep semver `vN` majors

Stay on `11.x`, `12.x`, bumping major on any breaking change at any time.

**Rejected because** it doesn't deliver the yearly cadence the maintainer wants, and "bump major whenever" makes the major number arbitrary rather than a calendar landmark the whole community can plan around. CalVer gives a predictable annual breaking window.

### C. Gitflow with a permanent `develop`

Dennis's suggestion: `develop` for integration, `main` for stable, `release/*` + `support/*` + `hotfix/*`.

**Substantially adopted (2026-05-30 amendment).** The amended model *is* gitflow with the project's vocabulary: `experimental` ≈ gitflow's `develop` (fast integration), `main` ≈ gitflow's stable trunk (preview), `release/YYYY` ≈ gitflow `release/*` (production staging, here long-lived per year), `support/*` for legacy/retired lines. The one deliberate deviation from textbook gitflow: **`main` is not the production/nuget.org line** — `release/YYYY` + `support/*` are. `main` is a `-preview` test channel that production is cut from. This keeps consumers off an as-yet-unstabilised `main` while still making it the least-surprise default branch.

### D. Date as the version core (`YYYY.MM.DD`)

Make daily builds literally `2026.05.29`.

**Rejected because** it collides with the CalVer core semantics — `2026.05.29` reads as a *stable* `MAJOR.MINOR.PATCH`, not a nightly — and there's no room left for minor/patch within a year. The build identifier lives in the **prerelease segment** instead (the `…-alpha.<height>.g<commit>` / `…-preview.<height>.g<commit>` forms — see §2).

## References

- [ADR-0001: Release-branch model & multi-channel CD](0001-release-branch-model.md) — parent; versioning section superseded here, channel model extended.
- [ADR-0002: v11 off nuget.org by default](0002-v11-off-nuget-by-default.md) — nuget.org-opt-in policy, retained.
- [docs/branching-and-release.md](../branching-and-release.md) — maintainer runbook (updated for this model).
- [docs/agents/release-and-versioning.md](../agents/release-and-versioning.md) — agent-facing branching/versioning/PR-flow reference (updated for this model).
- Discussion thread: [#302 — Calendar versioning + dual-pace channels (feedback)](https://github.com/Fallout-build/Fallout/discussions/302).
