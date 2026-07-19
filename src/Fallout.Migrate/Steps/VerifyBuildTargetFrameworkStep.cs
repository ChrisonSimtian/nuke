using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Fallout.Common.IO;
using Fallout.Migrate.Common;

namespace Fallout.Migrate.Steps;

/// <summary>
/// Warns when the repo's build orchestrator project (<c>_build.csproj</c>) targets an older .NET
/// than Fallout requires. Fallout's own tooling is built and tested against .NET 10; a build
/// project still on an older TFM can hit tool incompatibilities that aren't caught by this
/// migration's other, purely textual rewrites.
/// </summary>
internal sealed class VerifyBuildTargetFrameworkStep : IMigrationStep
{
    /// <summary>
    /// Matches a `<c>TargetFramework</c>` or `<c>TargetFrameworks</c>` element's raw value.
    /// </summary>
    private static readonly Regex targetFrameworkElementPattern = new(
        @"<TargetFrameworks?>(?<value>[^<]+)</TargetFrameworks?>",
        RegexOptions.Compiled);

    /// <inheritdoc />
    public Task ExecuteAsync(MigrationContext context, Summary summary)
    {
        foreach (AbsolutePath path in MigrationFileOperations.EnumerateFiles(context.RootDirectory, "_build.csproj"))
        {
            string content = path.ReadAllText();
            Match match = targetFrameworkElementPattern.Match(content);
            if (!match.Success)
            {
                continue;
            }

            string[] monikers = match.Groups["value"].Value
                .Split(';')
                .Select(m => m.Trim())
                .Where(m => m.Length > 0)
                .ToArray();

            string[] outdated = monikers
                .Where(moniker => TargetFrameworkMonikers.IsOlderThanMinimumSupported(moniker, BumpDotNetVersionStep.MinimumSupportedMajor))
                .ToArray();
            if (outdated.Length == 0)
            {
                continue;
            }

            summary.Warnings.Add(
                $"{MigrationFileOperations.RelativePath(context.RootDirectory, path)} targets " +
                $"{string.Join(", ", outdated)}, older than .NET {BumpDotNetVersionStep.MinimumSupportedMajor}. " +
                "Check that all tools you invoke from the build (SDKs, global tools, etc.) also " +
                $"support .NET {BumpDotNetVersionStep.MinimumSupportedMajor} before finishing this migration.");
        }

        return Task.CompletedTask;
    }
}
