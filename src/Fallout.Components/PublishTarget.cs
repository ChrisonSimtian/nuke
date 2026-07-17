#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Fallout.Common;
using Fallout.Common.IO;
using Fallout.Common.Tooling;
using Fallout.Common.Tools.DotNet;
using Fallout.Common.Utilities;
using Fallout.Common.Utilities.Collections;
using static Fallout.Common.Tools.DotNet.DotNetTasks;

#pragma warning disable FALLOUT005 // PublishTarget is the NuGet-feed IPublishTarget implementation (ADR-0009)

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
public sealed class PublishTarget : IPublishTarget
{
    /// <summary>Logical name, used by the <c>--publish-to</c> selector (e.g. <c>github-packages</c>, <c>nuget.org</c>).</summary>
    public required string Name { get; init; }

    /// <summary>NuGet feed URL packages are pushed to.</summary>
    public required string Source { get; init; }

    /// <summary>API key for the feed (required).</summary>
    public string? ApiKey { get; init; }

    /// <summary>Extra push configuration applied to this target's <c>dotnet nuget push</c> invocation.</summary>
    public Configure<DotNetNuGetPushSettings> PushSettings { get; init; } = _ => _;

    /// <summary>Per-package push configuration applied (with <see cref="PushSettings"/>) to each pushed package.</summary>
    public Configure<DotNetNuGetPushSettings> PackagePushSettings { get; init; } = _ => _;

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

    /// <summary>A NuGet feed accepts package artifacts; per-package include/exclude routing is applied in <see cref="DeployAsync"/>.</summary>
    public bool Accepts(Artifact artifact) => artifact.Kind == ArtifactKind.Packages;

    /// <summary>Fail fast on a missing API key before any push happens.</summary>
    public void Validate()
        => Assert.True(!ApiKey.IsNullOrWhiteSpace(), $"Publish target [{Name}] has no API key.");

    /// <summary>
    /// Push the package files from the given artifacts to this feed, applying this target's
    /// include/exclude routing. Identical push behaviour to the legacy single-loop path — the
    /// deploy logic now lives on the target (ADR-0009 D4).
    /// </summary>
    public Task DeployAsync(IReadOnlyList<Artifact> artifacts, DeploymentContext context)
    {
        var routed = artifacts
            .Where(x => x.Kind == ArtifactKind.Packages)
            .SelectMany(x => x.Files)
            .Where(x => Accepts(x.NameWithoutExtension))
            .ToList();

        if (routed.Count == 0)
        {
            Serilog.Log.Warning("Publish target {Target}: no packaged files matched its routing rules — skipping.", Name);
            return Task.CompletedTask;
        }

        Serilog.Log.Information("Publish target {Target}: pushing {Count} package(s) → {Source}.", Name, routed.Count, Source);
        DotNetNuGetPush(_ => _
                .SetSource(Source)
                .SetApiKey(ApiKey!)
                .When(SkipDuplicate, _ => _.EnableSkipDuplicate())
                .Apply(PushSettings)
                .CombineWith(routed, (_, v) => _
                    .SetTargetPath(v))
                .Apply(PackagePushSettings),
            context.DegreeOfParallelism,
            context.ContinueOnFailure);

        return Task.CompletedTask;
    }
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

    private static string GlobToRegex(string pattern)
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
