---
description: Draft (and optionally file) a terse, outcome-focused GitHub issue to the repo's canonical shape.
argument-hint: <one-line description of the problem/ask>
allowed-tools: Read, Bash(gh issue create:*), Bash(gh label list:*)
---

Read `docs/agents/issue-and-pr-style.md` and follow it as the binding style
contract. Then draft a GitHub issue for: **$ARGUMENTS**

Assemble the body to the canonical shape:

```markdown
### Problem
<1–2 sentences>

### Outcome
<observable "done">

### Acceptance criteria
- [ ] <testable>
- [ ] <testable>
```

Rules:

- Terse. Lead with the point. No preamble, no restating the title, no filler.
- Drop `Acceptance criteria` (and add a short `### Notes`) only if it doesn't
  fit the ask. Don't invent criteria — leave `- [ ]` stubs if unknown.
- Prefer links (`#123`, `path/to/file.cs:42`) over pasted blocks.

Show me the drafted title and body first. **Do not file it until I confirm.**
On confirmation, run `gh issue create --title "…" --body "…" --label enhancement`
plus the correct `target/YYYY` label (see the PR-creation flow), and report only
the resulting issue URL.
