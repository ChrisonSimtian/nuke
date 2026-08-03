# Branching and release flow

Maintainer reference for how Fallout branches, ships releases, hotfixes older lines, and uses GitHub Environments to gate publishes. Model defined by [ADR-0004](adr/0004-calendar-versioning-and-dual-pace-channels.md) (calendar versioning + dual-pace channels), amending [ADR-0001](adr/0001-release-branch-model.md) / [milestone #13](https://github.com/Fallout-build/Fallout/milestone/13) / [RFC #267](https://github.com/Fallout-build/Fallout/issues/267). The `experimental` branch and its `-alpha` channel have since been collapsed into `main` ([ADR-0008](adr/0008-collapse-experimental-into-main.md), channel ladder superseded) — `main` is now the sole prerelease lane.

> **Audience.** Repository maintainers cutting releases or hotfixing older lines. Contributors filing PRs against `main` don't need to read this — see [CONTRIBUTING.md](https://github.com/Fallout-build/Fallout/blob/main/CONTRIBUTING.md) instead. AI coding tools should read both this file and [docs/agents/release-and-versioning.md](agents/release-and-versioning.md).

## Branches at a glance

A maturity ladder feeding the production line (amended [ADR-0004](adr/0004-calendar-versioning-and-dual-pace-channels.md), 2026-05-30; the `experimental` rung collapsed into `main` per [ADR-0008](adr/0008-collapse-experimental-into-main.md)):

| Branch | Purpose | Lifetime | Protected | Source of releases? |
|---|---|---|---|---|
| `main` | **Integration trunk + sole prerelease lane (`-preview` channel).** Default branch. Both deliberate improvements / bug fixes **and** faster / AI-assisted work land here. Per-commit `…-preview` prereleases to GitHub Packages. **Never nuget.org.** Breaking work accumulates here gated behind `[Experimental("FALLOUT0xx")]` (or on a short-lived topic branch off `main`) for the yearly major. | Long-lived | Yes | **Preview only** (GitHub Packages, no nuget.org / no GH Release) |
| `release/YYYY` | **Production line** for the calendar year (e.g. `release/2026`), **cut from `main` on demand at the first release of the year, not preemptively** ([ADR-0007](adr/0007-cut-release-branch-on-demand.md)). `-rc.N` → GA. Non-breaking minors/patches only after the cut. | Cut on demand; long-lived once cut | Yes | **Yes** — tags pushed here fire the full release pipeline (nuget.org opt-in) |
| `release/vMAJOR.MINOR` | **Production line for a semver minor** (e.g. `release/v10.4`) — the shape in use while the line still ships `10.x`, before the CalVer major cut. Same rules and pipeline as `release/YYYY`; both are matched by `publicReleaseRefSpec` and by `validate-ref`. | Cut on demand; long-lived once cut | Yes | **Yes** — same pipeline |
| `support/v10` (+ `hotfix/v10.1`, `hotfix/v10.2`) | **Legacy** semver `10.x` maintenance line — security/critical fixes only. (Renamed from `release/v10`.) | Long-lived | Yes | Yes — tags fire the pipeline (nuget.org opt-in) |
| `support/YYYY` | **Retired** year production line (e.g. `support/2026` once 2027 supersedes it). Security/critical fixes only. | Long-lived | Yes | Yes — tags fire the pipeline (nuget.org opt-in) |
| `release/v11` | **Retired and deleted** — nothing clean shipped; work re-homed onto `2026`. Branch removed per [ADR-0007](adr/0007-cut-release-branch-on-demand.md) §6 (no unique history; dead branches are deletable, tags are the durable markers). | Deleted | — | No |
| `feature/<slug>`, `bugfix/<slug>`, `chore/<slug>`, `docs/<slug>`, `pr/<num>-<slug>` | Working branches | Short-lived; PR-and-merge then deleted | No | No |

This *is* gitflow with the project's vocabulary: `main` ≈ the integration trunk / `develop`, `release/YYYY` ≈ `release/*` (long-lived per year), `support/*` ≈ legacy/retired lines. The one deviation: **`main` is not the production/nuget.org line** — `release/YYYY` + `support/*` are. `main` is a `-preview` test channel that production is cut from.

`develop` (literal) and `master` are not used. **Breaking changes land on `main`** — gated behind the `[Experimental("FALLOUT0xx")]` attribute, or, when they can't be gated, on a short-lived topic branch off `main` — and are batched to the yearly major cut; the production-cut review is the backstop. A breaking-change PR targets `main`, never a `release/YYYY` production train. Stabilised non-breaking work is promoted **forward-only** `main → release/YYYY`. A stable-urgent fix lands on the production branch and is **forward-ported to `main`** so the trunk never regresses — see the [promotion + hotfix flow](#promotion-and-hotfixing) below.

## Channel taxonomy

### Lines live right now

Keep this block current — the examples further down use these values.

| Line | Branch | Ships | Latest |
|---|---|---|---|
| Preview | `main` | `10.4.0-preview.<height>.g<sha>` → GitHub Packages, per commit | rolling |
| Production | `release/v10.4` | `10.4.0-rc.N` → GitHub Packages + GH Release; nuget.org opt-in | `v10.4.0-rc.4` |
| Legacy | `support/v10` | `10.x` security/critical only | `10.3.47` |

`main` is deliberately **not** in `publicReleaseRefSpec`, which is why its previews carry the `.g<sha>` suffix — they're non-public builds by design. Production lines are listed there, so their packages are clean (see [ADR-0004](adr/0004-calendar-versioning-and-dual-pace-channels.md) for the CalVer target; the current line is still `10.x`).

### Channels

Releases fire to multiple channels, each with its own GitHub Environment:

**GitHub Packages = the test/preview channel; nuget.org = production.** The version ladder orders cleanly under SemVer: `…-preview.N` < `…-rc.N` < `…` (GA) — the `-alpha` rung was retired with the `experimental` branch ([ADR-0008](adr/0008-collapse-experimental-into-main.md)).

| Channel | Built from | Cadence | Gating | Version shape |
|---|---|---|---|---|
| **preview** → `github-packages` env | `main` | Per-commit | None | `10.4.0-preview.<height>.g<commit>` |
| **stable** → `nuget-org` env | `release/*` tags | Slow, deliberate | **Flag opt-in + approval-gated** | `10.4.0-rc.N` today; `YYYY.M.P` after the CalVer cut |
| **stable/legacy** → `github-packages` env | `release/*`, `support/*` tags | Every tag | None | Same as the tag |
| **legacy** → `nuget-org` env | `support/v10`, `support/YYYY` tags | Security/critical only | **Flag opt-in + approval-gated** | `10.x` / `YYYY.x` |
| `github-releases` env (bundled) | `release/*`, `support/*` tags | Same tag as the package publish | None | Same as the tag |
| Docker local NuGet server | Per-PR / per-commit | None (local) | PR-derived | Available via `tests/integration/docker-compose.yml` |

**Defaults:** `main` (preview) publishes to GitHub Packages only — **never nuget.org, never a GH Release**. `publish-packages-preview.yml` (main → `-preview`) is the only continuous publisher; the former `experimental.yml` workflow has been deleted ([ADR-0008](adr/0008-collapse-experimental-into-main.md)). Production tag pushes (`release/YYYY`, `support/*`) publish to GitHub Packages + GitHub Releases. nuget.org is **always opt-in** via the `workflow_dispatch` `publish-to-nugetorg` flag — used when a `release/YYYY` is stabilised enough for the broader consumer audience, or for a `support/v10` security patch. See [`project_release_channels` in agent memory](https://github.com/Fallout-build/Fallout/issues/267#issuecomment-4570408325) and [ADR-0004](adr/0004-calendar-versioning-and-dual-pace-channels.md).

## Cutting a release

### Prerelease numbers on a release branch are a manual counter

On a release branch, `version.json`'s `version` pins the prerelease number literally — `10.4.0-rc.4`, not `10.4.0-rc.{height}`. **Bump it in a PR before you tag.** Two consequences:

- Every commit on the branch reports the same version until you bump, so the number tracks *release intent* rather than however many commits a promotion happened to carry. (`{height}` sent `rc.3` straight to `rc.23` on the first 19-commit promotion.)
- Tagging twice without bumping republishes an existing version. `dotnet nuget push --skip-duplicate` swallows that silently, so the packages simply won't update — **check the number first**.

`main` keeps `{height}` (`10.4.0-preview.{height}`): per-commit previews want a value that always moves on its own.

### Routine stable release (GitHub Packages only)

The default path. Pushing a tag to a production branch publishes to GitHub Packages + GitHub Releases. nuget.org is **not** touched. Git tags keep the `v` prefix — `v10.4.0-rc.4`, `v2026.1.3` — so the `v*` tag-protection ruleset and `validate-ref` apply; the package version core drops it (`10.4.0-rc.4`).

Examples below use the live line, `release/v10.4`. A CalVer `release/YYYY` cut works identically — substitute the branch and tag.

```bash
# 1. Make sure your local release branch is up to date
git fetch
git switch release/v10.4
git pull --ff-only

# 2. Bump the rc number in version.json via a PR if you haven't (see above),
#    then verify what NB.GV will compute. Note PublicRelease=1: a plain local
#    run reports a .g<sha> suffix because your checked-out branch is only a
#    public ref in CI's eyes.
PublicRelease=1 dotnet nbgv get-version -v NuGetPackageVersion   # e.g. 10.4.0-rc.4

# 3. Create the tag + GitHub Release in one step.
#    --notes-start-tag is load-bearing: see "Release notes" below.
#    Add --prerelease for an rc.
gh release create v10.4.0-rc.4 \
    --target release/v10.4 \
    --title "v10.4.0-rc.4" \
    --prerelease \
    --generate-notes \
    --notes-start-tag 10.3.47           # the last GA, NOT the previous rc
```

### Release notes

**Always use `--generate-notes`.** It groups merged PRs by the label taxonomy in [`.github/release.yml`](https://github.com/Fallout-build/Fallout/blob/main/.github/release.yml) and credits every contributor, including a "New Contributors" section. Don't hand-write notes.

**Set `--notes-start-tag` to the last GA tag, not the previous prerelease.** Left to itself, `gh` picks the most recent tag — so an rc diffs against the rc before it and the notes collapse to whatever landed in between. Anchoring on the last GA (`10.3.47` for the 10.4 line) makes every rc's notes show the full set of changes since the last real release, which is what someone evaluating an rc wants to read.

One known gap: work promoted onto a release branch by cherry-pick gets **new commit SHAs**, so GitHub can't map it back to the PRs it came from and it shows up as the single promotion PR instead of the individual ones. The pre-cut history (inherited when the branch was cut) maps fine. If a promotion carried work worth itemising, add a short summary paragraph above the generated section rather than replacing it.

That tag push triggers `.github/workflows/publish-packages-release.yml`:

1. **`validate-ref`** confirms the tag points at a commit reachable from a production branch (`release/YYYY`, `release/vMAJOR.MINOR`, or `support/*`).
2. **`test-and-pack`** runs `dotnet fallout Test Pack`, uploads `output/packages/*.nupkg` as an artifact.
3. Three parallel publish jobs consume the artifact:
   - `publish-nuget-org` — **skipped** (not opt-in by default)
   - `publish-github-packages` — pushes **all** `*.nupkg` (Fallout.* + Nuke.*) to GitHub Packages
   - `publish-github-releases` — attaches all `*.nupkg` to the GitHub Release page

### Stabilised release (nuget.org publish)

When a release is stabilised enough for nuget.org, or for cutting a `support/v10` legacy security patch, use `workflow_dispatch` with the opt-in flag:

```bash
# Option A: via gh CLI
gh workflow run publish-packages-release.yml \
    --ref release/v10.4 \
    -f tag=v10.4.0-rc.4 \
    -f publish-to-nugetorg=true

# Option B: via Actions UI → publish-packages-release → "Run workflow" → set publish-to-nugetorg to true
```

> **`--ref` is not optional.** `workflow_dispatch` takes the **workflow definition** from the ref you dispatch against, while `-f tag=` only controls which source gets checked out and packed. Dispatch against the default branch and you run `main`'s copy of the pipeline — which will differ from the release branch's whenever a pipeline fix hasn't been forward-ported yet, and will happily push the resulting packages to nuget.org. Always pass the production branch.

The workflow:

1. Skips `validate-ref` (workflow_dispatch doesn't auto-validate the ref; you took the action consciously).
2. Re-runs `test-and-pack` against the named tag.
3. **`publish-nuget-org` fires** — pauses for approval at the `nuget-org` env gate (notification + entry on the run page; click "Review deployments" → check `nuget-org` → "Approve and deploy"). Then pushes Fallout.* to nuget.org.
4. `publish-github-packages` re-runs idempotently (`--skip-duplicate` skips what's already there).
5. `publish-github-releases` re-runs idempotently (uses `--clobber` for asset replacement if the GH Release already exists).

Two layers of safety on the nuget.org path: the flag opt-in + the env approval. You can also test the wiring without burning a release — set the flag, get the approval prompt, then cancel without approving.

**A green run is not proof anything published.** Every publish job is conditional, so a misconfigured condition skips it while the run still reports success — this happened to all three jobs on the `workflow_dispatch` path until 2026-07-26. After any release, check the jobs actually ran and then confirm the packages resolve:

```bash
# Did the publish jobs run, or silently skip?
gh api repos/Fallout-build/Fallout/actions/runs/<run-id>/jobs \
    --jq '.jobs[] | "\(.name): \(.conclusion)"'

# Is the version really on nuget.org? (expect the version listed)
curl -s https://api.nuget.org/v3-flatcontainer/fallout.common/index.json | jq '.versions[-3:]'
```

Allow a minute or two for nuget.org to index — a package can be pushed successfully and not yet appear, especially a brand-new package ID.

### If a publish fails partway through

Each `dotnet nuget push` uses `--skip-duplicate`. Re-running a publish job is idempotent on packages already pushed. For a transient failure mid-publish:

```bash
# Routine re-run — leave publish-to-nugetorg false
gh workflow run publish-packages-release.yml --ref release/v10.4 -f tag=v10.4.0-rc.4

# Stabilised re-run — include the flag if you want to retry the nuget.org push
gh workflow run publish-packages-release.yml --ref release/v10.4 -f tag=v10.4.0-rc.4 -f publish-to-nugetorg=true
```

## Promotion and hotfixing

The ladder flows **forward-only**: `main → release/YYYY`. One routine promotion direction plus the legacy case.

### Where work lands

All work — deliberate improvements, bug fixes, and faster / AI-assisted changes alike — lands directly on `main`; there is no separate fast lane any more ([ADR-0008](adr/0008-collapse-experimental-into-main.md)). Breaking work also lands on `main`, gated behind `[Experimental("FALLOUT0xx")]` (or on a short-lived topic branch off `main` when it can't be gated), and waits for the yearly cut — it is **not** promoted to a `release/YYYY` mid-year.

### Promoting `main → release/YYYY` (a stable patch/minor)

A stabilised non-breaking change on `main` is promoted to the production line, then tagged.

```bash
git fetch
git switch -c promote-XXXX-to-v10.4 release/v10.4
git cherry-pick <sha-on-main> [<sha> …]
git push origin HEAD
gh pr create --base release/v10.4 ...   # rigorous review tier
# once merged:
gh release create v10.4.0-rc.5 --target release/v10.4 --prerelease \
    --generate-notes --notes-start-tag 10.3.47
```

### Forward-porting a stable-urgent fix

If a fix must land on the production line first (prod-down), land it on `release/v10.4`, then **forward-port** to `main` so the trunk never regresses:

```bash
git switch -c forward-port-XXXX main
git cherry-pick <fix-sha>
git push origin HEAD
gh pr create --base main ...
```

### Legacy `support/v10`

A `support/v10` security/critical fix that doesn't apply to the current line (the code has moved on) lands **directly** on `support/v10` (or the relevant `hotfix/v10.x`) via PR — the expected path for a maintenance line, not the exception. Such a release is the nuget.org case (use the opt-in flag). The same applies to a retired `support/YYYY` line.

> Even one-commit cherry-picks go through a PR — branch protection blocks direct pushes and requires the `ubuntu-latest` status check on every protected branch.

## Cutting a new year (the yearly major)

At the yearly major cut, the outgoing year's production line is retired to `support/YYYY` and a new `release/YYYY` is cut from `main`. The breaking work accumulated on `main` (gated behind `[Experimental("FALLOUT0xx")]`, plus any short-lived topic branches held for the cut) becomes the new year's major.

```bash
# 1. Retire the outgoing production line: rename release/2026 → support/2026
#    (GitHub Settings → Branches → rename, or via API). It keeps taking
#    security/critical fixes only from here on.

# 2. Cut the new production line from main
git fetch
git switch main
git pull --ff-only
git switch -c release/2027 main
git push -u origin release/2027

# 3. Nothing to do — branch protection is already in force. The "Protect
#    release/** production lines" ruleset targets refs/heads/release/**, so a
#    new release branch is protected the moment it is pushed. See
#    "Branch protection" below.

# 4. On release/2027 (the branch itself), set version.json "version": "2027.0".
#    publicReleaseRefSpec already matches "^refs/heads/release/\\d{4}$" — confirm
#    it resolves so NB.GV produces clean versions, not git-sha-suffixed.
#    Commit via PR targeting release/2027.

# 5. Roll the preview lane forward so its prereleases sort above the new production
#    line. The accumulated breaking work is already on main (gated behind
#    [Experimental] / topic branches merged in); bump the core:
#      - main/version.json → "2027.1.0-preview.{height}"
```

### Step 4 — why on `release/2027`, not `main`

`publicReleaseRefSpec` is per-branch. The CalVer ref pattern (`^refs/heads/release/\d{4}$`) matches `release/2027` automatically, but the `"version"` field is per-branch: `release/2027` pins `"2027.0"` (a public ref → clean versions) while `main` moves on to the next preview target. This keeps the production line's number stable and avoids a patch-height collision with the preview lane.

## Deprecating a `support/*` line

Once a `support/YYYY` or `support/v10` line hits end-of-life:

1. Final patch release.
2. Announce EoL in the README + CHANGELOG.
3. Leave the branch in place — don't delete it. Future archaeology + historical hotfix-on-demand should remain possible (this is why `release/v11` stays around despite being retired).
4. Optionally apply a more restrictive protection profile (e.g. require admin approval on every merge) to make accidental tags less likely.

Branches are cheap. Deletion is destructive. Default to keeping.

## Branch protection

Production branches are protected by the **"Protect release/\*\* production lines"** ruleset ([ruleset 19766406](https://github.com/Fallout-build/Fallout/rules/19766406)), which targets `refs/heads/release/**`. Because it matches on a pattern, every release line — `release/v10.4` today, a `release/2027` CalVer cut later — is protected the moment the branch exists. There is no per-branch step to remember.

It mirrors `main`'s profile: no deletion, no force-push, linear history required, PRs required with CODEOWNERS review and conversation resolution, and the `ubuntu-latest` status check. Repo admins (`RepositoryRole 5`) bypass, matching the tag ruleset.

The payload lives at [`.github/release-branch-ruleset.json`](https://github.com/Fallout-build/Fallout/blob/main/.github/release-branch-ruleset.json) so the config is reviewable rather than only visible in repo settings. To re-apply after editing it:

```bash
# Update the existing ruleset in place (preferred — keeps the ID stable)
gh api -X PUT repos/Fallout-build/Fallout/rulesets/19766406 \
    --input .github/release-branch-ruleset.json

# Verify which rules actually bind to a branch
gh api repos/Fallout-build/Fallout/rules/branches/release%2Fv10.4 --jq '[.[].type]'
```

`support/*` lines are **not** covered by this ruleset — they carry their own classic per-branch protection, applied when the line is created.

> Historical note: `release/v10.4` ran unprotected from its cut until 2026-07-26, because the on-demand cut ([ADR-0007](adr/0007-cut-release-branch-on-demand.md)) had no protection step attached. The pattern-based ruleset exists so that can't recur.

## Tag protection

A repository ruleset blocks creation/deletion/update of tags matching `v*` for non-admins ([ruleset 17017817](https://github.com/Fallout-build/Fallout/rules/17017817)). Bypass actors: repo admins (`RepositoryRole 5`). Combined with the `nuget-org` env approval gate, that's two layers of "who can fire a production release."

## See also

- [docs/agents/release-and-versioning.md](agents/release-and-versioning.md) — PR-creation flow, semver policy, release pipeline reference, branch protection settings.
- [docs/adr/0004-calendar-versioning-and-dual-pace-channels.md](adr/0004-calendar-versioning-and-dual-pace-channels.md) — the versioning + channel decision (channel ladder superseded by ADR-0008).
- [docs/adr/0008-collapse-experimental-into-main.md](adr/0008-collapse-experimental-into-main.md) — collapses the `experimental` branch and its `-alpha` channel into `main`.
- [docs/adr/0001-release-branch-model.md](adr/0001-release-branch-model.md) — the release-branch + multi-channel CD model (versioning amended by 0004).
- [milestone #13](https://github.com/Fallout-build/Fallout/milestone/13) — full work-breakdown of how this shape was implemented.
- [RFC #267](https://github.com/Fallout-build/Fallout/issues/267) — original design discussion.
- [CONTRIBUTING.md](https://github.com/Fallout-build/Fallout/blob/main/CONTRIBUTING.md) — contributor-facing flow.
