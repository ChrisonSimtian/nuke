# Release and versioning

Branching, semver policy, the PR-creation procedure, and the release pipeline.

> [!IMPORTANT]
> **This document describes CURRENT STATE.** The project's North Star — calendar versioning and full GitFlow — is **not implemented**, and is recorded separately in [branching-and-release.md → North Star](../branching-and-release.md#north-star) and [versioning.md → North Star](../versioning.md#north-star). Do not write code, docs, or PR descriptions as though either is in force. See [ADR-0012](../adr/0012-current-state-semver-10x-north-star-calver-gitflow.md).

## Branching

The branch/channel/versioning model is defined by [ADR-0012](../adr/0012-current-state-semver-10x-north-star-calver-gitflow.md) (semver `10.x` now; CalVer + GitFlow as North Star), which defers [ADR-0004](../adr/0004-calendar-versioning-and-dual-pace-channels.md) §1. It builds on [ADR-0008](../adr/0008-collapse-experimental-into-main.md) (which collapsed the `experimental` lane into `main` and retired the `-alpha` channel), [ADR-0001](../adr/0001-release-branch-model.md) (release-branch + tag-triggered multi-channel CD), and [ADR-0002](../adr/0002-v11-off-nuget-by-default.md) (nuget.org opt-in).

A two-tier maturity ladder (`main` → `release/v<major>.<minor>`) feeding the production line. GitHub Packages = test/preview; nuget.org = production. Long-lived branches:

