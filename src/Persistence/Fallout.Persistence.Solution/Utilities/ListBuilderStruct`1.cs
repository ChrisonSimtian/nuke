// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Fallout.Persistence.Solution.Utilities;

/// <summary>
/// Provides a list builder that can be used to build a list of items without allocating
/// on the heap if the list is small.
/// </summary>
/// <typeparam name="T">The type of elements in the list.</typeparam>
internal ref struct ListBuilderStruct<T>
{
    private List<T>? items;

    [MaybeNull]
    private T item0;

    [MaybeNull]
    private T item1;

    [MaybeNull]
    private T item2;

    [MaybeNull]
    private T item3;

    public ListBuilderStruct()
    {
    }

    internal ListBuilderStruct(int capacity)
    {
        if (capacity > 4)
        {
            items = new List<T>(capacity - 4);
        }
    }

    internal int Count { get; private set; }

    internal T this[int index]
    {
        readonly get
        {
            return index switch
            {
                0 => item0,
                1 => item1,
                2 => item2,
                3 => item3,
                _ => items![index - 4],
            };
        }

        set
        {
            switch (index)
            {
                case 0: item0 = value; break;

                case 1: item1 = value; break;

                case 2: item2 = value; break;

                case 3: item3 = value; break;

                default: items![index - 4] = value; break;
            }
        }
    }

    public readonly Enumerator GetEnumerator()
    {
        return new Enumerator(this);
    }

    internal void Add(T item)
    {
        switch (Count)
        {
            case 0: item0 = item; break;

            case 1: item1 = item; break;

            case 2: item2 = item; break;

            case 3: item3 = item; break;

            default:
                items ??= [];
                items.Add(item);
                break;
        }

        Count++;
    }

    internal void AddRange(IReadOnlyCollection<T> items)
    {
        int newCapacity = Count + items.Count;
        if (newCapacity > 4)
        {
            this.items ??= new List<T>(newCapacity - 4);
            this.items.Capacity = newCapacity - 4;
        }

        foreach (T item in items)
        {
            Add(item);
        }
    }

    internal readonly T[] ToArray()
    {
        return Count switch
        {
            0 => [],
            1 => [item0],
            2 => [item0, item1],
            3 => [item0, item1, item2],
            4 => [item0, item1, item2, item3],
            _ => [item0, item1, item2, item3, .. items!],
        };
    }

    internal void Clear()
    {
        Count = 0;
        items = null;
    }

    internal ref struct Enumerator(ListBuilderStruct<T> builder)
    {
        private readonly ListBuilderStruct<T> builder = builder;
        private int index = -1;

        public readonly T Current => builder[index];

        public bool MoveNext()
        {
            index++;
            return index < builder.Count;
        }
    }
}
