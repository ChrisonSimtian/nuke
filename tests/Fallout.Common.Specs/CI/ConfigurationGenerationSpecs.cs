using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Fallout.Common.CI;
using Fallout.Common.CI.AppVeyor;
using Fallout.Common.CI.AzurePipelines;
using Fallout.Common.CI.GitHubActions;
using Fallout.Common.CI.TeamCity;
using Fallout.Common.Execution;
using Fallout.Common.IO;
using Fallout.Common.Tooling;
using VerifyXunit;
using Xunit;

namespace Fallout.Common.Specs.CI;

public class ConfigurationGenerationSpecs
{
    [Theory]
    [MemberData(nameof(GetAttributes))]
    public Task Test(string testName, ITestConfigurationGenerator attribute)
    {
        var build = new TestBuild();
        var relevantTargets = ExecutableTargetFactory.CreateAll(build, x => x.Compile);

        var stream = new MemoryStream();
        ((ConfigurationAttributeBase)attribute).Build = build;
        attribute.Stream = new StreamWriter(stream, leaveOpen: true);
        attribute.Generate(relevantTargets);

        stream.Seek(offset: 0, SeekOrigin.Begin);
        var reader = new StreamReader(stream);
        var str = reader.ReadToEnd();

        return Verifier.Verify(str)
            .UseParameters(testName, attribute.GetType().BaseType.NotNull().Name);
    }

    public static IEnumerable<object[]> GetAttributes()
    {
        return TestBuild.GetAttributes().Select(x => new object[] { x.TestName, x.Generator });
    }

