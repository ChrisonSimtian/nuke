using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using Fallout.Common;
using Fallout.Common.IO;
using Fallout.Migrate.Common;
using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Fallout.Migrate;

/// <summary>
/// The <c>fallout-migrate</c> CLI command. See the <see cref="DescriptionAttribute"/> below for the
/// user-facing summary of what the migration does.
/// </summary>
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
internal sealed class MigrateCommand : AsyncCommand<MigrateSettings>
{
    /// <summary>
    /// Resolves the repository root, runs the migration, and prints a summary.
    /// </summary>
    /// <param name="context">The Spectre.Console.Cli command context (unused).</param>
    /// <param name="settings">The parsed <see cref="MigrateSettings"/> for this invocation.</param>
    /// <returns>0 on success, 1 if the repository root could not be resolved.</returns>
    public override async Task<int> ExecuteAsync(CommandContext context, MigrateSettings settings)
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
        Summary summary = await migration.RunAsync();

        if (summary.Cancelled)
        {
            return 1;
        }

        PrintSummary(summary, settings.DryRun);
        return 0;
    }

    /// <summary>
    /// Resolves the repository root to migrate: the explicit <paramref name="explicitArg"/> if given,
    /// otherwise the nearest ancestor of the working directory that looks like a Fallout/NUKE build repo.
    /// </summary>
    /// <param name="explicitArg">The path argument passed on the command line, or <c>null</c>.</param>
    /// <returns>The resolved repository root, or <c>null</c> if none could be found.</returns>
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

    /// <summary>
    /// Prints the tool banner and, when applicable, the dry-run notice.
    /// </summary>
    /// <param name="rootDirectory">The repository root being migrated.</param>
    /// <param name="dryRun">Whether the migration is running in dry-run mode.</param>
    private static void PrintBanner(AbsolutePath rootDirectory, bool dryRun)
    {
        AnsiConsole.MarkupLineInterpolated($"[bold]fallout-migrate[/] — migrating: [blue]{rootDirectory}[/]");
        if (dryRun)
        {
            AnsiConsole.MarkupLine("[yellow](dry-run — no files will be modified)[/]");
        }

        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Prints the migration <see cref="Summary"/> (counts, warnings, and next-steps guidance) to the console.
    /// </summary>
    /// <param name="summary">The result of <see cref="Migration.RunAsync"/>.</param>
    /// <param name="dryRun">Whether the migration ran in dry-run mode.</param>
    private static void PrintSummary(Summary summary, bool dryRun)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLineInterpolated($"Files changed:   [bold]{summary.FilesChanged}[/]");
        AnsiConsole.MarkupLineInterpolated($"Edits made:      [bold]{summary.EditCount}[/]");
        AnsiConsole.MarkupLineInterpolated($"Directories:     [bold]{summary.DirectoriesRenamed}[/] renamed");

        if (summary.Warnings.Count > 0)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[yellow]Warnings:[/]");
            foreach (var w in summary.Warnings)
            {
                AnsiConsole.MarkupLineInterpolated($"[yellow]  - {w}[/]");
            }
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine(dryRun
            ? "[green]Dry-run complete.[/] Re-run without --dry-run to apply changes."
            : "[green]Migration complete.[/] Verify the build: ./build.ps1 (or ./build.sh on UNIX)");

        AnsiConsole.MarkupLine("[grey]Migration guide: https://fallout.build  (see #37 for the full guide)[/]");
    }
}
