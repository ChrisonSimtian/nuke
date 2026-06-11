# Branching and release flow

Maintainer reference for how Fallout branches, ships releases, hotfixes older lines, and uses GitHub Environments to gate publishes. Model defined by [ADR-0004](adr/0004-calendar-versioning-and-dual-pace-channels.md) (calendar versioning + dual-pace channels), amending [ADR-0001](adr/0001-release-branch-model.md) / [milestone #13](https://github.com/Fallout-build/Fallout/milestone/13) / [RFC #267](https://github.com/Fallout-build/Fallout/issues/267). The `experimental` branch and its `-alpha` channel have since been collapsed into `main` ([ADR-0008](adr/0008-collapse-experimental-into-main.md), channel ladder superseded) — `main` is now the sole prerelease lane.

> **Audience.** Repository maintainers cutting releases or hotfixing older lines. Contributors filing PRs against `main` don't need to read this — see [CONTRIBUTING.md](https://github.com/Fallout-build/Fallout/blob/main/CONTRIBUTING.md) instead. AI coding tools should read both this file and [docs/agents/release-and-versioning.md](agents/release-and-versioning.md).

## Branches at a glance

A maturity ladder feeding the production line (amended [ADR-0004](adr/0004-calendar-versioning-and-dual-pace-channels.md), 2026-05-30; the `experimental` rung collapsed into `main` per [ADR-0008](adr/0008-collapse-experimental-into-main.md)):

| Branch | Purpose | Lifetime | Protected | Source of releases? |
|---|---|---|---|---|
| `main` | **Integration trunk + sole prerelease lane (`-preview` channel).** Default branch. Both deliberate improvements / bug fixes **and** faster / AI-assisted work land here. Per-commit `…-preview` prereleases to GitHub Packages. **Never nuget.org.** Breaking work accumulates here gated behind `[Experimental("FALLOUT0xx")]` (or on a short-lived topic branch off `main`) for the yearly major. | Long-lived | Yes | **Preview only** (GitHub Packages, no nuget.org / no GH Release) |
| `release/YYYY` | **Production line** for the calendar year (e.g. `release/2026`), **cut from `main` on demand at the first release of the year, not preemptively** ([ADR-0007](adr/0007-cut-release-branch-on-demand.md)). `-rc.N` → GA. Non-breaking minors/patches only after the cut. | Cut on demand; long-lived once cut | Yes | **Yes** — tags pushed here fire the full release pipeline (nuget.org opt-in) |
| `support/v10` (+ `hotfix/v10.1`, `hotfix/v10.2`) | **Legacy** semver `10.x` maintenance line — security/critical fixes only. (Renamed from `release/v10`.) | Long-lived | Yes | Yes — tags fire the pipeline (nuget.org opt-in) |
| `support/YYYY` | **Retired** year production line (e.g. `support/2026` once 2027 supersedes it). Security/critical fixes only. | Long-lived | Yes | Yes — tags fire the pipeline (nuget.org opt-in) |
| `release/v11` | **Retired and deleted** — nothing clean shipped; work re-homed onto `2026`. Branch removed per [ADR-0007](adr/0007-cut-release-branch-on-demand.md) §6 (no unique history; dead branches are deletable, tags are the durable markers). | Deleted | — | No |
| `feature/<slug>`, `bugfix/<slug>`, `chore/<slug>`, `docs/<slug>`, `pr/<num>-<slug>` | Working branches | Short-lived; PR-and-merge then deleted | No | No |

This *is* gitflow with the project's vocabulary: `main` ≈ the integration trunk / `develop`, `release/YYYY` ≈ `release/*` (long-lived per year), `support/*` ≈ legacy/retired lines. The one deviation: **`main` is not the production/nuget.org line** — `release/YYYY` + `support/*` are. `main` is a `-preview` test channel that production is cut from.

`develop` (literal) and `master` are not used. **Breaking changes land on `main`** — gated behind the `[Experimental("FALLOUT0xx")]` attribute, or, when they can't be gated, on a short-lived topic branch off `main` — and are batched to the yearly major cut; the production-cut review is the backstop. A breaking-change PR targets `main`, never a `release/YYYY` production train. Stabilised non-breaking work is promoted **forward-only** `main → release/YYYY`. A stable-urgent fix lands on the production branch and is **forward-ported to `main`** so the trunk never regresses — see the [promotion + hotfix flow](#promotion-and-hotfixing) below.

## Channel taxonomy

Releases fire to multiple channels, each with its own GitHub Environment:

**GitHub Packages = the test/preview channel; nuget.org = production.** The version ladder orders cleanly under SemVer: `…-preview.N` < `…-rc.N` < `…` (GA) — the `-alpha` rung was retired with the `experimental` branch ([ADR-0008](adr/0008-collapse-experimental-into-main.md)).

| Channel | Built from | Cadence | Gating | Version shape |
|---|---|---|---|---|
| **preview** → `github-packages` env | `main` | Per-commit | None | `2026.1.0-preview.<height>.g<commit>` |
| **stable** → `nuget-org` env | `release/YYYY` tags | Slow, deliberate | **Flag opt-in + approval-gated** | `2026.1.3` (CalVer) |
| **stable/legacy** → `github-packages` env | `release/YYYY`, `support/*` tags | Every tag | None | CalVer / `10.x` |
| **legacy** → `nuget-org` env | `support/v10`, `support/YYYY` tags | Security/critical only | **Flag opt-in + approval-gated** | `10.x` / `YYYY.x` |
| `github-releases` env (bundled) | `release/*`, `support/*` tags | Same tag as the package publish | None | Same as the tag |
| Docker local NuGet server | Per-PR / per-commit | None (local) | PR-derived | Available via `tests/integration/docker-compose.yml` |

**Defaults:** `main` (preview) publishes to GitHub Packages only — **never nuget.org, never a GH Release**. `preview.yml` (main → `-preview`) is the only continuous publisher; the former `experimental.yml` workflow has been deleted ([ADR-0008](adr/0008-collapse-experimental-into-main.md)). Production tag pushes (`release/YYYY`, `support/*`) publish to GitHub Packages + GitHub Releases. nuget.org is **always opt-in** via the `workflow_dispatch` `publish-to-nugetorg` flag — used when a `release/YYYY` is stabilised enough for the broader consumer audience, or for a `support/v10` security patch. See [`project_release_channels` in agent memory](https://github.com/Fallout-build/Fallout/issues/267#issuecomment-4570408325) and [ADR-0004](adr/0004-calendar-versioning-and-dual-pace-channels.md).

## Cutting a release

### Routine stable release (GitHub Packages only)

The default path. Pushing a `v2026.1.X` tag to `release/2026` publishes to GitHub Packages + GitHub Releases. nuget.org is **not** touched. (Git tags keep the `v` prefix — `v2026.1.3` — so the `v*` tag-protection ruleset and `validate-ref` apply; the package version core is `2026.1.3`.)

```bash
# 1. Make sure your local release/YYYY is up to date
git fetch
git switch release/2026
git pull --ff-only

# 2. (Optional) Verify what version NB.GV will compute
dotnet nbgv get-version   # should report 2026.1.X clean, no -g<sha>

# 3. Create the tag + GitHub Release in one step
gh release create v2026.1.X \
    --target release/2026 \
    --title "v2026.1.X" \
    --generate-notes
```

That tag push triggers `.github/workflows/release.yml`:

1. **`validate-ref`** confirms the tag points at a commit reachable from a production branch (`release/YYYY` or `support/*`).
2. **`test-and-pack`** runs `dotnet fallout Test Pack`, uploads `output/packages/*.nupkg` as an artifact.
3. Three parallel publish jobs consume the artifact:
   - `publish-nuget-org` — **skipped** (not opt-in by default)
   - `publish-github-packages` — pushes **all** `*.nupkg` (Fallout.* + Nuke.*) to GitHub Packages
   - `publish-github-releases` — attaches all `*.nupkg` to the GitHub Release page

### Stabilised release (nuget.org publish)

When a `release/2026` release is stabilised enough for nuget.org, or for cutting a `support/v10` legacy security patch, use `workflow_dispatch` with the opt-in flag:

```bash
# Option A: via gh CLI
gh workflow run release.yml \
    -f tag=v2026.1.X \
    -f publish-to-nugetorg=true

# Option B: via Actions UI → release → "Run workflow" → set publish-to-nugetorg to true
```

The workflow:

1. Skips `validate-ref` (workflow_dispatch doesn't auto-validate the ref; you took the action consciously).
2. Re-runs `test-and-pack` against the named tag.
3. **`publish-nuget-org` fires** — pauses for approval at the `nuget-org` env gate (notification + entry on the run page; click "Review deployments" → check `nuget-org` → "Approve and deploy"). Then pushes Fallout.* to nuget.org.
4. `publish-github-packages` re-runs idempotently (`--skip-duplicate` skips what's already there).
5. `publish-github-releases` re-runs idempotently (uses `--clobber` for asset replacement if the GH Release already exists).

Two layers of safety on the nuget.org path: the flag opt-in + the env approval. You can also test the wiring without burning a release — set the flag, get the approval prompt, then cancel without approving.

### If a publish fails partway through

Each `dotnet nuget push` uses `--skip-duplicate`. Re-running a publish job is idempotent on packages already pushed. For a transient failure mid-publish:

```bash
# Routine re-run — leave publish-to-nugetorg false
gh workflow run release.yml -f tag=v2026.1.X

# Stabilised re-run — include the flag if you want to retry the nuget.org push
gh workflow run release.yml -f tag=v2026.1.X -f publish-to-nugetorg=true
```

## Promotion and hotfixing

The ladder flows **forward-only**: `main → release/YYYY`. One routine promotion direction plus the legacy case.

### Where work lands

All work — deliberate improvements, bug fixes, and faster / AI-assisted changes alike — lands directly on `main`; there is no separate fast lane any more ([ADR-0008](adr/0008-collapse-experimental-into-main.md)). Breaking work also lands on `main`, gated behind `[Experimental("FALLOUT0xx")]` (or on a short-lived topic branch off `main` when it can't be gated), and waits for the yearly cut — it is **not** promoted to a `release/YYYY` mid-year.

### Promoting `main → release/YYYY` (a stable patch/minor)

A stabilised non-breaking change on `main` is promoted to the production line, then tagged.

```bash
git fetch
git switch -c promote-XXXX-to-2026 release/2026
git cherry-pick <sha-on-main> [<sha> …]
git push origin HEAD
gh pr create --base release/2026 ...   # rigorous review tier
# once merged:
gh release create v2026.1.X+1 --target release/2026 --generate-notes
```

### Forward-porting a stable-urgent fix

If a fix must land on the production line first (prod-down), land it on `release/2026`, then **forward-port** to `main` so the trunk never regresses:

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

# 3. Apply branch protection (mirror main's profile — see
#    docs/agents/release-and-versioning.md → Branch protection on release/YYYY).
#    NOTE: scripts/release-branch-protection.json does not exist yet; capture
#    main's live protection JSON into it (or apply via repo Settings → Branches).
gh api -X PUT repos/Fallout-build/Fallout/branches/release/2027/protection \
    --input scripts/release-branch-protection.json

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
