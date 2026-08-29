using System;

namespace Fallout.Common.Utilities;

public static class DisposableExtensions
{
    /// <summary>
    /// Combines an existing <see cref="IDisposable"/> with another setup and cleanup delegate.
    /// </summary>
    public static IDisposable CombineWith(this IDisposable disposable, Action setup = null, Action cleanup = null)
    {
        return DelegateDisposable.CreateBracket(
            setup,
            () =>
            {
                cleanup?.Invoke();
                disposable.Dispose();
            });
    }

    /// <summary>
    /// Combines an existing <see cref="IDisposable"/> with another <see cref="IDisposable"/>.
    /// </summary>
    public static IDisposable CombineWith(this IDisposable disposable, IDisposable otherDisposable)
    {
        return disposable.CombineWith(cleanup: otherDisposable.Dispose);
    }
}
