# Architecture Decision Records

This directory holds ADRs — short, dated records of architectural decisions, the context that drove them, and their consequences. Format is [MADR](https://adr.github.io/madr/)-lite: title, status, context, decision, consequences, alternatives.

## When to write one

Write an ADR when a decision:

- Changes the shape of a public API or extension point.
- Picks a pattern that other code is expected to follow (e.g. "all CD primitives use tasks, not file generators").
- Locks in a non-obvious trade-off that a future contributor would otherwise relitigate.
- Closes out an RFC issue with a concrete commitment.

Do **not** write an ADR for routine implementation choices, refactors, or bug fixes — commit messages and the PR description carry those.

## Filename convention

`NNNN-kebab-case-title.md` — e.g. `0001-cd-primitives-attributes-vs-tasks.md`. Numbers are sequential, never reused. A superseded ADR keeps its number; the new one references it.

## Status lifecycle

- **Proposed** — written, under discussion.
- **Accepted** — agreed by maintainers (typically via PR review).
- **Superseded by NNNN** — replaced; the new ADR explains why.
- **Deprecated** — no longer applies, but kept for historical context.

## Status field is the contract

If you change a decision, do NOT silently rewrite the old ADR — add a new one and mark the old `Superseded by NNNN`. ADRs are *history*, not living documentation. Living docs go elsewhere in `docs/`.

## Index

| # | Title | Status |
|---|---|---|
| [0001](0001-cd-primitives-attributes-vs-tasks.md) | CD primitives — attributes for config, tasks for state | Proposed |
| [0001](0001-release-branch-model.md) | Release-branch model with tag-triggered multi-channel CD | Accepted (versioning amended by 0004; branch lifecycle by 0007) |
| [0002](0002-cross-provider-auth-and-secret-conventions.md) | Cross-provider auth and secret conventions | Proposed |
| [0002](0002-v11-off-nuget-by-default.md) | v11 publishes to GitHub Packages by default; nuget.org opt-in | Accepted |
| [0003](0003-variables-and-substitution.md) | Variables and `${…}` substitution layer | Proposed |
| [0004](0004-calendar-versioning-and-dual-pace-channels.md) | Calendar versioning + dual-pace channels (edge/stable) + experimental APIs | Accepted (§3 amended by 0007) |
| [0007](0007-cut-release-branch-on-demand.md) | Cut `release/YYYY` on demand, not preemptively | Accepted |
