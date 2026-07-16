using System.Threading.Tasks;

namespace Fallout.Migrate.Common;

/// <summary>
/// One unit of work performed against a repo during <c>fallout-migrate</c>. Add a new step by
/// implementing this interface and registering it in <see cref="Migration"/>'s step list.
/// </summary>
internal interface IMigrationStep
{
    /// <summary>
    /// Performs this step's work against <paramref name="context"/>, recording the outcome in
    /// <paramref name="summary"/>.
    /// </summary>
    /// <param name="context">The current migration context.</param>
    /// <param name="summary">The summary to update with files changed, edits made, or warnings.</param>
    Task ExecuteAsync(MigrationContext context, Summary summary);
}
