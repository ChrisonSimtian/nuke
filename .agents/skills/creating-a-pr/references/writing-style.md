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
- **Write for non-native English readers.** Plain words over idiom, short
  sentences, no slang or cultural references — many contributors read English as
  a second language. Clarity beats cleverness. Concretely:
  - **One idea per sentence.** Don't stack clauses with em-dashes or semicolons
    ("This doesn't do X — that stands — it does Y, and gives Z a path off W").
    Split into separate sentences or bullets.
  - **No idioms or figurative language.** "Blast radius", "shrinks the gap",
    "grace period", "ceiling", "shallow by design" don't translate. Say what you
    mean literally: "affects fewer consumers", "closes the gap", "temporary
    fallback", "limit", "handles the common case only".
  - **Define repo jargon on first use, or link it.** Terms like "shim",
    "sentinel", "canonical type" are fine once explained — link to the
    [glossary](../../../../docs/glossary.md) or a one-clause gloss the first
    time a PR uses them, don't assume the reader already knows the vocabulary.
  - **Spell out cross-references.** Don't lean on `#257`/`#253` alone — add a
    3–5 word gloss of what each one did ("#257, the ProjectModel rename").
  - **Prefer short, common words.** "use" over "leverage", "keep" over
    "preserve", "shows" over "surfaces", "fixes" over "remediates".
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
- **Create PRs as draft by default.** Use `gh pr create --draft` unless the user explicitly asks for a ready-for-review PR. Convert to ready only when the user explicitly requests it. This keeps incomplete work from accidentally entering review and ensures work stays flexible during early development.
- **Label the PR at creation time.** [`.github/release.yml`](../../../../.github/release.yml) is the source of truth for the changelog-category labels (`enhancement`, `bug`, `security`, `documentation`, `breaking-change`, `skip-changelog`) and a one-line blurb on each. Apply the one category that matches the change, in the same `gh pr create --label …` call — alongside the `target/vCurrent` (or `target/vNext`) process label — never as a follow-up. Don't leave a PR uncategorized; it falls through to "Other Changes".
- Add the `⚠️ Breaking change` callout **only** when the change is breaking — see the parent [SKILL.md](../SKILL.md) for what that requires.
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
| "This doesn't reclassify #257 — that break stands — it shrinks the blast radius for consumers upgrading from 11.0.1–11.0.12" | "#257 (the ProjectModel rename) is still breaking. This PR only makes upgrading past it easier for 11.0.1–11.0.12 users." |
| "Deliberately shallow… same ceiling as the generated shim" | "This only covers the common case. For everything else, use `fallout-migrate`." |
