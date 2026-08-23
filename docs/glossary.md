# Glossary of repo jargon

Terms that come up often in issues, PRs, and code comments. Link here (or
gloss the term in one clause) the first time a PR or issue uses one of these —
per the plain-English rule in [AGENTS.md](../AGENTS.md), don't assume the
reader already knows the vocabulary.

| Term | Meaning |
| --- | --- |
| **Shim** | A small compatibility layer that makes old code keep compiling/working against a new API, without being the real implementation. |
| **Transition shim** | A shim meant to be temporary — it exists only to ease an upgrade and is expected to be removed later. |
| **Sentinel** (e.g. "consumer sentinel") | A small test project whose only job is to fail the build if something regresses — an early-warning check, not a feature. |
| **Canonical type / namespace** | The current, "real" location of a type — as opposed to an old namespace kept alive only by a shim. |
| **Ceiling** (of a shim/fix) | The limit of what a shim or fix covers — what it does *not* handle, so the reader knows when to reach for something else. |
| **Shallow (by design)** | The change intentionally covers only the common case, not every possible scenario. |
| **Blast radius** | How many consumers/how much code is affected by a change. |
