#nullable enable
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

#pragma warning disable FALLOUT005 // defining the experimental CD model (ADR-0009)

namespace Fallout.Components;

/// <summary>
/// A promotion stage (e.g. <c>Dev</c> → <c>Staging</c> → <c>Production</c>) that groups the
/// <see cref="IPublishTarget"/>s deployed to at that stage and carries the gate between
/// stages. A target may belong to more than one environment (ADR-0009 D5). In the GitHub
/// Actions realization each *target* maps to a GitHub Environment for deployment tracking,
/// and the stage gate is a single <c>production</c> approval (ADR-0009 D3). Part of the
/// experimental CD model (<c>FALLOUT005</c>).
/// </summary>
[Experimental("FALLOUT005")]
public sealed record DeploymentEnvironment(
    string Name,
    int Order,
    bool RequiresApproval,
    IReadOnlyList<IPublishTarget> Targets);
