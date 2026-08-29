using System;
using System.Linq.Expressions;

namespace Fallout.Common.Utilities;

/// <summary>
/// Represents an <see cref="IDisposable"/> that executes a delegate upon <see cref="Dispose"/>.
/// </summary>
public class DelegateDisposable : IDisposable
{
    /// <summary>
    /// Creates an <see cref="IDisposable"/> from a setup and cleanup delegate.
    /// </summary>
    public static IDisposable CreateBracket(Action setup = null, Action cleanup = null)
    {
        setup?.Invoke();
        return new DelegateDisposable(cleanup);
    }

    /// <summary>
    /// Creates an <see cref="IDisposable"/> from a setup and cleanup delegate.
    /// </summary>
    public static IDisposable CreateBracket<T>(Func<T> setup, Action<T> cleanup)
    {
        T obj = default;
        return CreateBracket(() => obj = setup.Invoke(), () => cleanup.Invoke(obj));
    }

    public static IDisposable SetAndRestore<T>(Expression<Func<T>> memberProvider, T value)
    {
        var member = memberProvider.GetMemberInfo();
        var target = memberProvider.GetTarget();
        var previousValue = member.GetValue<T>(target);
        member.SetValue(target, value);
        return new DelegateDisposable(() => member.SetValue(target, previousValue));
    }

    private readonly Action cleanup;

    private DelegateDisposable(Action cleanup)
    {
        this.cleanup = cleanup;
    }

    public void Dispose()
    {
        cleanup?.Invoke();
    }
}
