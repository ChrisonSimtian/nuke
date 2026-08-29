using System;
using System.Diagnostics;

namespace Fallout.Common.Utilities;

[DebuggerNonUserCode]
[DebuggerStepThrough]
public static class Lazy
{
    /// <summary>
    /// Creates a <see cref="Lazy{T}"/> from a delegate.
    /// </summary>
    public static Lazy<T> Create<T>(Func<T> provider)
    {
        return new Lazy<T>(provider);
    }
}
