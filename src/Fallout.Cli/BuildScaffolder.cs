using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Fallout.Common;
using Fallout.Common.IO;
using Fallout.Common.Utilities;
using static Fallout.Common.Constants;
using static Fallout.Common.EnvironmentInfo;
using static Fallout.Common.Tooling.ProcessTasks;
using static Fallout.Common.Utilities.TemplateUtility;

namespace Fallout.Cli;

/// <summary>Writes the build scripts, configuration file and solution wiring when scaffolding a build.</summary>
internal interface IBuildScaffolder
{
    void WriteBuildScripts(AbsolutePath scriptDirectory, AbsolutePath rootDirectory);
    void WriteConfigurationFile(AbsolutePath rootDirectory, AbsolutePath solutionFile);
    void UpdateSolutionFileContent(List<string> content, string buildProjectFileRelative, string buildProjectGuid, string buildProjectName);
    void UpdateSolutionXmlFileContent(XDocument content, string buildProjectFileRelative);
    string[] GetTemplate(string templateName);
}

/// <inheritdoc />
internal sealed class BuildScaffolder : IBuildScaffolder
{
    private const string PROJECT_KIND = "9A19103F-16F7-4668-BE54-9A1E7A4F7556";

    public string[] GetTemplate(string templateName)
    {
        // Anchored to the Fallout.Cli root namespace where the templates/* resources are embedded.
        return ResourceUtility.GetResourceAllLines<BuildScaffolder>($"templates.{templateName}");
    }

    public void WriteBuildScripts(
        AbsolutePath scriptDirectory,
        AbsolutePath rootDirectory)
    {
        (scriptDirectory / "build.sh").WriteAllLines(
            FillTemplate(
                GetTemplate("build.sh"),
                tokens: GetDictionary(
                    new
                    {
                        RootDirectory = scriptDirectory.GetUnixRelativePathTo(rootDirectory),
                    })),
            platformFamily: PlatformFamily.Linux);

        (scriptDirectory / "build.ps1").WriteAllLines(
            FillTemplate(
                GetTemplate("build.ps1"),
                tokens: GetDictionary(
                    new
                    {
                        RootDirectory = scriptDirectory.GetWinRelativePathTo(rootDirectory),
                    })),
            platformFamily: PlatformFamily.Windows);

        // .config/dotnet-tools.json pins Fallout.GlobalTools as a local tool so the thin shims
        // (build.sh / build.ps1) can `dotnet tool restore` and `dotnet fallout` deterministically.
        // Skip if the consumer already has a manifest — they may have other tools pinned and we
        // don't want to clobber. They can add the `fallout.globaltools` entry manually.
        var toolManifest = rootDirectory / ".config" / "dotnet-tools.json";
        if (!toolManifest.FileExists())
        {
            (rootDirectory / ".config").CreateDirectory();
            toolManifest.WriteAllLines(
                FillTemplate(
                    GetTemplate("dotnet-tools.json"),
                    tokens: GetDictionary(
                        new
                        {
                            FalloutCliVersion = typeof(BuildScaffolder).GetTypeInfo().Assembly.GetVersionText(),
                        })));
        }

        MakeExecutable(scriptDirectory / "build.sh");

        void MakeExecutable(AbsolutePath scriptFile)
        {
            if (rootDirectory.ContainsDirectory(".git"))
                StartProcess("git", $"update-index --add --chmod=+x {scriptFile}", logInvocation: false, logOutput: false);

            if (rootDirectory.ContainsDirectory(".svn"))
                StartProcess("svn", $"propset svn:executable on {scriptFile}", logInvocation: false, logOutput: false);

            if (IsUnix)
                StartProcess("chmod", $"+x {scriptFile}", logInvocation: false, logOutput: false);
        }
    }

    public void WriteConfigurationFile(AbsolutePath rootDirectory, AbsolutePath solutionFile)
    {
        var parametersFile = GetDefaultParametersFile(rootDirectory);
        var dictionary = new Dictionary<string, string> { ["$schema"] = BuildSchemaFileName };
        if (solutionFile != null)
            dictionary["Solution"] = rootDirectory.GetUnixRelativePathTo(solutionFile).ToString();
        parametersFile.WriteJson(dictionary, JsonExtensions.DefaultSerializerOptions);
    }

    public void UpdateSolutionFileContent(
        List<string> content,
        string buildProjectFileRelative,
        string buildProjectGuid,
        string buildProjectName)
    {
        if (content.Any(x => x.Contains(buildProjectFileRelative)))
            return;

        var globalIndex = content.IndexOf("Global");
        Assert.True(globalIndex != -1, "Could not find a 'Global' section in solution file");

        var projectConfigurationIndex = content.FindIndex(x => x.Contains("GlobalSection(ProjectConfigurationPlatforms)"));
        if (projectConfigurationIndex == -1)
        {
            var solutionConfigurationIndex = content.FindIndex(x => x.Contains("GlobalSection(SolutionConfigurationPlatforms)"));
            if (solutionConfigurationIndex == -1)
            {
                content.Insert(globalIndex + 1, "\tGlobalSection(SolutionConfigurationPlatforms) = preSolution");
                content.Insert(globalIndex + 2, "\t\tDebug|Any CPU = Debug|Any CPU");
                content.Insert(globalIndex + 3, "\t\tRelease|Any CPU = Release|Any CPU");
                content.Insert(globalIndex + 4, "\tEndGlobalSection");

                solutionConfigurationIndex = globalIndex + 1;
            }

            var endGlobalSectionIndex = content.FindIndex(solutionConfigurationIndex, x => x.Contains("EndGlobalSection"));

            content.Insert(endGlobalSectionIndex + 1, "\tGlobalSection(ProjectConfigurationPlatforms) = postSolution");
            content.Insert(endGlobalSectionIndex + 2, "\tEndGlobalSection");

            projectConfigurationIndex = endGlobalSectionIndex + 1;
        }

        content.Insert(projectConfigurationIndex + 1, $"\t\t{{{buildProjectGuid}}}.Debug|Any CPU.ActiveCfg = Debug|Any CPU");
        content.Insert(projectConfigurationIndex + 2, $"\t\t{{{buildProjectGuid}}}.Release|Any CPU.ActiveCfg = Release|Any CPU");

        content.Insert(globalIndex,
            $"Project(\"{{{PROJECT_KIND}}}\") = \"{buildProjectName}\", \"{buildProjectFileRelative}\", \"{{{buildProjectGuid}}}\"");
        content.Insert(globalIndex + 1,
            "EndProject");
    }

    public void UpdateSolutionXmlFileContent(XDocument content, string buildProjectFileRelative)
    {
        var solutionElement = content.Root;
        Assert.True(solutionElement?.Name == "Solution", "Could not find a root 'Solution' element in solution file");

        // file uses forward slashes for paths on every platform
        var path = buildProjectFileRelative.Replace(oldChar: '\\', newChar: '/');

        if (solutionElement.Elements("Project").Any(x => x.GetAttributeValue("Path").EqualsOrdinalIgnoreCase(path)))
        {
            return;
        }

        var projectElement = new XElement("Project", new XAttribute("Path", path));
        projectElement.Add(new XElement("Build", new XAttribute("Project", value: false)));
        solutionElement.Add(projectElement);
    }
}
