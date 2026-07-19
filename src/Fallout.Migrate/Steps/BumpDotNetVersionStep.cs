using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Fallout.Migrate.Common;

namespace Fallout.Migrate.Steps;

/// <summary>
/// Bumps the repo's build orchestrator project (<c>_build.csproj</c>) to target
/// <see cref="TargetFramework"/> and pins <c>global.json</c>'s SDK version to
/// <see cref="SdkVersion"/> — but only when what's there is behind those minimums. Already
/// up-to-date or newer values (e.g. a build project already on <c>net11.0</c>) are left alone.
/// </summary>
internal sealed class BumpDotNetVersionStep : IMigrationStep
{
    /// <summary>
    /// The .NET target framework moniker to bump <c>_build.csproj</c> to when it's behind.
    /// </summary>
    private const string TargetFramework = "net10.0";

    /// <summary>
    /// The .NET SDK version to pin <c>global.json</c> to when it's behind.
    /// </summary>
    private const string SdkVersion = "10.0.100";

    /// <summary>
    /// The minimum .NET SDK version. Versions at or above this aren't touched.
    /// </summary>
    private static readonly Version minimumSupportedSdkVersion = new(10, 0, 100);

    /// <summary>
    /// Matches a modern, dotted .NET moniker such as <c>net8.0</c> or <c>net10.0</c>.
    /// </summary>
    private static readonly Regex modernMonikerPattern = new(
        @"^net(?<major>\d+)\.\d+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// The minimum .NET target framework major version, derived from <see cref="TargetFramework"/>.
    /// Monikers at or above this aren't touched.
    /// </summary>
    private static readonly int minimumSupportedMajor = int.Parse(
        modernMonikerPattern.Match(TargetFramework).Groups["major"].Value,
        CultureInfo.InvariantCulture);

    /// <summary>
    /// Matches the <c>TargetFramework</c> element's raw value. Build projects don't multi-target,
    /// so <c>TargetFrameworks</c> (plural) is intentionally not matched here.
    /// </summary>
    private static readonly Regex targetFrameworkElementPattern = new(
        @"<TargetFramework>(?<value>[^<]+)</TargetFramework>",
        RegexOptions.Compiled);

    /// <summary>
    /// Matches the value of <c>global.json</c>'s <c>sdk.version</c> property.
    /// </summary>
    private static readonly Regex sdkVersionPattern = new(
        @"(?<=""sdk""\s*:\s*\{[^}]*?""version""\s*:\s*"")[^""]+",
        RegexOptions.Compiled | RegexOptions.Singleline);

    /// <inheritdoc />
    public Task ExecuteAsync(MigrationContext context, Summary summary)
    {
        foreach (var path in MigrationFileOperations.EnumerateFiles(context.RootDirectory, "_build.csproj"))
        {
            MigrationFileOperations.ApplyRewrite(context, path, BumpTargetFramework, summary);
        }

        foreach (var path in MigrationFileOperations.EnumerateFiles(context.RootDirectory, "global.json"))
        {
            MigrationFileOperations.ApplyRewrite(context, path, BumpSdkVersion, summary);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Rewrites a build project's <c>TargetFramework</c> element to <see cref="TargetFramework"/>
    /// when its current moniker is behind <see cref="minimumSupportedMajor"/>.
    /// </summary>
    /// <param name="original">The original <c>_build.csproj</c> content.</param>
    /// <returns>The rewritten content and the number of edits made.</returns>
    public static RewriteResult BumpTargetFramework(string original)
    {
        Match match = targetFrameworkElementPattern.Match(original);
        if (!match.Success || !IsOlderThanMinimumSupportedFramework(match.Groups["value"].Value))
        {
            return new RewriteResult(original, 0);
        }

        string content = targetFrameworkElementPattern.Replace(
            original,
            $"<TargetFramework>{TargetFramework}</TargetFramework>",
            count: 1);

        return new RewriteResult(content, 1);
    }

    /// <summary>
    /// Rewrites <c>global.json</c>'s <c>sdk.version</c> to <see cref="SdkVersion"/> only when the
    /// current version is behind <see cref="minimumSupportedSdkVersion"/>.
    /// </summary>
    /// <param name="original">The original <c>global.json</c> content.</param>
    /// <returns>The rewritten content and the number of edits made.</returns>
    public static RewriteResult BumpSdkVersion(string original)
    {
        Match match = sdkVersionPattern.Match(original);
        if (!match.Success || !IsOlderThanMinimumSupportedSdk(match.Value))
        {
            return new RewriteResult(original, 0);
        }

        string content = sdkVersionPattern.Replace(original, SdkVersion, count: 1);
        return new RewriteResult(content, 1);
    }

    /// <summary>
    /// Returns <c>true</c> when <paramref name="moniker"/> targets an older .NET than
    /// <see cref="minimumSupportedMajor"/> — including non-modern monikers (.NET Framework,
    /// .NET Standard, out-of-support <c>netcoreapp*</c>) which never satisfy the minimum.
    /// </summary>
    /// <param name="moniker">A single target framework moniker, e.g. <c>net8.0</c>.</param>
    private static bool IsOlderThanMinimumSupportedFramework(string moniker)
    {
        Match match = modernMonikerPattern.Match(moniker);
        if (!match.Success)
        {
            return true;
        }

        int major = int.Parse(match.Groups["major"].Value, CultureInfo.InvariantCulture);
        return major < minimumSupportedMajor;
    }

    /// <summary>
    /// Returns <c>true</c> when <paramref name="version"/> is behind
    /// <see cref="minimumSupportedSdkVersion"/>, or can't be parsed as a version at all.
    /// </summary>
    /// <param name="version">The <c>sdk.version</c> value from <c>global.json</c>.</param>
    private static bool IsOlderThanMinimumSupportedSdk(string version)
    {
        Version parsed;
        if (!Version.TryParse(version, out parsed))
        {
            return true;
        }

        return parsed < minimumSupportedSdkVersion;
    }
}
