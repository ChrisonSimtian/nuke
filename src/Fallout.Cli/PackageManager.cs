using System.Linq;
using Fallout.Common;
using Fallout.Common.Utilities;
using Fallout.Solutions;

namespace Fallout.Cli;

/// <summary>Adds or replaces a package entry in the build project file.</summary>
internal interface IPackageManager
{
    void AddOrReplacePackage(string packageId, string packageVersion, string packageType, string buildProjectFile);
}

/// <inheritdoc />
internal sealed class PackageManager : IPackageManager
{
    public const string DownloadType = "PackageDownload";
    public const string ReferenceType = "PackageReference";

    public void AddOrReplacePackage(string packageId, string packageVersion, string packageType, string buildProjectFile)
    {
        var buildProject = ProjectModelTasks.ParseProject(buildProjectFile).NotNull();

        var previousPackage = buildProject.Items.SingleOrDefault(x => x.EvaluatedInclude == packageId);
        if (previousPackage != null)
            buildProject.RemoveItem(previousPackage);

        var packageDownloadItem = buildProject.AddItem(packageType, packageId).Single();
        packageDownloadItem.Xml.AddMetadata(
            "Version",
            packageType == ReferenceType ? packageVersion : $"[{packageVersion}]",
            expressAsAttribute: true);
        buildProject.Save();
    }
}
