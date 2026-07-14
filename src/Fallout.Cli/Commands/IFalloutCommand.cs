using System.Threading.Tasks;
using Fallout.Common.IO;

namespace Fallout.Cli.Commands;

/// <summary>
/// A single global-tool command (e.g. <c>fallout :run</c>, <c>fallout :setup</c>).
/// One command = one type, resolved by <see cref="Name"/> from the dependency-injection
/// container — replacing the historical reflection-over-<c>Program</c> dispatch.
/// </summary>
/// <remarks>
/// This surface is intentionally minimal. It is <b>not</b> a stable public plugin contract yet;
/// when a public command SDK lands (milestone #7) the API will be annotated and versioned
/// explicitly. Until then, treat additions here as internal-by-convention.
/// </remarks>
internal interface IFalloutCommand
{
    /// <summary>
    /// The command name as typed after the <c>:</c> prefix (prefer dash form, e.g. <c>"add-package"</c>).\
    /// Matched case-insensitively and with dashes ignored (so <c>:addpackage</c> also matches).\
    /// Legacy commands may still use PascalCase names (e.g. <c>"GetNextDirectory"</c>).
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Executes the command and returns the process exit code.
    /// </summary>
    /// <param name="args">The arguments following the command token.</param>
    /// <param name="rootDirectory">The resolved repository root, or <c>null</c> when none was found.</param>
    /// <param name="buildScript">The resolved build script / project file, or <c>null</c> when none applies.</param>
    Task<int> ExecuteAsync(string[] args, AbsolutePath rootDirectory, AbsolutePath buildScript);
}
