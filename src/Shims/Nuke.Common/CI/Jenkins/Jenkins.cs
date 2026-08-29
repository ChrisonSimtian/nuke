// Hand-written transition shim for the framework-injected CI host singleton.
// See src/Shims/Nuke.Common/CI/AppVeyor/AppVeyor.cs for the rationale shared
// across all CI host shims.

namespace Nuke.Common.CI.Jenkins;

public static class Jenkins
{
    public static Fallout.Common.CI.Jenkins.Jenkins Instance
        => Fallout.Common.CI.Jenkins.Jenkins.Instance;
}
