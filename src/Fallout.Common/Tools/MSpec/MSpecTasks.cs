using Fallout.Common.Tooling;

namespace Fallout.Common.Tools.MSpec;

partial class MSpecTasks
{
    protected override string GetToolPath(ToolOptions options = null)
    {
        return NuGetToolPathResolver.GetPackageExecutable(
            PackageId,
            EnvironmentInfo.Is64Bit ? "mspec-clr4.exe" : "mspec-x86-clr4.exe");
    }
}
