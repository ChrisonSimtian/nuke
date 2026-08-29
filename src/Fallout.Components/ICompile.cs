using System.Collections.Generic;
using Fallout.Common;
using Fallout.Common.Tooling;
using Fallout.Common.Tools.DotNet;
using Fallout.Common.Utilities;
using Fallout.Common.Utilities.Collections;
using Fallout.Solutions;
using static Fallout.Common.Tools.DotNet.DotNetTasks;

namespace Fallout.Components;

public interface ICompile : IRestore, IHasConfiguration
{
    Target Compile => _ => _
        .DependsOn(Restore)
        .WhenSkipped(DependencyBehavior.Skip)
        .Executes(() =>
        {
            ReportSummary(_ => _
                .WhenNotNull(this as IHasGitVersion, (_, o) => _
                    .AddPair("Version", o.Versioning.NuGetVersionV2))
                .WhenNotNull(this as IHasNerdbankGitVersioning, (_, o) => _
                    .AddPair("Version", o.Versioning.NuGetPackageVersion)));

            DotNetBuild(_ => _
                .Apply(CompileSettingsBase)
                .Apply(CompileSettings));

            DotNetPublish(_ => _
                    .Apply(PublishSettingsBase)
                    .Apply(PublishSettings)
                    .CombineWith(PublishConfigurations, (_, v) => _
                        .SetProject(v.Project)
                        .SetFramework(v.Framework)),
                PublishDegreeOfParallelism);
        });

    sealed Configure<DotNetBuildSettings> CompileSettingsBase => _ => _
        .SetProjectFile(Solution)
        .SetConfiguration(Configuration)
        .When(IsServerBuild, _ => _
            .EnableContinuousIntegrationBuild())
        .SetNoRestore(SucceededTargets.Contains(Restore))
        .WhenNotNull(this as IHasGitRepository, (_, o) => _
            .SetRepositoryUrl(o.GitRepository.HttpsUrl))
        .WhenNotNull(this as IHasGitVersion, (_, o) => _
            .SetAssemblyVersion(o.Versioning.AssemblySemVer)
            .SetFileVersion(o.Versioning.AssemblySemFileVer)
            .SetInformationalVersion(o.Versioning.InformationalVersion))
        .WhenNotNull(this as IHasNerdbankGitVersioning, (_, o) => _
            .SetAssemblyVersion(o.Versioning.AssemblyVersion)
            .SetFileVersion(o.Versioning.AssemblyFileVersion)
            .SetInformationalVersion(o.Versioning.AssemblyInformationalVersion));

    sealed Configure<DotNetPublishSettings> PublishSettingsBase => _ => _
        .SetConfiguration(Configuration)
        .EnableNoBuild()
        .EnableNoLogo()
        .When(IsServerBuild, _ => _
            .EnableContinuousIntegrationBuild())
        .WhenNotNull(this as IHasGitRepository, (_, o) => _
            .SetRepositoryUrl(o.GitRepository.HttpsUrl))
        .WhenNotNull(this as IHasGitVersion, (_, o) => _
            .SetAssemblyVersion(o.Versioning.AssemblySemVer)
            .SetFileVersion(o.Versioning.AssemblySemFileVer)
            .SetInformationalVersion(o.Versioning.InformationalVersion))
        .WhenNotNull(this as IHasNerdbankGitVersioning, (_, o) => _
            .SetAssemblyVersion(o.Versioning.AssemblyVersion)
            .SetFileVersion(o.Versioning.AssemblyFileVersion)
            .SetInformationalVersion(o.Versioning.AssemblyInformationalVersion));

    Configure<DotNetBuildSettings> CompileSettings => _ => _;

    Configure<DotNetPublishSettings> PublishSettings => _ => _;

    IEnumerable<(Project Project, string Framework)> PublishConfigurations
        => new (Project Project, string Framework)[0];

    int PublishDegreeOfParallelism => 10;
}
