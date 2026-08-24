# PR-creation flow — full policy background

The step-by-step procedure lives in the parent [SKILL.md](../SKILL.md). This
file is the "why" behind it: the versioning policy a breaking-change PR must
satisfy, and how milestones map to the `target/*` labels.

## Versioning policy

This project ships classic semver ([ADR-0009](../../../../docs/adr/0009-gitflow-and-semver-reversion.md)).
The rule: **breaking changes wait for the next major**, and there's no fixed
date for that — the project stays on `10.x` for as long as it can, and the
eventual move to v11 will go through `fallout-migrate`.

There is **no `CHANGELOG.md`** — the file was retired. Release notes are
generated from PR labels via [`.github/release.yml`](../../../../.github/release.yml).
The PR description and its labels are now the lasting record of a change.

- A breaking change lands on **`develop`, behind `[Experimental("FALLOUT0xx")]`**
  (or, if that doesn't fit, on a short-lived branch off `develop` held until
  the cut). It does **not** bump `version.json`'s major mid-cycle.
- **A `release/vX.Y` or `main` production line never takes a breaking change.**
  It only takes non-breaking work. The review before a production cut is the
  backstop that keeps an ungated breaking change off the production line.
- Surface that isn't ready to commit to yet can ship behind
  `[Experimental("FALLOUT0xx")]` instead of being held back — see the
  `marking-experimental-apis` skill.

**Reviewer responsibility:** if a PR carries `!` (or a flagged breaking
change), check that it targets `develop`, not a production branch. Check that
the breaking surface is behind `[Experimental("FALLOUT0xx")]` (or on a topic
branch, if it can't be gated). Check that the PR description has the
`⚠️ Breaking change` callout with a migration path. Block the PR if any of
that is missing.

## Milestones and version targeting

Milestones are **theme-based** (e.g. "Plugin Architecture Foundation &
Rebrand Completion", "Public Plugin SDK", "Continuous Delivery Vision") and
carry across releases; version targeting uses **evergreen `target/vCurrent` /
`target/vNext`** labels — `target/vCurrent` is the current release line,
`target/vNext` is the next major. A breaking change is held for the next
major, so its PR carries `target/vNext`. See [docs/roadmap.md](../../../../docs/roadmap.md)
for the current milestones.
