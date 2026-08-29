// Hand-written transition shim for the framework-injected CI host singleton.
// See src/Shims/Nuke.Common/CI/AppVeyor/AppVeyor.cs for the rationale shared
// across all CI host shims.

namespace Nuke.Common.CI.Bamboo;

public static class Bamboo
{
    public static Fallout.Common.CI.Bamboo.Bamboo Instance
        => Fallout.Common.CI.Bamboo.Bamboo.Instance;
}
