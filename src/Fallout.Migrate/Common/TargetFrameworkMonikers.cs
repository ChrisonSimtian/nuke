using System.Globalization;
using System.Text.RegularExpressions;

namespace Fallout.Migrate.Common;

/// <summary>
/// Shared parsing helpers for modern, dotted .NET target framework monikers (e.g. <c>net10.0</c>),
/// used by migration steps that compare a project's current TFM against a minimum.
/// </summary>
internal static class TargetFrameworkMonikers
{
    /// <summary>
    /// Matches a modern, dotted .NET moniker such as <c>net8.0</c> or <c>net10.0</c>.
    /// </summary>
    private static readonly Regex modernMonikerPattern = new(
        @"^net(?<major>\d+)\.\d+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Extracts the major version segment from a modern TFM moniker such as <c>net10.0</c>.
    /// </summary>
    /// <param name="moniker">A modern, dotted target framework moniker.</param>
    /// <returns>The parsed major version.</returns>
    public static int ExtractMajor(string moniker)
    {
        return int.Parse(
            modernMonikerPattern.Match(moniker).Groups["major"].Value,
            CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Returns <c>true</c> when <paramref name="moniker"/> targets an older .NET than
    /// <paramref name="minimumMajor"/> — including non-modern monikers (.NET Framework, .NET
    /// Standard, out-of-support <c>netcoreapp*</c>) which never satisfy any minimum.
    /// </summary>
    /// <param name="moniker">A single target framework moniker, e.g. <c>net8.0</c>.</param>
    /// <param name="minimumMajor">The minimum supported major version.</param>
    public static bool IsOlderThanMinimumSupported(string moniker, int minimumMajor)
    {
        Match match = modernMonikerPattern.Match(moniker);
        if (!match.Success)
        {
            return true;
        }

        int major = int.Parse(match.Groups["major"].Value, CultureInfo.InvariantCulture);
        return major < minimumMajor;
    }
}
