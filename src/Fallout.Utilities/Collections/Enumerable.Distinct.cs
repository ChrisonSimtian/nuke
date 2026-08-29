using System;
using System.Collections.Generic;
using System.Linq;

namespace Fallout.Common.Utilities.Collections;

public static partial class EnumerableExtensions
{
    /// <summary>
    /// Filters a collection to distinct/unique elements.
    /// </summary>
    public static IEnumerable<TSource> Distinct<TSource, TValue>(this IEnumerable<TSource> enumerable,
        Func<TSource, TValue> selector)
    {
        return enumerable.Distinct(new DelegateEqualityComparer<TSource, TValue>(selector));
    }

    private class DelegateEqualityComparer<TSource, TValue> : IEqualityComparer<TSource>
    {
        private readonly Func<TSource, TValue> selector;

        public DelegateEqualityComparer(Func<TSource, TValue> selector)
        {
            this.selector = selector;
        }

        public bool Equals(TSource x, TSource y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (ReferenceEquals(x, objB: null))
            {
                return false;
            }

            if (ReferenceEquals(y, objB: null))
            {
                return false;
            }

            if (x.GetType() != y.GetType())
            {
                return false;
            }

            return Equals(selector(x), selector(y));
        }

        public int GetHashCode(TSource obj)
        {
            return selector(obj).GetHashCode();
        }
    }
}
