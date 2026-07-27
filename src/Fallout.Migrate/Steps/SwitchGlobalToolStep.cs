using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CliWrap;
using CliWrap.Buffered;
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
    public async Task ExecuteAsync(MigrationContext context, Summary summary)
    {
        var listed = await ListGlobalToolsAsync(summary);
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
            await UninstallAsync(id, summary);
        }

        if (currentAlreadyInstalled)
        {
            // The current id was already there alongside a retired one. Removing the retired install
            // above is the whole fix; reinstalling would only risk downgrading a newer tool.
            return;
        }

        await InstallAsync(context.ToolVersion, summary);
    }

    /// <summary>
    /// Reads the machine-wide tool list via <c>dotnet tool list --global</c>.
    /// </summary>
    /// <param name="summary">The summary to warn on when the list cannot be read.</param>
    /// <returns>
    /// The installed package ids, lowercased, or <c>null</c> when the command could not be run — which
    /// is different from an empty list, meaning "ran fine, nothing installed".
    /// </returns>
    private static async Task<IReadOnlyCollection<string>> ListGlobalToolsAsync(Summary summary)
    {
        var result = await RunAsync(["tool", "list", "--global"]);
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
    private static async Task UninstallAsync(string packageId, Summary summary)
    {
        var result = await RunAsync(["tool", "uninstall", "--global", packageId]);
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
    private static async Task InstallAsync(string toolVersion, Summary summary)
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

        var result = await RunAsync(arguments);
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
    /// Runs <c>dotnet</c> with <paramref name="arguments"/> and buffers its output.
    /// </summary>
    /// <returns>
    /// The buffered result, or <c>null</c> when the process could not be started or timed out —
    /// CliWrap throws for both, and neither should end the migration.
    /// </returns>
    private static async Task<BufferedCommandResult> RunAsync(IEnumerable<string> arguments)
    {
        using var cancellation = new CancellationTokenSource(commandTimeout);

        try
        {
            return await Cli.Wrap("dotnet")
                .WithArguments(arguments)
                // Non-zero is an expected outcome here (tool not installed, feed unreachable), so it
                // is inspected via ExitCode rather than raised as an exception.
                .WithValidation(CommandResultValidation.None)
                .ExecuteBufferedAsync(cancellation.Token);
        }
        catch (Exception)
        {
            // No dotnet on PATH, or the command outlived commandTimeout.
            return null;
        }
    }

    /// <summary>Renders the install command's tail for a warning or a dry-run line.</summary>
    internal static string DescribeInstall(string toolVersion)
    {
        return toolVersion == null
            ? RewriteToolManifestStep.CurrentToolId
            : $"{RewriteToolManifestStep.CurrentToolId} --version {toolVersion}";
    }

    /// <summary>Appends the failing command's own error output to a warning, when there is any.</summary>
    private static string Describe(BufferedCommandResult result)
    {
        var error = result?.StandardError.Trim();

        return string.IsNullOrEmpty(error)
            ? string.Empty
            : $" ({error.Split('\n').First().Trim()})";
    }
}
