---
name: plain-english
description: >-
  Write clear, plain English for developer-facing prose aimed at an
  international audience. Use this skill whenever you draft or review a pull
  request description, commit message, changelog or release-note entry, issue
  report, code-review comment, or README / docs prose — especially in
  open-source repositories where many contributors read English as a second
  language. Trigger it even when the user does not say "plain English": any
  time the deliverable is short technical writing that other people will read,
  apply these rules. Also use it to rewrite or tighten existing text that is
  too long, too hedged, full of idioms, or full of unexplained jargon.
---

# Plain English for developer writing

Goal: text a non-native English reader understands on the first pass, without a
dictionary and without guessing at idioms. Applies to PR descriptions, commit
messages, changelogs, release notes, issue reports, review comments, and docs —
for new text and for rewrites.

This is the canonical source for AGENTS.md rule 8. Any other file in this repo
that talks about plain-English writing style (for example
[creating-a-pr](../creating-a-pr/SKILL.md)) should link here rather than
repeat these rules — one topic, one home.

## Rules

1. **Lead with the ask.** First sentence says what changed and why. Everything
   after it is support.
   - Before: After a lot of investigation I ended up touching the retry logic
     because CI was flaky.
   - After: Retry failed uploads up to three times, so transient network errors
     stop breaking CI.

2. **Match length to substance.** A one-line fix gets a one-line description.
   No padding, no minimum length to hit.

3. **Cut filler.** Drop preamble, restating the title, hedging, marketing words
   ("elegant", "robust", "seamlessly", "blazing fast"), and emoji headers.

4. **One idea per sentence.** Do not chain clauses with em-dashes or semicolons.
   - Before: This doesn't do X — that stands — it does Y, and gives Z a path
     off W.
   - After: This does not do X. It does Y. That gives Z a way to move off W.

5. **No idioms or figurative language.** Say it literally.

   | Figurative | Literal |
   | --- | --- |
   | blast radius | affects fewer consumers |
   | shrinks the gap | closes the gap |
   | grace period | temporary fallback |
   | ceiling | limit |
   | shallow by design | handles the common case only |
   | under the hood | internally |
   | low-hanging fruit | the easy cases |

6. **Define jargon on first use, or link it.** Terms like "shim", "sentinel",
   or "canonical type" get a one-clause gloss or a link to
   [docs/glossary.md](../../../docs/glossary.md) the first time they appear.
   Do not assume the reader knows the vocabulary.

7. **Gloss every cross-reference.** A bare `#257` tells the reader nothing.
   Write "#257 (the ProjectModel rename)".

8. **Prefer short, common words.**

   | Longer | Shorter |
   | --- | --- |
   | leverage / utilize | use |
   | preserve | keep |
   | surfaces | shows |
   | remediates | fixes |
   | facilitate | help |
   | in order to | to |
   | a number of | several |

## Extra rules for issues and PRs

The GitHub issue forms (`.github/ISSUE_TEMPLATE/*.yml`) define the canonical
shape for humans; the rules below are what AI tools are bound to (a `.yml`
form does not constrain an agent running `gh issue create`).

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

### Issue / user story shape

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

### PR description shape

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
- **Label the PR at creation time.** [`.github/release.yml`](../../../.github/release.yml) is the source of truth for the changelog-category labels (`enhancement`, `bug`, `security`, `documentation`, `breaking-change`, `skip-changelog`) and a one-line blurb on each. Apply the one category that matches the change, in the same `gh pr create --label …` call — alongside the `target/vCurrent` (or `target/vNext`) process label — never as a follow-up. Don't leave a PR uncategorized; it falls through to "Other Changes".
- Add the `⚠️ Breaking change` callout **only** when the change is breaking — see the [creating-a-pr skill](../creating-a-pr/SKILL.md) for what that requires.
- **Don't** restate the title, paste large code/log blocks, recount your
  process, or enumerate every touched file — the diff already shows that.
- Keep a `### Verification` line (what you actually ran) and, for a PR in a
  series, a short follow-ups list — those are the bits *not* visible in the diff.

### Anti-patterns

| Instead of… | Write… |
| --- | --- |
| "This PR introduces a comprehensive refactor that…" | "Replaces reflection dispatch with `IFalloutCommand`." |
| Three paragraphs restating the title | One line, then bullets |
| Pasting the full stack trace inline | Link to the run / collapse in `<details>` |
| "As part of this work, I also…" | A second bullet, or a second PR |
| "This doesn't reclassify #257 — that break stands — it shrinks the blast radius for consumers upgrading from 11.0.1–11.0.12" | "#257 (the ProjectModel rename) is still breaking. This PR only makes upgrading past it easier for 11.0.1–11.0.12 users." |
| "Deliberately shallow… same ceiling as the generated shim" | "This only covers the common case. For everything else, use `fallout-migrate`." |

## Rewriting someone else's text

Keep the technical meaning and any real caveats — a genuine "Linux only" is not
filler. Change wording and structure, not substance. Flag an ambiguous sentence
instead of guessing at what it meant.
