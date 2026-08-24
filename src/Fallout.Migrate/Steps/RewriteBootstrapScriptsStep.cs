using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Fallout.Common.IO;
using Fallout.Migrate.Common;

namespace Fallout.Migrate.Steps;

/// <summary>
/// Rewrites bootstrap scripts (<c>build.cmd</c>/<c>build.ps1</c>/<c>build.sh</c>): <c>dotnet nuke</c>
/// invocations, <c>.nuke</c> path references, and legacy <c>NUKE_*</c> environment variables become
/// their Fallout equivalents.
/// </summary>
internal sealed class RewriteBootstrapScriptsStep : IMigrationStep
{
    /// <summary>The ordered find/replace patterns applied by <see cref="Rewrite"/>.</summary>
    private static readonly (Regex Pattern, string Replacement)[] patterns =
    [
        // Strip any telemetry opt-out line entirely — telemetry was removed from Fallout
        // (ADR-0010), so there is nothing to opt out of. Handles bash `export`, PowerShell
        // `$env:`, and cmd `set` spellings by matching the whole line. Runs before the
        // env-var renames below so the line is gone rather than renamed to a dead variable.
        (new Regex(@"^.*\b(?:NUKE|FALLOUT)_TELEMETRY_OPTOUT\b.*\r?\n?", RegexOptions.Compiled | RegexOptions.Multiline), ""),
        // `dotnet nuke` invocations
        (new Regex(@"\bdotnet\s+nuke\b", RegexOptions.Compiled), "dotnet fallout"),
        // .nuke directory references → .fallout
        (new Regex(@"(?<=[\\/.""'\s])\.nuke(?=[\\/""'\s])", RegexOptions.Compiled), ".fallout"),
        // Legacy env vars (consumer-facing ones from P3.5c)
        (new Regex(@"\bNUKE_GLOBAL_TOOL_VERSION\b", RegexOptions.Compiled), "FALLOUT_GLOBAL_TOOL_VERSION"),
        (new Regex(@"\bNUKE_GLOBAL_TOOL_START_TIME\b", RegexOptions.Compiled), "FALLOUT_GLOBAL_TOOL_START_TIME"),
        (new Regex(@"\bNUKE_INTERNAL_INTERCEPTOR\b", RegexOptions.Compiled), "FALLOUT_INTERNAL_INTERCEPTOR")
    ];

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
                MigrationFileOperations.ApplyRewrite(context, path, Rewrite, summary);
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Rewrites <paramref name="original"/> script content, applying every pattern in
    /// <see cref="patterns"/> in order.
    /// </summary>
    /// <param name="original">The original script file content.</param>
    /// <returns>The rewritten content and the number of edits made.</returns>
    private static RewriteResult Rewrite(string original)
    {
        var edits = 0;
        var content = original;
        foreach (var (pattern, replacement) in patterns)
        {
            content = pattern.Replace(content, _ =>
            {
                edits++;
                return replacement;
            });
        }

        return new RewriteResult(content, edits);
    }
}
