using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fallout.Common;
using Fallout.Common.CI.AzurePipelines;
using Fallout.Common.CI.GitHubActions;
using Fallout.Common.CI.TeamCity;
using Fallout.Common.IO;
using Fallout.Solutions;
using Fallout.Common.Tooling;
using Fallout.Common.Tools.Coverlet;
using Fallout.Common.Tools.DotNet;
using Fallout.Common.Utilities;
using Fallout.Common.Utilities.Collections;
using static Fallout.Common.Tools.DotNet.DotNetTasks;

namespace Fallout.Components;

public interface ITest : ICompile, IHasArtifacts
{
    AbsolutePath TestResultDirectory => ArtifactsDirectory / "test-results";

    int TestDegreeOfParallelism => 1;

    Target Test => _ => _
        .DependsOn(Compile)
        .Produces(TestResultDirectory / "*.trx")
        .Produces(TestResultDirectory / "*.xml")
        .Executes(() =>
        {
            try
            {
                DotNetTest(_ => _
                        .Apply(TestSettingsBase)
                        .Apply(TestSettings)
                        .CombineWith(TestProjects, (_, v) => _
                            .Apply(TestProjectSettingsBase, v)
                            .Apply(TestProjectSettings, v)),
                    completeOnFailure: true,
                    degreeOfParallelism: TestDegreeOfParallelism);
            }
            finally
            {
                ReportTestResults();
                ReportTestCount();
            }
        });

    void ReportTestResults()
    {
        TestResultDirectory.GlobFiles("*.trx").ForEach(x =>
            AzurePipelines.Instance?.PublishTestResults(
                type: AzurePipelinesTestResultsType.VSTest,
                title: $"{Path.GetFileNameWithoutExtension(x)} ({AzurePipelines.Instance.StageDisplayName})",
                files: new string[] { x }));
    }

    void ReportTestCount()
    {
        IEnumerable<string> GetOutcomes(AbsolutePath file)
            => XmlTasks.XmlPeek(
                file,
                "/xn:TestRun/xn:Results/xn:UnitTestResult/@outcome",
                ("xn", "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"));

        var resultFiles = TestResultDirectory.GlobFiles("*.trx");
        var outcomes = resultFiles.SelectMany(GetOutcomes).ToList();
        var passedTests = outcomes.Count(x => x == "Passed");
        var failedTests = outcomes.Count(x => x == "Failed");
        var skippedTests = outcomes.Count(x => x == "NotExecuted");

        ReportSummary(_ => _
            .When(failedTests > 0, _ => _
                .AddPair("Failed", failedTests.ToString()))
            .AddPair("Passed", passedTests.ToString())
            .When(skippedTests > 0, _ => _
                .AddPair("Skipped", skippedTests.ToString())));
    }

    sealed Configure<DotNetTestSettings> TestSettingsBase => _ => _
        .SetConfiguration(Configuration)
        .SetNoBuild(SucceededTargets.Contains(Compile))
        .ResetVerbosity()
        .SetResultsDirectory(TestResultDirectory)
        .When(InvokedTargets.Contains((this as IReportCoverage)?.ReportCoverage) || IsServerBuild, _ => _
            .EnableCollectCoverage()
            .SetCoverletOutputFormat(CoverletOutputFormat.cobertura)
            .SetExcludeByFile("*.Generated.cs")
            .When(TeamCity.Instance is not null, _ => _
                .SetCoverletOutputFormat($"\\\"{CoverletOutputFormat.cobertura},{CoverletOutputFormat.teamcity}\\\""))
            .When(IsServerBuild, _ => _
                .EnableUseSourceLink()));

    sealed Configure<DotNetTestSettings, Project> TestProjectSettingsBase => (_, v) => _
        .SetProjectFile(v)
        // // https://github.com/Tyrrrz/GitHubActionsTestLogger
        // .When(GitHubActions.Instance is not null && v.HasPackageReference("GitHubActionsTestLogger"), _ => _
        //     .AddLoggers("GitHubActions;report-warnings=false"))
        // // https://github.com/JetBrains/TeamCity.VSTest.TestAdapter
        // .When(TeamCity.Instance is not null && v.HasPackageReference("TeamCity.VSTest.TestAdapter"), _ => _
        //     .AddLoggers("TeamCity")
        //     // https://github.com/xunit/visualstudio.xunit/pull/108
        //     .AddRunSetting("RunConfiguration.NoAutoReporters", bool.TrueString))
        // .AddLoggers($"trx;LogFileName={v.Name}.trx")
        .When(InvokedTargets.Contains((this as IReportCoverage)?.ReportCoverage) || IsServerBuild, _ => _
            .SetCoverletOutput(TestResultDirectory / $"{v.Name}.xml"));

    Configure<DotNetTestSettings> TestSettings => _ => _;
    Configure<DotNetTestSettings, Project> TestProjectSettings => (_, v) => _;

    IEnumerable<Project> TestProjects { get; }
}
