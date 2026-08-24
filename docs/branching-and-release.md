# Branching and release flow

Maintainer reference for branching, releasing, hotfixing, and the GitHub Environments that gate publishes. The model is [ADR-0009](adr/0009-gitflow-and-semver-reversion.md) (classic GitFlow, staying on semver `10.x`), which replaces [ADR-0004](adr/0004-calendar-versioning-and-dual-pace-channels.md) and changes part of [ADR-0001](adr/0001-release-branch-model.md) ([milestone #13](https://github.com/Fallout-build/Fallout/milestone/13), [RFC #267](https://github.com/Fallout-build/Fallout/issues/267)). It keeps [ADR-0007](adr/0007-cut-release-branch-on-demand.md)'s on-demand cut and [ADR-0008](adr/0008-collapse-experimental-into-main.md)'s decision against a separate `experimental` branch.

> **Audience.** Maintainers cutting releases or hotfixing older lines. Contributors filing PRs against `develop` don't need this — see [CONTRIBUTING.md](https://github.com/Fallout-build/Fallout/blob/main/CONTRIBUTING.md). An AI tool asked to cut a release should use the `cutting-a-release` skill (`.agents/skills/cutting-a-release/SKILL.md`), which follows this doc.

## Branches

`develop` and `main` are **renames** of branches that already existed (`main` and `release/v10.4`) — nothing was created from scratch.

| Branch | Purpose | Cut from / merges from | Publishes | Ships to |
|---|---|---|---|---|
| `develop` (was `main`) | Integration trunk. All work lands here, including fast/AI-assisted work. Default branch. | — | Every push: `10.5.0-preview.<height>.g<sha>` | GitHub Packages only |
| `release/vX.Y` | Stabilizes the next release. Cut on demand, at the first release of that minor — not ahead of time ([ADR-0007](adr/0007-cut-release-branch-on-demand.md)). Only non-breaking fixes land here. | `develop` | `-rc.N` prereleases | GitHub Packages |
| `main` (was `release/v10.4`) | Production trunk. Receives only `release/vX.Y` (GA) or `hotfix/vX.Y.Z` merges. | `release/vX.Y`, `hotfix/*` | GA tags | GitHub Packages + Releases, nuget.org opt-in |
| `support/v10` (+ `hotfix/v10.x`) | Older legacy line, versions before `10.4`. Security/critical fixes only. Separate from `main` — not affected by this model. | — | Tags | Same as `main` |
| `support/vN` | What a major becomes once a later major replaces it. Not in use yet — the project hasn't cut a major. | — | Tags | Same as `main` |
| `feature/<slug>`, `bugfix/<slug>`, `chore/<slug>`, `docs/<slug>` | Working branches, PR'd against `develop`. | `develop` | — | — |

Work moves **forward-only**: `develop → release/vX.Y → main`. A breaking change lands on `develop` behind `[Experimental("FALLOUT0xx")]` (or a short-lived branch off `develop`) and waits for v11 — there's no fixed date; see [ADR-0009](adr/0009-gitflow-and-semver-reversion.md). `master` is not used.

## Versioning

`version.json`'s `version` field is set per branch:

- `develop` uses `{height}`, so its preview build always moves forward on its own: `10.5.0-preview.{height}`.
- A `release/vX.Y` branch pins its prerelease number by hand — e.g. `10.5.0-rc.4`, not `10.5.0-rc.{height}`. **Bump it in a PR before tagging.** Two reasons: every commit reports the same version until you bump it (so the number reflects release intent, not commit count), and tagging twice without bumping silently republishes the same version (`--skip-duplicate` swallows it).
- `publicReleaseRefSpec` matches `main`, `release/v\d+(\.\d+)?`, and `support/v\d+` — not `develop`. That's why `develop`'s previews carry the `.g<sha>` suffix (non-public build) while production lines don't.

`GitVersion` is still installed as a transitional helper for `MajorMinorPatchVersion` in `Build.cs`; full removal is a follow-up.

## Cutting a release

1. **Cut the branch** (only when the release is actually ready — [ADR-0007](adr/0007-cut-release-branch-on-demand.md)):
   ```bash
   git fetch
   git switch develop && git pull --ff-only
   git switch -c release/v10.5 develop
   git push -u origin release/v10.5
   ```
   Branch protection attaches automatically (see [Protection](#protection) below) — nothing else to do.

2. **Bump `version.json` and tag an rc** on the release branch:
   ```bash
   # version.json: "version": "10.5.0-rc.1"
   PublicRelease=1 dotnet nbgv get-version -v NuGetPackageVersion   # confirm: 10.5.0-rc.1
   gh release create v10.5.0-rc.1 --target release/v10.5 --title "v10.5.0-rc.1" \
       --prerelease --generate-notes --notes-start-tag <last-GA-tag>
   ```
   Repeat with `rc.2`, `rc.3`, ... as fixes land, cherry-picked or PR'd onto the release branch.

   > **`--notes-start-tag` must be the last GA tag, not the previous rc.** Left alone, `gh` picks the most recent tag, so an rc's notes only show what changed since the *last rc* — anchoring on the last GA shows the full set of changes since the last real release.

3. **Merge to `main` and tag GA** once the release branch is ready:
   ```bash
   gh pr create --base main --head release/v10.5 ...   # rigorous review
   # once merged:
   gh release create v10.5.0 --target main --generate-notes --notes-start-tag <last-GA-tag>
   # merge main back into develop so the trunk carries the GA commit:
   gh pr create --base develop --head main ...
   ```

Both tag pushes (step 2 and step 3) fire `.github/workflows/publish-packages-release.yml`: `validate-ref` confirms the tag is reachable from `main`, `release/v*`, or `support/*`; `test-and-pack` builds and uploads the packages; then three jobs publish in parallel — `publish-github-packages` (all packages, always), `publish-github-releases` (attaches to the GH Release, always), and `publish-nuget-org` (**skipped unless you opt in**).

### Publishing to nuget.org

nuget.org is always opt-in, via `workflow_dispatch`:

```bash
gh workflow run publish-packages-release.yml --ref main -f tag=v10.5.0 -f publish-to-nugetorg=true
```

> **`--ref` must be the production branch, not the default branch.** `workflow_dispatch` runs whichever branch's *copy of the workflow* you dispatch against — `-f tag=` only picks which source gets packed. Dispatching against `develop` runs `develop`'s copy of the pipeline, which can differ from the release branch's.

This re-runs `test-and-pack` (skipping `validate-ref`, since dispatch is a conscious action), then `publish-nuget-org` pauses for approval at the `nuget-org` environment gate before pushing. The other two jobs re-run idempotently. Re-running the same command later (e.g. after a transient failure) is safe: every `dotnet nuget push` uses `--skip-duplicate`, and omitting `-f publish-to-nugetorg=true` re-runs everything except the nuget.org push.

> **A green run is not proof anything published.** Every publish job is conditional and can silently skip on a misconfigured condition. After a release, verify:
> ```bash
> gh api repos/Fallout-build/Fallout/actions/runs/<run-id>/jobs --jq '.jobs[] | "\(.name): \(.conclusion)"'
> curl -s https://api.nuget.org/v3-flatcontainer/fallout.common/index.json | jq '.versions[-3:]'
> ```
> Allow a minute or two for nuget.org to index a new package.

One known gap in release notes: work promoted by cherry-pick gets new commit SHAs, so GitHub can't map it back to its original PR — it shows up as the cherry-pick's own PR instead. Add a short manual summary above the generated notes if that matters for a given release.

## Hotfixing production

If production needs a fix before the next scheduled release, cut a `hotfix/vX.Y.Z` branch from `main`, fix it, tag it there, then forward-port the same fix to `develop` so the trunk doesn't fall behind:

```bash
git switch -c hotfix-v10.5.1 main
git cherry-pick <fix-sha>
gh pr create --base main ...
# once merged and tagged v10.5.1 on main:
git switch -c forward-port-XXXX develop
git cherry-pick <fix-sha>
gh pr create --base develop ...
```

A `support/v10` fix (for a version older than `10.4`, where the code has already moved on) lands directly on `support/v10` (or `hotfix/v10.x`) the same way — that's the normal maintenance path, not an exception. Even a one-commit cherry-pick goes through a PR; branch protection blocks direct pushes everywhere.

## Cutting v11 (when it eventually happens)

This follows the same steps as [cutting a release](#cutting-a-release) above, plus one difference: the breaking work that's been waiting on `develop` (behind `[Experimental]`, or on short-lived topic branches) is what goes into the release branch. `release/v11.0` is still cut from `develop` the same way, on demand, and still merges to `main` for GA the same way.

Once v11 ships, `10.x` consumers need a line that keeps getting security fixes without being forced onto v11. `support/v10` already exists for pre-`10.4` versions, so the last `10.x` minor (the one `main` was on right before the v11 merge) needs its own line — the exact branch name is a decision for when this actually happens, not something this doc pins down in advance.

Consumers upgrading from `10.x` to v11 use `fallout-migrate` — the same tool that already handles the NUKE-to-Fallout move, extended to cover whatever v11 changes.

## Deprecating a `support/*` line

Once a `support/*` line hits end-of-life: ship a final patch, announce it in the README, and leave the branch in place — don't delete it (branches are cheap; deletion is destructive). A genuinely dead branch with no unique history may be deleted per [ADR-0007](adr/0007-cut-release-branch-on-demand.md) §6. Optionally tighten its protection (e.g. require admin approval on every merge) to make accidental tags less likely.

## Protection

`develop`, `main`, every release line, and every `support/*` branch share the
same protection profile:

- Required status check: `ubuntu-latest`
- Linear history required (no merge commits)
- CODEOWNER review required (0 additional approvals)
- Direct pushes blocked (PRs only)
- Force-push and branch deletion blocked
- Conversation resolution required
- Admins not enforced (admins can bypass in emergencies)

Stale approvals are **not** dismissed when new commits land (`dismiss_stale_reviews: false`).

**Release branches** (`release/v10.5`, and so on) are covered by the **"Protect release/\*\* production lines"** ruleset ([19766406](https://github.com/Fallout-build/Fallout/rules/19766406)), targeting `refs/heads/release/**` — protection attaches the moment a branch is pushed, nothing to apply by hand. It mirrors `main`'s profile above. Repo admins bypass. The payload lives at [`.github/release-branch-ruleset.json`](https://github.com/Fallout-build/Fallout/blob/main/.github/release-branch-ruleset.json); re-apply after editing it with:

```bash
gh api -X PUT repos/Fallout-build/Fallout/rulesets/19766406 --input .github/release-branch-ruleset.json
```

**`develop`, `main`, and `support/*`** carry their own classic per-branch protection, configured individually rather than by pattern.

**Tags** matching `v*` are protected by a separate ruleset ([17017817](https://github.com/Fallout-build/Fallout/rules/17017817)) — only repo admins can create, delete, or update one. Combined with the `nuget-org` environment's approval gate, that's two layers on who can ship to production.

> **Maintainer follow-up.** This doc describes the target shape. Three GitHub-settings changes still need doing, separately from this repo's docs/CI: rename `main` to `develop`, rename `release/v10.4` to `main`, and make `develop` the default branch (which re-points `develop`'s branch protection from what used to apply to `main`). See [ADR-0009](adr/0009-gitflow-and-semver-reversion.md)'s Negative consequences.

## Adding a new `Fallout.X` package — first-publish gotcha

nuget.org's `Fallout.*` prefix reservation is per-ID, not per-prefix-wildcard: CI's first `nuget push` for any never-published `Fallout.X` package ID returns `403 (does not have permission to access the specified package)` until someone manually web-uploads one nupkg to register the ID. **Two traps when doing that upload:**

1. **Set the package owner to the org, not your personal account.** The nuget.org upload UI doesn't prompt you; ownership defaults to the uploading user's profile. If you forget, the package ID is reserved but the org's `NUGET_API_KEY` still 403s on subsequent pushes (the key is scoped to org-owned packages). Fix via `Manage Package → Owners → Add owner → <org>` then optionally remove your personal account. Or upload using credentials of the org's service account directly. See [#208](https://github.com/Fallout-build/Fallout/issues/208) for what this looks like when it goes wrong.
2. **Validation can lag** the upload by 5–30 minutes. The package page may say "approved" while the API key permission hasn't propagated yet. Wait, then rerun the release pipeline (`gh run rerun <id> --failed`); `--skip-duplicate` makes the retry safe for already-published packages.

## See also

- [docs/adr/0009-gitflow-and-semver-reversion.md](adr/0009-gitflow-and-semver-reversion.md) — the branching/versioning decision.
- [docs/adr/0001-release-branch-model.md](adr/0001-release-branch-model.md) — the release-branch + multi-channel CD model.
- [milestone #13](https://github.com/Fallout-build/Fallout/milestone/13) — the original work-breakdown.
- [RFC #267](https://github.com/Fallout-build/Fallout/issues/267) — original design discussion.
- [CONTRIBUTING.md](https://github.com/Fallout-build/Fallout/blob/main/CONTRIBUTING.md) — contributor-facing flow.
- `.agents/skills/cutting-a-release/SKILL.md` — the on-demand agent procedure that follows this doc.
