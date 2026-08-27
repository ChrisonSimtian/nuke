---
name: cutting-a-release
description: Procedure for tagging, publishing, promoting, or cutting a new release/vX.Y branch, hotfixing a production line, or cutting the eventual v11 major. Trigger on requests to cut a release, publish to nuget.org, tag a version, hotfix main or support/v10, or promote a release branch to GA.
---

The maintainer runbook is [docs/branching-and-release.md](../../../docs/branching-and-release.md)
— read it and follow it exactly; this file is a quick index into it plus the
gotchas worth knowing before you start.

## Which section of the runbook you need

| You're asked to... | Read |
| --- | --- |
| Cut a new `release/vX.Y` and tag `-rc.N` builds | [Cutting a release](../../../docs/branching-and-release.md#cutting-a-release) |
| Publish a GA tag to nuget.org | [Publishing to nuget.org](../../../docs/branching-and-release.md#publishing-to-nugetorg) |
| Ship an emergency fix to `main` or `support/v10` | [Hotfixing production](../../../docs/branching-and-release.md#hotfixing-production) |
| Cut the eventual v11 | [Cutting v11](../../../docs/branching-and-release.md#cutting-v11-when-it-eventually-happens) |
| Retire an old `support/*` line | [Deprecating a support/* line](../../../docs/branching-and-release.md#deprecating-a-support-line) |
| First-ever publish of a new `Fallout.X` package | [First-publish gotcha](../../../docs/branching-and-release.md#adding-a-new-falloutx-package--first-publish-gotcha) |

## Gotchas worth knowing up front

- **`--ref` on `workflow_dispatch` must be the production branch**, not the
  default branch — it runs *that ref's copy* of the workflow.
- **`--notes-start-tag` must be the last GA tag**, not the previous rc, or the
  generated notes only cover since the last rc.
- **A green pipeline run is not proof anything published** — every publish job
  is conditional. Verify job conclusions and the nuget.org index after a
  release (commands in the runbook).
- **nuget.org is always opt-in** via `workflow_dispatch -f publish-to-nugetorg=true`;
  a tag push alone only reaches GitHub Packages + GitHub Releases.
- Never bypass a hotfix through direct pushes — branch protection blocks it
  everywhere; even a one-commit cherry-pick goes through a PR.
