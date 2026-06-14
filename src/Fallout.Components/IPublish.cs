#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fallout.Common;
using Fallout.Common.IO;
using Fallout.Common.Tooling;
using Fallout.Common.Tools.DotNet;
using Fallout.Common.Utilities;
using Fallout.Common.Utilities.Collections;
using static Fallout.Common.Tools.DotNet.DotNetTasks;

namespace Fallout.Components;

public interface IPublish : IPack, ITest
{
    [Parameter] string NuGetSource => TryGetValue(() => NuGetSource) ?? "https://api.nuget.org/v3/index.json";
    [Parameter] [Secret] string NuGetApiKey => TryGetValue(() => NuGetApiKey);

    /// <summary>
    /// The channels this build publishes to (<c>FALLOUT001</c>). Override to fan a single
    /// <c>Pack</c> output across multiple feeds with per-feed package routing (e.g. GitHub
    /// Packages for everything, nuget.org for <c>Fallout.*</c> only). The default reproduces
    /// the legacy single-feed push from <see cref="NuGetSource"/> / <see cref="NuGetApiKey"/>.
    /// </summary>
    [Experimental("FALLOUT001")]
    IEnumerable<PublishTarget> PublishTargets =>
        new[] { new PublishTarget { Name = "default", Source = NuGetSource, ApiKey = NuGetApiKey } };

    /// <summary>
    /// Names of the configured <see cref="PublishTargets"/> to push to this run (<c>FALLOUT001</c>).
    /// Empty selects all. Wire from the CLI as <c>--publish-to github-packages nuget.org</c>.
    /// </summary>
    [Parameter("Publish only to these named targets (default: all configured PublishTargets).")]
    [Experimental("FALLOUT001")]
    string[] PublishTo => TryGetValue(() => PublishTo) ?? Array.Empty<string>();

    /// <summary>Extra per-push configuration applied to every target's <c>dotnet nuget push</c>.</summary>
    Configure<DotNetNuGetPushSettings> PushSettings => _ => _;

    /// <summary>Per-package push configuration applied (with <see cref="PushSettings"/>) to every target.</summary>
    Configure<DotNetNuGetPushSettings> PackagePushSettings => _ => _;

    /// <summary>
    /// Legacy single-feed base settings (source + key from <see cref="NuGetSource"/>/<see cref="NuGetApiKey"/>).
    /// Retained for back-compat; the multi-channel <see cref="Publish"/> path sets source/key per
    /// <see cref="PublishTarget"/> instead, so it does not apply this.
    /// </summary>
    sealed Configure<DotNetNuGetPushSettings> PushSettingsBase => _ => _
        .SetSource(NuGetSource)
        .SetApiKey(NuGetApiKey);

    /// <summary>Candidate package set routed across the selected targets. Defaults to every packed <c>*.nupkg</c>.</summary>
    IEnumerable<AbsolutePath> PushPackageFiles => PackagesDirectory.GlobFiles("*.nupkg");

    bool PushCompleteOnFailure => true;
    int PushDegreeOfParallelism => 5;

    Target Publish => _ => _
        .DependsOn(Test, Pack)
        .Executes(() =>
        {
#pragma warning disable FALLOUT001 // configuring the experimental multi-channel surface is the point of this target
            var configured = PublishTargets.ToList();
            var selection = PublishTo;
#pragma warning restore FALLOUT001

            var targets = selection.Length == 0
                ? configured
                : configured.Where(x => selection.Contains(x.Name, StringComparer.OrdinalIgnoreCase)).ToList();

            Assert.True(targets.Count > 0,
                selection.Length == 0
                    ? "No publish targets are configured — override IPublish.PublishTargets."
                    : $"--publish-to [{selection.JoinComma()}] matched none of the configured targets [{configured.Select(x => x.Name).JoinComma()}].");

            var candidates = PushPackageFiles.ToList();
            Assert.True(candidates.Count > 0,
                "No packages found — nothing to publish. Ensure Pack produced *.nupkg files (override IPublish.PushPackageFiles if needed).");

            // Validate every selected target's key up front: a missing key is a config error
            // we can know before pushing anything, so fail fast rather than push some feeds and
            // then break half-way. (Per-push failures stay independent — see PushCompleteOnFailure.)
            var keyless = targets.Where(x => x.ApiKey.IsNullOrWhiteSpace()).Select(x => x.Name).ToList();
            Assert.True(keyless.Count == 0, $"Publish target(s) [{keyless.JoinComma()}] have no API key.");

            foreach (var target in targets)
            {
                var routed = candidates.Where(x => target.Accepts(x.NameWithoutExtension)).ToList();
                if (routed.Count == 0)
                {
                    Serilog.Log.Warning("Publish target {Target}: no packaged files matched its routing rules — skipping.", target.Name);
                    continue;
                }

                Serilog.Log.Information("Publish target {Target}: pushing {Count} package(s) → {Source}.", target.Name, routed.Count, target.Source);
                DotNetNuGetPush(_ => _
                        .SetSource(target.Source)
                        .SetApiKey(target.ApiKey!)
                        .When(target.SkipDuplicate, _ => _.EnableSkipDuplicate())
                        .Apply(PushSettings)
                        .CombineWith(routed, (_, v) => _
                            .SetTargetPath(v))
                        .Apply(PackagePushSettings),
                    PushDegreeOfParallelism,
                    PushCompleteOnFailure);
            }
        });
}
