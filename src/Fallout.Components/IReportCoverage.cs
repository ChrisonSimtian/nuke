using System.IO;
using System.Linq;
using Fallout.Common;
using Fallout.Common.CI.AzurePipelines;
using Fallout.Common.IO;
using Fallout.Common.Tooling;
using Fallout.Common.Tools.Codecov;
using Fallout.Common.Tools.ReportGenerator;
using Fallout.Common.Utilities;
using Fallout.Common.Utilities.Collections;
using static Fallout.Common.Tools.Codecov.CodecovTasks;
using static Fallout.Common.Tools.ReportGenerator.ReportGeneratorTasks;

namespace Fallout.Components;

public interface IReportCoverage : ITest, IHasReports, IHasGitRepository
{
    bool CreateCoverageHtmlReport { get; }

    bool ReportToCodecov { get; }

    [Parameter]
    [Secret]
    string CodecovToken => TryGetValue(() => CodecovToken);

    AbsolutePath CoverageReportDirectory => ReportDirectory / "coverage-report";

    AbsolutePath CoverageReportArchive => CoverageReportDirectory.WithExtension("zip");

    Target ReportCoverage => _ => _
        .DependsOn(Test)
        .TryAfter<ITest>()
        .Consumes(Test)
        .Produces(CoverageReportArchive)
        .Requires(() => !ReportToCodecov || CodecovToken != null)
        .Executes(() =>
        {
            if (ReportToCodecov)
            {
                Codecov(_ => _
                    .Apply(CodecovSettingsBase)
                    .Apply(CodecovSettings));
            }

            if (CreateCoverageHtmlReport)
            {
                ReportGenerator(_ => _
                    .Apply(ReportGeneratorSettingsBase)
                    .Apply(ReportGeneratorSettings));

                CoverageReportDirectory.ZipTo(CoverageReportArchive, fileMode: FileMode.Create);
            }

            UploadCoverageData();
        });

    sealed Configure<CodecovSettings> CodecovSettingsBase => _ => _
        .SetFiles(TestResultDirectory.GlobFiles("*.xml").Select(x => x.ToString()))
        .SetToken(CodecovToken)
        .SetBranch(GitRepository.Branch)
        .SetSha(GitRepository.Commit)
        .WhenNotNull(this as IHasGitVersion, (_, o) => _
            .SetBuild(o.Versioning.FullSemVer))
        .SetFramework("netcoreapp3.0");

    Configure<CodecovSettings> CodecovSettings => _ => _;

    sealed Configure<ReportGeneratorSettings> ReportGeneratorSettingsBase => _ => _
        .SetReports(TestResultDirectory / "*.xml")
        .SetReportTypes(ReportTypes.HtmlInline)
        .SetTargetDirectory(CoverageReportDirectory)
        .SetFramework("netcoreapp2.1");

    Configure<ReportGeneratorSettings> ReportGeneratorSettings => _ => _;

    void UploadCoverageData()
    {
        TestResultDirectory.GlobFiles("*.xml").ForEach(x =>
            AzurePipelines.Instance?.PublishCodeCoverage(
                AzurePipelinesCodeCoverageToolType.Cobertura,
                x,
                CoverageReportDirectory));
    }
}
