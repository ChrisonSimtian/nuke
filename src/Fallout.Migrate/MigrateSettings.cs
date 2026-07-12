using System.ComponentModel;
using JetBrains.Annotations;
using Spectre.Console.Cli;

namespace Fallout.Migrate;

[UsedImplicitly]
internal sealed class MigrateSettings : CommandSettings
{
    [CommandArgument(0, "[path]")]
    [Description("Repository root. Defaults to walking up from the working directory to find " +
                 "one (looking for build.cmd / build.ps1 / build.sh, .nuke/, or build/).")]
    public string Path { get; init; }

    [CommandOption("-n|--dry-run")]
    [Description("Show what would change without writing.")]
    public bool DryRun { get; init; }
}
