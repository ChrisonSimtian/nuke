using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fallout.Common.Tooling;
using Fallout.Common.Utilities;
using Fallout.Migrate.Common;
using Spectre.Console;

namespace Fallout.Migrate.Steps;

/// <summary>
/// Moves a machine-wide (<c>--global</c>) install of a retired Fallout or NUKE tool onto
/// <see cref="RewriteToolManifestStep.CurrentToolId"/>: uninstalls each retired id that is installed,
/// then installs the current one.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="RewriteToolManifestStep"/> only fixes a repo's <c>.config/dotnet-tools.json</c>. That
/// leaves a globally installed old tool in place, and two packages that both provide the
/// <c>fallout</c> command conflict — the command resolves to whichever one the SDK finds first. So the
/// manifest rewrite alone is not enough to leave the machine in a working state.
/// </para>
/// <para>
/// Every command here is best-effort. A migration must not fail because the machine has no global
/// install, is offline, or has a tool the user installed from a private feed. Failures become
/// <see cref="Summary.Warnings"/> entries naming the command to run by hand.
/// </para>
/// </remarks>
internal sealed class SwitchGlobalToolStep : IMigrationStep
{
    /// <summary>
    /// How long any single <c>dotnet tool</c> invocation may run before it is abandoned. An install
    /// hits the network, so this is generous; the point is only to stop a hung process from blocking
    /// the whole migration.
    /// </summary>
    private static readonly TimeSpan commandTimeout = TimeSpan.FromMinutes(2);

    /// <inheritdoc />
    public Task ExecuteAsync(MigrationContext context, Summary summary)
    {
        Execute(context, summary);

        return Task.CompletedTask;
    }

    /// <summary>
    /// The step's actual work. Synchronous because <see cref="ProcessTasks"/> is; the interface's
    /// async signature is satisfied by the wrapper above.
    /// </summary>
    private static void Execute(MigrationContext context, Summary summary)
    {
        if (!context.SwitchGlobalTool)
        {
            // Opt-in only. Migrating a repository must not install or uninstall software on the
            // machine by surprise, and MigrationIntegrationSpecs runs this pipeline for real.
            return;
        }

        var listed = ListGlobalTools(summary);
        if (listed == null)
        {
            // Could not read the global tool list, so we don't know what is installed. Warned already.
            return;
        }

        var installedRetiredIds = RewriteToolManifestStep.RetiredToolIds
            .Where(x => listed.Contains(x, StringComparer.OrdinalIgnoreCase))
            .ToList();

        if (installedRetiredIds.Count == 0)
        {
            // Nothing retired is installed machine-wide. Installing the current tool is not this
            // step's job when the user never had a global install in the first place.
            return;
        }

        bool currentAlreadyInstalled = listed.Contains(
            RewriteToolManifestStep.CurrentToolId,
            StringComparer.OrdinalIgnoreCase);

        if (context.DryRun)
        {
            foreach (var id in installedRetiredIds)
            {
                context.Log.WriteLine($"would run: dotnet tool uninstall --global {id}");
            }

            if (!currentAlreadyInstalled)
            {
                context.Log.WriteLine($"would run: dotnet tool install --global {DescribeInstall(context.ToolVersion)}");
            }

            return;
        }

        foreach (var id in installedRetiredIds)
        {
            Uninstall(id, summary);
        }

        if (currentAlreadyInstalled)
        {
            // The current id was already there alongside a retired one. Removing the retired install
            // above is the whole fix; reinstalling would only risk downgrading a newer tool.
            return;
        }

        Install(context.ToolVersion, summary);
    }

    /// <summary>
    /// Reads the machine-wide tool list via <c>dotnet tool list --global</c>.
    /// </summary>
    /// <param name="summary">The summary to warn on when the list cannot be read.</param>
    /// <returns>
    /// The installed package ids, lowercased, or <c>null</c> when the command could not be run — which
    /// is different from an empty list, meaning "ran fine, nothing installed".
    /// </returns>
    private static IReadOnlyCollection<string> ListGlobalTools(Summary summary)
    {
        var result = Run(["tool", "list", "--global"]);
        if (result == null || result.ExitCode != 0)
        {
            summary.Warnings.Add(
                "could not read the global dotnet tool list; skipped the global tool switch — check `dotnet tool list --global` by hand");

            return null;
        }

        return ParseInstalledToolIds(result.StandardOutput);
    }

