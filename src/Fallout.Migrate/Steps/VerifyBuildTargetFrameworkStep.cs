using System.Globalization;
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
    /// The minimum .NET major version Fallout's tooling targets.
    /// </summary>
    private const int MinimumSupportedMajor = 10;

    /// <summary>
    /// Matches a `<c>TargetFramework</c>` or `<c>TargetFrameworks</c>` element's raw value.
    /// </summary>
    private static readonly Regex targetFrameworkElementPattern = new(
        @"<TargetFrameworks?>(?<value>[^<]+)</TargetFrameworks?>",
        RegexOptions.Compiled);

    /// <summary>
    /// Matches a modern, dotted .NET moniker such as <c>net8.0</c> or <c>net10.0</c>.
    /// </summary>
    private static readonly Regex modernMonikerPattern = new(
        @"^net(?<major>\d+)\.\d+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

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

            string[] outdated = monikers.Where(IsOlderThanMinimumSupported).ToArray();
            if (outdated.Length == 0)
            {
                continue;
            }

            summary.Warnings.Add(
                $"{MigrationFileOperations.RelativePath(context.RootDirectory, path)} targets " +
                $"{string.Join(", ", outdated)}, older than .NET {MinimumSupportedMajor}. " +
                "Check that all tools you invoke from the build (SDKs, global tools, etc.) also " +
                $"support .NET {MinimumSupportedMajor} before finishing this migration.");
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Returns <c>true</c> when <paramref name="moniker"/> targets an older .NET than
    /// <see cref="MinimumSupportedMajor"/> — including non-modern monikers (.NET Framework,
    /// .NET Standard, out-of-support <c>netcoreapp*</c>) which never satisfy the minimum.
    /// </summary>
    /// <param name="moniker">A single target framework moniker, e.g. <c>net8.0</c>.</param>
    private static bool IsOlderThanMinimumSupported(string moniker)
    {
        Match match = modernMonikerPattern.Match(moniker);
        if (!match.Success)
        {
            return true;
        }

        int major = int.Parse(match.Groups["major"].Value, CultureInfo.InvariantCulture);
        return major < MinimumSupportedMajor;
    }
}
