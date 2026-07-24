using System;
using System.Collections.Generic;
using System.Linq;
using Fallout.Common.Execution;
using Fallout.Common.IO;
using Fallout.Common.Tooling;
using Serilog;
using static Fallout.Common.CI.BuildServerConfigurationGeneration;

namespace Fallout.Common.CI;

public class InvokeBuildServerConfigurationGenerationAttribute
    : BuildServerConfigurationGenerationAttributeBase, IOnBuildCreated
{
    public void OnBuildCreated(IReadOnlyCollection<ExecutableTarget> executableTargets)
    {
        if (Build.IsServerBuild || Build.IsInterceptorExecution)
            return;

        var hasConfigurationChanged = GetGenerators(Build)
            .Where(x => x.AutoGenerate)
            .AsParallel()
            .Select(HasConfigurationChanged).ToList();
        if (hasConfigurationChanged.All(x => !x))
            return;

        if (Build.Help)
            return;

        if (Console.IsInputRedirected)
            return;

        Host.Information("Press any key to continue ...");
        Console.ReadKey();
    }

    private bool HasConfigurationChanged(IConfigurationGenerator generator)
    {
        var generatedFiles = generator.GeneratedFiles.ToList();
        generatedFiles.ForEach(x => x.Parent.CreateDirectory());

        var previousHashes = generatedFiles
            .WhereFileExists()
            .ToDictionary(x => x, x => x.GetFileHash());

        ProcessTasks.StartProcess(
                Build.BuildAssemblyFile,
                $"--{ConfigurationParameterName} {generator.Id} --host {generator.HostName}",
                logInvocation: false,
                logOutput: false)
            .AssertZeroExitCode();

        var changedFiles = generatedFiles
            .Where(x => x.GetFileHash() != previousHashes.GetValueOrDefault(x))
            .Select(x => Build.RootDirectory.GetRelativePathTo(x)).ToList();

        if (changedFiles.Count == 0)
            return false;

        // TODO: multi-line logging
        Log.Warning("Configuration files for {Configuration} have changed.", generator.DisplayName);
        changedFiles.ForEach(x => Log.Verbose("Updated {File}", x));
        return true;
    }
}
