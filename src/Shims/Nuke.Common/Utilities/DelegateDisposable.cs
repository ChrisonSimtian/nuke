// Hand-written transition shim. The canonical DelegateDisposable has a private
// ctor and exposes itself only via static factories; the generator skips it as
// no-accessible-ctor, and a subclass wouldn't compile anyway. This static-class
// shim re-exposes the three factories so consumers keep compiling on
// `using Nuke.Common.Utilities;`.

using System;
using System.Linq.Expressions;

namespace Nuke.Common.Utilities;

public static class DelegateDisposable
{
    public static IDisposable CreateBracket(Action setup = null, Action cleanup = null)
        => Fallout.Common.Utilities.DelegateDisposable.CreateBracket(setup, cleanup);

    public static IDisposable CreateBracket<T>(Func<T> setup, Action<T> cleanup)
        => Fallout.Common.Utilities.DelegateDisposable.CreateBracket(setup, cleanup);

    public static IDisposable SetAndRestore<T>(Expression<Func<T>> memberProvider, T value)
        => Fallout.Common.Utilities.DelegateDisposable.SetAndRestore(memberProvider, value);
}
