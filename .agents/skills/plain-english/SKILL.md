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

## Rewriting someone else's text

Keep the technical meaning and any real caveats — a genuine "Linux only" is not
filler. Change wording and structure, not substance. Flag an ambiguous sentence
instead of guessing at what it meant.
