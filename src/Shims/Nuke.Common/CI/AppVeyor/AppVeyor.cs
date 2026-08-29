// Hand-written transition shim for the framework-injected CI host singleton.
// The TransitionShimGenerator can't bridge these because consumers receive
// canonical-typed instances from Host.Instance (which can't be cast to a shim
// subclass) and field-injection via [CI] is canonical-typed at runtime. This
// shim re-exposes the static Instance accessor so the common idiom
// `AppVeyor.Instance.AccountName` keeps compiling under
// `using Nuke.Common.CI.AppVeyor;`.

namespace Nuke.Common.CI.AppVeyor;

public static class AppVeyor
{
    public static Fallout.Common.CI.AppVeyor.AppVeyor Instance
        => Fallout.Common.CI.AppVeyor.AppVeyor.Instance;

    public static int MessageLimit
    {
        get => Fallout.Common.CI.AppVeyor.AppVeyor.MessageLimit;
        set => Fallout.Common.CI.AppVeyor.AppVeyor.MessageLimit = value;
    }
}
