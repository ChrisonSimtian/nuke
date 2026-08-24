---
name: adding-a-migration-step
description: How to make fallout-migrate handle a new rename, removal, or rewrite by extending an existing IMigrationStep, and when a genuinely new step is warranted. Trigger when asked to add a rename/rewrite rule to fallout-migrate, or to touch src/Fallout.Migrate/Migration.cs or its Steps.
---

`fallout-migrate` runs a fixed, ordered list of `IMigrationStep` implementations,
built in `src/Fallout.Migrate/Migration.cs`. A **step is one operation over one
set of files**, and it owns the rewrite rules for those files.
`RewriteCsprojsStep` holds the rules for `*.csproj`; `RewriteCsFilesStep` holds
the rules for `*.cs`.

## Adding a new rename, removal, or rewrite

1. Find the step for that file type — `RewriteCsprojsStep` for `*.csproj`,
   `RewriteCsFilesStep` for `*.cs`, `RewriteBootstrapScriptsStep` for the
   bootstrap scripts.
2. Add a `private static readonly Regex` field to that step, with a comment
   saying what moved and why.
3. Apply it as another statement in the step's `Rewrite` method, incrementing
   `edits` per replacement.
4. Add cases to that step's spec class in `tests/Fallout.Migrate.Specs`.

**Do not add a step per rename.** A new step means a second pass over the same
files, so each file is written twice and the `Summary` edit count is inflated.
Rule order also matters — a specific rule usually has to run before a general
one — and inside one `Rewrite` method that order is plain statement order.
Split across steps it becomes a hidden dependency between entries in
`Migration.steps`.

## When a new step actually is warranted

Only for a new set of files or a genuinely different operation, such as
renaming a directory or prompting the user. That's one new class implementing
`IMigrationStep` plus one line in `Migration.steps`. If the new step depends on
an earlier one, say so in the comment on that list entry, the way
`ResolveFalloutVersionStep` does.

Keep `Steps/` free of helper classes. A rewriter that only serves one step
belongs inside that step — see `aef6c073` (`CsprojRewriter`) and #528
(`CodeRewriter`) for the shape.
