# Issue & PR writing style

The single source of truth for how issues, user stories, and pull-request
descriptions are written in this repo — by humans and by AI tools alike.

Goal: **terse, scannable, human-readable.** A busy maintainer should get the
point on the first screen, on a phone, without scrolling. The GitHub issue
forms (`.github/ISSUE_TEMPLATE/*.yml`) define the canonical *shape* for humans;
this doc defines the *style* and is what AI tools are bound to (a `.yml` form
does not constrain an agent running `gh issue create`).

## Principles (apply to issues and PRs)

- **Lead with the ask in one line.** First sentence = what and why. Everything
  else is support.
- **Match length to substance.** A one-line fix gets a one-line description.
  There is no minimum length to hit.
- **Cut filler.** No preamble, no restating the title, no hedging, no
  marketing tone ("elegant", "robust", "seamlessly"), no emoji section headers.
- **Bullets over prose** for anything enumerable.
- **Link, don't recap.** Reference issues (`#123`), PRs, docs, and code
  (`path/to/file.cs:42`) instead of pasting them.
- **Describe outcomes, not your process.** What changed and why it matters —
  not the journey you took to get there.
- **Cut what the reader can get elsewhere.** If the diff, a linked issue, or the
  discussion thread already carries it, don't repeat it — reference and
  summarize. Keep only what the reader *can't* get without you. This is the
  single best test when deciding whether a line earns its place.
- **It's probably just an issue.** Don't reach for RFC or ADR framing by
  default — most work is a plain story, task, or idea. Reserve RFC/ADR for
  genuinely cross-cutting decisions that need a durable record. When in doubt,
  write a plain issue.

## Issue / user story shape

```markdown
### Problem
<1–2 sentences: what's wrong or missing, and for whom>

### Outcome
<what "done" looks like — observable behaviour, not implementation>

### Acceptance criteria
- [ ] <testable>
- [ ] <testable>
```

Optional `### Notes` (≤3 lines) for links or constraints. **Drop any section
that doesn't apply** rather than padding it.

If there are genuinely open questions, add a short `### Open questions` list
(a handful of one-liners). Do **not** stage a `D1`/`D2`/… decision record in the
issue body — decisions get made in the comment thread or the PR, not
pre-memorialized in the spec before anyone has replied.

## PR description shape

```markdown
<one line: what this PR does and why>

### What changed
- <short bullet — not a file-by-file diff narration>
- <short bullet>

### Why
<only if non-obvious from the summary>

Closes #<issue>
```

- **Link the issue it implements** (`Closes #123`, or `Part of #123` for one PR
  in a series). Summarize the need in a line — don't recite the issue's
  Problem/Outcome/criteria back; the reader can click through. The PR explains
  *the change*; the issue holds *the requirement*.
- Add the `⚠️ Breaking change` callout **only** when the change is breaking —
  see the [PR-creation flow](release-and-versioning.md#pr-creation-flow) for
  what that requires.
- **Don't** restate the title, paste large code/log blocks, recount your
  process, or enumerate every touched file — the diff already shows that.
- Keep a `### Verification` line (what you actually ran) and, for a PR in a
  series, a short follow-ups list — those are the bits *not* visible in the diff.

## Anti-patterns

| Instead of… | Write… |
| --- | --- |
| "This PR introduces a comprehensive refactor that…" | "Replaces reflection dispatch with `IFalloutCommand`." |
| Three paragraphs restating the title | One line, then bullets |
| Pasting the full stack trace inline | Link to the run / collapse in `<details>` |
| "As part of this work, I also…" | A second bullet, or a second PR |
