using System.Threading.Tasks;
using Fallout.Migrate.Common;

namespace Fallout.Migrate.Steps;

/// <summary>
/// Rewrites every <c>*.cs</c> file under the repository root via <see cref="CodeRewriter"/>.
/// </summary>
internal sealed class RewriteCsFilesStep : IMigrationStep
{
    /// <inheritdoc />
    public Task ExecuteAsync(MigrationContext context, Summary summary)
    {
        foreach (var path in MigrationFileOperations.EnumerateFiles(context.RootDirectory, "*.cs"))
        {
            MigrationFileOperations.ApplyRewrite(context, path, CodeRewriter.Rewrite, summary);
        }

        return Task.CompletedTask;
    }
}
