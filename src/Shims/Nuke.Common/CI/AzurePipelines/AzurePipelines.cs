// Hand-written transition shim for the framework-injected CI host singleton.
// See src/Shims/Nuke.Common/CI/AppVeyor/AppVeyor.cs for the rationale shared
// across all CI host shims.

namespace Nuke.Common.CI.AzurePipelines;

public static class AzurePipelines
{
    public static Fallout.Common.CI.AzurePipelines.AzurePipelines Instance
        => Fallout.Common.CI.AzurePipelines.AzurePipelines.Instance;
}
