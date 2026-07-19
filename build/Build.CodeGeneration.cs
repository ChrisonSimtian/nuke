using System;
using Fallout.Common;
using Fallout.Common.IO;
using Fallout.Common.Utilities.Collections;
using static Fallout.CodeGeneration.CodeGenerator;
using static Fallout.CodeGeneration.ReferenceUpdater;
using static Fallout.Common.Tools.Git.GitTasks;

partial class Build
{
    AbsolutePath SpecificationsDirectory => RootDirectory / "src" / "Fallout.Common" / "Tools";
    AbsolutePath ReferencesDirectory => RootDirectory / "docs" / "cli-tools";

    // Hardcoded rather than derived from the locally-resolved git remote (e.g. via
    // GitRepository.Identifier): the generated header comment must stay identical regardless of
    // which fork or clone regenerates it, otherwise every regeneration diffs by org name alone.
    const string CanonicalRepositoryIdentifier = "Fallout-build/Fallout";

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
                    sourceFileProvider: x => $"https://github.com/{CanonicalRepositoryIdentifier}/blob/{MainBranch}/{RootDirectory.GetUnixRelativePathTo(x.SpecificationFile)}"));
        });

    // CI gate: `GenerateTools` only runs when a contributor remembers to invoke it, so a .json
    // spec edited without regenerating its .Generated.cs could merge silently and ship stale
    // wrapper code. `Requires` is asserted for the whole scheduled plan before any target runs,
    // so the "start clean" check below still fires before GenerateTools regenerates anything;
    // the explicit re-check afterward catches drift with a message pointing at the fix.
    Target VerifyGeneratedTools => _ => _
        .Requires(() => GitHasCleanWorkingCopy())
        .DependsOn(GenerateTools)
        .Executes(() =>
        {
            Assert.True(
                GitHasCleanWorkingCopy(),
                "Generated tool wrappers are out of sync with their .json specs. Run './build.ps1 GenerateTools' locally and commit the result.");
        });
}
