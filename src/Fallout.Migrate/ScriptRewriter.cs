using System.Text.RegularExpressions;

namespace Fallout.Migrate;

internal static class ScriptRewriter
{
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
