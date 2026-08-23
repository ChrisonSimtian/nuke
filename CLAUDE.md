# CLAUDE.md

This project uses [`AGENTS.md`](AGENTS.md) as its canonical brief for AI coding tools. Claude Code imports it via the file-reference syntax below.

@./AGENTS.md

## Skills

Skills live under `.agents/skills/` (tool-neutral — Copilot CLI auto-discovers this folder; Claude Code does not, hence this explicit list). Read the linked `SKILL.md` when the task matches:

- [.agents/skills/creating-a-pr/SKILL.md](.agents/skills/creating-a-pr/SKILL.md) — opening a PR, writing commits, picking a base branch, writing plain terse PR/issue descriptions.
- [.agents/skills/plain-english/SKILL.md](.agents/skills/plain-english/SKILL.md) — writing any PR, commit, issue, or doc text so non-native English readers follow it on the first pass.
- [.agents/skills/adding-a-tool-wrapper/SKILL.md](.agents/skills/adding-a-tool-wrapper/SKILL.md) — adding or extending a `Tools/<Tool>/<Tool>.json` wrapper.
- [.agents/skills/marking-experimental-apis/SKILL.md](.agents/skills/marking-experimental-apis/SKILL.md) — adding public API that isn't stable yet, or deprecating one.
- [.agents/skills/editing-ci-workflows/SKILL.md](.agents/skills/editing-ci-workflows/SKILL.md) — touching `.github/workflows/**` or `build/Build.CI.GitHubActions.cs`.
- [.agents/skills/cutting-a-release/SKILL.md](.agents/skills/cutting-a-release/SKILL.md) — tagging, publishing, promoting, or cutting a release branch.
- [.agents/skills/adding-a-migration-step/SKILL.md](.agents/skills/adding-a-migration-step/SKILL.md) — adding a rename/rewrite rule to `fallout-migrate`.
