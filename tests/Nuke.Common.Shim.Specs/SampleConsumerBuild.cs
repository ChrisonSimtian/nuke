//
// This file is the **compile-only test** for the Nuke.Common shim. It mirrors what a
// typical NUKE-era consumer Build.cs imports. If this file compiles, the MVP shim covers
// the surface it claims to cover. Nothing here is executed.

#pragma warning disable CS0649 // unassigned readonly — these are injected at runtime
#pragma warning disable CS0169 // unused fields — same reason

using Fallout.Common.Git;
using Fallout.Common.IO;
using Fallout.Solutions;
using Nuke.Common.CI.AppVeyor;
using Nuke.Common.CI.AzurePipelines;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.CI.TeamCity;
using Nuke.Common.Utilities;

namespace Nuke.Common.Shim.Specs;

// Consumer's typical Build.cs entry-point — inherits the shim's NukeBuild,
// uses the canonical Fallout types via `Fallout.Common.Target` (shim doesn't
// bridge delegates; that's `fallout-migrate`'s job). The [GlobbingOptions(...)]
// attribute resolves through the shim now that the canonical is un-sealed.
[IO.GlobbingOptions(GlobbingCaseSensitivity.CaseInsensitive)]
public abstract class SampleConsumerBuild : NukeBuild, INukeBuild
{
    [Parameter("Configuration to build")]
    private readonly string Configuration;

    [Parameter]
    private readonly bool RunTests;

    [Secret]
    private readonly string NuGetApiKey;

    [ProjectModel.Solution]
    private readonly Solution Solution;

    [ProjectModel.Solution("path/to/explicit.slnx")]
    private readonly Solution ExplicitSolution;

    [Git.GitRepository]
    private readonly GitRepository GitRepository;

    // CI-host shims expose only the static `Instance` accessor. Consumers can
    // still chain into instance members because the returned type is canonical.
    // Field-injection patterns like `[CI] readonly GitHubActions GitHubActions`
    // are intentionally NOT supported — those need `fallout-migrate` to flip
    // the type reference to canonical.
    private void TouchCiHostShims()
    {
        _ = GitHubActions.Instance?.Workflow;
        _ = AzurePipelines.Instance?.AgentName;
        _ = TeamCity.Instance?.BuildNumber;
        _ = AppVeyor.Instance?.AccountName;
        _ = AppVeyor.MessageLimit;
    }

    // DelegateDisposable shim re-exposes the static factories. Consumer
    // usage stays canonical-typed at runtime (the factories return
    // canonical IDisposable instances).
    private void TouchDelegateDisposableShim()
    {
        using var bracket = DelegateDisposable.CreateBracket(
            setup: () => { },
            cleanup: () => { });

        using var typed = DelegateDisposable.CreateBracket(
            setup: () => 42,
            cleanup: _ => { });
    }
}
