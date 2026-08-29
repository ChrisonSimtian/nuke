// Hand-written transition shim for the framework-injected CI host singleton.
// See src/Shims/Nuke.Common/CI/AppVeyor/AppVeyor.cs for the rationale shared
// across all CI host shims.

namespace Nuke.Common.CI.TravisCI;

public static class TravisCI
{
    public static Fallout.Common.CI.TravisCI.TravisCI Instance
        => Fallout.Common.CI.TravisCI.TravisCI.Instance;
}
