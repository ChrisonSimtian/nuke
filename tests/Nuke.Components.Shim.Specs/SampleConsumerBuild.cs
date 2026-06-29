//
// Compile-only contract test for the IHaz* obsolete aliases shipped in the
// Nuke.Components shim. A NUKE-era consumer declaring `class Build : NukeBuild,
// IHazSolution, IHazGitRepository` should continue to compile against Fallout —
// they'll see CS0618 warnings pointing at the renamed IHas* form, but no hard
// breakage. This file exercises every alias to lock the contract in.

#pragma warning disable CS0618  // IHaz* are intentionally obsolete; exercising them is the point.

using Nuke.Common;
using Nuke.Components;

namespace Nuke.Components.Shim.Specs;

// Exercises every IHaz* alias in one type. Abstract so we don't have to
// satisfy any concrete-build requirements — declaration alone proves the
// inheritance chain (IHaz* → IHas* → IFalloutBuild) resolves.
public abstract class SampleConsumerBuildWithIHazAliases
    : NukeBuild,
      IHazArtifacts,
      IHazChangelog,
      IHazConfiguration,
      IHazGitRepository,
      IHazGitVersion,
      IHazNerdbankGitVersioning,
      IHazReports,
      IHazSolution,
      IHazTwitterCredentials
{
}
