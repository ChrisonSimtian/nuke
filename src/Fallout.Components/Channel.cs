#nullable enable
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Fallout.Components;

/// <summary>
/// A release channel: which <see cref="DeploymentEnvironment"/>s (by name, in order) a
/// release built on this channel is eligible to promote through. A <c>preview</c> channel
/// might reach only <c>Dev</c> and <c>Staging</c>; a <c>release</c> channel reaches
/// <c>Production</c> too. Maturity is fixed at build time by the channel; environments
/// control distribution reach, never version (ADR-0009 D1). Part of the experimental CD
/// model (<c>FALLOUT005</c>).
/// </summary>
[Experimental("FALLOUT005")]
public sealed record Channel(string Name, IReadOnlyList<string> Environments);
