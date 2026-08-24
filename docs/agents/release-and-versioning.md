# Release and versioning

Branching, semver policy, the PR-creation procedure, and the release pipeline.

## Branching

The branch, channel, and versioning model is defined by [ADR-0009](../adr/0009-gitflow-and-semver-reversion.md) (classic GitFlow, staying on semver `10.x`). It replaces [ADR-0004](../adr/0004-calendar-versioning-and-dual-pace-channels.md) (calendar versioning) in full. It keeps [ADR-0007](../adr/0007-cut-release-branch-on-demand.md)'s on-demand release cut and [ADR-0008](../adr/0008-collapse-experimental-into-main.md)'s decision not to have a separate `experimental` lane, just renamed onto the branches below. It also changes part of [ADR-0001](../adr/0001-release-branch-model.md) — the tag-source branch moves back to `main`. [ADR-0002](../adr/0002-v11-off-nuget-by-default.md) (nuget.org opt-in) is unaffected.

Classic GitFlow (`develop` → `release/vX.Y` → `main`) feeds the production line. GitHub Packages carries test, preview, and rc builds; nuget.org carries production. `develop` and `main` are renames of branches that already existed (`main` and `release/v10.4`), not new branches. Long-lived branches:

- `develop` (renamed from `main`) — the **integration trunk and the only prerelease lane.** This is the default branch. Both deliberate work and fast, AI-assisted work land here. Every push publishes a prerelease build, `MAJOR.MINOR.PATCH-preview.<height>.g<commit>` (`10.5.0-preview.<height>.g<commit>` right now), to **GitHub Packages only — never nuget.org.** Ordinary review applies.
- `release/vX.Y` (e.g. `release/v10.5`) — the branch that **stabilizes the next release.** It's **cut from `develop` on demand, at the first release, not ahead of time** (this keeps [ADR-0007](../adr/0007-cut-release-branch-on-demand.md)'s rule) — this applies to every `10.x` minor, not just to an eventual v11. Review here is rigorous, since this is where the release gets hardened. It publishes `-rc.N` prereleases. After it's cut, only non-breaking fixes land on it.
- `main` (renamed from `release/v10.4`) — the **production trunk.** It only takes merges from `release/vX.Y` (for GA) or `hotfix/vX.Y.Z` branches. GA tags are created here, which fires the tag-triggered pipeline (the nuget.org tier). See the protection policy below.
- `support/v10` (plus `hotfix/v10.1`, `hotfix/v10.2`) — the **older legacy line**, covering versions before `10.4` (`10.0.x`–`10.3.x`). It takes security and critical fixes only, no new features. This line is not affected by the model above.
- `support/vN` — this is what a major becomes once a later major replaces it. Not in use yet — the project is staying on `10.x` and has not cut a major.

Short-lived branches (rebase-merged through a PR): `feature/<slug>`, `bugfix/<slug>`, `chore/<slug>`, `docs/<slug>`, `pr/<num>-<slug>`. They target `develop`. A breaking change that can't be gated behind `[Experimental("FALLOUT0xx")]` waits for the next major on a short-lived branch off `develop`.

`master` is not used. Work flows **forward-only**: `develop → release/vX.Y → main`. The `support/*` lines only take maintenance fixes — a security or critical fix lands via a PR targeting (or cherry-picked to) `support/vN` (or the matching `hotfix/vX.x`) and is tagged from there.

