using System;
using System.Linq;
using System.Text.Json.Nodes;
using Fallout.Common;
using Fallout.Common.Execution;
using Fallout.Common.IO;
using Fallout.Solutions;
using Fallout.Common.Tools.DotNet;
using Fallout.Common.Utilities;
using static Fallout.Common.Constants;

namespace Fallout.Cli;

partial class Program
{
    public static int Update(string[] args, AbsolutePath rootDirectory, AbsolutePath buildScript)
    {
        PrintInfo();
        Logging.Configure();

        Assert.NotNull(rootDirectory);

        if (buildScript != null)
        {
            ConfirmExecution("Update build scripts", () => UpdateBuildScripts(rootDirectory, buildScript));
            ConfirmExecution("Update build project", () => UpdateBuildProject(buildScript));
        }

        ConfirmExecution("Update configuration file", () => UpdateConfigurationFile(rootDirectory));
        ConfirmExecution("Update global.json", () => UpdateGlobalJsonFile(rootDirectory));

        ShowCompletion("Updates");

        return 0;
    }

    private static void UpdateBuildScripts(AbsolutePath rootDirectory, AbsolutePath buildScript)
    {
        var configuration = GetConfiguration(buildScript, evaluate: true);
        var buildProjectFile = (AbsolutePath) configuration[BUILD_PROJECT_FILE];

        WriteBuildScripts(
            scriptDirectory: buildScript.Parent,
            rootDirectory);
    }

    private static void UpdateBuildProject(AbsolutePath buildScript)
    {
        var configuration = GetConfiguration(buildScript, evaluate: true);
        var projectFile = configuration[BUILD_PROJECT_FILE];
        ProjectModelTasks.Initialize();
        ProjectUpdater.Update(projectFile);
    }

    private static void UpdateConfigurationFile(AbsolutePath rootDirectory)
    {
        var configurationFile = rootDirectory / FalloutDirectoryName;
        if (!configurationFile.Exists())
            return;

        var solutionFile = rootDirectory / configurationFile.ReadAllLines().FirstOrDefault(x => !x.IsNullOrEmpty());
        configurationFile.DeleteFile();

        WriteConfigurationFile(rootDirectory, solutionFile);
        Host.Warning($"The previous {FalloutFileName} file was transformed to a {FalloutDirectoryName} directory.");
        Host.Warning($"The .tmp directory can be cleared, as it is moved to {FalloutDirectoryName}/temp as well.");
        if (solutionFile != null)
            Host.Warning($"Verify the property referencing the solution has the same name as the member with the {nameof(SolutionAttribute)}.");
    }

    private static void UpdateGlobalJsonFile(AbsolutePath rootDirectory)
    {
        var latestInstalledSdk = DotNetTasks.DotNet("--list-sdks", logInvocation: false, logOutput: false)
            .LastOrDefault().Text?.Split(" ").First();
        if (latestInstalledSdk == null)
            return;

        var globalJsonFile = rootDirectory / "global.json";
        var jobject = globalJsonFile.Existing()?.ReadJsonObject() ?? new JsonObject();
        jobject["sdk"] ??= new JsonObject();
        jobject["sdk"].NotNull()["version"] = latestInstalledSdk;
        globalJsonFile.WriteJson(jobject, JsonExtensions.DefaultSerializerOptions);
    }
}
