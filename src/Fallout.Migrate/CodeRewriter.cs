using System.Text.RegularExpressions;

namespace Fallout.Migrate;

internal static class CodeRewriter
{
    // Anchored prefix swap: `\bNuke\.` → `Fallout.`. Covers using directives,
    // attribute references, qualified type names, namespace declarations.
    // The trailing `(?=[A-Z])` lookahead avoids matching `Nuke.json` filenames
    // or other lowercase tails the prefix audit deliberately preserved.
    private static readonly Regex namespacePrefix =
        new(@"\bNuke\.(?=[A-Z])", RegexOptions.Compiled);

    // Bare type renames done in the Fallout rebrand (#59).
    private static readonly Regex nukeBuildType = new(@"\bNukeBuild\b", RegexOptions.Compiled);
    private static readonly Regex iNukeBuildType = new(@"\bINukeBuild\b", RegexOptions.Compiled);

    public static RewriteResult Rewrite(string original)
    {
        var edits = 0;

        var content = namespacePrefix.Replace(original, _ =>
        {
            edits++;
            return "Fallout.";
        });

        content = iNukeBuildType.Replace(content, _ =>
        {
            edits++;
            return "IFalloutBuild";
        });

        content = nukeBuildType.Replace(content, _ =>
        {
            edits++;
            return "FalloutBuild";
        });

        return new RewriteResult(content, edits);
    }
}
