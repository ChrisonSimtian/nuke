using System;
using Fallout.Common;
using Fallout.Common.IO;
using Fallout.Common.Tools.GitHub;
using Fallout.Common.Utilities.Collections;
using Fallout.Components;
using static Fallout.CodeGeneration.CodeGenerator;
using static Fallout.CodeGeneration.ReferenceUpdater;
using static Fallout.Common.Tools.Git.GitTasks;

// Step: (re)generate the tool wrappers from their JSON specs and the CLI reference docs.
interface IGenerateTools : IFalloutBuild, IHasGitRepository
{
    AbsolutePath SpecificationsDirectory => RootDirectory / "src" / "Fallout.Common" / "Tools";
    AbsolutePath ReferencesDirectory => RootDirectory / "docs" / "cli-tools";

    // Branch that generated source links point at. Mirrors Build.MainBranch, which must
    // stay a const there (it feeds a [GitHubActions] attribute and so can't be shared here).
    string ToolsSourceBranch => "main";

    Target References => _ => _
        .Requires(() => GitHasCleanWorkingCopy())
        .Executes(() =>
        {
            ReferencesDirectory.CreateOrCleanDirectory();

            UpdateReferences(SpecificationsDirectory, ReferencesDirectory);
        });

    Target GenerateTools => _ => _
        .Executes(() =>
        {
            SpecificationsDirectory.GlobFiles("*/*.json").ForEach(x =>
                GenerateCode(
                    x,
                    namespaceProvider: x => $"Fallout.Common.Tools.{x.Name}",
                    sourceFileProvider: x => GitRepository.SetBranch(ToolsSourceBranch).GetGitHubBrowseUrl(x.SpecificationFile)));
        });
}