- `main` — the **integration trunk *and* the sole prerelease lane.** Default branch. **Both** deliberate improvements + bug fixes **and** faster/AI-assisted work land here. Every push publishes an NB.GV-native prerelease `MAJOR.MINOR.PATCH-preview.<height>.g<commit>` (currently `10.5.0-preview.<height>.g<commit>`) to **GitHub Packages only — never nuget.org.** Ordinary review.
- `release/v<major>.<minor>` (currently `release/v10.4`; next `release/v10.5`) — the **production line**. **Cut from `main` on demand at the first release of the line, not preemptively** ([ADR-0007](../adr/0007-cut-release-branch-on-demand.md)); until then `main` (`-preview`) is the most-stable line. Hardened deliberately (slow crowd's domain, rigorous review), `-rc.N` → GA. After the cut it takes **non-breaking minors + patches only** — never a breaking change. Tag-triggered releases fire from here (the nuget.org tier). Protected per the policy below.
- `support/v10` (+ `hotfix/v10.x`) — **legacy maintenance line**, pinned `"10.3"` with `versionHeightOffset: 24`, **security and critical fixes only, no new features** (renamed from `release/v10`). Coexists indefinitely and does **not** retire when a newer line is cut.
- `release/v11` — **retired.** Nothing clean shipped under it (the `11.0.x` packages were unlisted); its rebrand/plugin work re-homed onto the `10.x` line. Not a release target. **`11.0.1`–`11.0.18` are burned** — nuget.org never frees a version that has existed, so that range can never be reused.

Short-lived branches (rebase-merged via PR): `feature/<slug>`, `bugfix/<slug>`, `chore/<slug>`, `docs/<slug>`, `pr/<num>-<slug>`. They target `main`. Breaking work that cannot be gated behind `[Experimental("FALLOUT0xx")]` waits for the next major cut on a short-lived topic branch off `main`.

No `develop` (literal) or `master` branches **today** — note that the GitFlow North Star would introduce `develop`, but it is not implemented. The ladder flows **forward-only**: `main → release/v<major>.<minor>`. The `support/*` lines are maintenance-only — security/critical fixes land via a PR targeting (or cherry-pick to) `support/v10` (or the relevant `hotfix/v10.x`) and are tagged from there.

CI providers in use: **GitHub Actions only** (others were dropped — see [#8](https://github.com/Fallout-build/Fallout/issues/8) for the demand-driven revival roadmap).

### Branch protection on `release/*` and `support/*`

`main`, every release line, and every `support/*` branch share `main`'s protection profile:

- Required status check: `ubuntu-latest`
- Linear history required (no merge commits)
- CODEOWNER review required (0 additional approvals)
- Direct pushes blocked (PRs only)
- Force-push and branch deletion blocked
- Conversation resolution required
- Admins not enforced (admins can bypass in emergencies)

Stale approvals are **not** dismissed when new commits land (`dismiss_stale_reviews: false`).

**How it's applied differs by branch:**

- **Release lines** (`release/v10.4`, a future `release/2027`) — covered by the pattern-based ruleset on `refs/heads/release/**` ([19766406](https://github.com/Fallout-build/Fallout/rules/19766406)), so protection attaches automatically at branch creation. Payload committed at `.github/release-branch-ruleset.json`. **Nothing to apply by hand.**
- **`main` and `support/*`** — classic per-branch protection, configured individually.

Tag protection for `v*` tags is a separate ruleset ([17017817](https://github.com/Fallout-build/Fallout/rules/17017817)).

**Validation workflows.** `build.yml` runs Test+Pack on Linux on every PR targeting `main`, `release/*`, or `support/*` (with `paths-ignore` for `docs/**`, `.assets/**`, `**/*.md`); its job `ubuntu-latest` is the only required status check. `build-cross-platform.yml` runs Test+Pack on Windows and macOS (one job each) only on PRs targeting `release/*` / `support/*` and on `v*` tag pushes — cross-platform is gated to release intent, not routine `main` work. This is a deliberate cost trade-off. (Both workflows are **generated** from `build/Build.CI.GitHubActions.cs` — change the branch lists in the `MainBranch`/`*BranchPattern` constants there and regenerate, don't hand-edit the `.yml`. The `build-skip.yml` no-op shim reports the `ubuntu-latest` check on docs-only PRs.)

**Merging.** Rebase merge only. Plain merge commits are disabled by repo setting; **squash is still enabled at the repo level**, so on release branches the convention — not the setting — is what keeps squashes out. Squashing a promotion would collapse it into one opaque commit, defeating the point of promoting reviewed commits verbatim. Every reviewed commit lands on `main` verbatim, so curate commits into a clean sequence before final approval. See [CONTRIBUTING.md → Merging](https://github.com/Fallout-build/Fallout/blob/main/CONTRIBUTING.md#merging) for the convention.

## Versioning

**Semantic versioning on the `10.x` line.** Full mechanics — git height, `-rc.N` pinning, and the two traps that have shipped bugs — are in **[docs/versioning.md](../versioning.md)**. Read that before touching `version.json` or cutting a release. The essentials:

- Per-branch via `version.json`. The preview lane is a **non-public ref** carrying the next planned version with a prerelease tag: `main` → `"10.5.0-preview.{height}"` (`firstUnstableTag` is `preview`). A release branch pins the **full** version literally, prerelease segment included — `"10.4.0-rc.4"`, then `"10.4.0"` at GA. `support/v10` carries `"10.3"` + `versionHeightOffset: 24`.
- `publicReleaseRefSpec` matches the production branch patterns (**not** `main`). It also retains `\d{4}` CalVer patterns that match nothing today — harmless, and they mean a future CalVer adoption needs no change to that field.
- Preview-lane builds carry the height + commit in the **prerelease segment** (`10.5.0-preview.<height>.g<commit>`), never the version core. `main` is a non-public ref, so NB.GV appends the `.g<commit>` suffix. The ladder orders cleanly: `-preview` < `-rc` < GA.
- **`{height}` is automatic and preview-only.** It resets whenever the `version` field changes. `-rc.N` is bumped by hand — driving it from height made the counter track promotion size, sending `rc.3` to `rc.23`.
- **Two traps** (both have shipped bugs — see [versioning.md](../versioning.md)): `main`'s preview core must be rolled forward in the same sitting as a release cut, or previews strand below the shipped release; and tag builds run on a detached HEAD that matches no `publicReleaseRefSpec` entry, which is why the release workflow sets `PublicRelease: true`.

Versioning is Nerdbank.GitVersioning only — GitVersion is no longer referenced.

## Versioning policy

This project ships [Semantic Versioning](https://semver.org/spec/v2.0.0.html). The rule is: **breaking changes are batched to the next major cut.**

There is **no `CHANGELOG.md`** — the file was retired. Release notes are generated from PR labels via [`.github/release.yml`](https://github.com/Fallout-build/Fallout/blob/main/.github/release.yml), so the PR description and its labels are the durable record of a change.

- A breaking change lands on **`main`, gated behind `[Experimental("FALLOUT0xx")]`** (or, when it can't be gated, on a short-lived topic branch off `main` held until the cut), is held for the next major (it does **not** bump `version.json`'s major mid-line), and describes its migration path in the PR description under a `⚠️ Breaking change` callout.
- **A `release/v<major>.<minor>` production line never takes a breaking change** — it's strictly non-breaking (minor = features, patch = fixes). The production-cut review is the backstop that keeps ungated breaking work off the production line (ADR-0008).
- Surface that isn't ready to commit to can ship behind `[Experimental("FALLOUT0xx")]` instead of being held back — opt-in for consumers, and not a breaking change to add or remove. With the `experimental` branch retired, the attribute is now the primary per-API isolation tool.

A "breaking change" is any of:

- A conventional-commit subject with the `!` suffix (e.g. `feat(globaltool)!: …`, `fix(security)!: …`).
- A `BREAKING CHANGE:` footer in the commit body.
- A change a reviewer reasonably flags as breaking even without the marker (renamed/removed public API, package ID change, on-disk format change, CI/CD shape change consumers depend on) — **except** changes to `[Experimental]` surface, which carry no stability guarantee.

**Reviewer responsibility:** if a PR carries `!` (or a flagged breaking change), confirm it targets `main` (not a production train), that the breaking surface is gated behind `[Experimental("FALLOUT0xx")]` (or held on a topic branch when it can't be gated), and that the PR description carries the `⚠️ Breaking change` callout with a migration path. Block otherwise. The production-cut review is the backstop for any ungated breaking change reaching a production cut.

## Milestones and version targeting

Milestones are **theme-based** (e.g. "Plugin Architecture Foundation & Rebrand Completion", "Public Plugin SDK", "Continuous Delivery Vision") and carry across releases; version targeting uses **evergreen `target/vCurrent` / `target/vNext`** labels — `target/vCurrent` is the current release line, `target/vNext` is the next major. A breaking change is held for the next major — so its PR carries `target/vNext`.

## PR-creation flow

Write the PR description terse and to the canonical shape — see
[issue-and-pr-style.md](issue-and-pr-style.md). At PR-creation time — not after,
not as a follow-up — every PR gets:

0. **Working from a fork? Branch from `upstream/main`, push to `origin`, PR against `upstream`.** If `git remote -v` shows both an `origin` (a personal fork, e.g. `<you>/Fallout`) and an `upstream` remote pointing at `Fallout-build/Fallout`, treat `upstream` as the core repo: `git fetch upstream main` and branch from `upstream/main` (never `origin/main`, which can be arbitrarily stale relative to the core repo and will produce huge, spurious merge conflicts), push the new branch to `origin`, and open the PR against `upstream` — `gh pr create --repo Fallout-build/Fallout --draft ...`. Do this by default unless the user explicitly asks to branch off the fork's own `main` instead. Doesn't apply to a plain single-remote clone.
1. **Create the PR as a draft** — `gh pr create --draft` (see [issue-and-pr-style.md](issue-and-pr-style.md#pr-description-shape)) unless the user explicitly asks for a ready-for-review PR. This is easy to miss because it's a small flag on the same `gh pr create` call as the labels below — don't drop it.
2. **A `target/vCurrent` or `target/vNext` label** matching where it will release. Default to `target/vCurrent` (the current release line). If the PR carries a breaking change, it's held for the next major — use `target/vNext`. Pass via `--label target/vCurrent` to `gh pr create`.
3. **A changelog-category label** describing the change, from [`.github/release.yml`](../../.github/release.yml) — that file is the source of truth for the taxonomy and carries a one-line blurb on each label. Apply the one category the PR belongs under (`enhancement`, `bug`, `security`, `documentation`; `breaking-change` when it applies — see below), or `skip-changelog` for housekeeping with no release note. Pass it in the same `gh pr create --label …` call. Don't leave a PR uncategorized — it falls through to "Other Changes". This is the labelling AI applies on the user's behalf whenever it raises a PR.

If the PR includes a **breaking change** (any commit uses `!`, has a `BREAKING CHANGE:` footer, or otherwise meets the breaking-change definition above), additionally:

4. **Add the `breaking-change` label** (this is its changelog category — use it instead of `enhancement`/`bug`). `gh pr create --label target/vNext --label breaking-change …`.
5. **Open the PR body with a `⚠️ Breaking change` callout** that names the affected surface (public API, package ID, CLI flag, on-disk format, CI/CD shape, etc.) and the consumer-side impact in one sentence. This is what reviewers and downstream consumers read first.
6. **Confirm the PR targets `main`, not a `release/v<major>.<minor>` production train, and that the breaking surface is gated behind `[Experimental("FALLOUT0xx")]`** (or, when it can't be gated, lives on a short-lived topic branch off `main` held until the cut). Breaking changes accumulate on `main` for the next major; they may not land on a production train. (Do **not** bump `version.json`'s major in the PR — the major is set once, at the cut.)
7. **Spell out the migration path in the PR description** (one paragraph minimum) — what a consumer has to change, and what to run. The `breaking-change` label carries it into the generated release notes; there is no `CHANGELOG.md` to record it in.

If you only discover the breaking nature mid-review, apply all relevant steps before requesting re-review.

## Release pipeline

`.github/workflows/publish-packages-release.yml` is **tag-triggered**: pushing a `v*` tag on a production branch (`release/v<major>.<minor>` or `support/*`) fires the pipeline. The workflow validates the tag is reachable from such a branch, then fans out a Test+Pack job to three parallel publish jobs:

| Job | Environment | Fires on tag push? | What ships | Gating |
|---|---|---|---|---|
| `publish-nuget-org` | `nuget-org` | **No — opt-in only** via `workflow_dispatch` flag | `Fallout.*.nupkg` to https://api.nuget.org/v3/index.json | Workflow flag + approval-gated env |
| `publish-github-packages` | `github-packages` | Yes | **All** `*.nupkg` (Fallout.* + Nuke.*) to https://nuget.pkg.github.com/Fallout-build/index.json | None |
| `publish-github-releases` | `github-releases` | Yes | All `*.nupkg` attached to a GitHub Release on the tag, auto-generated notes | None |

### Preview lane (from `main`)

Pushes to `main` publish **preview prereleases** (`YYYY.MINOR.PATCH-preview.<height>.g<commit>`) to **GitHub Packages only** — never nuget.org, never a GitHub Release. `main` is the sole continuous prerelease lane (per [ADR-0008](../adr/0008-collapse-experimental-into-main.md), which collapsed the former `experimental`/`-alpha` lane into `main`). It does not cause nuget.org Dependabot fan-out into consumer repos (GitHub Packages is opt-in for consumers — the reason this lane is non-publishing to nuget.org per ADR-0001/0002). Implemented in `.github/workflows/publish-packages-preview.yml` (the former `experimental.yml` is deleted).

### Why nuget.org stays opt-in

**GitHub Packages is the default channel for the preview lane and for stable tag pushes.** nuget.org is reserved for the deliberate publish of a stabilised production line (or a `support/v10` legacy security patch). To publish Fallout.* to nuget.org you must run `workflow_dispatch` with `publish-to-nugetorg=true` — a conscious "this release is ready for nuget.org" switch. Tag pushes alone publish to GitHub Packages + GitHub Releases only.

Two layers of protection on the nuget.org path: the input flag opt-in, plus the `nuget-org` environment's required-reviewer rule.

### Nuke.* shims

`Nuke.*` transition-shim package IDs are owned by the original NUKE maintainer on nuget.org (see [#47](https://github.com/Fallout-build/Fallout/issues/47)) — they're permanently routed to GitHub Packages, never nuget.org, regardless of the input flag.

### Re-runs

Each `dotnet nuget push` uses `--skip-duplicate`, so re-runs of a partial publish (one channel failed transiently) are idempotent on packages that already succeeded.

### Tag protection

`v*` tags are protected via a repository ruleset (rules: creation, deletion, update). Bypass actors: repo admins only. Combined with the workflow-dispatch flag and env approval, the nuget.org path has *three* layers (tag-creation + flag opt-in + env approval).

### `workflow_dispatch` inputs

- `tag` (required) — existing tag to (re-)release.
- `publish-to-nugetorg` (boolean, default `false`) — opt into the nuget.org publish job for this run.

Common use cases: re-running a transient-failed publish (`tag` only), or shipping a stabilised release to nuget.org (`tag` + `publish-to-nugetorg=true`).

### Channel philosophy

Per [RFC #267](https://github.com/Fallout-build/Fallout/issues/267): nuget.org = production-grade & slow; GitHub Packages = faster cadence (the preview channel — `main`'s `-preview` prereleases + every tag's packages); GitHub Releases = bundled artifacts. A planned Tier 3 (Docker-based local NuGet server for pre-merge testing) shipped via [#279](https://github.com/Fallout-build/Fallout/issues/279) — see `tests/integration/docker-compose.yml`.

`NUGET_API_KEY` is scoped to the `nuget-org` GitHub Environment (per [#273](https://github.com/Fallout-build/Fallout/issues/273)) — only resolves in the gated job. Prefix reservation tracked in [#33](https://github.com/Fallout-build/Fallout/issues/33).

## Adding a new `Fallout.X` package — first-publish gotcha

nuget.org's `Fallout.*` prefix reservation is per-ID, not per-prefix-wildcard: CI's first `nuget push` for any never-published `Fallout.X` package ID returns `403 (does not have permission to access the specified package)` until someone manually web-uploads one nupkg to register the ID. **Two traps when doing that upload:**

1. **Set the package owner to the org, not your personal account.** The nuget.org upload UI doesn't prompt you; ownership defaults to the uploading user's profile. If you forget, the package ID is reserved but the org's `NUGET_API_KEY` still 403s on subsequent pushes (the key is scoped to org-owned packages). Fix via `Manage Package → Owners → Add owner → <org>` then optionally remove your personal account. Or upload using credentials of the org's service account directly. See [#208](https://github.com/Fallout-build/Fallout/issues/208) for what this looks like when it goes wrong.
2. **Validation can lag** the upload by 5–30 minutes. The package page may say "approved" while the API key permission hasn't propagated yet. Wait, then rerun the release pipeline (`gh run rerun <id> --failed`); `--skip-duplicate` makes the retry safe for already-published packages.
