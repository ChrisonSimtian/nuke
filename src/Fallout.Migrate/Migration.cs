using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Fallout.Common.IO;

namespace Fallout.Migrate;

internal sealed class Migration
{
    private readonly AbsolutePath rootDirectory;
    private readonly bool dryRun;
    private readonly TextWriter log;

    public Migration(AbsolutePath rootDirectory, bool dryRun, TextWriter log)
    {
        this.rootDirectory = rootDirectory ?? throw new ArgumentNullException(nameof(rootDirectory));
        this.dryRun = dryRun;
        this.log = log ?? throw new ArgumentNullException(nameof(log));
    }

    public Summary Run()
    {
        var summary = new Summary();

        RewriteCsprojs(summary);
        RewriteCsFiles(summary);
        RewriteBootstrapScripts(summary);
        RenameNukeDirectory(summary);

        return summary;
    }

    private void RewriteCsprojs(Summary summary)
    {
        var falloutVersion = ResolveFalloutVersion();
        foreach (var path in EnumerateFiles("*.csproj"))
        {
            ApplyRewrite(path, content => CsprojRewriter.Rewrite(content, falloutVersion), summary);
        }
    }

    // Pinned into migrated `<PackageReference Include="Fallout.X" Version="..." />` lines.
    // Uses the running migrate tool's own SemVer (Nerdbank.GitVersioning, set on
    // AssemblyInformationalVersion) so the migration output aligns with the tool the user
    // just installed. For dev/local builds without a `+` in InformationalVersion (i.e. no
    // build-metadata suffix), falls back to a known-published floor so we never emit a
    // bogus pin like Version="LOCAL". Inlined to keep Fallout.Migrate dependency-free.
    private static string ResolveFalloutVersion()
    {
        const string fallback = "10.3.49";

        var informational = typeof(Migration).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;

        if (string.IsNullOrEmpty(informational))
        {
            return fallback;
        }

        int plusIndex = informational.IndexOf('+');
        if (plusIndex == -1)
        {
            return fallback;
        }

        return informational[..plusIndex];
    }

    private void RewriteCsFiles(Summary summary)
    {
        foreach (var path in EnumerateFiles("*.cs"))
        {
            ApplyRewrite(path, CodeRewriter.Rewrite, summary);
        }
    }

    private void RewriteBootstrapScripts(Summary summary)
    {
        foreach (var name in new[]
                 {
                     "build.cmd",
                     "build.ps1",
                     "build.sh"
                 })
        {
            var path = rootDirectory / name;
            if (path.FileExists())
            {
                ApplyRewrite(path, ScriptRewriter.Rewrite, summary);
            }
        }
    }

    private void RenameNukeDirectory(Summary summary)
    {
        var legacy = rootDirectory / ".nuke";
        var canonical = rootDirectory / ".fallout";

        if (!legacy.DirectoryExists())
        {
            return;
        }

        if (canonical.DirectoryExists())
        {
            summary.Warnings.Add(
                "Both .nuke/ and .fallout/ exist. Skipped rename; merge their contents manually.");

            return;
        }

        Log($"rename {RelativePath(legacy)} -> {RelativePath(canonical)}");
        if (!dryRun)
        {
            // We purposely use the .NET directory move as this is atomic. Fallout's own
            // Move/MoveDirectory moves them one by one.
            Directory.Move(legacy, canonical);
        }

        summary.DirectoriesRenamed++;
    }

    private IEnumerable<AbsolutePath> EnumerateFiles(string pattern)
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

    private static bool IsIgnored(AbsolutePath path)
    {
        string text = path;
        return text.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
               || text.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
               || text.Contains($"{Path.DirectorySeparatorChar}.git{Path.DirectorySeparatorChar}", StringComparison.Ordinal);
    }

    private void ApplyRewrite(AbsolutePath path, Func<string, RewriteResult> rewriter, Summary summary)
    {
        string original;
        try
        {
            original = path.ReadAllText();
        }
        catch (IOException ex)
        {
            summary.Warnings.Add($"could not read {RelativePath(path)}: {ex.Message}");
            return;
        }

        var result = rewriter(original);
        if (result.EditCount == 0)
        {
            return;
        }

        Log($"edit    {RelativePath(path)}  ({result.EditCount} change{(result.EditCount == 1 ? "" : "s")})");
        summary.FilesChanged++;
        summary.EditCount += result.EditCount;

        if (!dryRun)
        {
            // eofLineBreak: false preserves today's exact byte-for-byte write behavior;
            // AbsolutePath.WriteAllText's default normalizes trailing line endings, which
            // would otherwise sneak unrelated diff noise into migrated consumer repos.
            path.WriteAllText(result.Content, eofLineBreak: false);
        }
    }

    private string RelativePath(AbsolutePath absolute) =>
        rootDirectory.GetUnixRelativePathTo(absolute);

    private void Log(string line) => log.WriteLine(line);

    internal sealed class Summary
    {
        public int FilesChanged { get; set; }

        public int EditCount { get; set; }

        public int DirectoriesRenamed { get; set; }

        public List<string> Warnings { get; } = new();
    }
}

internal readonly record struct RewriteResult(string Content, int EditCount);
