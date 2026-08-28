using Fallout.Common;
using Fallout.Common.Git;
using Fallout.Common.IO;
using Fallout.Common.Tooling;
using Fallout.Common.Tools.PackageGuard;
using Fallout.Common.Utilities;
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

    // The SBOM and risk report (HTML + SARIF) are only worth generating where they're actually
    // consumed: security-scan.yml uploads the SARIF on a push to one of these four branches, and
    // the release workflow attaches the SBOM/HTML to the GitHub Release. Neither happens on a PR
    // (build.yml checks out the contributor's own branch via github.head_ref, never one of
    // these), or on a tag-triggered release checkout (detached HEAD — hence the
    // GitHubActions.Workflow fallback, since that workflow's own validate-ref job already
    // proved the tag is reachable from a production branch).
    bool IsOnLongLivedBranch =>
        GitRepository.IsOnMainBranch() ||
        GitRepository.IsOnDevelopBranch() ||
        GitRepository.IsOnReleaseBranch() ||
        GitRepository.IsOnSupportBranch() ||
        GitHubActions?.Workflow == ReleaseWorkflow;

    // Runs unconditionally — this is the PR gate's policy-violation check (build.yml), so it has
    // to run on every branch, including a contributor's feature branch. Only the SBOM/risk-report
    // generation is restricted to the four long-lived branches, via IsOnLongLivedBranch below.
    Target PackageGuard => _ => _
        .DependsOn<IRestore>()
        .Produces(PackageGuardSbomFile)
        .Produces(PackageGuardSarifFile)
        .Produces(PackageGuardDirectory / "*.html")
        .Executes(() =>
        {
            var generateReports = IsOnLongLivedBranch;

            if (generateReports)
                PackageGuardDirectory.CreateOrCleanDirectory();

            PackageGuardTasks.PackageGuard(_ => _
                .SetProjectPath(Solution.Path)
                .SetGitHubApiKey(From<ICreateGitHubRelease>().GitHubToken)
                .When(generateReports, _ => _
                    .EnableReportRisk()
                    .SetSbom(SbomFormat.cyclonedx)
                    .SetSbomOutput(PackageGuardSbomFile)
                    .SetProcessEnvironmentVariable(PackageGuardReportRiskPathOverrideEnvironmentVariable, PackageGuardSarifFile)));
        });
}
