using System.Collections.Generic;
using Fallout.Common.CI.GitHubActions;
using Fallout.Common.CI.GitHubActions.Configuration;
using Fallout.Components;

// Three generated workflows. build.yml and build-cross-platform.yml both run Test+Pack; all
// three are GENERATED from the attributes below — edit here and regenerate (`./build.sh`),
// never hand-edit the `.yml`.
//
//   build.yml               — the Linux PR gate, and the ONLY required status
//                             check (job `ubuntu-latest`; branch protection keys on
//                             that job name, not the workflow file/name). PR-only:
//                             feature-branch pushes run zero CI until a PR is opened
//                             against a long-lived branch (#327), targeting develop,
//                             main, release/*, or support/*. CheckoutRef = github.head_ref
//                             pins checkout to the PR source branch instead of the
//                             merge SHA, keeping HEAD attached so
//                             GitHubTasksTest.GitHubRepositoryFromLocalDirectoryTest
//                             (which reads .git/HEAD via GitRepository.FromLocalDirectory)
//                             resolves a non-null branch.
//
//   build-cross-platform.yml — macOS + Windows in ONE workflow (one job per image).
//                             Cross-platform full Test+Pack is gated to RELEASE
//                             INTENT (#318/#326): it runs only on a PR into a
//                             production branch (main, release/*, support/*) and on
//                             a release tag push (v*) — never on routine pushes/PRs to
//                             develop. On develop "we've got our edge": the
//                             ubuntu-latest gate above + the preview pipeline
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
    // PRs targeting develop, main, or any release/* / support/* branch — all
    // long-lived and protected; all require the ubuntu-latest check.
    OnPullRequestBranches = new[] { DevelopBranch, MainBranch, ReleaseBranchPattern, SupportBranchPattern },
    OnPullRequestExcludePaths = new[] { "docs/**", ".assets/**", "**/*.md" },
    InvokedTargets = new[] { nameof(VerifyGeneratedTools), nameof(ITest.Test), nameof(IPack.Pack) },
    PublishArtifacts = false)]
[GitHubActions(
    "build-cross-platform",
    GitHubActionsImage.MacOsLatest,
    GitHubActionsImage.WindowsLatest,
    FetchDepth = 0,
    ConcurrencyGroup = "${{ github.workflow }}-${{ github.ref }}",
    ConcurrencyCancelInProgress = true,
    OnPushTags = new[] { "v*" },
    // main is the production trunk now (ADR-0009). A PR into it is always a
    // release/vX.Y GA merge or a hotfix, so it belongs on this release-intent gate,
    // alongside release/* and support/*.
    OnPullRequestBranches = new[] { MainBranch, ReleaseBranchPattern, SupportBranchPattern },
    OnPullRequestExcludePaths = new[] { "docs/**", ".assets/**", "**/*.md" },
    InvokedTargets = new[] { nameof(ITest.Test), nameof(IPack.Pack) },
    PublishArtifacts = false)]
//   security-scan.yml        — the PackageGuard compliance scan (build/Build.PackageGuard.cs).
//                             Push-only, and only to the four long-lived branches — this is
//                             the CI-side half of the branch restriction; the target's own
//                             OnlyWhenStatic(IsOnLongLivedBranch) guard is the other half, for
//                             anyone invoking the target outside this workflow. Runs on its own
//                             schedule rather than piggy-backing on build.yml/build-cross-platform.yml,
//                             because those check out the PR's source branch (never one of the
//                             four) — a PackageGuard run gated on being IN one of those workflows
//                             would just always skip. EnableGitHubToken feeds GITHUB_TOKEN to
//                             PackageGuard (avoids GitHub API rate-limiting on license lookups)
//                             and to the upload-sarif step's security-events:write use.
[GitHubActions(
    "security-scan",
    GitHubActionsImage.UbuntuLatest,
    FetchDepth = 0,
    ConcurrencyGroup = "${{ github.workflow }}-${{ github.ref }}",
    ConcurrencyCancelInProgress = true,
    OnPushBranches = new[] { DevelopBranch, MainBranch, ReleaseBranchPattern, SupportBranchPattern },
    OnPushExcludePaths = new[] { "docs/**", ".assets/**", "**/*.md" },
    InvokedTargets = new[] { nameof(PackageGuard) },
    EnableGitHubToken = true,
    // Specifying any `permissions:` block switches the job from GitHub's default read-all to
    // explicit-only — contents:read has to be listed too, or upload-sarif (and checkout) lose
    // it. See GitHub's own upload-sarif docs for this exact pairing.
    ReadPermissions = new[] { GitHubActionsPermissions.Contents },
    WritePermissions = new[] { GitHubActionsPermissions.SecurityEvents },
    PublishArtifacts = false)]
partial class Build : IConfigureGitHubActions
{
    // The release workflow is intentionally hand-written at
    // .github/workflows/publish-packages-release.yml — that lets us name the GitHub
    // secret NUGET_API_KEY (conventional screaming-snake-case) while keeping the
    // Build.cs property name NuGetApiKey (idiomatic C#). The NUKE attribute
    // generator would force the two to match. This constant must match that
    // workflow's `name:` — it gates ICreateGitHubRelease.CreateGitHubRelease
    // (Build.cs) to the release workflow only.
    const string ReleaseWorkflow = "publish-packages-release";

    // Injects the SARIF upload after security-scan's "dotnet fallout PackageGuard" run step —
    // GitHubActionsStepPosition.PostRun is exactly "after the run block, before the built-in
    // artifact upload". Scoped to this one generated job by WorkflowName; other jobs get no
    // insertions.
    void IConfigureGitHubActions.ConfigureSteps(GitHubActionsStepPipeline pipeline)
    {
        if (pipeline.WorkflowName == "security-scan")
        {
            pipeline.Insert(GitHubActionsStepPosition.PostRun, new GitHubActionsCustomStep
            {
                Name = "Upload risk-report SARIF to GitHub code scanning",
                Uses = "github/codeql-action/upload-sarif@v3",
                With = new Dictionary<string, string>
                {
                    ["sarif_file"] = "output/packageguard/risk-report.sarif",
                },
            });
        }
    }
}
