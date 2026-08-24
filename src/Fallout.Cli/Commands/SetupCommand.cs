using System;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Fallout.Cli.Prompts;
using Fallout.Common;
using Fallout.Common.Execution;
using Fallout.Common.IO;
using Fallout.Common.Tooling;
using Fallout.Common.Utilities;
using Fallout.Common.Utilities.Collections;
using Spectre.Console;
using static Fallout.Constants;
using static Fallout.Common.EnvironmentInfo;
using static Fallout.Common.Utilities.TemplateUtility;

namespace Fallout.Cli.Commands;

/// <summary>
/// <c>fallout :setup</c>: scaffolds a new build (build scripts, build project, configuration) interactively.
/// </summary>
internal sealed class SetupCommand : IFalloutCommand
{
    private const string TARGET_FRAMEWORK = "net10.0";

    private readonly IConsolePrompts prompts;
    private readonly IBuildScaffolder scaffolder;

    public SetupCommand(IConsolePrompts prompts, IBuildScaffolder scaffolder)
    {
        this.prompts = prompts;
        this.scaffolder = scaffolder;
    }

    public string Name => "setup";

    public async Task<int> ExecuteAsync(string[] args, AbsolutePath rootDirectory, AbsolutePath buildScript)
    {
        ToolBanner.Print();
        Logging.Configure();

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]Let's setup a new build![/]");
        AnsiConsole.WriteLine();

        #region Basic

        var falloutLatestReleaseVersion = NuGetVersionResolver.GetLatestVersion(FalloutCommonPackageId, includePrereleases: false);
        var falloutLatestPrereleaseVersion = NuGetVersionResolver.GetLatestVersion(FalloutCommonPackageId, includePrereleases: true);
        var falloutLatestLocalVersion = NuGetPackageResolver.GetGlobalInstalledPackage(FalloutCommonPackageId, version: null, packagesConfigFile: null)
            ?.Version.ToString();

        if (rootDirectory == null)
            rootDirectory = WorkingDirectory.FindParentOrSelf(x => x.ContainsDirectory(".git") || x.ContainsDirectory(".svn"));

        if (rootDirectory == null)
        {
            Host.Warning("Could not find root directory. Falling back to working directory ...");
            rootDirectory = WorkingDirectory;
        }
        prompts.ShowInput("deciduous_tree", "Root directory", rootDirectory);

        var buildProjectName = prompts.PromptForInput("How should the project be named?", "_build");
        prompts.ClearPreviousLine();
        prompts.ShowInput("bookmark", "Build project name", buildProjectName);

        var buildProjectRelativeDirectory = prompts.PromptForInput("Where should the project be located?", "./build");
        prompts.ClearPreviousLine();
        prompts.ShowInput("round_pushpin", "Build project location", buildProjectRelativeDirectory);

        var falloutVersion = prompts.PromptForChoice("Which Fallout.Common version should be used?",
            new[]
                {
                    ("latest release", await falloutLatestReleaseVersion),
                    ("latest prerelease", await falloutLatestPrereleaseVersion),
                    ("latest local", falloutLatestLocalVersion),
                    ("same as global tool", typeof(SetupCommand).GetTypeInfo().Assembly.GetVersionText())
                }
                .Where(x => x.Item2 != null)
                .Distinct(x => x.Item2)
                .Select(x => (x.Item2, $"{x.Item2} ({x.Item1})")).ToArray());
        prompts.ShowInput("gem_stone", "Fallout.Common version", falloutVersion);

        var solutionFile = (AbsolutePath) prompts.PromptForChoice(
            "Which solution should be the default?",
            choices: new DirectoryInfo(rootDirectory)
                .EnumerateFiles("*", SearchOption.AllDirectories)
                .Where(x => x.FullName.EndsWithOrdinalIgnoreCase(".sln") || x.FullName.EndsWithOrdinalIgnoreCase(".slnx"))
                .OrderByDescending(x => x.FullName)
                .Select(x => (x, rootDirectory.GetRelativePathTo(x.FullName).ToString()))
                .Concat((null, "None")).ToArray())?.FullName;
        prompts.ShowInput("toolbox", "Default solution", solutionFile != null ? rootDirectory.GetRelativePathTo(solutionFile) : "<none>");

        #endregion

        #region Generation

        var buildDirectory = rootDirectory / buildProjectRelativeDirectory;
        var buildProjectFile = rootDirectory / buildProjectRelativeDirectory / buildProjectName + ".csproj";
        var buildProjectGuid = Guid.NewGuid().ToString().ToUpper();

        (rootDirectory / FalloutDirectoryName).CreateDirectory();

        scaffolder.WriteBuildScripts(
            scriptDirectory: WorkingDirectory,
            rootDirectory);

        scaffolder.WriteConfigurationFile(rootDirectory, solutionFile);

        if (solutionFile != null)
        {
            var buildProjectFileRelative = solutionFile.Parent.GetWinRelativePathTo(buildProjectFile);
            if (solutionFile.Extension.EqualsOrdinalIgnoreCase(".slnx"))
            {
                var solutionDocument = XDocument.Load(solutionFile);
                scaffolder.UpdateSolutionXmlFileContent(solutionDocument, buildProjectFileRelative);

                var settings = new XmlWriterSettings { OmitXmlDeclaration = true, Indent = true };
                using var writer = XmlWriter.Create(solutionFile, settings);
                solutionDocument.Save(writer);
            }
            else
            {
                var solutionFileContent = solutionFile.ReadAllLines().ToList();
                scaffolder.UpdateSolutionFileContent(solutionFileContent, buildProjectFileRelative, buildProjectGuid, buildProjectName);
                solutionFile.WriteAllLines(solutionFileContent, Encoding.UTF8);
            }
        }

        buildProjectFile.WriteAllLines(
            FillTemplate(
                scaffolder.GetTemplate("_build.csproj"),
                GetDictionary(
                    new
                    {
                        RootDirectory = buildDirectory.GetWinRelativePathTo(rootDirectory),
                        ScriptDirectory = buildDirectory.GetWinRelativePathTo(WorkingDirectory),
                        TargetFramework = TARGET_FRAMEWORK,
                        FalloutVersion = falloutVersion,
                    })));

        (buildDirectory / "Directory.Build.props").WriteAllLines(scaffolder.GetTemplate("Directory.Build.props"));
        (buildDirectory / "Directory.Build.targets").WriteAllLines(scaffolder.GetTemplate("Directory.Build.targets"));
        (buildDirectory / "Build.cs").WriteAllLines(FillTemplate(scaffolder.GetTemplate("Build.cs")));
        (buildDirectory / "Configuration.cs").WriteAllLines(scaffolder.GetTemplate("Configuration.cs"));

        #endregion

        prompts.ShowCompletion("Setup");

        return 0;
    }
}
