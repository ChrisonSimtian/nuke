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

#pragma warning disable FALLOUT005 // IPublish drives the experimental CD model (IPublishTarget/Artifact/DeploymentContext — ADR-0009)

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
    IEnumerable<IPublishTarget> PublishTargets =>
        new IPublishTarget[] { new PublishTarget { Name = "default", Source = NuGetSource, ApiKey = NuGetApiKey } };

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
        .Executes(async () =>
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

            // Fail fast on configuration errors (e.g. a missing API key) before pushing anything,
            // rather than pushing some targets and then breaking half-way (ADR-0009 D4).
            foreach (var target in targets)
                target.Validate();

            // Build once, deploy many: one immutable artifact fanned out to each target that
            // accepts it, the target owning its own deploy protocol (ADR-0009 D1/D4). The nupkgs
            // carry their own versions, so the artifact's Version label is not needed here.
            var artifact = new Artifact(ArtifactKind.Packages, Version: string.Empty, Files: candidates);
            var context = new DeploymentContext
            {
                DegreeOfParallelism = PushDegreeOfParallelism,
                ContinueOnFailure = PushCompleteOnFailure,
            };

            foreach (var target in targets)
            {
                if (!target.Accepts(artifact))
                    continue;
                await target.DeployAsync(new[] { artifact }, context);
            }
        });
}
