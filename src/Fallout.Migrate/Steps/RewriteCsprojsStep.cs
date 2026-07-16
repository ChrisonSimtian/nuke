using System.Threading.Tasks;
using Fallout.Migrate.Common;

namespace Fallout.Migrate.Steps;

/// <summary>
/// Rewrites every <c>*.csproj</c> file under the repository root via <see cref="CsprojRewriter"/>.
/// </summary>
internal sealed class RewriteCsprojsStep : IMigrationStep
{
    /// <inheritdoc />
    public Task ExecuteAsync(MigrationContext context, Summary summary)
    {
        foreach (var path in MigrationFileOperations.EnumerateFiles(context.RootDirectory, "*.csproj"))
        {
            MigrationFileOperations.ApplyRewrite(
                context,
                path,
                content => CsprojRewriter.Rewrite(content, context.FalloutVersion),
                summary);
        }

        return Task.CompletedTask;
    }
}
