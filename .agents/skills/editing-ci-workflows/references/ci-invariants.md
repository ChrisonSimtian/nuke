# CI pipeline & trigger invariants

Shaped by [milestone #18](https://github.com/Fallout-build/Fallout/milestone/18)
and the branch model in [ADR-0009](../../../../docs/adr/0009-gitflow-and-semver-reversion.md)
(carrying forward [ADR-0008](../../../../docs/adr/0008-collapse-experimental-into-main.md),
which collapsed `experimental` into the integration trunk).

- **Feature branches run zero CI until a PR is opened.** Push triggers list
  **only** long-lived branches; nothing fires on `feature/*`, `bugfix/*`, etc.
  until they're PR'd against `develop`/`main`/`release/*`/`support/*`. Do
  **not** add a working-branch pattern to any `OnPush*`/`branches:` trigger.
- **The Linux PR gate (job `ubuntu-latest`, from `build.yml`) is the only
  required check** — runs on PRs to the long-lived branches. (Branch
  protection keys on the job name, not the workflow file.)
- **A push to `develop` publishes `-preview`** to GitHub Packages
  (`publish-packages-preview.yml`). It's the only continuous publisher —
  there is still no `experimental.yml`.
- **Cross-platform `windows`/`macos` only run on release intent** — one
  `build-cross-platform.yml` workflow (a job per OS), firing on a PR into
  `main`/`release/*`/`support/*`, or a `v*` tag push. They do **not** run on
  `develop` pushes. `develop` relies on the Linux gate instead.
- **`concurrency: cancel-in-progress` on every build workflow except
  `publish-packages-release.yml`** — never cancel a publish mid-flight.
- **Canonical CI-ignore paths:** `docs/**`, `.assets/**`, `**/*.md` — applied
  to every PR/push trigger.
- The `build.yml` (Linux gate) and `build-cross-platform.yml` (macOS+Windows)
  workflows are **generated** from `build/Build.CI.GitHubActions.cs` — edit
  the attributes + constants there and regenerate (`./build.sh`), never
  hand-edit the `.yml`. `build-skip.yml`, `publish-packages-preview.yml`, and
  `publish-packages-release.yml` are hand-written.
- **Every publishing lane runs `Test` before it publishes** (#324).
  `publish-packages-preview.yml` and `publish-packages-release.yml` both run
  a single `dotnet fallout Test Pack` invocation — NUKE executes it as
  discrete internal stages (Restore → Compile → Test → Pack) and fails at the
  breaking stage, so a test failure stops the job before the push step. Don't
  split a lane into separate `dotnet fallout Compile`/`Test`/`Pack` steps —
  each invocation re-runs the dependency graph (double-compile); the single
  invocation *is* the staged build.
- **Caching** (#328): every workflow caches `~/.nuget/packages` +
  `.fallout/temp`, keyed on `global.json` + `**/*.csproj` +
  `Directory.Packages.props` (the dependency-affecting set), with a
  `restore-keys:` prefix fallback for partial restores. There is no
  `packages.lock.json` to add to the key, and build outputs (`bin`/`obj`) are
  deliberately **not** cached (stale-artifact correctness risk).
- Don't add `submodules: recursive` to a checkout — there are no submodules
  (no `.gitmodules`); it's a dead init step.
- Don't add `develop` (or any working-branch pattern) to the **push** triggers
  of the cross-platform workflows — they're release-intent-gated on purpose
  (milestone #18 / #318 / #326).
