using System.Collections.Generic;
using System.Linq;
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
    // We don't mach MSBuild variables (`$(...)`) here, because they are handled below
    private static readonly Regex nukePackageWithInlineVersionPattern = new(
        @"(?<prefix><PackageReference\s+Include="")Nuke\.(?<name>[A-Z][A-Za-z0-9.]+)(?<between>""[^>]*?\s+Version="")(?!\$\()[^""]+",
        RegexOptions.Compiled);

    // PackageReference / ProjectReference `Include="Nuke.X"` → `Include="Fallout.X"` — namespace
    // only. Catches references that DON'T have an inline Version (central package management).
    // Must run AFTER NukePackageWithInlineVersionPattern so it only touches what's left.
    private static readonly Regex packageReferencePattern =
        new(@"(?<=\b(?:Include|Update|Remove)="")Nuke\.(?=[A-Z])", RegexOptions.Compiled);

    // Detects MSBuild variables used by already-rewritten Fallout.* PackageReferences:
    // Version="$(MyVar)". Scoped to Fallout.* so variables shared with unrelated packages
    // aren't mistaken for Fallout version variables.
    private static readonly Regex falloutPackageReferenceVariablePattern = new(
        @"<PackageReference\s+Include=""Fallout\.[^""]+""[^>]*?Version=""\$\((?<variable>[^)]+)\)""",
        RegexOptions.Compiled);

    // Same variable-usage detection, but for PackageReferences that are NOT Fallout.* — used to
    // detect a variable ambiguously shared between a Fallout package and an unrelated one.
    private static readonly Regex nonFalloutPackageReferenceVariablePattern = new(
        @"<PackageReference\s+Include=""(?!Fallout\.)[^""]+""[^>]*?Version=""\$\((?<variable>[^)]+)\)""",
        RegexOptions.Compiled);

    // MSBuild element/property names that begin with `Nuke` followed by an uppercase
    // letter (e.g. <NukeRootDirectory>...). Limited to known consumer-facing names from
    // P3.5b so we don't rewrite unrelated user-defined identifiers that happen to start
    // with the literal "Nuke".
    private static readonly Regex msBuildPropertyPattern = new(
        @"\bNuke(?=" +
        "(?:Version|RootDirectory|ScriptDirectory|BaseDirectory|BaseNamespace|" +
        "UseNestedNamespaces|RepositoryUrl|UpdateReferences|ContinueOnError|TaskTimeout|" +
        "Timeout|TasksEnabled|DefaultExcludes|ExcludeBoot|ExcludeConfig|ExcludeLogs|" +
        "ExcludeDirectoryBuild|ExcludeCi|SpecificationFiles|ExternalFiles|TasksAssembly|" +
        "TasksDirectory)\\b)",
        RegexOptions.Compiled);

    // Strip the telemetry-version property entirely — telemetry was removed from Fallout
    // (ADR-0010), so a migrated project must not carry a dead <FalloutTelemetryVersion>.
    // Matches the whole element line (either legacy Nuke* or already-Fallout* spelling).
    private static readonly Regex telemetryVersionPropertyPattern = new(
        @"^[ \t]*<(?<tag>(?:Nuke|Fallout)TelemetryVersion)>.*?</\k<tag>>[ \t]*\r?\n?",
        RegexOptions.Compiled | RegexOptions.Multiline);

    // Strip explicit `System.Security.Cryptography.Xml` PackageReferences. NUKE-era projects
    // often pinned this directly at an older major (e.g. 9.x). Fallout.Common 10.2.12+ transitively
    // requires a newer version (10.0.6+) and the conflict trips NU1605 ("Detected package
    // downgrade"). Removing the explicit pin lets the transitive version win, which is what the
    // migrated project wants (#217). Matches a self-closing element with optional surrounding
    // indentation + trailing newline.
    private static readonly Regex cryptographyXmlPackageRefPattern = new(
        @"^[ \t]*<PackageReference\s+Include=""System\.Security\.Cryptography\.Xml""[^/]*/>[ \t]*\r?\n?",
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
    private static RewriteResult Rewrite(string original, string falloutVersion)
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

        // Pass 3 — strip the telemetry-version property (feature removed in ADR-0010).
        content = telemetryVersionPropertyPattern.Replace(content, _ =>
        {
            edits++;
            return string.Empty;
        });

        // Pass 4 — strip the stale System.Security.Cryptography.Xml direct pin.
        content = cryptographyXmlPackageRefPattern.Replace(content, _ =>
        {
            edits++;
            return string.Empty;
        });

        return HandleMsBuildVariable(falloutVersion, content, edits);
    }

    // Pass 5 — extract variables used by Fallout.* PackageReferences, decouple the ones ambiguously
    // shared with non-Fallout packages via a dedicated $(FalloutVersion) property, and bump every
    // variable that's now exclusively Fallout's to the current Fallout version.
    private static RewriteResult HandleMsBuildVariable(string falloutVersion, string content, int edits)
    {
        const string falloutVersionVariable = "FalloutVersion";

        var (variablesToBump, ambiguousVariables) =
            ClassifyPackageReferenceVariables(content, falloutVersionVariable);

        (content, int redirectEdits) = RedirectAmbiguousVariablesToFalloutVersion(content, ambiguousVariables, falloutVersionVariable);
        edits += redirectEdits;

        content = EnsureFalloutVersionPropertyExists(content, ambiguousVariables, falloutVersionVariable, falloutVersion, ref edits);

        (content, int bumpEdits) = BumpVariableProperties(content, variablesToBump, falloutVersion);
        edits += bumpEdits;

        return new RewriteResult(content, edits);
    }

    // A variable also shared with a non-Fallout package is ambiguous: bumping it directly would
    // change that unrelated package's version too, so it's decoupled instead — the Fallout
    // reference is redirected to a dedicated $(FalloutVersion) property.
    private static (HashSet<string> variablesToBump, HashSet<string> ambiguousVariables) ClassifyPackageReferenceVariables(
        string content, string falloutVersionVariable)
    {
        var nonFalloutVariables = nonFalloutPackageReferenceVariablePattern.Matches(content)
            .Select(m => m.Groups["variable"].Value)
            .ToHashSet();

        var variablesToBump = new HashSet<string> { falloutVersionVariable };
        var ambiguousVariables = new HashSet<string>();

        foreach (var variable in falloutPackageReferenceVariablePattern.Matches(content)
                     .Select(m => m.Groups["variable"].Value))
        {
            (nonFalloutVariables.Contains(variable) ? ambiguousVariables : variablesToBump).Add(variable);
        }

        return (variablesToBump, ambiguousVariables);
    }

    private static (string content, int edits) RedirectAmbiguousVariablesToFalloutVersion(
        string content, HashSet<string> ambiguousVariables, string falloutVersionVariable)
    {
        var edits = 0;

        foreach (var variable in ambiguousVariables)
        {
            // Matches `Version="$(variable)"` on a Fallout.* PackageReference only, capturing
            // everything up to and including the opening `Version="` so it can be re-emitted
            // unchanged while just swapping the variable reference.
            var redirectPattern = new Regex(
                $@"(<PackageReference\s+Include=""Fallout\.[^""]+""[^>]*?Version="")\$\({Regex.Escape(variable)}\)");
            content = redirectPattern.Replace(content, m =>
            {
                edits++;
                return m.Groups[1].Value + $"$({falloutVersionVariable})";
            });
        }

        return (content, edits);
    }

    private static string EnsureFalloutVersionPropertyExists(
        string content, HashSet<string> ambiguousVariables, string falloutVersionVariable, string falloutVersion, ref int edits)
    {
        if (ambiguousVariables.Count == 0 || content.Contains($"<{falloutVersionVariable}>"))
        {
            return content;
        }

        var propertyGroupIndex = content.IndexOf("<PropertyGroup>", System.StringComparison.Ordinal);
        if (propertyGroupIndex >= 0)
        {
            edits++;
            return content.Insert(
                propertyGroupIndex + "<PropertyGroup>".Length,
                $"\n    <{falloutVersionVariable}>{falloutVersion}</{falloutVersionVariable}>");
        }

        // No PropertyGroup exists at all (e.g. a project relying solely on Directory.Build.props
        // for properties) — synthesize one right after the opening <Project> tag so the newly
        // introduced $(FalloutVersion) reference has somewhere to resolve from.
        var projectTagStart = content.IndexOf("<Project", System.StringComparison.Ordinal);
        var projectTagEnd = projectTagStart >= 0
            ? content.IndexOf('>', projectTagStart)
            : -1;
        if (projectTagEnd < 0)
        {
            return content;
        }

        edits++;
        return content.Insert(
            projectTagEnd + 1,
            $"\n  <PropertyGroup>\n    <{falloutVersionVariable}>{falloutVersion}</{falloutVersionVariable}>\n  </PropertyGroup>");
    }

    private static (string content, int edits) BumpVariableProperties(string content, HashSet<string> variablesToBump, string falloutVersion)
    {
        var edits = 0;

        foreach (var variable in variablesToBump)
        {
            // Matches the text content of the <variable>...</variable> property element itself
            // (via lookbehind/lookahead, so the tags aren't part of the match and stay intact).
            var pattern = $@"(?<=<{variable}\s*>)[^<]+(?=</{variable}\s*>)";
            content = Regex.Replace(content,
                pattern,
                m =>
                {
                    if (m.Value == falloutVersion)
                    {
                        return m.Value;
                    }

                    edits++;
                    return falloutVersion;
                });
        }

        return (content, edits);
    }
}
