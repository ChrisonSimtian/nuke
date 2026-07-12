using Fallout.Common.CI.GitHubActions;
using Fallout.Components;

// Two generated build workflows. Both run Test+Pack; both are GENERATED from the
// attributes below — edit here and regenerate (`./build.sh`), never hand-edit the
// `.yml`.
//
//   build.yml               — the Linux PR gate, and the ONLY required status
//                             check (job `ubuntu-latest`; branch protection keys on
//                             that job name, not the workflow file/name). PR-only:
//                             feature-branch pushes run zero CI until a PR is opened
//                             against a long-lived branch (#327), targeting main,
//                             release/YYYY, or support/*. CheckoutRef = github.head_ref
//                             pins checkout to the PR source branch instead of the
//                             merge SHA, keeping HEAD attached so
//                             GitHubTasksTest.GitHubRepositoryFromLocalDirectoryTest
//                             (which reads .git/HEAD via GitRepository.FromLocalDirectory)
//                             resolves a non-null branch.
//
//   build-cross-platform.yml — macOS + Windows in ONE workflow (one job per image).
//                             Cross-platform full Test+Pack is gated to RELEASE
//                             INTENT (#318/#326): it runs only on a PR into a
//                             production branch (release/YYYY, support/*) and on a
//                             release tag push (v*) — never on routine pushes/PRs to
//                             main. On main "we've got our edge": the ubuntu-latest
//                             gate above + the preview pipeline
//                             (.github/workflows/publish-packages-preview.yml).
//                             (workflow_dispatch as a manual cross-platform trigger
//                             isn't emitted — the generator only writes
//                             workflow_dispatch when it has inputs; GitHub's built-in
//                             run re-run covers the on-demand case.)
//
// concurrency cancel-in-progress (#322): superseded runs are cancelled rather than
// stacked. Never applied to the publish-packages-release workflow (a publish must
// not be cancelled).
[GitHubActions(
    "build",
    GitHubActionsImage.UbuntuLatest,
    FetchDepth = 0,
    ConcurrencyGroup = "${{ github.workflow }}-${{ github.ref }}",
    ConcurrencyCancelInProgress = true,
    CheckoutRef = "${{ github.head_ref }}",
    // PRs targeting main or any release/YYYY / support/* branch — all long-lived and
    // protected; all require the ubuntu-latest check.
    OnPullRequestBranches = new[] { MainBranch, ReleaseBranchPattern, SupportBranchPattern },
    OnPullRequestExcludePaths = new[] { "docs/**", ".assets/**", "**/*.md" },
    InvokedTargets = new[] { nameof(ITest.Test), nameof(IPack.Pack) },
    PublishArtifacts = false)]
[GitHubActions(
    "build-cross-platform",
    GitHubActionsImage.MacOsLatest,
    GitHubActionsImage.WindowsLatest,
    FetchDepth = 0,
    ConcurrencyGroup = "${{ github.workflow }}-${{ github.ref }}",
    ConcurrencyCancelInProgress = true,
    OnPushTags = new[] { "v*" },
    OnPullRequestBranches = new[] { ReleaseBranchPattern, SupportBranchPattern },
    OnPullRequestExcludePaths = new[] { "docs/**", ".assets/**", "**/*.md" },
    InvokedTargets = new[] { nameof(ITest.Test), nameof(IPack.Pack) },
    PublishArtifacts = false)]
partial class Build
{
    // The release workflow is intentionally hand-written at
    // .github/workflows/publish-packages-release.yml — that lets us name the GitHub
    // secret NUGET_API_KEY (conventional screaming-snake-case) while keeping the
    // Build.cs property name NuGetApiKey (idiomatic C#). The NUKE attribute
    // generator would force the two to match. This constant must match that
    // workflow's `name:` — it gates ICreateGitHubRelease.CreateGitHubRelease
    // (Build.cs) to the release workflow only.
    const string ReleaseWorkflow = "publish-packages-release";
}
