---
name: editing-ci-workflows
description: Invariants to preserve when touching .github/workflows/**, build/Build.CI.GitHubActions.cs, or anything about which branches/tags trigger CI or publish packages. Trigger before editing a workflow YAML file, the CI generator source, or branch trigger lists.
---

Read [references/ci-invariants.md](references/ci-invariants.md) in full before
changing a workflow — most mistakes here are "looks fine, quietly breaks a
gating assumption." The short version:

- **`build.yml` and `build-cross-platform.yml` are generated** from
  `build/Build.CI.GitHubActions.cs` — edit the attributes/constants there and
  regenerate (`./build.sh`), never hand-edit those two `.yml` files.
  `build-skip.yml`, `publish-packages-preview.yml`, and
  `publish-packages-release.yml` are hand-written; those you do edit directly.
- **Feature branches run zero CI** until a PR targets a long-lived branch
  (`develop`/`main`/`release/*`/`support/*`). Never add a working-branch
  pattern (`feature/*`, `bugfix/*`, …) to a push/PR trigger.
- **Cross-platform (`windows`/`macos`) is release-intent-gated** — PRs into
  `main`/`release/*`/`support/*`, or a `v*` tag push. Never add `develop` to
  its push triggers.
- **`concurrency: cancel-in-progress` everywhere except `publish-packages-release.yml`**
  — never cancel a publish mid-flight.
- **Every publishing lane runs `Test` before it publishes**, as a single
  `dotnet fallout Test Pack` invocation, not split steps — splitting
  double-compiles.
- Don't add `submodules: recursive` to a checkout step — there are no
  submodules in this repo.

See also [docs/architecture.md](../../../docs/architecture.md#ci-layout) for
the current workflow-to-trigger table, and
[docs/branching-and-release.md](../../../docs/branching-and-release.md) for
the branch-protection rulesets these triggers back onto.
