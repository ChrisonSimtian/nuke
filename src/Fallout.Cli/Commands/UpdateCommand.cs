using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Text.Json.Nodes;
using Fallout.Cli.Prompts;
using Fallout.Common;
using Fallout.Common.Execution;
using Fallout.Common.IO;
using Fallout.Solutions;
using Fallout.Common.Tools.DotNet;
using Fallout.Common.Utilities;
using static Fallout.Common.Constants;

namespace Fallout.Cli.Commands;

/// <summary>
/// <c>fallout :update</c>: updates the build scripts, build project, configuration file and global.json.
/// </summary>
internal sealed class UpdateCommand : IFalloutCommand
{
    private readonly IConsolePrompts _prompts;
    private readonly IConfigurationReader _configuration;
    private readonly IBuildScaffolder _scaffolder;

    public UpdateCommand(IConsolePrompts prompts, IConfigurationReader configuration, IBuildScaffolder scaffolder)
    {
        _prompts = prompts;
        _configuration = configuration;
        _scaffolder = scaffolder;
    }

    public string Name => "update";

    public Task<int> ExecuteAsync(string[] args, AbsolutePath rootDirectory, AbsolutePath buildScript)
        => Task.FromResult(Execute(args, rootDirectory, buildScript));

    private int Execute(string[] args, AbsolutePath rootDirectory, AbsolutePath buildScript)
    {
        ToolBanner.Print();
        Logging.Configure();

        Assert.NotNull(rootDirectory);

        if (buildScript != null)
        {
            _prompts.ConfirmExecution("Update build scripts", () => UpdateBuildScripts(rootDirectory, buildScript));
            _prompts.ConfirmExecution("Update build project", () => UpdateBuildProject(buildScript));
        }

        _prompts.ConfirmExecution("Update configuration file", () => UpdateConfigurationFile(rootDirectory));
        _prompts.ConfirmExecution("Update global.json", () => UpdateGlobalJsonFile(rootDirectory));

        _prompts.ShowCompletion("Updates");

        return 0;
    }

    private void UpdateBuildScripts(AbsolutePath rootDirectory, AbsolutePath buildScript)
    {
        _scaffolder.WriteBuildScripts(
            scriptDirectory: buildScript.Parent,
            rootDirectory);
    }

    private void UpdateBuildProject(AbsolutePath buildScript)
    {
        var configuration = _configuration.Read(buildScript, evaluate: true);
        var projectFile = configuration[ConfigurationReader.BuildProjectFileKey];
        ProjectModelTasks.Initialize();
        ProjectUpdater.Update(projectFile);
    }

    private void UpdateConfigurationFile(AbsolutePath rootDirectory)
    {
        var configurationFile = rootDirectory / FalloutDirectoryName;
        if (!configurationFile.Exists())
            return;

        var solutionFile = rootDirectory / configurationFile.ReadAllLines().FirstOrDefault(x => !x.IsNullOrEmpty());
        configurationFile.DeleteFile();

        _scaffolder.WriteConfigurationFile(rootDirectory, solutionFile);
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
