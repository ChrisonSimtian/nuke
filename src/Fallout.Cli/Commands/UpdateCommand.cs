using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Fallout.Cli.Prompts;
using Fallout.Common;
using Fallout.Common.Execution;
using Fallout.Common.IO;
using Fallout.Common.Tooling;
using Fallout.Common.Tools.DotNet;
using Fallout.Common.Utilities;
using Fallout.Solutions;
using NuGet.Versioning;
using static Fallout.Common.Constants;
using Project = Microsoft.Build.Evaluation.Project;

namespace Fallout.Cli.Commands;

/// <summary>
/// <c>fallout :update</c>: updates the build scripts, build project, configuration file and global.json.
/// </summary>
internal sealed class UpdateCommand : IFalloutCommand
{
    private readonly IConsolePrompts prompts;
    private readonly IConfigurationReader configuration;
    private readonly IBuildScaffolder scaffolder;

    public UpdateCommand(IConsolePrompts prompts, IConfigurationReader configuration, IBuildScaffolder scaffolder)
    {
        this.prompts = prompts;
        this.configuration = configuration;
        this.scaffolder = scaffolder;
    }

    public string Name => "update";

    public async Task<int> ExecuteAsync(string[] args, AbsolutePath rootDirectory, AbsolutePath buildScript)
    {
        ToolBanner.Print();
        Logging.Configure();

        rootDirectory.NotNull();

        if (buildScript != null)
        {
            prompts.ConfirmExecution("Update build scripts", () => UpdateBuildScripts(rootDirectory, buildScript));
            await prompts.ConfirmExecutionAsync("Update build project", () => UpdateBuildProjectAsync(buildScript));
        }

        prompts.ConfirmExecution("Update configuration file", () => UpdateConfigurationFile(rootDirectory));
        prompts.ConfirmExecution("Update global.json", () => UpdateGlobalJsonFile(rootDirectory));

        prompts.ShowCompletion("Updates");

        return 0;
    }

    private void UpdateBuildScripts(AbsolutePath rootDirectory, AbsolutePath buildScript)
    {
        scaffolder.WriteBuildScripts(
            scriptDirectory: buildScript.Parent,
            rootDirectory);
    }

    private async Task UpdateBuildProjectAsync(AbsolutePath buildScript)
    {
        var configuration = this.configuration.Read(buildScript, evaluate: true);
        var projectFile = configuration[ConfigurationReader.BuildProjectFileKey];

        ProjectModelTasks.Initialize();
        var buildProject = ProjectModelTasks.ParseProject(projectFile).NotNull();

        UpdateTargetFramework(buildProject);
        var previousPackageVersion = await UpdateFalloutCommonPackageAsync(buildProject);

        if (previousPackageVersion.MinVersion >= NuGetVersion.Parse("0.23.5"))
        {
            RemoveLegacyFileIncludes(buildProject);
        }

        buildProject.Save();
    }

    internal static void UpdateTargetFramework(Project buildProject)
    {
        buildProject.SetProperty("TargetFramework", "net10.0");
    }

    private static async Task<FloatRange> UpdateFalloutCommonPackageAsync(Project buildProject)
    {
        var packageItem = buildProject.Items.SingleOrDefault(x => x.EvaluatedInclude == FalloutCommonPackageId).NotNull();
        var previousPackageVersion = FloatRange.Parse(packageItem.GetMetadataValue("Version"));

        var latestPackageVersion = await NuGetVersionResolver.GetLatestVersion(FalloutCommonPackageId, includePrereleases: false);
        if (!previousPackageVersion.Satisfies(NuGetVersion.Parse(latestPackageVersion)))
        {
            packageItem.SetMetadataValue("Version", latestPackageVersion);
        }

        return previousPackageVersion;
    }

    private static void RemoveLegacyFileIncludes(Project buildProject)
    {
        var legacyIncludes =
            new[]
            {
                "csproj.DotSettings",
                "build.ps1",
                "build.sh",
                ".nuke",
                "global.json",
                "nuget.config",
                "azure-pipelines.yml",
                "Jenkinsfile",
                "appveyor.yml",
                ".travis.yml",
                "GitVersion.yml"
            };

        buildProject.Xml.Items
            .Where(x => x.ItemType == "None").ToList()
            .Where(x => x.Include.ContainsAnyOrdinalIgnoreCase(legacyIncludes) ||
                        x.Remove.ContainsAnyOrdinalIgnoreCase(legacyIncludes)).ToList()
            .ForEach(x =>
            {
                var itemGroupElement = x.Parent;
                itemGroupElement.RemoveChild(x);
                if (itemGroupElement.Children.Count == 0)
                {
                    itemGroupElement.Parent.RemoveChild(itemGroupElement);
                }
            });
    }

    private void UpdateConfigurationFile(AbsolutePath rootDirectory)
    {
        var configurationFile = rootDirectory / FalloutDirectoryName;
        if (!configurationFile.Exists())
        {
            return;
        }

        var solutionFile = rootDirectory / configurationFile.ReadAllLines().FirstOrDefault(x => !x.IsNullOrEmpty());
        configurationFile.DeleteFile();

        scaffolder.WriteConfigurationFile(rootDirectory, solutionFile);
        Host.Warning($"The previous {FalloutFileName} file was transformed to a {FalloutDirectoryName} directory.");
        Host.Warning($"The .tmp directory can be cleared, as it is moved to {FalloutDirectoryName}/temp as well.");
        if (solutionFile != null)
        {
            Host.Warning(
                $"Verify the property referencing the solution has the same name as the member with the {nameof(SolutionAttribute)}.");
        }
    }

    private static void UpdateGlobalJsonFile(AbsolutePath rootDirectory)
    {
        var latestInstalledSdk = DotNetTasks.DotNet("--list-sdks", logInvocation: false, logOutput: false)
            .LastOrDefault().Text?.Split(" ").First();

        if (latestInstalledSdk == null)
        {
            return;
        }

        var globalJsonFile = rootDirectory / "global.json";
        var jobject = globalJsonFile.Existing()?.ReadJsonObject() ?? new JsonObject();
        jobject["sdk"] ??= new JsonObject();
        jobject["sdk"].NotNull()["version"] = latestInstalledSdk;
        globalJsonFile.WriteJson(jobject, JsonExtensions.DefaultSerializerOptions);
    }
}