CI providers in use: **GitHub Actions only** (others were dropped — see [#8](https://github.com/Fallout-build/Fallout/issues/8) for the demand-driven revival roadmap).

### Branch protection on `release/vX.Y` and `support/*`

`develop`, `main`, every release line, and every `support/*` branch share the same protection profile:

- Required status check: `ubuntu-latest`
- Linear history required (no merge commits)
- CODEOWNER review required (0 additional approvals)
- Direct pushes blocked (PRs only)
- Force-push and branch deletion blocked
- Conversation resolution required
- Admins not enforced (admins can bypass in emergencies)

Stale approvals are **not** dismissed when new commits land (`dismiss_stale_reviews: false`).

**How it's applied differs by branch:**

- **Release lines** (e.g. the next one, `release/v10.5`) — covered by the pattern-based ruleset on `refs/heads/release/**` ([19766406](https://github.com/Fallout-build/Fallout/rules/19766406)), so protection attaches automatically at branch creation. Payload committed at `.github/release-branch-ruleset.json`. **Nothing to apply by hand.**
- **`develop`, `main`, and `support/*`** — classic per-branch protection, configured individually.

Tag protection for `v*` tags is a separate ruleset ([17017817](https://github.com/Fallout-build/Fallout/rules/17017817)).

**Validation workflows.** `build.yml` runs Test+Pack on Linux on every PR targeting `develop`, `main`, `release/*`, or `support/*` (with `paths-ignore` for `docs/**`, `.assets/**`, `**/*.md`); its job `ubuntu-latest` is the only required status check. `build-cross-platform.yml` runs Test+Pack on Windows and macOS (one job each) only on PRs targeting `main` / `release/*` / `support/*` and on `v*` tag pushes — cross-platform is gated to release intent, not routine `develop` work. This is a deliberate cost trade-off. (Both workflows are **generated** from `build/Build.CI.GitHubActions.cs` — change the branch lists in the `DevelopBranch`/`MainBranch`/`*BranchPattern` constants there and regenerate, don't hand-edit the `.yml`. The `build-skip.yml` no-op shim reports the `ubuntu-latest` check on docs-only PRs.)

**Merging.** Rebase merge only. Plain merge commits are disabled by repo setting; **squash is still enabled at the repo level**, so on release branches the convention — not the setting — is what keeps squashes out. Squashing a promotion would collapse it into one opaque commit, defeating the point of promoting reviewed commits verbatim. Every reviewed commit lands on `develop` verbatim, so curate commits into a clean sequence before final approval. See [CONTRIBUTING.md → Merging](https://github.com/Fallout-build/Fallout/blob/main/CONTRIBUTING.md#merging) for the convention.

## Versioning

**Classic semver: `MAJOR.MINOR.PATCH`, staying on `10.x`** (see [ADR-0009](../adr/0009-gitflow-and-semver-reversion.md), which replaces [ADR-0004](../adr/0004-calendar-versioning-and-dual-pace-channels.md)). [Nerdbank.GitVersioning](https://github.com/dotnet/Nerdbank.GitVersioning), NuGet, and version ordering all work the same as before.

- **`MAJOR`** stays `10` until a breaking change is actually needed — there is no fixed date for the next bump (v11). **`MINOR`** is a feature drop within the major. **`PATCH`** is a git-height fix.
- This is set per branch, in `version.json`. The preview lane is a **non-public ref** — it carries the next planned version with a prerelease tag: `develop` → `"10.5.0-preview.{height}"` right now (`firstUnstableTag` is `preview`). Each `release/vX.Y` sets its prerelease number by hand (e.g. `"version": "10.5.0-rc.N"` — see the runbook for how to bump it). `support/v10` keeps `"version": "10.x"` for versions before `10.4`. `publicReleaseRefSpec` matches three patterns: `^refs/heads/main$`, `^refs/heads/release/v\d+(\.\d+)?$`, `^refs/heads/support/v\d+$` — **not** `develop`.
- A preview build carries its height and commit in the **prerelease part** of the version (e.g. `10.5.0-preview.<height>.g<commit>`), never in the main version number. `develop` is a non-public ref, so NB.GV adds the `.g<commit>` suffix. The order is: `-preview`, then `-rc`, then GA.

GitVersion is still installed as a transitional helper for `MajorMinorPatchVersion` in `Build.cs`; full removal is a follow-up.

## Versioning policy

This project ships classic semver, valid under [Semantic Versioning](https://semver.org/spec/v2.0.0.html). The rule: **breaking changes wait for the next major**, and there's no fixed date for that — the project stays on `10.x` for as long as it can, and the eventual move to v11 will go through `fallout-migrate` (see [ADR-0009](../adr/0009-gitflow-and-semver-reversion.md)).

There is **no `CHANGELOG.md`** — the file was retired. Release notes are generated from PR labels via [`.github/release.yml`](https://github.com/Fallout-build/Fallout/blob/main/.github/release.yml). The PR description and its labels are now the lasting record of a change.

- A breaking change lands on **`develop`, behind `[Experimental("FALLOUT0xx")]`** (or, if that doesn't fit, on a short-lived branch off `develop` held until the cut). It waits for the next major — it does **not** bump `version.json`'s major mid-cycle. Its PR description describes the migration path under a `⚠️ Breaking change` callout.
- **A `release/vX.Y` or `main` production line never takes a breaking change.** It only takes non-breaking work: a minor adds features, a patch fixes bugs. The review before a production cut is the backstop that keeps an ungated breaking change off the production line (ADR-0008, kept by ADR-0009).
- Surface that isn't ready to commit to yet can ship behind `[Experimental("FALLOUT0xx")]` instead of being held back. This is opt-in for consumers, and adding or removing it is not a breaking change. With no separate `experimental` branch, this attribute is the main way to isolate unstable APIs.

A "breaking change" is any of:

- A conventional-commit subject with the `!` suffix (e.g. `feat(globaltool)!: …`, `fix(security)!: …`).
- A `BREAKING CHANGE:` footer in the commit body.
- A change a reviewer reasonably flags as breaking even without the marker (renamed/removed public API, package ID change, on-disk format change, CI/CD shape change consumers depend on) — **except** changes to `[Experimental]` surface, which carry no stability guarantee.

**Reviewer responsibility:** if a PR carries `!` (or a flagged breaking change), check that it targets `develop`, not a production branch. Check that the breaking surface is behind `[Experimental("FALLOUT0xx")]` (or on a topic branch, if it can't be gated). Check that the PR description has the `⚠️ Breaking change` callout with a migration path. Block the PR if any of that is missing. The review at a production cut is the last check that catches any ungated breaking change before it reaches a `release/vX.Y`.

## Milestones and version targeting

Milestones are **theme-based** (e.g. "Plugin Architecture Foundation & Rebrand Completion", "Public Plugin SDK", "Continuous Delivery Vision") and carry across releases; version targeting uses **evergreen `target/vCurrent` / `target/vNext`** labels — `target/vCurrent` is the current release line, `target/vNext` is the next major. A breaking change is held for the next major — so its PR carries `target/vNext`.

## PR-creation flow

Write the PR description terse and to the canonical shape — see
[issue-and-pr-style.md](issue-and-pr-style.md). At PR-creation time — not after,
not as a follow-up — every PR gets:

0. **Working from a fork? Branch from `upstream/develop`, push to `origin`, and open the PR against `upstream`.** Check `git remote -v` first. If it shows both an `origin` (a personal fork, e.g. `<you>/Fallout`) and an `upstream` remote pointing at `Fallout-build/Fallout`, treat `upstream` as the core repo. Run `git fetch upstream develop`, then branch from `upstream/develop` — never from `origin/develop`, which can be far behind the core repo and cause large, unnecessary merge conflicts. Push the new branch to `origin`, then open the PR against `upstream`: `gh pr create --repo Fallout-build/Fallout --draft ...`. Do this by default, unless the user asks to branch off the fork's own `develop` instead. This step doesn't apply to a plain single-remote clone.
1. **Create the PR as a draft** — `gh pr create --draft` (see [issue-and-pr-style.md](issue-and-pr-style.md#pr-description-shape)) unless the user explicitly asks for a ready-for-review PR. This is easy to miss because it's a small flag on the same `gh pr create` call as the labels below — don't drop it.
2. **A `target/vCurrent` or `target/vNext` label** matching where it will release. Default to `target/vCurrent` (the current release line). If the PR carries a breaking change, it's held for the next major — use `target/vNext`. Pass via `--label target/vCurrent` to `gh pr create`.
3. **A changelog-category label** describing the change, from [`.github/release.yml`](../../.github/release.yml) — that file is the source of truth for the taxonomy and carries a one-line blurb on each label. Apply the one category the PR belongs under (`enhancement`, `bug`, `security`, `documentation`; `breaking-change` when it applies — see below), or `skip-changelog` for housekeeping with no release note. Pass it in the same `gh pr create --label …` call. Don't leave a PR uncategorized — it falls through to "Other Changes". This is the labelling AI applies on the user's behalf whenever it raises a PR.

If the PR includes a **breaking change** (any commit uses `!`, has a `BREAKING CHANGE:` footer, or otherwise meets the breaking-change definition above), additionally:

4. **Add the `breaking-change` label** (this is its changelog category — use it instead of `enhancement`/`bug`). `gh pr create --label target/vNext --label breaking-change …`.
5. **Open the PR body with a `⚠️ Breaking change` callout** that names the affected surface (public API, package ID, CLI flag, on-disk format, CI/CD shape, etc.) and the consumer-side impact in one sentence. This is what reviewers and downstream consumers read first.
6. **Confirm the PR targets `develop`**, not a `release/vX.Y` or `main` production branch, **and that the breaking surface is behind `[Experimental("FALLOUT0xx")]`** (or, if that doesn't fit, on a short-lived branch off `develop` held until the major cut). Breaking changes build up on `develop` for the next major — they may not land on a production branch. Do **not** bump `version.json`'s major in the PR; the major is set once, at the cut.
7. **Spell out the migration path in the PR description** (one paragraph minimum) — what a consumer has to change, and what to run. The `breaking-change` label carries it into the generated release notes; there is no `CHANGELOG.md` to record it in.

If you only discover the breaking nature mid-review, apply all relevant steps before requesting re-review.

## Release pipeline

`.github/workflows/publish-packages-release.yml` is **tag-triggered**: pushing a `v*` tag on a production branch (`main`, `release/vMAJOR.MINOR`, or `support/*`) fires the pipeline. The workflow validates the tag is reachable from such a branch, then fans out a Test+Pack job to three parallel publish jobs:

| Job | Environment | Fires on tag push? | What ships | Gating |
|---|---|---|---|---|
| `publish-nuget-org` | `nuget-org` | **No — opt-in only** via `workflow_dispatch` flag | `Fallout.*.nupkg` to https://api.nuget.org/v3/index.json | Workflow flag + approval-gated env |
| `publish-github-packages` | `github-packages` | Yes | **All** `*.nupkg` (Fallout.* + Nuke.*) to https://nuget.pkg.github.com/Fallout-build/index.json | None |
| `publish-github-releases` | `github-releases` | Yes | All `*.nupkg` attached to a GitHub Release on the tag, auto-generated notes | None |

### Preview lane (from `develop`)

A push to `develop` publishes a **preview prerelease** (`MAJOR.MINOR.PATCH-preview.<height>.g<commit>`) to **GitHub Packages only** — never nuget.org, never a GitHub Release. `develop` is the only continuous prerelease lane (per [ADR-0008](../adr/0008-collapse-experimental-into-main.md), kept by ADR-0009 — there is still no `experimental`/`-alpha` lane). This doesn't cause a nuget.org Dependabot fan-out into consumer repos, because GitHub Packages is opt-in for consumers (the reason this lane skips nuget.org, per ADR-0001/0002). It's implemented in `.github/workflows/publish-packages-preview.yml`.

### Why nuget.org stays opt-in

**GitHub Packages is the default channel, for both the preview lane and stable tag pushes.** nuget.org is reserved for a deliberate publish: a stabilized `main` GA release, or a `support/v10` legacy security patch. To publish Fallout.* to nuget.org, run `workflow_dispatch` with `publish-to-nugetorg=true` — a conscious "this release is ready for nuget.org" switch. A tag push on its own only publishes to GitHub Packages and GitHub Releases.

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

Per [RFC #267](https://github.com/Fallout-build/Fallout/issues/267): nuget.org = production-grade & slow; GitHub Packages = faster cadence (the preview channel — `develop`'s `-preview` prereleases + every tag's packages); GitHub Releases = bundled artifacts. A planned Tier 3 (Docker-based local NuGet server for pre-merge testing) shipped via [#279](https://github.com/Fallout-build/Fallout/issues/279) — see `tests/integration/docker-compose.yml`.

`NUGET_API_KEY` is scoped to the `nuget-org` GitHub Environment (per [#273](https://github.com/Fallout-build/Fallout/issues/273)) — only resolves in the gated job. Prefix reservation tracked in [#33](https://github.com/Fallout-build/Fallout/issues/33).

## Adding a new `Fallout.X` package — first-publish gotcha

nuget.org's `Fallout.*` prefix reservation is per-ID, not per-prefix-wildcard: CI's first `nuget push` for any never-published `Fallout.X` package ID returns `403 (does not have permission to access the specified package)` until someone manually web-uploads one nupkg to register the ID. **Two traps when doing that upload:**

1. **Set the package owner to the org, not your personal account.** The nuget.org upload UI doesn't prompt you; ownership defaults to the uploading user's profile. If you forget, the package ID is reserved but the org's `NUGET_API_KEY` still 403s on subsequent pushes (the key is scoped to org-owned packages). Fix via `Manage Package → Owners → Add owner → <org>` then optionally remove your personal account. Or upload using credentials of the org's service account directly. See [#208](https://github.com/Fallout-build/Fallout/issues/208) for what this looks like when it goes wrong.
2. **Validation can lag** the upload by 5–30 minutes. The package page may say "approved" while the API key permission hasn't propagated yet. Wait, then rerun the release pipeline (`gh run rerun <id> --failed`); `--skip-duplicate` makes the retry safe for already-published packages.
