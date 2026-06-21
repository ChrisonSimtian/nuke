# Code review — issue #385: workflow-level `env:` block (GitHub Actions generator)

- **Date**: 2026-06-21
- **Branch**: `features/385-github-actions-env-block` (base `main` @ `e87f6921`)
- **Mode**: own (pre-PR) · **Reviewers**: 3 parallel dimension agents (correctness/edge-cases, .NET best-practices/API, test-quality) + skeptic verify pass + `compliance-reviewer`
- **Verdict**: PASS — all findings fixed before staging.

## Scope reviewed
- `src/Fallout.Common/CI/GitHubActions/GitHubActionsAttribute.cs` — new public `Env` property, validation in `GetConfiguration`, pass-through.
- `src/Fallout.Common/CI/GitHubActions/Configuration/GitHubActionsConfiguration.cs` — new `Env` field, emission in `Write`.
- `tests/Fallout.Common.Tests/CI/ConfigurationGenerationTest.cs` — `env-block` + `env-block-with-permissions` Verify cases.
- `tests/Fallout.Common.Tests/CI/GitHubActionsEnvValidationTest.cs` — validation unit tests.
- Two `*.verified.txt` snapshots.

## Findings & resolution

| # | Severity | Finding | Verdict | Resolution |
|---|---|---|---|---|
| 1 | Important | `null` entry in `Env` → raw `NullReferenceException` at `variable.IndexOf`, not a clean `ArgumentException`. | Real (traced) | Added `Assert.True(variable != null, ...)` as first guard. Test: `[InlineData(null)]`. |
| 2 | Important | `"KEY:value"` (no space after colon) passed validation but emits invalid YAML — `key:value` is a plain scalar, not a mapping entry, so the `env:` block silently breaks. Contradicts the issue's "build error rather than malformed YAML silently" goal. | Real (YAML spec: `:` needs trailing space/newline to be a mapping indicator) | Added assert requiring the colon be followed by whitespace or end-of-entry (`KEY:` empty value still allowed). Tests: `[InlineData("KEY:value")]` rejects, `[InlineData("EMPTY_VALUE:")]` allowed. |
| 3 | Important | Happy-path snapshot only covered `env: → jobs:`; nothing proved `env:` is emitted *before* `permissions:`/`concurrency:` with correct spacing. A reordering regression would pass all tests. | Real coverage gap | Added `env-block-with-permissions` snapshot locking `on: → env: → permissions: → concurrency: → jobs:`. |
| 4 | Minor | Validation matrix missing `""`, `"   "`, multi-entry-with-bad-element. | Valid | Added those `InlineData` rows + `Malformed_entry_after_a_valid_one_still_throws` fact. |
| 5 | Minor | Values emitted verbatim — values needing YAML quoting (`*`, leading `#`, significant spaces) are the caller's responsibility. Consistent with how the whole writer treats strings (no quoting anywhere). | Accepted by design | Documented in the `Env` XML doc rather than adding escaping logic (out of issue scope). |
| 6 | Minor (compliance) | `GitHubActionsConfiguration.Env` lacked an `= new string[0]` initializer that the attribute property has. | Valid consistency gap | Added the initializer; guards direct construction. |

Confirmed **fine** by reviewers (no action): blank-line spacing in all block combinations; `IndexOf(char)` is ordinal (no culture concern); `Substring(0, separatorIndex)` bounds are safe given `separatorIndex > 0`; `string[]` of `"KEY: value"` is the only attribute-legal shape (attribute args must be compile-time constants); change is additive/non-breaking; `Env` naming avoids the `EnvironmentName` deployment-keyword clash.

## Compliance
All four issue ACs PASS. AGENTS.md: additive/non-breaking (→ `target/2026`, no `breaking-change`); tests next to code mirroring namespaces; xUnit + FluentAssertions + Verify only; no license headers; no generated tool wrappers touched; no inline package versions.
