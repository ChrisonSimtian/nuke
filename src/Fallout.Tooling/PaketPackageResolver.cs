using System.IO;

namespace Fallout.Common.Tooling;

// TODO: Add similar methods to NuGetPackageResolver
public static class PaketPackageResolver
{
    public static string GetLocalInstalledPackageDirectory(string packageId, string packagesConfigFile)
    {
        var packagesDirectory = GetPackagesDirectory(packagesConfigFile);
        return Path.Combine(packagesDirectory, packageId);
    }

    private static string GetPackagesDirectory(string packagesConfigFile)
    {
        return Path.Combine(Path.GetDirectoryName(packagesConfigFile).NotNull(), "packages");
    }
}
