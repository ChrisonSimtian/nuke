#nullable enable
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

#pragma warning disable FALLOUT005 // defining the experimental CD model (ADR-0009)

namespace Fallout.Components;

/// <summary>
/// A distribution endpoint a built <see cref="Artifact"/> can be deployed to — a NuGet
/// feed, a GitHub Release, a Homebrew tap, a docs site, … . One implementation per
/// provider (see <see cref="PublishTarget"/> for the NuGet-feed implementation). This is
/// the <c>DeploymentTarget</c> of issue #334 and the seam the CD model fans a single build
/// out across. Part of the experimental CD model (<c>FALLOUT005</c>, ADR-0009).
/// </summary>
[Experimental("FALLOUT005")]
public interface IPublishTarget
{
    /// <summary>Logical name, used by the <c>--publish-to</c> selector (e.g. <c>github-packages</c>, <c>nuget.org</c>).</summary>
    string Name { get; }

    /// <summary>Whether this target accepts the given artifact (typically by <see cref="Artifact.Kind"/>).</summary>
    bool Accepts(Artifact artifact);

    /// <summary>Deploy the accepted artifacts to this endpoint. Each call is one tracked deployment.</summary>
    Task DeployAsync(IReadOnlyList<Artifact> artifacts, DeploymentContext context);

    /// <summary>
    /// Fail-fast configuration check run before any deployment in a run (e.g. a missing API key).
    /// Default: no-op.
    /// </summary>
    void Validate() { }
}

/// <summary>
/// Provider-neutral per-run deployment knobs. Provider-specific configuration (a NuGet
/// feed's push settings, say) lives on the concrete <see cref="IPublishTarget"/>, not here.
/// Part of the experimental CD model (<c>FALLOUT005</c>).
/// </summary>
[Experimental("FALLOUT005")]
public sealed class DeploymentContext
{
    /// <summary>How many pushes to run concurrently within a single target deploy.</summary>
    public int DegreeOfParallelism { get; init; } = 1;

    /// <summary>Continue the deploy after an individual push failure rather than aborting.</summary>
    public bool ContinueOnFailure { get; init; }

    /// <summary>Plan the deployment without performing any external writes.</summary>
    public bool DryRun { get; init; }
}
