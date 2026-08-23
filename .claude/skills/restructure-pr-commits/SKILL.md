---
name: restructure-pr-commits
description: "Restructure the commits on an existing PR into focused, reviewable commits"
---

Restructure the commits on PR `<number>` into focused, reviewable commits.
Follow these steps in order exactly.

## 1 — Find what the PR actually changes

Run:
```
git diff upstream/main HEAD --stat
```

This is the authoritative list of files the PR modifies. `git log` is misleading
on branches kept up to date via merge: merge commits drag upstream history into the
log as if it were authored on this branch — it isn't.

## 2 — Find the branch's own commits

Run:
```
git log --first-parent --no-merges upstream/main..HEAD --oneline
```

This shows only commits made directly on this branch.

## 3 — Create a backup branch

```
git branch backup-pr-<number> HEAD
```

Do this before changing anything. Never skip this step.

## 4 — Build the restructured history

Check out a fresh branch from `upstream/main`:
```
git checkout upstream/main -b restructured-pr-<number>
```

Then restore the changed files from the backup:
```
git checkout backup-pr-<number> -- <files from step 1>
```

Handle renames explicitly: `git rm <old>` then `git checkout backup-pr-<number> -- <new>`.

Commit in logical groups — one concern per commit. Commit message rules (from the project's AGENTS.md):
- Functional imperative phrases; no conventional-commit prefixes (`fix:`, `feat:`, etc.)
- Subject completes "This commit will…" without saying so
- Body (when needed) explains *why*, not *what*; no file/class/type names

## 5 — Verify file content is unchanged

Run both and report the output:
```
git diff HEAD backup-pr-<number>
git diff HEAD upstream/main --stat
```

The first command must produce **no output**; if it does, stop and investigate before continuing.
The second must list **exactly the same files** as step 1.

## 6 — Stop and wait for explicit confirmation

Show the new `git log --oneline` and ask the user to confirm the history looks
correct. Do not proceed until the user explicitly says yes.

## 7 — Push with --force-with-lease only after confirmation

```
git push origin HEAD:<pr-branch-name> --force-with-lease
```

Never use `--force` alone.

## 8 — Update the PR title and description after confirmation

Rewrite the PR title and description to match the restructured commits.
Structure: Summary → one Changes section per commit → optional Combined effect.
Each Changes section maps 1-to-1 to a commit. Apply the PR-creation flow from
`docs/agents/release-and-versioning.md` for labels and breaking-change callouts.
