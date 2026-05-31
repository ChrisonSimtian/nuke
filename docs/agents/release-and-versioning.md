# Release and versioning

Branching, semver policy, the PR-creation procedure, and the release pipeline.

## Branching

The branch/channel/versioning model is defined by [ADR-0004](../adr/0004-calendar-versioning-and-dual-pace-channels.md) (calendar versioning + dual-pace channels), which amends [ADR-0001](../adr/0001-release-branch-model.md) (release-branch + tag-triggered multi-channel CD) and [ADR-0002](../adr/0002-v11-off-nuget-by-default.md) (nuget.org opt-in).

Long-lived branches:

- `main` — integration trunk **and the published `edge` channel**. PRs target here. Every push (or a daily build) publishes a date-stamped prerelease `YYYY.MINOR.PATCH-edge.<YYYYMMDD>.<h>` to **GitHub Packages only** (never nuget.org). Edge is intentionally unstable — the fast/AI-assisted lane. Light/fast review.
- `release/YYYY` (e.g. `release/2026`) — the **stable train** for the calendar year. Cut from `main`; hardened deliberately (slow crowd's domain, rigorous review). After the cut it takes **non-breaking minors + patches only** — never a breaking change. Tag-triggered releases fire from here. Protected per the policy below.
- `release/v10` (+ `hotfix/v10.1`, `hotfix/v10.2`) — **legacy semver maintenance line**, `10.x`, **security and critical fixes only, no new features**. Not renumbered into CalVer. This line coexists indefinitely.
- `release/v11` — **retired.** Nothing clean shipped under it (the `11.0.x` packages were unlisted); its rebrand/plugin work re-homed onto the `2026` line. Kept for archaeology, marked EoL — not a release target.

Short-lived branches (opened as PRs against `main`, then squash- or rebase-merged):

- `feature/<slug>`, `bugfix/<slug>`, `chore/<slug>`, `docs/<slug>`, `pr/<num>-<slug>`.

No `develop` or `master` branches. The trunk is `main` (edge): new work integrates there and is promoted to the stable train at the yearly cut / via non-breaking backport. The `release/v10` legacy line is maintenance-only — security/critical fixes land via a PR targeting (or a cherry-pick to) `release/v10` (or the relevant `hotfix/v10.x`) and are tagged from there. Fixes that also apply to the current stable train land on `main` first via a normal PR, then are cherry-picked to `release/YYYY` and tagged.

CI providers in use: **GitHub Actions only** (others were dropped — see [#8](https://github.com/ChrisonSimtian/Fallout/issues/8) for the demand-driven revival roadmap).

### Branch protection on `release/vN`

When a `release/vN` branch is cut, it gets the same protection profile as `main`:

- Required status check: `ubuntu-latest`
- Linear history required (no merge commits)
- CODEOWNER review required
- Dismiss stale approvals when new commits land
- Direct pushes blocked (PRs only)
- Force-push and branch deletion blocked
- Conversation resolution required
- Admins not enforced (admins can bypass in emergencies)

Apply by mirroring `main`'s protection JSON to the new branch via the GitHub API (or via repo Settings → Branches). Tag protection for `v*` tags (restricting who can fire a release tag) is tracked separately under milestone #13.

**Validation workflows.** `ubuntu-latest` runs on every PR targeting `main` (with `paths-ignore` for `docs/**`, `.assets/**`, `**/*.md`). `windows-latest` and `macos-latest` run only on push to `main` — they're post-merge / release validation, not PR gates. This is a deliberate cost trade-off.

**Merging.** Both squash and rebase merge are enabled (plain merge commits are disabled by repo setting and would fail linear-history protection anyway). Squash is the default; rebase is opt-in for curated commit sequences. See [CONTRIBUTING.md → Merging](../../CONTRIBUTING.md#merging) for the convention.

## Versioning

**Calendar versioning: `YYYY.MINOR.PATCH`** (see [ADR-0004](../adr/0004-calendar-versioning-and-dual-pace-channels.md)). It is mechanically valid SemVer 2.0 — all three components are numeric — so [Nerdbank.GitVersioning](https://github.com/dotnet/Nerdbank.GitVersioning), NuGet, and version ordering all work unchanged. The major *is* the calendar year.

- **`MAJOR` = year**, hand-set in `version.json` at the yearly cut. **`MINOR`** = feature drop within the year. **`PATCH`** = git-height fixes.
- Per-branch via `version.json`. `main` carries the **next** planned version with an `-edge` prerelease tag (so edge builds sort above current stable). Each `release/YYYY` carries `"version": "YYYY.x"`; `release/v10` keeps `"version": "10.x"`. `publicReleaseRefSpec` matches **both** `^refs/heads/release/\d{4}$` and `^refs/heads/release/v\d+$`.
- Edge builds put the date in the **prerelease segment** (`2026.2.0-edge.20260529.<h>`), never the version core — a core of `2026.05.29` would be a *stable* release, not a nightly.

GitVersion is still installed as a transitional helper for `MajorMinorPatchVersion` in `Build.cs`; full removal is a follow-up.

## Versioning policy

This project ships calendar versions that are valid [Semantic Versioning](https://semver.org/spec/v2.0.0.html) per [CHANGELOG.md](../../CHANGELOG.md). The rule is: **breaking changes are batched to the yearly major cut.**

- A breaking change may land on **`main` only**, is held for the next yearly major (it does **not** bump `version.json`'s major mid-year), and is recorded in `CHANGELOG.md` under the next-major `[Unreleased]` heading with a migration path.
- **`release/YYYY` stable trains never take a breaking change** — mid-year stable is strictly non-breaking (minor = features, patch = fixes). A PR that breaks may not target a stable train.
- Surface that isn't ready to commit to can ship behind `[Experimental("FALLOUT0xx")]` instead of being held back — opt-in for consumers, and not a breaking change to add or remove.

A "breaking change" is any of:

- A conventional-commit subject with the `!` suffix (e.g. `feat(globaltool)!: …`, `fix(security)!: …`).
- A `BREAKING CHANGE:` footer in the commit body.
- A change a reviewer reasonably flags as breaking even without the marker (renamed/removed public API, package ID change, on-disk format change, CI/CD shape change consumers depend on) — **except** changes to `[Experimental]` surface, which carry no stability guarantee.

**Reviewer responsibility:** if a PR carries `!` (or a flagged breaking change), confirm it targets `main` (not a stable train) and that the CHANGELOG entry sits under the next-major heading. Block otherwise.

## Milestones and version targeting

Milestones are **theme-based** (e.g. "Plugin Architecture Foundation & Rebrand Completion", "Public Plugin SDK", "Continuous Delivery Vision") and carry across releases; version targeting uses **`target/YYYY`** labels (`target/2026`, `target/2027`, …). Legacy v10 maintenance work uses `target/v10`. A breaking change is held for the next yearly major — so its PR carries `target/<next-year>`.

## PR-creation flow

At PR-creation time — not after, not as a follow-up — every PR gets:

1. **A `target/YYYY` label** matching where it will release. Default to `target/<current-year>` (`target/2026`). If the PR carries a breaking change, it's held for the next yearly major — use `target/<next-year>`. Legacy v10 maintenance work uses `target/v10`. Pass via `--label target/2026` to `gh pr create`.

If the PR includes a **breaking change** (any commit uses `!`, has a `BREAKING CHANGE:` footer, or otherwise meets the breaking-change definition above), additionally:

2. **Add the `breaking-change` label.** `gh pr create --label target/<next-year> --label breaking-change …`.
3. **Open the PR body with a `⚠️ Breaking change` callout** that names the affected surface (public API, package ID, CLI flag, on-disk format, CI/CD shape, etc.) and the consumer-side impact in one sentence. This is what reviewers and downstream consumers read first.
4. **Confirm the PR targets `main`, not a `release/YYYY` stable train.** Breaking changes accumulate on `main` for the next yearly major; they may not land on a stable train. (Do **not** bump `version.json`'s major in the PR — the major is set once, at the yearly cut.)
5. **Add a `CHANGELOG.md` entry** under the next-major `[Unreleased]` heading, in the same PR, describing the breaking change and the migration path (one paragraph minimum).

If you only discover the breaking nature mid-review, apply all relevant steps before requesting re-review.

## Release pipeline

`.github/workflows/release.yml` is **tag-triggered**: pushing a `v*` tag on a `release/v*` branch fires the pipeline. The workflow validates the tag is reachable from a `release/v*` branch, then fans out a Test+Pack job to three parallel publish jobs:

| Job | Environment | Fires on tag push? | What ships | Gating |
|---|---|---|---|---|
| `publish-nuget-org` | `nuget-org` | **No — opt-in only** via `workflow_dispatch` flag | `Fallout.*.nupkg` to https://api.nuget.org/v3/index.json | Workflow flag + approval-gated env |
| `publish-github-packages` | `github-packages` | Yes | **All** `*.nupkg` (Fallout.* + Nuke.*) to https://nuget.pkg.github.com/Fallout-build/index.json | None |
| `publish-github-releases` | `github-releases` | Yes | All `*.nupkg` attached to a GitHub Release on the tag, auto-generated notes | None |

### Edge channel (from `main`)

Pushes to `main` publish **edge prereleases** (`YYYY.MINOR.PATCH-edge.<YYYYMMDD>.<h>`) to **GitHub Packages only** — never nuget.org, never a GitHub Release. This is the fast/AI-assisted lane; it is intentionally unstable and causes no nuget.org Dependabot fan-out into consumer repos (the reason `main` was made non-publishing in ADR-0001 — GitHub Packages is opt-in for consumers). _(Wiring tracked alongside ADR-0004 — see the milestone for the PR that adds the `main`-trigger edge job.)_

### Why nuget.org stays opt-in

**GitHub Packages is the default channel for both edge and stable.** nuget.org is reserved for the deliberate publish of a stabilised `release/YYYY` (or a `release/v10` legacy security patch). To publish Fallout.* to nuget.org you must run `workflow_dispatch` with `publish-to-nugetorg=true` — a conscious "this release is ready for nuget.org" switch. Tag pushes alone publish to GitHub Packages + GitHub Releases only.

Two layers of protection on the nuget.org path: the input flag opt-in, plus the `nuget-org` environment's required-reviewer rule.

### Nuke.* shims

`Nuke.*` transition-shim package IDs are owned by the original NUKE maintainer on nuget.org (see [#47](https://github.com/ChrisonSimtian/Fallout/issues/47)) — they're permanently routed to GitHub Packages, never nuget.org, regardless of the input flag.

### Re-runs

Each `dotnet nuget push` uses `--skip-duplicate`, so re-runs of a partial publish (one channel failed transiently) are idempotent on packages that already succeeded.

### Tag protection

`v*` tags are protected via a repository ruleset (rules: creation, deletion, update). Bypass actors: repo admins only. Combined with the workflow-dispatch flag and env approval, the nuget.org path has *three* layers (tag-creation + flag opt-in + env approval).

### `workflow_dispatch` inputs

- `tag` (required) — existing tag to (re-)release.
- `publish-to-nugetorg` (boolean, default `false`) — opt into the nuget.org publish job for this run.

Common use cases: re-running a transient-failed publish (`tag` only), or shipping a stabilised release to nuget.org (`tag` + `publish-to-nugetorg=true`).

### Channel philosophy

Per [RFC #267](https://github.com/ChrisonSimtian/Fallout/issues/267): nuget.org = production-grade & slow; GitHub Packages = faster cadence (currently the v11 release channel); GitHub Releases = bundled artifacts. A planned Tier 3 (Docker-based local NuGet server for pre-merge testing) shipped via [#279](https://github.com/ChrisonSimtian/Fallout/issues/279) — see `tests/integration/docker-compose.yml`.

`NUGET_API_KEY` is scoped to the `nuget-org` GitHub Environment (per [#273](https://github.com/ChrisonSimtian/Fallout/issues/273)) — only resolves in the gated job. Prefix reservation tracked in [#33](https://github.com/ChrisonSimtian/Fallout/issues/33).

## Adding a new `Fallout.X` package — first-publish gotcha

nuget.org's `Fallout.*` prefix reservation is per-ID, not per-prefix-wildcard: CI's first `nuget push` for any never-published `Fallout.X` package ID returns `403 (does not have permission to access the specified package)` until someone manually web-uploads one nupkg to register the ID. **Two traps when doing that upload:**

1. **Set the package owner to the org, not your personal account.** The nuget.org upload UI doesn't prompt you; ownership defaults to the uploading user's profile. If you forget, the package ID is reserved but the org's `NUGET_API_KEY` still 403s on subsequent pushes (the key is scoped to org-owned packages). Fix via `Manage Package → Owners → Add owner → <org>` then optionally remove your personal account. Or upload using credentials of the org's service account directly. See [#208](https://github.com/ChrisonSimtian/Fallout/issues/208) for what this looks like when it goes wrong.
2. **Validation can lag** the upload by 5–30 minutes. The package page may say "approved" while the API key permission hasn't propagated yet. Wait, then rerun the release pipeline (`gh run rerun <id> --failed`); `--skip-duplicate` makes the retry safe for already-published packages.
