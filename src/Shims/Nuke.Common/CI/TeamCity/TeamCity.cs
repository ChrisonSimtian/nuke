// Hand-written transition shim for the framework-injected CI host singleton.
// See src/Shims/Nuke.Common/CI/AppVeyor/AppVeyor.cs for the rationale shared
// across all CI host shims.

namespace Nuke.Common.CI.TeamCity;

public static class TeamCity
{
    public static Fallout.Common.CI.TeamCity.TeamCity Instance
        => Fallout.Common.CI.TeamCity.TeamCity.Instance;
}
