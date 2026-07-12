using System;
using System.ComponentModel;
using System.IO;
using Fallout.Common;
using Fallout.Common.IO;
using JetBrains.Annotations;
using Spectre.Console.Cli;

namespace Fallout.Migrate;

[Description("""
             Migrate a NUKE consumer repo to Fallout.

             What it does:
               - Rewrites Nuke.* PackageReferences and MSBuild properties in .csproj
               - Rewrites `using Nuke.*` directives and qualified type references in .cs
               - Rewrites `dotnet nuke` -> `dotnet fallout` and legacy NUKE_* env vars
                 in build.cmd / build.ps1 / build.sh
               - Renames .nuke/ to .fallout/
               - Prints a summary of files changed and warnings to address manually
             """)]
[UsedImplicitly]
internal sealed class MigrateCommand : Command<MigrateSettings>
{
    public override int Execute(CommandContext context, MigrateSettings settings)
    {
        var rootDirectory = ResolveRootDirectory(settings.Path);
        if (rootDirectory is null)
        {
            Console.Error.WriteLine(
                "error: could not locate a repository root containing a build orchestrator project (_build.csproj) under the working directory.");

            Console.Error.WriteLine("       pass an explicit path: fallout-migrate <path>");
            return 1;
        }

        PrintBanner(rootDirectory, settings.DryRun);

        var migration = new Migration(rootDirectory, settings.DryRun, Console.Out);
        var summary = migration.Run();

        PrintSummary(summary, settings.DryRun);
        return 0;
    }

    private static AbsolutePath ResolveRootDirectory(string explicitArg)
    {
        if (explicitArg != null)
        {
            return Path.GetFullPath(explicitArg);
        }

        return EnvironmentInfo.WorkingDirectory.FindParentOrSelf(current =>
            (current / "build").DirectoryExists() ||
            (current / ".nuke").DirectoryExists() ||
            (current / ".fallout").DirectoryExists() ||
            (current / "build.cmd").FileExists() ||
            (current / "build.ps1").FileExists() ||
            (current / "build.sh").FileExists());
    }

    private static void PrintBanner(AbsolutePath rootDirectory, bool dryRun)
    {
        Console.WriteLine($"fallout-migrate — migrating: {rootDirectory}");
        if (dryRun)
        {
            Console.WriteLine("(dry-run — no files will be modified)");
        }

        Console.WriteLine();
    }

    private static void PrintSummary(Migration.Summary summary, bool dryRun)
    {
        Console.WriteLine();
        Console.WriteLine($"Files changed:   {summary.FilesChanged}");
        Console.WriteLine($"Edits made:      {summary.EditCount}");
        Console.WriteLine($"Directories:     {summary.DirectoriesRenamed} renamed");
        if (summary.Warnings.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("Warnings:");
            foreach (var w in summary.Warnings)
            {
                Console.WriteLine($"  - {w}");
            }
        }

        Console.WriteLine();
        Console.WriteLine(dryRun
            ? "Dry-run complete. Re-run without --dry-run to apply changes."
            : "Migration complete. Verify the build:  ./build.ps1   (or ./build.sh on unix)");

        Console.WriteLine("Migration guide: https://fallout.build  (see #37 for the full guide)");
    }
}
