using System;
using Fallout.Migrate.Common;
using Spectre.Console;

namespace Fallout.Migrate.Steps;

/// <summary>
/// Asks the user to confirm before any subsequent step writes to disk. Runs last among the
/// read-only/advisory steps, immediately before <see cref="RewriteCsprojsStep"/> — the first step
/// in <see cref="Migration.steps"/> that mutates files. Sets <see cref="Summary.Cancelled"/> when
/// the user declines, which <see cref="Migration.Run"/> checks to stop early.
/// </summary>
internal sealed class ConfirmMigrationStep : IMigrationStep
{
    /// <inheritdoc />
    public void Execute(MigrationContext context, Summary summary)
    {
        if (context.DryRun)
        {
            // Nothing will be written; no point confirming.
            return;
        }

        if (Console.IsInputRedirected)
        {
            // Non-interactive (CI, piped input, automated tests) — nothing to prompt against, so
            // proceed as if confirmed.
            return;
        }

        bool proceed = AnsiConsole.Confirm(
            $"This will modify files under [blue]{context.RootDirectory}[/]. Continue?");

        if (!proceed)
        {
            summary.Cancelled = true;
            AnsiConsole.MarkupLine("[yellow]Migration cancelled — no files were changed.[/]");
        }
    }
}
