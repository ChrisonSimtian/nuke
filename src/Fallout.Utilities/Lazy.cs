using System;
using System.Diagnostics;
using System.Linq;

namespace Fallout.Common.Utilities;

[DebuggerNonUserCode]
[DebuggerStepThrough]
public static class Lazy
{
    /// <summary>
    /// Creates a <see cref="Lazy{T}"/> from a delegate.
    /// </summary>
    [Obsolete("Use 'new Lazy<T>(provider)' directly. This forwarder will be removed in a future release.")]
    public static Lazy<T> Create<T>(Func<T> provider)
    {
        return new Lazy<T>(provider);
    }
}
