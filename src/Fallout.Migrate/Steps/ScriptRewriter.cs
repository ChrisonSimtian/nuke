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
