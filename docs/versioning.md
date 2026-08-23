# Versioning

Where our version numbers come from, why some are automatic and some are not, and the two traps that have bitten us in production.

> [!NOTE]
> This document has two halves. **Current state** describes what the repository does *today* — it is kept true, and if it drifts from reality that's a bug. **North Star** describes where we intend to go next; nothing in it is implemented.

---

# Current state

We use [Nerdbank.GitVersioning](https://github.com/dotnet/Nerdbank.GitVersioning) (nbgv). It's wired in centrally — `Directory.Build.props` adds the `PackageReference`, `Directory.Packages.props` pins the version — so no project opts in individually. The single source of truth for the number is [`version.json`](../version.json), and it's **per-branch**: each long-lived branch carries its own copy with its own `version` field.

We ship **Semantic Versioning on the `10.x` line.**

## The one-paragraph version

`main` produces previews with an automatic, always-moving number. Release branches produce releases with a number we pin by hand. That's the whole model — the rest of this half is why.

## What each branch produces

| Branch | `version.json` `version` | Produces | Published to |
|---|---|---|---|
| `main` | `10.5.0-preview.{height}` | `10.5.0-preview.<height>.g<commit>` | GitHub Packages only |
| `release/v10.4` (last GA) | `10.4.0` | `10.4.0` | GitHub Packages + Releases; nuget.org opt-in |
| `support/v10` (legacy) | `10.3` + `versionHeightOffset: 24` | `10.3.x` | security/critical only |

A release branch pins the **full** version, prerelease segment included — `10.4.0-rc.4`, then `10.4.0` at GA — not just the `MAJOR.MINOR` core.

## Height is automatic — and only shows up on previews

`{height}` is **git height**: the number of commits since the `version` field in `version.json` last changed. nbgv computes it. Nobody sets it by hand.

That placeholder only appears on `main`, deliberately — a per-commit preview lane needs a number that moves on its own, without a human bumping anything.

Release branches don't use it. `release/v10.4` pins `"version": "10.4.0"`, so the number comes straight out of the file and nbgv appends nothing.

> [!IMPORTANT]
> **Changing the `version` field resets the height to zero.** The next preview after such a change restarts at `.1`, not wherever the count had reached. Fine as long as the new core sorts *above* the old one, but it's a visible discontinuity — say so in the commit message.

There is exactly one place we nudge height by hand: `support/v10` carries `"versionHeightOffset": 24`, keeping that line's numbering continuous across an earlier restructure.

## `-rc.N` is pinned by hand, on purpose

On a release branch the prerelease number is written literally — `10.5.0-rc.1`, then `10.5.0-rc.2` — and you bump it yourself for each candidate.

We tried letting `{height}` drive it, and the v10.4 cycle is the record of why it doesn't work. `v10.4.0-rc.1` and `-rc.3` were cut with `"version": "10.4.0-rc.{height}"`; the number then tracks *however many commits a promotion happened to carry* rather than release intent, and on the first 19-commit promotion it sent `rc.3` straight to `rc.23`. From `-rc.4` onward the field was pinned literally (`"10.4.0-rc.4"`, `"10.4.0-rc.5"`, then `"10.4.0"`), and that's the practice now.

Two consequences:

- Every commit on a release branch reports the same version until you bump. Intended.
- **Tagging twice without bumping republishes an existing version.** `dotnet nuget push --skip-duplicate` swallows that silently, so packages simply don't update and nothing fails. Check the number before you tag.

## Trap 1 — the preview core must stay ahead of the last GA

`main` holds newer code than any release branch, so its previews must sort *above* everything shipped. They're compared as ordinary SemVer, and a prerelease sorts **below** the release of the same core: `10.4.0-preview.9` < `10.4.0`.

So once `10.4.0` is GA, `main` must move to a core that hasn't shipped — `10.5.0-preview.{height}`. If it stays on the shipped core, or an older one, anyone who has seen the GA will never be offered a preview again.

We have shipped this bug. `main` sat at `10.0.0-preview.{height}` — a core that predated the whole 10.4 line — so its previews sorted below every release on the shelf.

It's fixed, and the fix is the practice to copy: `10.4.0` was tagged at 10:28:56 on 2026-08-07, and `eeeca700` ("Move main's preview lane onto the 10.5 core") landed twenty seconds later. Rolling the core forward was part of the cut, not a follow-up.

> [!WARNING]
> **Rolling `main`'s core forward is part of cutting a release line, not an afterthought.** The moment `release/v10.N` is cut, `main` moves to `10.(N+1).0-preview.{height}`. Do it in the same sitting — the twenty-second gap above is the standard to hold.

## Trap 2 — tag builds are detached, and nbgv notices

`publicReleaseRefSpec` decides whether nbgv considers a build "public". Public builds get a clean number; non-public builds get a `.g<commit>` suffix appended so they can't be mistaken for a release.

Every entry in that list is a **branch** ref, and nbgv never matches the spec against `refs/tags/*`. The release workflow checks out the tag — a detached HEAD — which therefore matches nothing, and nbgv treats a production release build as non-public.

That is how `v10.4.0-rc.3` shipped to consumers as `10.4.0-rc.3.geabd043cc2`.

The fix is already in [`publish-packages-release.yml`](../.github/workflows/publish-packages-release.yml): the Test+Pack step sets `PublicRelease: true` explicitly. **Don't remove it**, and if you add another job that packs from a tag, set it there too.

`main` is deliberately *absent* from `publicReleaseRefSpec`. That's what gives previews their `.g<commit>` suffix, and it's intentional — previews should never look like releases.

## Branch naming patterns

nbgv's `release.branchName` is `release/v{version}`, so the tooling and the conventions below agree. (It was `release/{version}` until this document landed — which would have generated `release/10.5`, missing the `v`.)

| Pattern | Example | Meaning |
|---|---|---|
| `release/v<major>.<minor>` | `release/v10.5` | Production line. Cut on demand at first release, not preemptively. |
| `support/v<major>` | `support/v10` | Legacy maintenance line. Security and critical fixes only. |
| `hotfix/v<major>.<minor>` | `hotfix/v10.4` | Short-lived fix branch off a support line. |
| `feature/<slug>`, `bugfix/<slug>`, `chore/<slug>`, `docs/<slug>` | `feature/plugin-host` | Short-lived, target `main`, rebase-merged. |

Tags carry a `v` prefix (`v10.5.0`) so the `v*` tag-protection ruleset applies. The package version core is the bare number (`10.5.0`).

## Cutting the next line

```bash
# 1. Cut the line from main.
git switch main && git pull upstream main
git switch -c release/v10.5

# 2. Pin the first candidate literally — no {height}, no bare core.
#    version.json -> "version": "10.5.0-rc.1"
#    release/v10.5 matches ^refs/heads/release/v\d+\.\d+$ in publicReleaseRefSpec,
#    so it is a public ref and gets clean numbers.
git commit -am "Pin release/v10.5 to 10.5.0-rc.1"
git push -u upstream release/v10.5

# 3. Roll main forward IN THE SAME SITTING. See Trap 1.
git switch main
#    version.json -> "version": "10.6.0-preview.{height}"
git commit -am "Move main's preview lane onto the 10.6 core"
git push upstream main

# 4. Verify, then tag the candidate.
git switch release/v10.5
dotnet nbgv get-version          # expect clean 10.5.0-rc.1 — no -g<sha> suffix
git tag v10.5.0-rc.1
git push upstream v10.5.0-rc.1

# 5. Each further candidate: bump the pinned field BY HAND, then tag.
#    version.json -> "version": "10.5.0-rc.2"
git commit -am "Bump release/v10.5 to 10.5.0-rc.2"
git tag v10.5.0-rc.2 && git push upstream v10.5.0-rc.2

# 6. GA: drop the prerelease segment, then tag.
#    version.json -> "version": "10.5.0"
git commit -am "Pin release/v10.5 to 10.5.0 for GA"
git tag v10.5.0 && git push upstream v10.5.0

# 7. nuget.org is opt-in — tags alone never publish there.
gh workflow run publish-packages-release.yml \
  --repo Fallout-build/Fallout \
  -f tag=v10.5.0 -f publish-to-nugetorg=true
```

> [!CAUTION]
> Step 5 is the one people skip. Tagging twice without bumping the pinned field republishes an existing version, and `--skip-duplicate` swallows the failure silently — the release "succeeds" and ships nothing.

## Unstable public surface

`[Experimental("FALLOUT0xx")]` marks opt-in public API that carries no stability guarantee — adding or removing it is not a breaking change. The mechanism is wired up and the diagnostic-ID registry exists ([docs/experimental-apis.md](experimental-apis.md)); today it's used in one place, `FALLOUT001` on `IPublish`.

---

# North Star

Not implemented. This is the direction, recorded so the gap stays visible rather than being rediscovered each time.

## Calendar versioning

Move from semver `10.x` to **`YYYY.MINOR.PATCH`** — mechanically valid SemVer 2.0 (all three components numeric), so nbgv, NuGet, and version ordering keep working unchanged. The major *is* the calendar year.

| | Current | North Star |
|---|---|---|
| Preview lane | `10.5.0-preview.{height}` | `YYYY.MINOR.0-preview.{height}` |
| Production line | `release/v10.5` pinning `"10.5.0-rc.N"` → `"10.5.0"` | `release/YYYY` pinning `"YYYY.MINOR.P-rc.N"` → `"YYYY.MINOR.P"` |
| Legacy line | `support/v10` | `support/v10` unchanged; retired years become `support/YYYY` |
| Next major | `10.6`, `10.7`, … | the yearly cut |
| Breaking changes | batched to the next major, no fixed date | batched to the **yearly** major cut |

`publicReleaseRefSpec` already carries the `\d{4}` patterns (`^refs/heads/release/\d{4}$`, `^refs/heads/support/\d{4}$`). They match nothing today and cost nothing — adopting CalVer needs no change to that field.

The full rationale is [ADR-0004](adr/0004-calendar-versioning-and-dual-pace-channels.md); [ADR-0012](adr/0012-current-state-semver-10x-north-star-calver-gitflow.md) records why it is deferred rather than abandoned.

> [!NOTE]
> **The burned `11.0.x` range is a footnote, not a blocker.** `11.0.1`–`11.0.18` were published then unlisted, and NuGet never frees a version that has existed. Under CalVer the next major is the yearly cut (e.g. `2027.0.0`), which never touches the `11.x` space — so the range is simply never revisited.

## Routine `[Experimental]` gating

Today the attribute exists and is used once. The intent is that **every breaking surface is gated behind it before landing**, so breaking work can accumulate on the integration trunk without a separate branch — the discipline ADR-0008 assumed when it retired the `experimental` lane.

## Where the versioning North Star meets the branching one

CalVer pairs with the GitFlow North Star in [branching-and-release.md](branching-and-release.md). Under full GitFlow the preview lane publishes from `develop` rather than `main`, which means `version.json`'s `{height}` core and `publicReleaseRefSpec`'s exclusion both move to `develop`. That is a direct conflict with [ADR-0008](adr/0008-collapse-experimental-into-main.md) ("`main` is the sole prerelease lane") and will need a superseding ADR when we adopt it — the two North Stars have to land together or not at all.

## See also

- [branching-and-release.md](branching-and-release.md) — how branches flow and how to publish a release
- [ADR-0012](adr/0012-current-state-semver-10x-north-star-calver-gitflow.md) — why `10.x` now, CalVer + GitFlow next
- [ADR-0004](adr/0004-calendar-versioning-and-dual-pace-channels.md) — the deferred calendar-versioning decision
- [docs/experimental-apis.md](experimental-apis.md) — the `FALLOUT0xx` diagnostic-ID registry
