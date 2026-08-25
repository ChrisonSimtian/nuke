using Fallout.Common;
using Fallout.Common.Git;
using Fallout.Common.IO;
using Fallout.Common.Tooling;
using Fallout.Common.Tools.PackageGuard;
using Fallout.Components;

partial class Build
{
    // PackageGuard's own CLI only exposes an env-var override for the risk-report path
    // (see PackageGuard.json's help text on ReportRisk) — there's no `--report-risk <path>`
    // argument in our wrapper, matching the repo-wide convention that bool CLI flags stay
    // presence-only. Setting this process env var instead pins the SARIF/HTML pair to a
    // deterministic path we can reference from CI (upload-sarif, the release asset step).
    const string PackageGuardReportRiskPathOverrideEnvironmentVariable = "PACKAGEGUARD_REPORT_RISK_PATH_OVERRIDE";

    AbsolutePath PackageGuardDirectory => OutputDirectory / "packageguard";
    AbsolutePath PackageGuardSbomFile => PackageGuardDirectory / "sbom.json";
    AbsolutePath PackageGuardSarifFile => PackageGuardDirectory / "risk-report.sarif";

    // Every real invocation path is already tied to one of these four branches:
    //   - the dedicated "security-scan" workflow only triggers on a push to develop/main/
    //     release/*/support/* (Build.CI.GitHubActions.cs), so GitRepository.Branch resolves
    //     to one of them there.
    //   - the tag-triggered release workflow checks out a detached HEAD (no branch to
    //     resolve), but its own validate-ref job already proves the tag is reachable from
    //     main/release/*/support/* before this ever runs — hence the GitHubActions.Workflow
    //     fallback below.
    // A local run on a feature branch, or the PR gate (build.yml checks out the contributor's
    // branch via github.head_ref), matches neither and skips — which is the point: this scan
    // is deliberately not part of every PR's Test+Pack run.
    bool IsOnLongLivedBranch =>
        GitRepository.IsOnMainBranch() ||
        GitRepository.IsOnDevelopBranch() ||
        GitRepository.IsOnReleaseBranch() ||
        GitRepository.IsOnSupportBranch() ||
        GitHubActions?.Workflow == ReleaseWorkflow;

    Target PackageGuard => _ => _
        .DependsOn<IRestore>()
        .OnlyWhenStatic(() => IsOnLongLivedBranch)
        .Produces(PackageGuardSbomFile)
        .Produces(PackageGuardSarifFile)
        .Produces(PackageGuardDirectory / "*.html")
        .Executes(() =>
        {
            PackageGuardDirectory.CreateOrCleanDirectory();

            PackageGuardTasks.PackageGuard(_ => _
                .SetProjectPath(Solution.Path)
                .EnableReportRisk()
                .SetSbom(SbomFormat.cyclonedx)
                .SetSbomOutput(PackageGuardSbomFile)
                .SetGitHubApiKey(From<ICreateGitHubRelease>().GitHubToken)
                .SetProcessEnvironmentVariable(PackageGuardReportRiskPathOverrideEnvironmentVariable, PackageGuardSarifFile));
        });
}
