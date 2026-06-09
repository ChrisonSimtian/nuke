#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Fallout.Components;

/// <summary>
/// A routable publish destination: a package feed plus the rules deciding which
/// packages go to it. Consumed by <see cref="IPublish"/> to fan a single
/// <c>Pack</c> output out across multiple channels (e.g. GitHub Packages for
/// everything, nuget.org for <c>Fallout.*</c> only). Part of the experimental
/// multi-channel publishing surface (<c>FALLOUT001</c>).
/// </summary>
// A sealed class (not a record) so the transition-shim generator skips it: it can't
// derive a Nuke.* shim from a sealed type (CS0509), and this is a new type with no
// pre-rename consumers to bridge. We don't rely on record value-equality / `with`.
public sealed class PublishTarget
{
    /// <summary>Logical name, used by the <c>--publish-to</c> selector (e.g. <c>github-packages</c>, <c>nuget.org</c>).</summary>
    public required string Name { get; init; }

    /// <summary>NuGet feed URL packages are pushed to.</summary>
    public required string Source { get; init; }

    /// <summary>API key for the feed (required).</summary>
    public string? ApiKey { get; init; }

    /// <summary>Package-name globs (<c>*</c>, <c>?</c>) this target accepts. Default: everything.</summary>
    public IReadOnlyList<string> IncludePackages { get; init; } = new[] { "*" };

    /// <summary>Package-name globs this target rejects. Exclusion wins over inclusion.</summary>
    public IReadOnlyList<string> ExcludePackages { get; init; } = Array.Empty<string>();

    /// <summary>Pass <c>--skip-duplicate</c> so re-runs are idempotent on already-published versions.</summary>
    public bool SkipDuplicate { get; init; } = true;

    /// <summary>
    /// Whether this target accepts the given package name. <paramref name="packageName"/> is matched
    /// against the include/exclude globs; callers pass the package file name without its
    /// <c>.nupkg</c> extension, so a pattern like <c>Fallout.*</c> matches <c>Fallout.Common.2026.1.0</c>.
    /// </summary>
    public bool Accepts(string packageName)
        => PublishPackageRouter.MatchesAny(IncludePackages, packageName)
           && !PublishPackageRouter.MatchesAny(ExcludePackages, packageName);
}

/// <summary>
/// Pure routing logic for <see cref="PublishTarget"/> — kept free of any tooling
/// or filesystem dependency so it is unit-testable in isolation.
/// </summary>
public static class PublishPackageRouter
{
    /// <summary>Returns the package names accepted by <paramref name="target"/> from the candidate set.</summary>
    public static IEnumerable<string> Route(PublishTarget target, IEnumerable<string> packageNames)
        => packageNames.Where(target.Accepts);

    /// <summary>True when <paramref name="value"/> matches at least one glob in <paramref name="patterns"/> (case-insensitive).</summary>
    public static bool MatchesAny(IEnumerable<string> patterns, string value)
        => patterns.Any(pattern => GlobMatches(pattern, value));

    /// <summary>Case-insensitive glob match supporting <c>*</c> (any run) and <c>?</c> (one char).</summary>
    public static bool GlobMatches(string pattern, string value)
        => Regex.IsMatch(value, GlobToRegex(pattern), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    static string GlobToRegex(string pattern)
    {
        var builder = new StringBuilder("^");
        foreach (var c in pattern)
        {
            builder.Append(c switch
            {
                '*' => ".*",
                '?' => ".",
                _ => Regex.Escape(c.ToString())
            });
        }

        return builder.Append('$').ToString();
    }
}
