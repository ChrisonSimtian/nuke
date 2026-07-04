using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fallout.Common.Tooling;
using Fallout.Common.Utilities;
using static Fallout.Common.IO.PathConstruction;

namespace Fallout.MSBuildTasks.Engine;

/// <summary>
/// MSBuild-free NuGet package discovery for self-contained embedding and tool packing. Lifted out of
/// <c>EmbedPackagesForSelfContainedTask</c> / <c>PackPackageToolsTask</c>.
/// </summary>
public static class PackageToolingEngine
{
    /// <summary>Package files carrying a <c>tools</c> folder, excluding the runtime pack.</summary>
    public static IReadOnlyList<string> GetEmbeddablePackageFiles(string projectAssetsFile)
    {
        return NuGetPackageResolver.GetLocalInstalledPackages(projectAssetsFile)
            .Where(x => !x.Id.StartsWithOrdinalIgnoreCase("microsoft.netcore.app.runtime"))
            .Where(x => Directory.GetDirectories(x.Directory, "tools").Any())
            .Select(x => (string)x.File)
            .ToList();
    }

    /// <summary>Every file under each installed package's <c>tools</c> folder, with its pack metadata.</summary>
    public static IReadOnlyList<PackagedToolFile> GetPackageToolFiles(
        string projectAssetsFile,
        string nuGetPackageRoot,
        string targetFramework)
    {
        return NuGetPackageResolver.GetLocalInstalledPackages(projectAssetsFile)
            .SelectMany(x => GetFiles(nuGetPackageRoot, targetFramework, x.Id, x.Version.ToString()))
            .ToList();
    }

    private static IEnumerable<PackagedToolFile> GetFiles(
        string nuGetPackageRoot,
        string targetFramework,
        string packageId,
        string packageVersion)
    {
        var packageToolsDirectory = Path.Combine(
            nuGetPackageRoot, packageId.ToLowerInvariant(), packageVersion.ToLowerInvariant(), "tools");
        if (!Directory.Exists(packageToolsDirectory))
            yield break;

        foreach (var file in Directory.GetFiles(packageToolsDirectory, "*", SearchOption.AllDirectories))
        {
            yield return new PackagedToolFile(
                File: file,
                BuildAction: "None",
                PackagePath: Path.Combine(
                    "tools", targetFramework, "any", packageId, GetRelativePath(packageToolsDirectory, file)));
        }
    }
}
