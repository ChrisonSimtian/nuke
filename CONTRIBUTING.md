# Contribution Guidelines

Fallout welcomes contributions. As a community, we want to help each other, provide constructive feedback, and make a better product. Our [code of conduct](https://github.com/Fallout-build/.github/blob/main/CODE_OF_CONDUCT.md) applies at all times.

> **About the project.** Fallout is the hard-fork successor to [NUKE](https://github.com/nuke-build/nuke) (originally by [Matthias Koch](https://github.com/matkoch) and contributors). Maintenance moved to a new team in 2026; the codebase is in active rebrand. See the [README](README.md) for the full backstory.

## Where to start

- Discuss non-trivial changes in an [issue](https://github.com/Fallout-build/Fallout/issues) first.
- Small fixes (typos, broken links, tool wrapper additions) can go straight to a PR against `develop`.
- **`develop` feeds the production line.** It's the default branch, and the only preview channel. Both deliberate work and faster work land here. Breaking changes land here too, behind `[Experimental("FALLOUT0xx")]` (or on a short-lived branch off `develop` when that doesn't fit), waiting for the next major. **Stable releases ship from `main`**, reached through a `release/vX.Y` branch cut on demand from `develop`. That's the nuget.org tier. The legacy `support/v10` line takes security and critical fixes only (see [Branching and release flow](docs/branching-and-release.md) and [ADR-0009](docs/adr/0009-gitflow-and-semver-reversion.md) for the full model). **Branch from, and PR against, `develop`.** The only time you target a production branch directly is for a maintainer-driven hotfix.

## Baseline contributions

- Star the [GitHub project](https://github.com/Fallout-build/Fallout/stargazers) to help others find it.
- Show the [badge](docs/website/badge.md) in your own README if you build with Fallout — copy-paste markdown, nothing to host.
- File issues with concrete reproduction steps, version info, and logs.
- Help triage existing issues — confirming bugs or pointing to fixes counts.

## Issues

### Before creating an issue

- Search existing/closed issues — your problem may already have an answer.
- Check the [releases](https://github.com/Fallout-build/Fallout/releases) for recent changes that affect your scenario.
- For tool wrappers, send a PR instead of an issue — they're mechanical to add.

### When creating an issue

- State the issue as concisely as possible.
- Use [markdown](https://docs.github.com/en/get-started/writing-on-github) for code, logs, and special text fragments.
- Avoid pasting screenshots of text — paste the text itself in a code block.

### What gets triaged first

- Bugs blocking active enterprise CI/CD usage.
- Regressions versus the last NUKE 10.x release.
- Rebrand-track work (see the [Fallout rebrand milestone](https://github.com/Fallout-build/Fallout/milestone/1)).
- Demand-driven items where multiple users have weighed in.

## Pull requests

### Before opening a PR

- Branch from `develop` (the base for all PRs). Name your branch `feature/<slug>`, `bugfix/<slug>`, or `chore/<slug>`.
- Make sure your employer allows the contribution.
- Read [AGENTS.md](AGENTS.md) for the codebase conventions — package versions go in `Directory.Packages.props`, tests live next to code, no per-file license headers (the `LICENSE` file at the root is the single source of truth). (AGENTS.md is the canonical brief for both human contributors and AI tools; GitHub Copilot reads it natively and `CLAUDE.md` points to it.)
- The bootstrappers are now thin: `./build.ps1` / `./build.sh` provision .NET if needed, then run `dotnet tool restore` + `dotnet fallout "$@"`. The `Fallout.GlobalTool` version is pinned in `.config/dotnet-tools.json`.
- Run `./build.ps1 Test` (or `./build.sh Test`, or directly `dotnet fallout Test` once your tools are restored) locally first.

### When writing the PR

- **Write functional commit and PR titles** — describe what the change accomplishes, not how it's categorised. Do not use conventional-commit prefixes (`feat:`, `fix:`, `chore:`, `refactor:`, etc.). Good examples: "Add retry logic to the HTTP tool wrapper", "Fix null-reference in target dependency resolution". The `!` suffix (e.g. `fix(security)!: …`) is recognised only as a breaking-change detection signal, not a general style requirement.
- Aim for qualitative, readable code that matches the surrounding style.
- There's no committed `.editorconfig` or ReSharper/`*.DotSettings` file — they were removed during the takeover. Rely on `dotnet format` defaults and review; don't reintroduce them without a maintainer-level decision.
- Add tests when meaningful — every `Foo` project has a sibling `Foo.Tests`.
- Commit the regenerated `.cs` output alongside the `.json` spec — `VerifyGeneratedTools` fails CI if they drift.
- **Label the PR `target/vCurrent`** for the current release line (use `target/vNext` for work held for the next major). **Breaking changes wait for the next major.** They land on `develop`, behind `[Experimental("FALLOUT0xx")]` (or, if that doesn't fit, on a short-lived branch off `develop`) — never on a `release/vX.Y` or `main` production branch. They also get a `breaking-change` label plus a `⚠️ Breaking change` callout in the PR description that names the migration path. Surface that isn't ready to commit to yet can ship behind `[Experimental("FALLOUT0xx")]` instead of being held back. See the [PR-creation flow](docs/agents/release-and-versioning.md#pr-creation-flow) for the full procedure.

### Tool wrappers

Tool wrapper JSON lives under `src/Fallout.Common/Tools/<Tool>/<Tool>.json`. When adding or extending one:

- Copy the shape from a neighbouring tool.
- Cover at least a full command with all its arguments.
- Use formatting tags in `help`:
  - `<c>` for inline code
  - `<a>` for links
  - `<ul>` / `<ol>` for lists
  - `<em>` for emphasized text
  - `<para/>` between paragraphs (not `<p>...</p>`)
- Don't write `secret: false` (it's the default).
- Don't write `default: xxx` (obsolete).
- Run `./build.ps1 GenerateTools` to regenerate the `.cs` output.
- Commit the regenerated `.cs` output alongside the `.json` spec — `VerifyGeneratedTools` fails CI if they drift.

### After opening a PR

- The PR gate is the `ubuntu-latest` job (from `build.yml`) only — fires on PRs against `develop`, `main`, `release/*`, or `support/*`. Docs-only PRs hit a no-op shim workflow (`build-skip.yml`) that reports the same status check name. `build-cross-platform.yml` runs Windows + macOS validation on `main` / `release/*` / `support/*` PRs and `v*` tag pushes (gated to release intent), not on routine `develop` work.
- **Review rises with the ladder.** PRs to `develop` (preview) get ordinary review — it's the integration trunk. A `release/vX.Y` stabilization branch (and the GA merge into `main`) gets rigorous, unhurried review — that's the project's quality gate. Match your expectations to where the PR is headed.
- Address review feedback in additional commits rather than force-pushing — easier to review the changes.
- If CI fails on something unrelated to your change, ping a maintainer.

### Merging

**Rebase is the only merge button.** Squash and plain merge commits are both disabled by repo setting. We keep a **linear history** where every reviewed commit lands on `develop` verbatim — it makes per-change diffs easy to read, keeps `git bisect`/`git blame`/revert precise, and gives change control a discrete, traceable record per logical change (a squash would collapse that into one opaque commit).

- **Curate your commits before final approval.** Everything you rebase onto `develop` becomes a permanent bisect target, so aim for one logical change per commit. Run `git rebase -i` to fold "address review feedback" / "fix typo" noise into the commits it belongs to.
- **During review, prefer additional commits** (see above) for easy diffing, then tidy them into a clean sequence before the final approval and merge.

The merger (typically a CODEOWNER) clicks **Rebase and merge** — there's no button to choose.

## Releases

Merging to `develop` publishes a **preview prerelease** (`10.MINOR.PATCH-preview.…`) to **GitHub Packages only** — never nuget.org. A `release/vX.Y` branch, cut on demand from `develop`, publishes **rc prereleases** (`-rc.N`) while it stabilizes. Merging that branch into `main` and tagging it there fires the **stable release**: GitHub Packages and GitHub Releases by default, nuget.org by opt-in. The full lifecycle is documented in [docs/branching-and-release.md](docs/branching-and-release.md):

- How releases happen (cut `release/vX.Y` from `develop`, stabilize, merge to `main`, tag, parallel publish jobs)
- The channel taxonomy (preview/rc → GitHub Packages; stable → GitHub Packages + GitHub Releases, nuget.org opt-in; Docker local for pre-merge)
- Promotion + hotfix flow (`develop → release/vX.Y → main`, merged back into `develop`; the legacy `support/v10` line takes security/critical fixes directly)
- When to cut a new major

Contributors don't usually need to do any of this — releases are maintainer-driven. But if you're filing a fix for the legacy `support/v10` line, or one that carries a breaking change held for the next major, expect the maintainer to route it accordingly.
