// Hand-written transition shim for the framework-injected CI host singleton.
// See src/Shims/Nuke.Common/CI/AppVeyor/AppVeyor.cs for the rationale shared
// across all CI host shims.

namespace Nuke.Common.CI.GitLab;

public static class GitLab
{
    public static Fallout.Common.CI.GitLab.GitLab Instance
        => Fallout.Common.CI.GitLab.GitLab.Instance;
}
