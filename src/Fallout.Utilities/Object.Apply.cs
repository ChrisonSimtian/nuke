using System;

namespace Fallout.Common.Utilities;

partial class ObjectExtensions
{
    public static TOutput Apply<TInput, TOutput>(this TInput input, Func<TInput, TOutput> transform)
    {
        return transform.Invoke(input);
    }
}