    [AppVeyorSecret("GitHubToken", "encrypted-yaml")]
    [TeamCityToken("GitHubToken", "74928d76-46e8-45cc-ad22-6438915ac070")]
    public class TestBuild : FalloutBuild
    {
        public static IEnumerable<(string TestName, IConfigurationGenerator Generator)> GetAttributes()
        {
            yield return
            (
                null,
                new TestTeamCityAttribute
                {
                    Description = "description",
                    Version = "1.3.3.7",
                    NonEntryTargets = new[] { nameof(Clean) },
                    VcsTriggeredTargets = new[] { nameof(Test), nameof(Pack) },
                    ManuallyTriggeredTargets = new[] { nameof(Publish) },
                    NightlyTriggeredTargets = new[] { nameof(Publish) },
                    NightlyTriggerBranchFilters = new[] { "nightly_branch_filter" },
                    VcsTriggerBranchFilters = new[] { "vcs_branch_filter" },
                    ImportSecrets = new[] { "GitHubToken", "ManualToken" }
                }
            );

            yield return
            (
                null,
                new TestAzurePipelinesAttribute(
                    AzurePipelinesImage.Ubuntu2204,
                    AzurePipelinesImage.Windows2019)
                {
                    NonEntryTargets = new[] { nameof(Clean) },
                    InvokedTargets = new[] { nameof(Test) },
                    ExcludedTargets = new[] { nameof(Pack) },
                    EnableAccessToken = true,
                    ImportVariableGroups = new[] { "variable-group-1" },
                    ImportSecrets = new[] { nameof(ApiKey) },
                    TriggerBatch = true,
                    TriggerBranchesInclude = new[] { "included_branch" },
                    TriggerBranchesExclude = new[] { "excluded_branch" },
                    TriggerPathsInclude = new[] { "included_path" },
                    TriggerPathsExclude = new[] { "excluded_path" },
                    TriggerTagsInclude = new[] { "included_tags" },
                    TriggerTagsExclude = new[] { "excluded_tags" },
                    Submodules = true,
                    LargeFileStorage = false,
                    Clean = true,
                    FetchDepth = 1
                }
            );

            yield return
            (
                null,
                new TestAppVeyorAttribute(
                    AppVeyorImage.UbuntuLatest,
                    AppVeyorImage.VisualStudio2022)
                {
                    InvokedTargets = new[] { nameof(Test) },
                    BranchesOnly = new[] { "only_branch" },
                    BranchesExcept = new[] { "except_branch" },
                    SkipTags = true,
                    SkipBranchesWithPullRequest = true,
                    Submodules = true,
                    Secrets = new[] { "GitHubToken" }
                }
            );

            yield return
            (
                "simple-triggers",
                new TestGitHubActionsAttribute(
                    GitHubActionsImage.MacOsLatest,
                    GitHubActionsImage.UbuntuLatest,
                    GitHubActionsImage.WindowsLatest)
                {
                    On = new[] { GitHubActionsTrigger.Push, GitHubActionsTrigger.PullRequest },
                    InvokedTargets = new[] { nameof(Test) },
                    ImportSecrets = new[] { nameof(ApiKey) },
                    EnableGitHubToken = true,
                    WritePermissions = new[] { GitHubActionsPermissions.Contents },
                    ReadPermissions = new[] { GitHubActionsPermissions.Actions }
                }
            );

            yield return
            (
                "detailed-triggers",
                new TestGitHubActionsAttribute(
                    GitHubActionsImage.MacOsLatest,
                    GitHubActionsImage.UbuntuLatest,
                    GitHubActionsImage.WindowsLatest)
                {
                    InvokedTargets = new[] { nameof(Test) },
                    OnCronSchedule = "* 0 * * *",
                    OnPushBranches = new[] { "push_branch" },
                    OnPushTags = new[] { "push_tag/*" },
                    OnPushIncludePaths = new[] { "push_include_path" },
                    OnPushExcludePaths = new[] { "push_exclude_path" },
                    OnPullRequestBranches = new[] { "pull_request_branch" },
                    OnPullRequestTags = new[] { "pull_request_tag" },
                    OnPullRequestIncludePaths = new[] { "pull_request_include_path" },
                    OnPullRequestExcludePaths = new[] { "pull_request_exclude_path/**" },
#pragma warning disable CS0618 // regression guard: the obsolete legacy path must still emit correctly
                    OnWorkflowDispatchOptionalInputs = new[] { "OptionalInput" },
                    OnWorkflowDispatchRequiredInputs = new[] { "RequiredInput" },
#pragma warning restore CS0618
                    PublishCondition = "success() || failure()",
                    Submodules = GitHubActionsSubmodules.Recursive,
                    Lfs = true,
                    FetchDepth = 2,
                    Progress = false,
                    Filter = "tree:0",
                    TimeoutMinutes = 30,
                    ConcurrencyCancelInProgress = true,
                    JobConcurrencyCancelInProgress = true,
                    JobConcurrencyGroup = "custom-job-group",
                    EnvironmentName = "environment-name",
                    EnvironmentUrl = "environment-url"
                }
            );

            // Regression guard: when CheckoutRef is set, the generator must also emit a
            // repository: line that resolves to the PR's head repo (for cross-repo / fork PRs)
            // with a fallback to the current repo (for push events). Without this, fork PRs
            // fail at the checkout step because the branch only exists on the fork, not on
            // origin.
            yield return
            (
                "checkout-ref",
                new TestGitHubActionsAttribute(GitHubActionsImage.UbuntuLatest)
                {
                    InvokedTargets = new[] { nameof(Test) },
                    OnPullRequestBranches = new[] { "main" },
                    CheckoutRef = "${{ github.head_ref }}"
                }
            );

            yield return
            (
                "env-block",
                new TestGitHubActionsAttribute(GitHubActionsImage.UbuntuLatest)
                {
                    On = new[] { GitHubActionsTrigger.Push, GitHubActionsTrigger.PullRequest },
                    InvokedTargets = new[] { nameof(Test) },
                    Env = new[]
                          {
                              "DOTNET_CLI_TELEMETRY_OPTOUT: 1",
                              "DOTNET_NOLOGO: true",
                              "NUGET_XMLDOC_MODE: skip",
                              "Configuration: Release"
                          }
                }
            );

            // Ordering guard: with Env, permissions, and concurrency all set, the env: block must be
            // emitted after on: and before permissions:/concurrency:/jobs:, with correct blank lines.
            yield return
            (
                "env-block-with-permissions",
                new TestGitHubActionsAttribute(GitHubActionsImage.UbuntuLatest)
                {
                    On = new[] { GitHubActionsTrigger.Push },
                    InvokedTargets = new[] { nameof(Test) },
                    Env = new[] { "DOTNET_NOLOGO: true", "Configuration: Release" },
                    WritePermissions = new[] { GitHubActionsPermissions.Contents },
                    ReadPermissions = new[] { GitHubActionsPermissions.Actions },
                    ConcurrencyCancelInProgress = true
                }
            );

            // Ordering guard: extra CheckoutWith inputs emit verbatim inside the with: block, after
            // every typed key (here fetch-depth) and in the order supplied.
            yield return
            (
                "checkout-with",
                new TestGitHubActionsAttribute(GitHubActionsImage.UbuntuLatest)
                {
                    On = new[] { GitHubActionsTrigger.Push },
                    InvokedTargets = new[] { nameof(Test) },
                    FetchDepth = 0,
                    CheckoutWith = new[]
                                   {
                                       "token: ${{ secrets.CI_PAT }}",
                                       "path: src",
                                       "persist-credentials: false"
                                   }
                }
            );

            // Guard: CheckoutWith with no typed checkout key must still open the with: block.
            yield return
            (
                "checkout-with-only",
                new TestGitHubActionsAttribute(GitHubActionsImage.UbuntuLatest)
                {
                    On = new[] { GitHubActionsTrigger.Push },
                    InvokedTargets = new[] { nameof(Test) },
                    CheckoutWith = new[] { "path: src" }
                }
            );

            // Multi-line block scalar: the case a 'KEY: value' validator would reject. Proves raw
            // verbatim emission preserves the caller-supplied continuation lines and indentation.
            yield return
            (
                "checkout-with-sparse",
                new TestGitHubActionsAttribute(GitHubActionsImage.UbuntuLatest)
                {
                    On = new[] { GitHubActionsTrigger.Push },
                    InvokedTargets = new[] { nameof(Test) },
                    CheckoutWith = new[]
                                   {
                                       "sparse-checkout: |",
                                       "  src",
                                       "  build"
                                   }
                }
            );

            // Guard: every typed input kind emits its type:/default:/options: correctly; a string input
            // omits type: so untyped output stays byte-identical. The typed inputs alone drive the
            // detailed workflow_dispatch trigger (no shorthand On).
            yield return
            (
                "dispatch-typed-inputs",
                new TestGitHubActionsAttribute(GitHubActionsImage.UbuntuLatest)
                {
                    InvokedTargets = new[] { nameof(Test) },
                    Inputs = new[]
                             {
                                 new GitHubActionsInputAttribute("Plain"),
                                 new GitHubActionsInputAttribute("Verbose") { Type = GitHubActionsInputType.Boolean, Default = "false" },
                                 new GitHubActionsInputAttribute("Retries") { Type = GitHubActionsInputType.Number, Default = "3" },
                                 new GitHubActionsInputAttribute("Channel") { Type = GitHubActionsInputType.Choice, Options = new[] { "alpha", "beta", "stable" }, Default = "beta" },
                                 new GitHubActionsInputAttribute("Target") { Type = GitHubActionsInputType.Environment }
                             }
                }
            );

            // Ordering guard: a Workflows-scoped input appears only in the named workflow; one scoped to
            // a different workflow is filtered out for this one.
            yield return
            (
                "dispatch-input-scoping",
                new TestGitHubActionsAttribute("publish", GitHubActionsImage.UbuntuLatest)
                {
                    InvokedTargets = new[] { nameof(Test) },
                    WorkflowNames = new[] { "publish", "build" },
                    Inputs = new[]
                             {
                                 new GitHubActionsInputAttribute("ForPublishOnly") { Workflows = new[] { "publish" } },
                                 new GitHubActionsInputAttribute("ForOtherWorkflow") { Workflows = new[] { "build" } }
                             }
                }
            );

            // Regression guard: legacy arrays and typed inputs coexist; legacy emit first and untyped,
            // typed follow.
            yield return
            (
                "dispatch-legacy-plus-typed",
                new TestGitHubActionsAttribute(GitHubActionsImage.UbuntuLatest)
                {
                    InvokedTargets = new[] { nameof(Test) },
#pragma warning disable CS0618 // regression guard: the obsolete legacy path must still emit correctly
                    OnWorkflowDispatchOptionalInputs = new[] { "LegacyOptional" },
                    OnWorkflowDispatchRequiredInputs = new[] { "LegacyRequired" },
#pragma warning restore CS0618
                    Inputs = new[]
                             {
                                 new GitHubActionsInputAttribute("TypedFlag") { Type = GitHubActionsInputType.Boolean, Default = "true" }
                             }
                }
            );

            yield return
            (
                "runs-on-labels",
                new TestGitHubActionsAttribute(GitHubActionsImage.UbuntuLatest)
                {
                    On = new[] { GitHubActionsTrigger.Push },
                    InvokedTargets = new[] { nameof(Test) },
                    RunsOnLabels = new[] { "self-hosted", "linux", "x64" }
                }
            );

            yield return
            (
                "default-shell",
                new TestGitHubActionsAttribute(GitHubActionsImage.UbuntuLatest)
                {
                    On = new[] { GitHubActionsTrigger.Push },
                    InvokedTargets = new[] { nameof(Test) },
                    DefaultShell = "pwsh"
                }
            );

            // Ordering guard: with DefaultShell, permissions, and concurrency all set, the defaults:
            // block must be emitted after concurrency: and before jobs:, with correct blank lines.
            yield return
            (
                "default-shell-with-permissions",
                new TestGitHubActionsAttribute(GitHubActionsImage.UbuntuLatest)
                {
                    On = new[] { GitHubActionsTrigger.Push },
                    InvokedTargets = new[] { nameof(Test) },
                    DefaultShell = "pwsh",
                    WritePermissions = new[] { GitHubActionsPermissions.Contents },
                    ReadPermissions = new[] { GitHubActionsPermissions.Actions },
                    ConcurrencyCancelInProgress = true
                }
            );

            yield return
            (
                null,
                new TestSpaceAutomationAttribute("Name", "mcr.microsoft.com/dotnet/sdk:5.0")
                {
                    InvokedTargets = new[] { nameof(Test) },
                    VolumeSize = "10.gb",
                    ResourcesCpu = "1.cpu",
                    ResourcesMemory = "2000.mb",
                    OnPush = true,
                    OnPushBranchIncludes = new[] { "refs/heads/include" },
                    OnPushBranchExcludes = new[] { "refs/heads/exclude" },
                    OnPushBranchRegexIncludes = new[] { @"\binclude\b" },
                    OnPushBranchRegexExcludes = new[] { @"\bexclude\b" },
                    OnPushPathIncludes = new[] { "include-path" },
                    OnPushPathExcludes = new[] { "exclude-path" },
                    OnCronSchedule = "* 0 * * *",
                    ImportSecrets = new[] { "GitHubToken" },
                    TimeoutInMinutes = 15
                }
            );
        }

