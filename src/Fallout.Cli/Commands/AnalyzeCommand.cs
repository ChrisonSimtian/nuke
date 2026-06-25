using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Fallout.Common;
using Fallout.Common.IO;
using Fallout.NuGet.Analysis;
using Fallout.Solutions;
using Spectre.Console;

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
            Host.Error("Usage: fallout :analyze packages [<path>] [--tfm <moniker>] [--severity none|trace|normal|warning|error] [--format table|flat] [--exclude <id>[,<id>...]]");
            return 1;
        }

        if (!TryParseAnalyzeArguments(args.Skip(1).ToArray(), out var path, out var tfm, out var severity, out var format, out var excludes))
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

        if (format == OutputFormat.Table)
            RenderTables(findings, analyzed.Count, restoreMissing);
        else
            RenderFlat(findings, severity, analyzed.Count, restoreMissing);

        var failing = severity == LogLevel.Error && findings.Count > 0;
        return failing ? 1 : 0;
    }

    private enum OutputFormat
    {
        Table,
        Flat,
    }

    private static bool TryParseAnalyzeArguments(
        string[] args,
        out string path,
        out string tfm,
        out LogLevel severity,
        out OutputFormat format,
        out HashSet<string> excludes)
    {
        path = null;
        tfm = null;
        severity = LogLevel.Warning;
        format = OutputFormat.Table;
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
                case "--format":
                    if (++i >= args.Length) { Host.Error("--format requires a value."); return false; }
                    if (!TryParseFormat(args[i], out format)) { Host.Error($"Unknown format '{args[i]}' (use table|flat)."); return false; }
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

    private static bool TryParseFormat(string value, out OutputFormat format)
    {
        switch (value.ToLowerInvariant())
        {
            case "table": format = OutputFormat.Table; return true;
            case "flat": case "lines": format = OutputFormat.Flat; return true;
            default: format = OutputFormat.Table; return false;
        }
    }

    private static List<string> ResolveProjectFiles(string path)
    {
        if (string.IsNullOrEmpty(path))
            return GlobProjects(Directory.GetCurrentDirectory());

        var full = Path.GetFullPath(path);

        if (File.Exists(full))
        {
            if (full.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
                return new List<string> { full };

            if (full.EndsWith(".sln", StringComparison.OrdinalIgnoreCase) ||
                full.EndsWith(".slnx", StringComparison.OrdinalIgnoreCase))
                return ReadSolutionProjects(full);

            // Some other file — fall back to globbing its directory.
            return GlobProjects(Path.GetDirectoryName(full));
        }

        if (Directory.Exists(full))
            return GlobProjects(full);

        Host.Error($"Path not found: {path}");
        return null;
    }

    private static List<string> ReadSolutionProjects(string solutionFile)
    {
        var solution = ((AbsolutePath)solutionFile).ReadSolution();
        return solution.AllProjects
            .Select(x => (string)x.Path)
            .Where(x => x.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            .Where(File.Exists)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
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

    private static void RenderTables(IReadOnlyList<Finding> findings, int projectCount, int restoreMissing)
    {
        var redundant = findings.Where(x => x.Kind != FindingKind.VersionConflict)
            .OrderBy(x => x.Project).ThenBy(x => x.PackageId).ToList();
        var conflicts = findings.Where(x => x.Kind == FindingKind.VersionConflict)
            .OrderBy(x => x.PackageId).ToList();

        if (findings.Count == 0)
        {
            AnsiConsole.MarkupLine($"[green]✓ No redundant or conflicting package references across {projectCount} target(s).[/]");
            if (restoreMissing > 0)
                AnsiConsole.MarkupLine($"[grey]{restoreMissing} project(s) skipped — not restored.[/]");
            return;
        }

        if (redundant.Count > 0)
        {
            var table = new Table { Border = TableBorder.Rounded, Title = new TableTitle("[bold]Redundant package references[/]") };
            table.AddColumn("Project");
            table.AddColumn("TFM");
            table.AddColumn("Package");
            table.AddColumn("Version");
            table.AddColumn("Action");
            table.AddColumn("Provided by");

            foreach (var finding in redundant)
            {
                var action = finding.SafeToRemove ? "[green]remove[/]" : "[yellow]review[/]";
                var via = finding.Kind == FindingKind.RedundantViaProject ? "[blue]proj[/]" : "[grey]pkg[/]";
                table.AddRow(
                    Markup.Escape(finding.Project ?? string.Empty),
                    Markup.Escape(finding.TargetFramework ?? string.Empty),
                    Markup.Escape(finding.PackageId ?? string.Empty),
                    Markup.Escape(finding.ResolvedVersion ?? string.Empty),
                    action,
                    $"{via} {Markup.Escape(string.Join(", ", finding.Providers))}");
            }

            AnsiConsole.Write(table);
        }

        if (conflicts.Count > 0)
        {
            var table = new Table { Border = TableBorder.Rounded, Title = new TableTitle("[bold]Version conflicts[/]") };
            table.AddColumn("Package");
            table.AddColumn("Resolved versions (projects)");

            foreach (var finding in conflicts)
                table.AddRow(Markup.Escape(finding.PackageId ?? string.Empty), Markup.Escape(ConflictBreakdown(finding)));

            AnsiConsole.Write(table);
        }

        AnsiConsole.MarkupLine(
            $"[bold]Summary:[/] [yellow]{redundant.Count}[/] redundant, [yellow]{conflicts.Count}[/] conflict(s) across {projectCount} target(s)." +
            (restoreMissing > 0 ? $" [grey]({restoreMissing} skipped — not restored)[/]" : string.Empty));
    }

    private static string ConflictBreakdown(Finding finding)
    {
        // Finding.Detail looks like "<id> resolves to multiple versions: 3.0.0 (A, B); 4.0.0 (C)."
        var detail = finding.Detail ?? string.Empty;
        var marker = detail.IndexOf(": ", StringComparison.Ordinal);
        var body = marker >= 0 ? detail.Substring(marker + 2).TrimEnd('.') : detail;
        return string.Join("\n", body.Split("; "));
    }

    private static void RenderFlat(IReadOnlyList<Finding> findings, LogLevel severity, int projectCount, int restoreMissing)
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
