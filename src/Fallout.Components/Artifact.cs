#nullable enable
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fallout.Common.IO;

#pragma warning disable FALLOUT005 // defining the experimental CD model (ADR-0009)

namespace Fallout.Components;

/// <summary>
/// The kinds of thing a build produces from a release. A <see cref="IPublishTarget"/>
/// declares which kinds it <see cref="IPublishTarget.Accepts"/>. Part of the
/// experimental continuous-delivery model (<c>FALLOUT005</c>, ADR-0009).
/// </summary>
[Experimental("FALLOUT005")]
public enum ArtifactKind
{
    /// <summary>NuGet packages (<c>.nupkg</c>).</summary>
    Packages,

    /// <summary>Compiled assemblies (<c>.dll</c>).</summary>
    Assemblies,

    /// <summary>Release notes / changelog for a release.</summary>
    ReleaseNotes,

    /// <summary>Debug symbols (<c>.snupkg</c> / <c>.pdb</c>).</summary>
    Symbols,

    /// <summary>A source archive of the release snapshot.</summary>
    SourceArchive,

    /// <summary>Rendered documentation (e.g. a Docusaurus site).</summary>
    Documentation,

    /// <summary>A Homebrew formula for the CLI global tool.</summary>
    HomebrewFormula,
}

/// <summary>
/// An immutable, versioned output of building a release. The same <see cref="Artifact"/>
/// is deployed to every eligible <see cref="IPublishTarget"/> — build once, deploy many
/// (ADR-0009). Part of the experimental CD model (<c>FALLOUT005</c>).
/// </summary>
[Experimental("FALLOUT005")]
public sealed record Artifact(ArtifactKind Kind, string Version, IReadOnlyList<AbsolutePath> Files);