        public AbsolutePath SourceDirectory => RootDirectory / "src";

        public Target Clean => _ => _
            .Before(Restore);

        [Parameter] public readonly bool IgnoreFailedSources;

        public Target Restore => _ => _
            .Produces(SourceDirectory / "*/obj/**");

        [Parameter("Configuration for compilation")]
        public readonly Configuration Configuration = Configuration.Debug;

        [Parameter] public readonly string[] StringArray = new[] { "first", "second" };
        [Parameter] public readonly int[] IntegerArray = new[] { 1, 2 };
        [Parameter] public readonly Configuration[] ConfigurationArray = new[] { Configuration.Debug, Configuration.Release };

        public AbsolutePath OutputDirectory => RootDirectory / "output";

        public Target Compile => _ => _
            .DependsOn(Restore)
            .Produces(SourceDirectory / "*/bin/**");

        public AbsolutePath PackageDirectory => OutputDirectory / "packages";

        public Target Pack => _ => _
            .DependsOn(Compile)
            .Consumes(Restore, Compile)
            .Produces(PackageDirectory / "*.nupkg");

        public AbsolutePath TestResultDirectory => OutputDirectory / "test-results";

        public Target Test => _ => _
            .DependsOn(Compile)
            .Produces(TestResultDirectory / "*.trx")
            .Produces(TestResultDirectory / "*.xml")
            .Partition(2);

        public string CoverageReportArchive => OutputDirectory / "coverage-report.zip";

        public Target Coverage => _ => _
            .DependsOn(Test)
            .TriggeredBy(Test)
            .Consumes(Test)
            .Produces(CoverageReportArchive);

        [Parameter("NuGet Api Key")] [Secret] public readonly string ApiKey;

        [Parameter("NuGet Source for Packages")]
        public readonly string Source = "https://api.nuget.org/v3/index.json";

        public Target Publish => _ => _
            .DependsOn(Clean, Test, Pack)
            .Consumes(Pack)
            .Requires(() => ApiKey);

        public Target Announce => _ => _
            .TriggeredBy(Publish)
            .AssuredAfterFailure();
    }

    [TypeConverter(typeof(TypeConverter<Configuration>))]
    public class Configuration : Enumeration
    {
        public static Configuration Debug = new() { Value = nameof(Debug) };
        public static Configuration Release = new() { Value = nameof(Release) };

        public static implicit operator string(Configuration configuration)
        {
            return configuration.Value;
        }
    }
}
