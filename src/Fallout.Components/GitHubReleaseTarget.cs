#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;

#pragma warning disable FALLOUT005 // part of the experimental CD model (ADR-0009)

namespace Fallout.Components;

/// <summary>
/// An <see cref="IPublishTarget"/> that materializes a GitHub Release for a tag and attaches
/// its artifacts (release notes plus packages, symbols, and a source archive). Demonstrates
/// that a single target accepts several <see cref="ArtifactKind"/>s (ADR-0009 D4).
/// <para/>
/// First-cut stub: the deploy path builds on <c>GitHubReleaseTasks</c> (the <c>*Tasks → REST</c>
/// pattern from ADR-0001), which is not yet implemented (#334). The interface shape is the
/// deliverable here; the provider follows in a refinement pass.
/// </summary>
[Experimental("FALLOUT005")]
public sealed class GitHubReleaseTarget : IPublishTarget
{
    /// <inheritdoc />
    public required string Name { get; init; }

    /// <inheritdoc />
    public bool Accepts(Artifact artifact)
        => artifact.Kind is ArtifactKind.ReleaseNotes
            or ArtifactKind.Packages
            or ArtifactKind.Symbols
            or ArtifactKind.SourceArchive;

    /// <inheritdoc />
    public Task DeployAsync(IReadOnlyList<Artifact> artifacts, DeploymentContext context)
        => throw new NotImplementedException(
            "GitHubReleaseTarget.DeployAsync is not implemented yet — it builds on GitHubReleaseTasks "
            + "(ADR-0001, the *Tasks → REST pattern) tracked by #334. See ADR-0009.");
}
