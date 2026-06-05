using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fallout.Common.Utilities;
using static Fallout.Common.Constants;

namespace Fallout.Common.Execution;

internal class UpdateNotificationAttribute : BuildExtensionAttributeBase, IOnBuildCreated, IOnBuildFinished
{
    public void OnBuildCreated(IReadOnlyCollection<ExecutableTarget> executableTargets)
    {
        if (Build.IsLocalBuild && ShouldNotify)
        {
            Notify();
            Host.Information("Press any key to continue without update ...");
            Console.ReadKey();
        }
    }

    public void OnBuildFinished()
    {
        if (Build.IsServerBuild && ShouldNotify)
            Notify();
    }

    private bool ShouldNotify => !Directory.Exists(GetFalloutDirectory(Build.RootDirectory)) &&
                                 !Build.IsInterceptorExecution;

    private static void Notify()
    {
        Host.Warning(
            new[]
            {
                "--- UPDATE RECOMMENDED FROM 5.1.0 ---",
                "1. Update your global tool",
                "   dotnet tool update Fallout.GlobalTools -g",
                "2. Update your build",
                "   fallout :update",
                "3. Confirm on update for configuration file and build scripts",
                "   (Others are be optional)",
                string.Empty
            }.JoinNewLine());
    }
}
