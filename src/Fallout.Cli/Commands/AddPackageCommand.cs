using System.Linq;
using System.Threading.Tasks;
using Fallout.Common;
using Fallout.Common.Execution;
using Fallout.Common.IO;
using Fallout.Solutions;
using Fallout.Common.Tooling;
using Fallout.Common.Tools.DotNet;

namespace Fallout.Cli.Commands;

/// <summary>
/// <c>fallout :add-package</c>: adds (or upgrades) a NuGet package reference in the build project.
/// </summary>
internal sealed class AddPackageCommand : IFalloutCommand
{
    private readonly IConfigurationReader _configuration;
    private readonly IPackageManager _packages;

    public AddPackageCommand(IConfigurationReader configuration, IPackageManager packages)
    {
        _configuration = configuration;
        _packages = packages;
    }

    public string Name => "add-package";

    public async Task<int> ExecuteAsync(string[] args, AbsolutePath rootDirectory, AbsolutePath buildScript)
    {
        ToolBanner.Print();
        Logging.Configure();
        Telemetry.AddPackage();
        ProjectModelTasks.Initialize();

        var packageId = args.ElementAt(0);
        var packageVersion =
            (EnvironmentInfo.GetNamedArgument<string>("version") ??
             args.ElementAtOrDefault(1) ??
             await NuGetVersionResolver.GetLatestVersion(packageId, includePrereleases: false) ??
             NuGetPackageResolver.GetGlobalInstalledPackage(packageId, version: null, packagesConfigFile: null)?.Version.ToString())
            .NotNull("packageVersion != null");

        var configuration = _configuration.Read(buildScript, evaluate: true);
        var buildProjectFile = configuration[ConfigurationReader.BuildProjectFileKey];
        Host.Information($"Installing {packageId}/{packageVersion} to {buildProjectFile} ...");
        _packages.AddOrReplacePackage(packageId, packageVersion, PackageManager.DownloadType, buildProjectFile);
        DotNetTasks.DotNet($"restore {buildProjectFile}");

        var installedPackage = NuGetPackageResolver.GetGlobalInstalledPackage(packageId, packageVersion, packagesConfigFile: null)
            .NotNull("installedPackage != null");
        var hasToolsDirectory = installedPackage.Directory.GlobDirectories("tools").Any();
        if (!hasToolsDirectory)
            _packages.AddOrReplacePackage(packageId, packageVersion, PackageManager.ReferenceType, buildProjectFile);

        Host.Information($"Done installing {packageId}/{packageVersion} to {buildProjectFile}");
        return 0;
    }
}
