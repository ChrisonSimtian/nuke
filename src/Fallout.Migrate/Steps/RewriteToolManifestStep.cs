using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Fallout.Migrate.Common;

namespace Fallout.Migrate.Steps;

/// <summary>
/// Rewrites every <c>dotnet-tools.json</c> manifest under the repository root: a tool entry naming a
/// retired Fallout or NUKE tool package is renamed to <c>fallout.globaltools</c> and re-pinned to a
/// version that exists under that id.
/// </summary>
/// <remarks>
/// The published tool id changed twice — <c>nuke.globaltool</c> to <c>fallout.globaltool</c> to
/// <c>fallout.globaltools</c> — and the older ids were left on nuget.org with no successor pointer.
/// A manifest pinning one of them cannot reach 10.4 or later, because <c>rollForward</c> resolves
/// inside a single package id and cannot cross to a different one. See #575.
/// </remarks>
internal sealed class RewriteToolManifestStep : IMigrationStep
{
    /// <summary>The current published tool package id.</summary>
    private const string CurrentToolId = "fallout.globaltools";

    /// <summary>Tool ids this step migrates away from.</summary>
    private static readonly IReadOnlyList<string> retiredToolIds =
    [
        "nuke.globaltool",
        "fallout.globaltool",
        "fallout.cli"
    ];

    /// <summary>
    /// One tool entry in a manifest's <c>"tools"</c> object whose key is a retired id, captured
    /// together with its body so the version inside it can be re-pinned without touching the pins of
    /// any other tool in the same manifest. <c>[^{}]*</c> is enough for a tool entry: its properties
    /// are a string, a string array, and a boolean, so the body contains no nested object.
    /// Case-insensitive, because a hand-written manifest may use the package's display casing rather
    /// than the lowercase form <c>dotnet tool install</c> writes.
    /// </summary>
    private static readonly Regex retiredToolEntry = new(
        $@"""(?:{string.Join("|", retiredToolIds.Select(x => x.Replace(".", @"\.")))})""(?<between>\s*:\s*\{{)(?<body>[^{{}}]*)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>The <c>version</c> property inside a single tool entry body.</summary>
    private static readonly Regex versionProperty = new(
        @"(?<prefix>""version""\s*:\s*"")(?<version>[^""]*)(?="")",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <inheritdoc />
    public Task ExecuteAsync(MigrationContext context, Summary summary)
    {
        var editsBefore = summary.EditCount;

        foreach (var path in MigrationFileOperations.EnumerateFiles(context.RootDirectory, "dotnet-tools.json"))
        {
            MigrationFileOperations.ApplyRewrite(
                context,
                path,
                content => Rewrite(content, context.ToolVersion),
                summary);
        }

        if (summary.EditCount == editsBefore)
        {
            // No manifest pinned a retired tool id, so the resolved tool version doesn't matter here.
            return Task.CompletedTask;
        }

        if (context.ToolVersion == null)
        {
            summary.Warnings.Add(
                $"could not resolve a {CurrentToolId} version from NuGet; renamed the tool id in dotnet-tools.json but left its version unchanged — run `dotnet tool update {CurrentToolId}` to pin a real one");
        }
        else if (context.ToolVersion.Contains('-'))
        {
            summary.Warnings.Add(
                $"{CurrentToolId} has no stable release yet; pinned the prerelease {context.ToolVersion} in dotnet-tools.json");
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Rewrites <paramref name="original"/> manifest content, renaming retired tool ids to
    /// <see cref="CurrentToolId"/> and re-pinning each renamed entry's version to
    /// <paramref name="toolVersion"/>.
    /// </summary>
    /// <param name="original">The original <c>dotnet-tools.json</c> content.</param>
    /// <param name="toolVersion">
    /// The version to pin under the new id. When <c>null</c> the version is left untouched, so an
    /// offline run renames the id rather than writing a version it could not verify.
    /// </param>
    /// <returns>The rewritten content and the number of edits made.</returns>
    public static RewriteResult Rewrite(string original, string toolVersion)
    {
        var edits = 0;

        var content = retiredToolEntry.Replace(original, match =>
        {
            edits++;

            var body = match.Groups["body"].Value;
            if (toolVersion != null)
            {
                body = versionProperty.Replace(body, versionMatch =>
                {
                    if (versionMatch.Groups["version"].Value == toolVersion)
                    {
                        return versionMatch.Value;
                    }

                    edits++;
                    return versionMatch.Groups["prefix"].Value + toolVersion;
                });
            }

            return $"\"{CurrentToolId}\"" + match.Groups["between"].Value + body;
        });

        return new RewriteResult(content, edits);
    }
}