    /// <summary>
    /// Extracts the package ids from <c>dotnet tool list --global</c> output.
    /// </summary>
    /// <param name="standardOutput">The command's raw standard output.</param>
    /// <returns>The installed package ids, lowercased.</returns>
    /// <remarks>
    /// The output is a table: a header row, a row of dashes under it, then one row per tool whose
    /// first column is the package id. Both leading rows are dropped by matching the dashes rather
    /// than by counting lines, because a row of dashes is the one part of the shape the SDK's
    /// localised output keeps in every language.
    /// </remarks>
    internal static IReadOnlyCollection<string> ParseInstalledToolIds(string standardOutput)
    {
        var lines = standardOutput.Replace("\r", string.Empty).Split('\n');

        var separatorIndex = Array.FindIndex(lines, x => x.TrimStart().StartsWith("---", StringComparison.Ordinal));
        if (separatorIndex == -1)
        {
            // No table at all. Treating the header as a tool id would be worse than reporting none.
            return [];
        }

        return lines
            .Skip(separatorIndex + 1)
            .Select(x => x.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.ToLowerInvariant())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Uninstalls one retired tool id, warning rather than failing when it doesn't work.</summary>
    private static void Uninstall(string packageId, Summary summary)
    {
        var result = Run(["tool", "uninstall", "--global", packageId]);
        if (result == null || result.ExitCode != 0)
        {
            summary.Warnings.Add(
                $"could not uninstall the global tool {packageId} — run `dotnet tool uninstall --global {packageId}` by hand{Describe(result)}");

            return;
        }

        AnsiConsole.MarkupLineInterpolated($"[grey]Uninstalled the global tool [bold]{packageId}[/].[/]");
    }

    /// <summary>
    /// Installs <see cref="RewriteToolManifestStep.CurrentToolId"/>, pinned to
    /// <paramref name="toolVersion"/> when one was resolved.
    /// </summary>
    private static void Install(string toolVersion, Summary summary)
    {
        var arguments = new List<string> { "tool", "install", "--global", RewriteToolManifestStep.CurrentToolId };
        if (toolVersion != null)
        {
            arguments.AddRange(["--version", toolVersion]);

            // A resolved prerelease is only reachable with this flag. ResolveFalloutVersionStep
            // returns one when the major has no stable release yet.
            if (toolVersion.Contains('-'))
            {
                arguments.Add("--prerelease");
            }
        }

        var result = Run(arguments);
        if (result == null || result.ExitCode != 0)
        {
            summary.Warnings.Add(
                $"could not install the global tool {RewriteToolManifestStep.CurrentToolId} — run `dotnet tool install --global {DescribeInstall(toolVersion)}` by hand{Describe(result)}");

            return;
        }

        AnsiConsole.MarkupLineInterpolated(
            $"[grey]Installed the global tool [bold]{RewriteToolManifestStep.CurrentToolId}[/] {toolVersion ?? "(latest)"}.[/]");
    }

    /// <summary>
    /// Runs <c>dotnet</c> with <paramref name="arguments"/> and collects its output.
    /// </summary>
    /// <returns>
    /// The exit code and captured streams, or <c>null</c> when the process could not be started or
    /// outlived <see cref="commandTimeout"/>. Neither should end the migration.
    /// </returns>
    /// <remarks>
    /// Uses the framework's own <see cref="ProcessTasks"/> rather than a process-runner package, so
    /// the migrate tool takes no dependency the framework does not already carry. Output logging is
    /// off: <see cref="ProcessTasks"/> logs through Serilog, and this tool renders through Spectre.
    /// </remarks>
    private static CommandResult Run(IEnumerable<string> arguments)
    {
        try
        {
            // ProcessTasks takes one pre-quoted argument string rather than a list.
            var argumentLine = arguments.Select(x => x.DoubleQuoteIfNeeded()).JoinSpace();

            using var process = ProcessTasks.StartProcess(
                "dotnet",
                argumentLine,
                timeout: (int)commandTimeout.TotalMilliseconds,
                logOutput: false,
                logInvocation: false);

            if (process == null || !process.WaitForExit())
            {
                // Timed out. WaitForExit has already killed the process.
                return null;
            }

            // Non-zero is an expected outcome here (tool not installed, feed unreachable), so the
            // exit code is returned for inspection rather than asserted.
            return new CommandResult(
                process.ExitCode,
                Join(process.Output, OutputType.Std),
                Join(process.Output, OutputType.Err));
        }
        catch (Exception)
        {
            // No dotnet on PATH — ToolPathResolver asserts rather than returning null.
            return null;
        }

        static string Join(IEnumerable<Output> output, OutputType type)
            => output.Where(x => x.Type == type).Select(x => x.Text).JoinNewLine();
    }

    /// <summary>The parts of a finished <c>dotnet</c> invocation this step reads.</summary>
    private sealed record CommandResult(int ExitCode, string StandardOutput, string StandardError);

    /// <summary>Renders the install command's tail for a warning or a dry-run line.</summary>
    internal static string DescribeInstall(string toolVersion)
    {
        return toolVersion == null
            ? RewriteToolManifestStep.CurrentToolId
            : $"{RewriteToolManifestStep.CurrentToolId} --version {toolVersion}";
    }

    /// <summary>Appends the failing command's own error output to a warning, when there is any.</summary>
    private static string Describe(CommandResult result)
    {
        var error = result?.StandardError.Trim();

        return string.IsNullOrEmpty(error)
            ? string.Empty
            : $" ({error.Split('\n').First().Trim()})";
    }
}
