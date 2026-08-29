using System;
using System.Collections.Generic;
using System.Linq;
using Fallout.Common.Tooling;
using Fallout.Common.Utilities;
using Fallout.Common.Utilities.Collections;

namespace Fallout.Common.Tools.NerdbankGitVersioning;

partial class NerdbankGitVersioningTasks
{
    protected override object GetResult<T>(ToolOptions options, IReadOnlyCollection<Output> output)
    {
        // var output = process.Output.EnsureOnlyStd().Select(x => x.Text).JoinNewLine();
        try
        {
            return output.EnsureOnlyStd().StdToJson<NerdbankGitVersioning>();
        }
        catch (Exception exception)
        {
            throw new Exception(
                $"Cannot parse {nameof(NerdbankGitVersioning)} output:".Concat(output.Select(x => x.Text)).JoinNewLine(),
                exception);
        }
    }
}

public record NerdbankGitVersioning(
    string CloudBuildNumber,
    List<string> BuildMetadataWithCommitId,
    string AssemblyVersion,
    string AssemblyFileVersion,
    string AssemblyInformationalVersion,
    bool PublicRelease,
    string PrereleaseVersion,
    string PrereleaseVersionNoLeadingHyphen,
    string SimpleVersion,
    int BuildNumber,
    int VersionRevision,
    string MajorMinorVersion,
    int VersionMajor,
    int VersionMinor,
    string GitCommitId,
    string GitCommitIdShort,
    DateTime GitCommitDate,
    int VersionHeight,
    int VersionHeightOffset,
    string Version,
    List<string> BuildMetadata,
    string BuildMetadataFragment,
    string NuGetPackageVersion,
    string ChocolateyPackageVersion,
    string NpmPackageVersion,
    string SemVer1,
    string SemVer2,
    int SemVer1NumericIdentifierPadding);
