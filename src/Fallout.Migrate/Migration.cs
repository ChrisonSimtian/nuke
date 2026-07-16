using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Fallout.Common.IO;
using Fallout.Migrate.Common;
using Fallout.Migrate.Steps;

namespace Fallout.Migrate;

/// <summary>
/// Orchestrates a migration run: builds a <see cref="MigrationContext"/> and executes the fixed,
/// ordered list of <see cref="IMigrationStep"/>s against it. Add a new step by implementing
/// <see cref="IMigrationStep"/> and appending it to <see cref="steps"/>.
/// </summary>
/// <param name="rootDirectory">The repository root to migrate.</param>
/// <param name="dryRun">When <c>true</c>, reports intended changes without writing them.</param>
/// <param name="log">The writer steps use to report progress.</param>
internal sealed class Migration(AbsolutePath rootDirectory, bool dryRun, TextWriter log)
{
    /// <summary>
    /// The steps executed by <see cref="RunAsync"/>, in order. <see cref="ResolveFalloutVersionStep"/> must
    /// run first, since later steps read <see cref="MigrationContext.FalloutVersion"/> from it.
    /// </summary>
    private static readonly IReadOnlyList<IMigrationStep> steps =
    [
        new ResolveFalloutVersionStep(),
        new RewriteCsprojsStep(),
        new RewriteCsFilesStep(),
        new RewriteBootstrapScriptsStep(),
        new RenameNukeDirectoryStep()
    ];

    /// <summary>
    /// Runs every step in <see cref="steps"/> against a fresh <see cref="MigrationContext"/>.
    /// </summary>
    /// <returns>A <see cref="Summary"/> of the files changed, edits made, and any warnings.</returns>
    public async Task<Summary> RunAsync()
    {
        var summary = new Summary();
        var context = new MigrationContext(rootDirectory, dryRun, log);

        foreach (var step in steps)
        {
            await step.ExecuteAsync(context, summary);
        }

        return summary;
    }
}
