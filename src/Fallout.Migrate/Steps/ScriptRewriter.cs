using System.Text.RegularExpressions;
using Fallout.Migrate.Common;

namespace Fallout.Migrate.Steps;

/// <summary>
/// Rewrites bootstrap scripts (<c>build.cmd</c>/<c>build.ps1</c>/<c>build.sh</c>): <c>dotnet nuke</c>
/// invocations, <c>.nuke</c> path references, and legacy <c>NUKE_*</c> environment variables become
/// their Fallout equivalents. Driven by <see cref="RewriteBootstrapScriptsStep"/>.
/// </summary>
internal static class ScriptRewriter
{
    /// <summary>The ordered find/replace patterns applied by <see cref="Rewrite"/>.</summary>
    private static readonly (Regex Pattern, string Replacement)[] patterns =
    {
        // `dotnet nuke` invocations
        (new Regex(@"\bdotnet\s+nuke\b", RegexOptions.Compiled), "dotnet fallout"),
        // .nuke directory references → .fallout
        (new Regex(@"(?<=[\\/.""'\s])\.nuke(?=[\\/""'\s])", RegexOptions.Compiled), ".fallout"),
        // Legacy env vars (consumer-facing ones from P3.5c)
        (new Regex(@"\bNUKE_TELEMETRY_OPTOUT\b", RegexOptions.Compiled), "FALLOUT_TELEMETRY_OPTOUT"),
        (new Regex(@"\bNUKE_GLOBAL_TOOL_VERSION\b", RegexOptions.Compiled), "FALLOUT_GLOBAL_TOOL_VERSION"),
        (new Regex(@"\bNUKE_GLOBAL_TOOL_START_TIME\b", RegexOptions.Compiled), "FALLOUT_GLOBAL_TOOL_START_TIME"),
        (new Regex(@"\bNUKE_INTERNAL_INTERCEPTOR\b", RegexOptions.Compiled), "FALLOUT_INTERNAL_INTERCEPTOR"),
    };

    /// <summary>
    /// Rewrites <paramref name="original"/> script content, applying every pattern in
    /// <see cref="patterns"/> in order.
    /// </summary>
    /// <param name="original">The original script file content.</param>
    /// <returns>The rewritten content and the number of edits made.</returns>
    public static RewriteResult Rewrite(string original)
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
