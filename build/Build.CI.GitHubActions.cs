using Fallout.Common.CI.GitHubActions;
using Fallout.Components;

// Cross-platform (macOS/Windows) full Test+Pack is gated to RELEASE INTENT
// (#318/#326): it runs only on a PR into a production branch (release/YYYY,
// support/*) and on a release tag push (v*) — never on routine pushes to
// main/experimental, never on a per-merge basis. On main/experimental "we've
// got our edge": the ubuntu-latest PR gate + the alpha/preview pipelines.
// (workflow_dispatch as a manual cross-platform trigger isn't emitted here —
// the generator only writes workflow_dispatch when it has inputs; GitHub's
// built-in run re-run covers the on-demand case.)
//
// concurrency cancel-in-progress (#322): superseded runs are cancelled rather
// than stacked. Never applied to release.yml (a publish must not be cancelled).
[GitHubActions(
    "macos-latest",
    GitHubActionsImage.MacOsLatest,
    FetchDepth = 0,
    ConcurrencyGroup = "${{ github.workflow }}-${{ github.ref }}",
    ConcurrencyCancelInProgress = true,
    OnPushTags = new[] { "v*" },
    OnPullRequestBranches = new[] { ReleaseBranchPattern, SupportBranchPattern },
    OnPullRequestExcludePaths = new[] { "docs/**", ".assets/**", "**/*.md" },
    InvokedTargets = new[] { nameof(ITest.Test), nameof(IPack.Pack) },
    PublishArtifacts = false)]
[GitHubActions(
    "windows-latest",
    GitHubActionsImage.WindowsLatest,
    FetchDepth = 0,
    ConcurrencyGroup = "${{ github.workflow }}-${{ github.ref }}",
    ConcurrencyCancelInProgress = true,
    OnPushTags = new[] { "v*" },
    OnPullRequestBranches = new[] { ReleaseBranchPattern, SupportBranchPattern },
    OnPullRequestExcludePaths = new[] { "docs/**", ".assets/**", "**/*.md" },
    InvokedTargets = new[] { nameof(ITest.Test), nameof(IPack.Pack) },
    PublishArtifacts = false)]
// The Linux PR gate — the only required status check. pull_request only:
// feature-branch pushes run zero CI until a PR is opened against a long-lived
// branch (#327). CheckoutRef = github.head_ref pins checkout to the PR source
// branch instead of the merge SHA, keeping HEAD attached so
// GitHubTasksTest.GitHubRepositoryFromLocalDirectoryTest (which reads .git/HEAD
// via GitRepository.FromLocalDirectory) resolves a non-null branch.
[GitHubActions(
    "ubuntu-latest",
    GitHubActionsImage.UbuntuLatest,
    FetchDepth = 0,
    ConcurrencyGroup = "${{ github.workflow }}-${{ github.ref }}",
    ConcurrencyCancelInProgress = true,
    CheckoutRef = "${{ github.head_ref }}",
    // Trigger for PRs targeting experimental, main, or any release/YYYY / support/*
    // branch — all are long-lived and protected; all require the ubuntu-latest check.
    OnPullRequestBranches = new[] { ExperimentalBranch, MainBranch, ReleaseBranchPattern, SupportBranchPattern },
    OnPullRequestExcludePaths = new[] { "docs/**", ".assets/**", "**/*.md" },
    InvokedTargets = new[] { nameof(ITest.Test), nameof(IPack.Pack) },
    PublishArtifacts = false)]
partial class Build
{
    // The release workflow is intentionally hand-written at
    // .github/workflows/release.yml — that lets us name the GitHub secret
    // NUGET_API_KEY (conventional screaming-snake-case) while keeping the
    // Build.cs property name NuGetApiKey (idiomatic C#). The NUKE attribute
    // generator would force the two to match.
    const string ReleaseWorkflow = "release";
}
