using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fallout.Common.IO;
using Serilog;

namespace Fallout.Common.Execution;

internal class HandleReSharperSurrogateArgumentsAttribute : BuildExtensionAttributeBase, IOnBuildCreated
{
    private AbsolutePath ReSharperSurrogateFile => FalloutPaths.GetReSharperSurrogateFile(Build.RootDirectory);

    public void OnBuildCreated(IReadOnlyCollection<ExecutableTarget> executableTargets)
    {
        if (!ReSharperSurrogateFile.Exists())
            return;

        var argumentLines = ReSharperSurrogateFile.ReadAllLines();
        var lastWriteTime = File.GetLastWriteTime(ReSharperSurrogateFile);

        Assert.HasSingleItem(argumentLines, $"{ReSharperSurrogateFile} must have only one single line");
        ReSharperSurrogateFile.DeleteFile();
        if (lastWriteTime.AddMinutes(value: 1) < DateTime.Now)
        {
            Log.Warning("Last write time of {File} was {LastWriteTime}. Skipping ...", ReSharperSurrogateFile, lastWriteTime);
            return;
        }

        var arguments = argumentLines.Single();
        EnvironmentInfo.ArgumentParser = new ArgumentParser(arguments);
    }
}
