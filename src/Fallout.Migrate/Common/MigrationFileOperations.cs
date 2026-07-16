using System;
using System.Collections.Generic;
using System.IO;
using Fallout.Common.IO;

namespace Fallout.Migrate.Common;

/// <summary>
/// Shared file-walking and rewrite-application helpers used by multiple <see cref="IMigrationStep"/>
/// implementations. Kept as static, dependency-free functions so steps stay small and independent.
/// </summary>
internal static class MigrationFileOperations
{
    /// <summary>
    /// Recursively enumerates files under <paramref name="rootDirectory"/> matching
    /// <paramref name="pattern"/>, skipping <c>bin/</c>, <c>obj/</c>, and <c>.git/</c>.
    /// </summary>
    /// <param name="rootDirectory">The directory to search from.</param>
    /// <param name="pattern">A file-name glob pattern, e.g. <c>*.csproj</c>.</param>
    /// <returns>The matching, non-ignored files.</returns>
    public static IEnumerable<AbsolutePath> EnumerateFiles(AbsolutePath rootDirectory, string pattern)
    {
        foreach (var file in rootDirectory.GetFiles(pattern, depth: int.MaxValue))
        {
            if (IsIgnored(file))
            {
                continue;
            }

            yield return file;
        }
    }

    /// <summary>
    /// Reads <paramref name="path"/>, applies <paramref name="rewriter"/>, logs and records the edit,
    /// and writes the result back unless <see cref="MigrationContext.DryRun"/> is set.
    /// </summary>
    /// <param name="context">The current migration context.</param>
    /// <param name="path">The file to rewrite.</param>
    /// <param name="rewriter">The rewrite function to apply to the file's content.</param>
    /// <param name="summary">The summary to update with the outcome.</param>
    public static void ApplyRewrite(
        MigrationContext context,
        AbsolutePath path,
        Func<string, RewriteResult> rewriter,
        Summary summary)
    {
        string original;
        try
        {
            original = path.ReadAllText();
        }
        catch (IOException ex)
        {
            summary.Warnings.Add($"could not read {RelativePath(context.RootDirectory, path)}: {ex.Message}");
            return;
        }

        var result = rewriter(original);
        if (result.EditCount == 0)
        {
            return;
        }

        context.Log.WriteLine(
            $"edit    {RelativePath(context.RootDirectory, path)}  ({result.EditCount} change{(result.EditCount == 1 ? "" : "s")})");

        summary.FilesChanged++;
        summary.EditCount += result.EditCount;

        if (!context.DryRun)
        {
            // eofLineBreak: false preserves today's exact byte-for-byte write behavior;
            // AbsolutePath.WriteAllText's default normalizes trailing line endings, which
            // would otherwise sneak unrelated diff noise into migrated consumer repos.
            path.WriteAllText(result.Content, eofLineBreak: false);
        }
    }

    /// <summary>
    /// Formats <paramref name="absolute"/> as a path relative to <paramref name="rootDirectory"/>,
    /// using forward slashes, for log output.
    /// </summary>
    /// <param name="rootDirectory">The directory to make the path relative to.</param>
    /// <param name="absolute">The path to format.</param>
    public static string RelativePath(AbsolutePath rootDirectory, AbsolutePath absolute) =>
        rootDirectory.GetUnixRelativePathTo(absolute);

    /// <summary>
    /// Returns <c>true</c> if <paramref name="path"/> sits under a <c>bin/</c>, <c>obj/</c>, or
    /// <c>.git/</c> directory and should be skipped by <see cref="EnumerateFiles"/>.
    /// </summary>
    private static bool IsIgnored(AbsolutePath path)
    {
        string text = path;
        return text.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
               || text.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
               || text.Contains($"{Path.DirectorySeparatorChar}.git{Path.DirectorySeparatorChar}", StringComparison.Ordinal);
    }
}
