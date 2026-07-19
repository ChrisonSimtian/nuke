using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Fallout.Migrate.Common;

namespace Fallout.Migrate.Steps;

/// <summary>
/// Rewrites every <c>*.csproj</c> file under the repository root: <c>Nuke.*</c> package/project
/// references become <c>Fallout.*</c> (pinning the current Fallout version where an inline
/// <c>Version</c> attribute was present), <c>Nuke*</c> MSBuild properties are renamed to
/// <c>Fallout*</c>, and stale explicit <c>System.Security.Cryptography.Xml</c> pins are stripped.
/// </summary>
internal sealed class RewriteCsprojsStep : IMigrationStep
{
    // Combined rewrite: Nuke.X PackageReference WITH an inline Version attribute → Fallout.X
    // at the current Fallout version. NUKE-era pins (e.g. `Version="10.1.0"`) don't exist as
    // Fallout.* packages and produce NU1603 ("not found, falling back to next-higher") which
    // `WarningsAsErrors` in the migrated project escalates. Bumping in the same pass avoids
    // a broken post-migrate build (#217). Tolerates extra attributes between Include and Version
    // (e.g. `PrivateAssets="all"`).
    private static readonly Regex nukePackageWithInlineVersionPattern = new(
        @"(?<prefix><PackageReference\s+Include="")Nuke\.(?<name>[A-Z][A-Za-z0-9.]+)(?<between>""[^>]*?\s+Version="")[^""]+",
        RegexOptions.Compiled);

    // PackageReference / ProjectReference `Include="Nuke.X"` → `Include="Fallout.X"` — namespace
    // only. Catches references that DON'T have an inline Version (central package management).
    // Must run AFTER NukePackageWithInlineVersionPattern so it only touches what's left.
    private static readonly Regex packageReferencePattern =
        new(@"(?<=\b(?:Include|Update|Remove)="")Nuke\.(?=[A-Z])", RegexOptions.Compiled);

    // MSBuild element/property names that begin with `Nuke` followed by an uppercase
    // letter (e.g. <NukeRootDirectory>...). Limited to known consumer-facing names from
    // P3.5b so we don't rewrite unrelated user-defined identifiers that happen to start
    // with the literal "Nuke".
    private static readonly Regex msBuildPropertyPattern = new(
        @"\bNuke(?=" +
        "(?:RootDirectory|ScriptDirectory|TelemetryVersion|BaseDirectory|BaseNamespace|" +
        "UseNestedNamespaces|RepositoryUrl|UpdateReferences|ContinueOnError|TaskTimeout|" +
        "Timeout|TasksEnabled|DefaultExcludes|ExcludeBoot|ExcludeConfig|ExcludeLogs|" +
        "ExcludeDirectoryBuild|ExcludeCi|SpecificationFiles|ExternalFiles|TasksAssembly|" +
        "TasksDirectory)\\b)",
        RegexOptions.Compiled);

    // Strip explicit `System.Security.Cryptography.Xml` PackageReferences. NUKE-era projects
    // often pinned this directly at an older major (e.g. 9.x). Fallout.Common 10.2.12+ transitively
    // requires a newer version (10.0.6+) and the conflict trips NU1605 ("Detected package
    // downgrade"). Removing the explicit pin lets the transitive version win, which is what the
    // migrated project wants (#217). Matches a self-closing element with optional surrounding
    // indentation + trailing newline.
    private static readonly Regex cryptographyXmlPackageRefPattern = new(
        @"^[ \t]*<PackageReference\s+Include=""System\.Security\.Cryptography\.Xml""[^/]*/>\s*\r?\n?",
        RegexOptions.Compiled | RegexOptions.Multiline);

    /// <inheritdoc />
    public Task ExecuteAsync(MigrationContext context, Summary summary)
    {
        foreach (var path in MigrationFileOperations.EnumerateFiles(context.RootDirectory, "*.csproj"))
        {
            MigrationFileOperations.ApplyRewrite(
                context,
                path,
                content => Rewrite(content, context.FalloutVersion),
                summary);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Rewrites <paramref name="original"/> content, replacing <c>Nuke.*</c> references and MSBuild
    /// properties with their <c>Fallout.*</c> equivalents and stripping stale pins.
    /// </summary>
    /// <param name="original">The original <c>.csproj</c> file content.</param>
    /// <param name="falloutVersion">The Fallout version to pin into rewritten inline-versioned references.</param>
    /// <returns>The rewritten content and the number of edits made.</returns>
    public static RewriteResult Rewrite(string original, string falloutVersion)
    {
        var edits = 0;
        var content = original;

        // Pass 1 — combined Include + Version rewrite for Nuke.X PackageReferences with inline Version.
        content = nukePackageWithInlineVersionPattern.Replace(content, m =>
        {
            edits++;
            return m.Groups["prefix"].Value
                   + "Fallout." + m.Groups["name"].Value
                   + m.Groups["between"].Value
                   + falloutVersion;
        });

        // Pass 2 — namespace-only rewrites for anything Pass 1 didn't consume (CPM-managed
        // PackageReferences without inline Version, ProjectReferences, MSBuild properties).
        content = packageReferencePattern.Replace(content, _ =>
        {
            edits++;
            return "Fallout.";
        });

        content = msBuildPropertyPattern.Replace(content, _ =>
        {
            edits++;
            return "Fallout";
        });

        // Pass 3 — strip the stale System.Security.Cryptography.Xml direct pin.
        content = cryptographyXmlPackageRefPattern.Replace(content, _ =>
        {
            edits++;
            return string.Empty;
        });

        return new RewriteResult(content, edits);
    }
}
