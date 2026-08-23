---
name: creating-a-pr
description: Steps for opening a pull request in the Fallout repo — picking the right base branch, creating it as a draft, applying the target/vCurrent-or-vNext and changelog-category labels, and handling breaking changes. Trigger whenever you are about to run `gh pr create`, decide a branch/base, write commit messages, or write a PR/issue description.
---

Follow this exactly when opening a PR. Don't skip the labelling — it's easy to
drop because it's just flags on the same `gh pr create` call.

## 0. Working from a fork?

Check `git remote -v`. If it shows both `origin` (your fork) and `upstream`
(`Fallout-build/Fallout`), branch from `upstream/develop` — never
`origin/develop`, which can be far behind and cause needless conflicts:

```bash
git fetch upstream develop
git switch -c <branch> upstream/develop
git push -u origin <branch>
gh pr create --repo Fallout-build/Fallout --draft ...
```

Skip this for a plain single-remote clone.

## 1. Create as a draft

`gh pr create --draft` unless the user explicitly asked for ready-for-review.

## 2. Label at creation time, not as a follow-up

- **`target/vCurrent`** (default) or **`target/vNext`** (breaking changes — see
  below) — pass `--label target/vCurrent`.
- **One changelog-category label** from [`.github/release.yml`](../../../.github/release.yml):
  `enhancement`, `bug`, `security`, `documentation`, `breaking-change`, or
  `skip-changelog` for housekeeping. Don't leave a PR uncategorized — it falls
  through to "Other Changes".

## 3. Breaking change? Do all of this too

A change is breaking if a commit uses the `!` suffix, has a `BREAKING CHANGE:`
footer, or a reviewer would reasonably flag it (renamed/removed public API,
package ID change, on-disk format change, CI/CD shape change consumers depend
on) — except changes to `[Experimental]` surface, which carries no guarantee.

1. `--label target/vNext --label breaking-change` (use `breaking-change`
   instead of `enhancement`/`bug` as the changelog category).
2. Open the PR body with a `⚠️ Breaking change` callout: name the affected
   surface and the consumer-side impact in one sentence.
3. **Target `develop`**, never `release/vX.Y` or `main` — confirm the breaking
   surface sits behind `[Experimental("FALLOUT0xx")]` (see the
   `marking-experimental-apis` skill), or, if it can't be gated, on a
   short-lived branch off `develop` held for the next major. Don't bump
   `version.json`'s major — that happens once, at the cut.
4. Spell out the migration path (one paragraph minimum) — what a consumer
   changes and what they run. There's no `CHANGELOG.md`; the `breaking-change`
   label is what carries this into the generated release notes.

If you only discover the breaking nature mid-review, apply all of this before
requesting re-review.

## Writing the description

Follow [references/writing-style.md](references/writing-style.md) for the
terse, scannable shape (issues too) — lead with the point, bullets over
prose, link don't recap.

## Full policy reference

[references/pr-creation-flow.md](references/pr-creation-flow.md) has the
complete versioning-policy and milestone-labelling background behind the
steps above, if you need the "why".
