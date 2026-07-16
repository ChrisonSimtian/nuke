using System.ComponentModel;
using JetBrains.Annotations;
using Spectre.Console.Cli;

namespace Fallout.Migrate;

/// <summary>
/// Command-line arguments accepted by <see cref="MigrateCommand"/>.
/// </summary>
[UsedImplicitly]
internal sealed class MigrateSettings : CommandSettings
{
    /// <summary>
    /// The repository root to migrate. When <c>null</c>, <see cref="MigrateCommand"/> resolves it by
    /// walking up from the working directory.
    /// </summary>
    [CommandArgument(0, "[path]")]
    [Description("Repository root. Defaults to walking up from the working directory to find " +
                 "one (looking for build.cmd / build.ps1 / build.sh, .nuke/, or build/).")]
    public string Path { get; init; }

    /// <summary>
    /// When <c>true</c>, reports the changes <see cref="Migration"/> would make without writing them.
    /// </summary>
    [CommandOption("-n|--dry-run")]
    [Description("Show what would change without writing.")]
    public bool DryRun { get; init; }
}
