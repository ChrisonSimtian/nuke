using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Fallout.Common;
using Fallout.Common.IO;
using Fallout.NuGet.Analysis;

namespace Fallout.Cli.Commands;

/// <summary>
/// <c>fallout :analyze packages</c>: reads the post-restore <c>project.assets.json</c> graph and
/// reports redundant and conflicting NuGet package references. <c>:analyze</c> is a command
/// namespace so future analyzers slot in alongside.
/// </summary>
internal sealed class AnalyzeCommand : IFalloutCommand
{
    public string Name => "analyze";

    public Task<int> ExecuteAsync(string[] args, AbsolutePath rootDirectory, AbsolutePath buildScript)
        => Task.FromResult(Execute(args));

    private static int Execute(string[] args)
    {
        var subCommand = args.ElementAtOrDefault(0);
        if (!string.Equals(subCommand, "packages", StringComparison.OrdinalIgnoreCase))
        {
            Host.Error("Usage: fallout :analyze packages [<path>] [--tfm <moniker>] [--severity none|trace|normal|warning|error] [--exclude <id>[,<id>...]]");
            return 1;
        }

        if (!TryParseAnalyzeArguments(args.Skip(1).ToArray(), out var path, out var tfm, out var severity, out var excludes))
            return 1;

        var projectFiles = ResolveProjectFiles(path);
        if (projectFiles == null)
            return 1;

        var analyzed = new List<AnalyzedProject>();
        var restoreMissing = 0;
        foreach (var projectFile in projectFiles)
        {
            var assetsFile = ProjectAssetsReader.FindAssetsFile(projectFile);
            if (assetsFile == null)
            {
                restoreMissing++;
                Host.Verbose($"Skipping {Path.GetFileName(projectFile)} — no obj/project.assets.json (run 'dotnet restore').");
                continue;
            }

            try
            {
                analyzed.AddRange(ProjectAssetsReader.Read(assetsFile));
            }
            catch (Exception exception)
            {
                Host.Warning($"Could not read assets for {Path.GetFileName(projectFile)}: {exception.Message}");
            }
        }

        if (analyzed.Count == 0)
        {
            Host.Warning(restoreMissing > 0
                ? $"No restored projects found ({restoreMissing} project(s) need 'dotnet restore')."
                : "No analyzable projects found.");
            return 0;
        }

        var options = new AnalyzerOptions { TargetFramework = tfm };
        foreach (var exclude in excludes)
            options.ExcludedPackageIds.Add(exclude);

        var findings = new PackageAnalyzer().Analyze(analyzed, options);

        Report(findings, severity, analyzed.Count, restoreMissing);

        var failing = severity == LogLevel.Error && findings.Count > 0;
        return failing ? 1 : 0;
    }

    private static bool TryParseAnalyzeArguments(
        string[] args,
        out string path,
        out string tfm,
        out LogLevel severity,
        out HashSet<string> excludes)
    {
        path = null;
        tfm = null;
        severity = LogLevel.Warning;
        excludes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            switch (arg.ToLowerInvariant())
            {
                case "--tfm":
                    if (++i >= args.Length) { Host.Error("--tfm requires a value."); return false; }
                    tfm = args[i];
                    break;
                case "--severity":
                    if (++i >= args.Length) { Host.Error("--severity requires a value."); return false; }
                    if (!TryParseSeverity(args[i], out severity)) { Host.Error($"Unknown severity '{args[i]}'."); return false; }
                    break;
                case "--exclude":
                    if (++i >= args.Length) { Host.Error("--exclude requires a value."); return false; }
                    foreach (var id in args[i].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                        excludes.Add(id);
                    break;
                default:
                    if (arg.StartsWith("--", StringComparison.Ordinal))
                    {
                        Host.Error($"Unknown option '{arg}'.");
                        return false;
                    }

                    path = arg;
                    break;
            }
        }

        return true;
    }

    private static bool TryParseSeverity(string value, out LogLevel severity)
    {
        switch (value.ToLowerInvariant())
        {
            case "none": severity = (LogLevel)(-1); return true;
            case "trace": severity = LogLevel.Trace; return true;
            case "normal": case "info": case "information": severity = LogLevel.Normal; return true;
            case "warning": case "warn": severity = LogLevel.Warning; return true;
            case "error": severity = LogLevel.Error; return true;
            default: severity = LogLevel.Warning; return false;
        }
    }

    private static List<string> ResolveProjectFiles(string path)
    {
        if (string.IsNullOrEmpty(path))
            return GlobProjects(Directory.GetCurrentDirectory());

        var full = Path.GetFullPath(path);

        if (File.Exists(full) && full.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            return new List<string> { full };

        if (Directory.Exists(full))
            return GlobProjects(full);

        // .sln / .slnx (or any file): analyze the projects under its directory.
        if (File.Exists(full))
            return GlobProjects(Path.GetDirectoryName(full));

        Host.Error($"Path not found: {path}");
        return null;
    }

    private static List<string> GlobProjects(string directory)
    {
        return Directory.GetFiles(directory, "*.csproj", SearchOption.AllDirectories)
            .Where(x => !ContainsSegment(x, "obj") && !ContainsSegment(x, "bin"))
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool ContainsSegment(string filePath, string segment)
    {
        return filePath.Contains($"{Path.DirectorySeparatorChar}{segment}{Path.DirectorySeparatorChar}") ||
               filePath.Contains($"{Path.AltDirectorySeparatorChar}{segment}{Path.AltDirectorySeparatorChar}");
    }

    private static void Report(IReadOnlyList<Finding> findings, LogLevel severity, int projectCount, int restoreMissing)
    {
        void Emit(string text)
        {
            switch (severity)
            {
                case LogLevel.Trace: Host.Verbose(text); break;
                case LogLevel.Normal: Host.Information(text); break;
                case LogLevel.Warning: Host.Warning(text); break;
                case LogLevel.Error: Host.Error(text); break;
                default: break; // none — summary only
            }
        }

        var redundant = findings.Where(x => x.Kind != FindingKind.VersionConflict).ToList();
        var conflicts = findings.Where(x => x.Kind == FindingKind.VersionConflict).ToList();

        if (findings.Count == 0)
        {
            Host.Information($"No redundant or conflicting package references found across {projectCount} project/framework target(s).");
            if (restoreMissing > 0)
                Host.Information($"({restoreMissing} project(s) were skipped — not restored.)");
            return;
        }

        foreach (var finding in redundant.OrderBy(x => x.Project).ThenBy(x => x.PackageId))
        {
            var marker = finding.SafeToRemove ? "can be removed" : "might be removed";
            Emit($"[{finding.Project} ({finding.TargetFramework})] {finding.PackageId} {finding.ResolvedVersion} — {marker}. {finding.Detail}");
        }

        foreach (var finding in conflicts.OrderBy(x => x.PackageId))
            Emit($"[version conflict] {finding.Detail}");

        Host.Information(
            $"Summary: {redundant.Count} redundant reference(s), {conflicts.Count} version conflict(s) across {projectCount} target(s).");
        if (restoreMissing > 0)
            Host.Information($"({restoreMissing} project(s) skipped — not restored.)");
    }
}
