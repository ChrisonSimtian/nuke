using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fallout.Cli.Prompts;
using Fallout.Common;
using Fallout.Common.Execution;
using Fallout.Common.IO;
using Fallout.Common.Utilities;
using Fallout.Solutions;
using static Fallout.Common.EnvironmentInfo;

namespace Fallout.Cli.Commands;

/// <summary>
/// <c>fallout :cake-convert</c>: best-effort conversion of <c>*.cake</c> scripts to Fallout C#.
/// </summary>
internal sealed class CakeConvertCommand : IFalloutCommand
{
    private readonly IConsolePrompts prompts;
    private readonly IConfigurationReader configuration;
    private readonly IPackageManager packages;
    private readonly SetupCommand setup;

    public CakeConvertCommand(
        IConsolePrompts prompts,
        IConfigurationReader configuration,
        IPackageManager packages,
        SetupCommand setup)
    {
        this.prompts = prompts;
        this.configuration = configuration;
        this.packages = packages;
        this.setup = setup;
    }

    public string Name => "cake-convert";

    public async Task<int> ExecuteAsync(string[] args, AbsolutePath rootDirectory, AbsolutePath buildScript)
    {
        ToolBanner.Print();
        Logging.Configure();
        ProjectModelTasks.Initialize();

        Host.Warning(
            new[]
            {
                "Converting .cake files is a best effort approach using syntax rewriting.",
                "Compile errors are to be expected, however, the following elements are currently covered:",
                "  - Target definitions",
                "  - Default targets",
                "  - Parameter declarations",
                "  - Absolute paths",
                "  - Globbing patterns",
                "  - Tool invocations (dotnet CLI, SignTool)",
                "  - Addin and tool references",
            }.JoinNewLine());

        Host.Debug();
        if (!prompts.PromptForConfirmation("Continue?"))
        {
            return 0;
        }

        Host.Debug();

        if (buildScript == null &&
            prompts.PromptForConfirmation("Should a NUKE project be created for better results?"))
        {
            await setup.ExecuteAsync(args, rootDirectory: null, buildScript: null);
        }

        var buildScriptFile = WorkingDirectory / CliConventions.CurrentBuildScriptName;
        var buildProjectFile = buildScriptFile.Exists()
            ? configuration.Read(buildScriptFile, evaluate: true)
                .GetValueOrDefault(ConfigurationReader.BuildProjectFileKey, defaultValue: null)
            : null;

        foreach (var cakeFile in CakeConverter.GetCakeFiles())
        {
            var outputFile = cakeFile.Parent / cakeFile.NameWithoutExtension.Capitalize() + ".cs";
            var content = CakeConverter.GetConvertedContent(cakeFile.ReadAllText());
            outputFile.WriteAllText(content);
        }

        if (buildProjectFile != null)
        {
            var packages = CakeConverter.GetCakeFiles().SelectMany(x => CakeConverter.GetPackages(x.ReadAllText()));
            foreach (var package in packages)
            {
                this.packages.AddOrReplacePackage(package.Id, package.Version, package.Type, buildProjectFile);
            }
        }

        return 0;
    }
}
