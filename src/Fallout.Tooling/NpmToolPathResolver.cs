using System;
using System.Linq;
using Fallout.Common.IO;

namespace Fallout.Common.Tooling;

public static class NpmToolPathResolver
{
    public static AbsolutePath NpmPackageJsonFile;

    /// <summary>Resets the per-run package.json location so a subsequent build in the same process starts from defaults. FT-1 / #306.</summary>
    public static void Reset()
    {
        NpmPackageJsonFile = null;
    }

    public static string GetNpmExecutable(string npmExecutable)
    {
        Assert.FileExists(NpmPackageJsonFile);

        return ProcessTasks.StartProcess(
                toolPath: ToolPathResolver.GetPathExecutable("npx"),
                arguments: $"which {npmExecutable}",
                workingDirectory: NpmPackageJsonFile.Parent / "node_modules",
                logInvocation: false,
                logOutput: false)
            .AssertZeroExitCode()
            .Output.StdToText();
    }
}