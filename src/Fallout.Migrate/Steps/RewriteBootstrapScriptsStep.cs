using System.Threading.Tasks;
using Fallout.Common.IO;
using Fallout.Migrate.Common;

namespace Fallout.Migrate.Steps;

/// <summary>
/// Rewrites <c>build.cmd</c>, <c>build.ps1</c>, and <c>build.sh</c> at the repository root, when
/// present, via <see cref="ScriptRewriter"/>.
/// </summary>
internal sealed class RewriteBootstrapScriptsStep : IMigrationStep
{
    /// <inheritdoc />
    public Task ExecuteAsync(MigrationContext context, Summary summary)
    {
        foreach (var name in new[]
                 {
                     "build.cmd",
                     "build.ps1",
                     "build.sh"
                 })
        {
            var path = context.RootDirectory / name;
            if (path.FileExists())
            {
                MigrationFileOperations.ApplyRewrite(context, path, ScriptRewriter.Rewrite, summary);
            }
        }

        return Task.CompletedTask;
    }
}
