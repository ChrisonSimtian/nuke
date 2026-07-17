using System;
using System.Runtime.InteropServices;
using Fallout.Common.Tooling;

namespace Fallout.Common.Tools.SignTool;

partial class SignToolTasks
{
    protected override string GetToolPath(ToolOptions options = null)
    {
        var architecture = RuntimeInformation.OSArchitecture switch
        {
            Architecture.Arm64 => "arm64",
            Architecture.X86 => "x86",
            Architecture.X64 => "x64",
            _ => throw new ArgumentException("Unsupported architecture")
        };

        return NuGetToolPathResolver.GetPackageExecutable(
            packageId: PackageId,
            packageExecutable: PackageExecutable,
            framework: architecture);
    }
}
