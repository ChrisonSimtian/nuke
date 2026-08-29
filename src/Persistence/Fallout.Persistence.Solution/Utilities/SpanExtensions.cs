// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Fallout.Persistence.Solution.Utilities.Internal;

/// <summary>
/// Extension methods for <see cref="ReadOnlySpan{T}"/>.
/// </summary>
internal static class SpanExtensions
{
    /// <summary>
    /// Breaks the provided <paramref name="span"/> into sections based on the provided <paramref name="separator"/>.
    /// </summary>
    /// <param name="span">The input span.</param>
    /// <param name="separator">The delimiter to use.</param>
    /// <param name="splitOptions"><see cref="StringSplitOptions"/> enum indicating how split should function.</param>
    /// <returns>A <see cref="CharSpanSplitEnumerator"/> that can be enumerated to evaluate the segments.</returns>
    internal static CharSpanSplitEnumerator Split(this ReadOnlySpan<char> span, char separator,
        StringSplitOptions splitOptions = StringSplitOptions.None)
    {
        return new CharSpanSplitEnumerator(span, separator, int.MaxValue, splitOptions);
    }

    /// <summary>
    /// Breaks the provided <paramref name="span"/> into sections based on the provided <paramref name="separator"/>.
    /// </summary>
    /// <param name="span">The input span.</param>
    /// <param name="separator">The delimiter to use.</param>
    /// <param name="count">The maximum number of elements to return.</param>
    /// <param name="splitOptions"><see cref="StringSplitOptions"/> enum indicating how split should function.</param>
    /// <returns>A <see cref="CharSpanSplitEnumerator"/> that can be enumerated to evaluate the segments.</returns>
    internal static CharSpanSplitEnumerator Split(this ReadOnlySpan<char> span, char separator, int count,
        StringSplitOptions splitOptions = StringSplitOptions.None)
    {
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        return new CharSpanSplitEnumerator(span, separator, count, splitOptions);
    }

    /// <summary>
    /// Breaks the provided <paramref name="span"/> into sections based on the provided <paramref name="separator"/>.
    /// </summary>
    /// <param name="span">The input span.</param>
    /// <param name="separator">The separator to use.</param>
    /// <param name="splitOptions"><see cref="StringSplitOptions"/> enum indicating how split should function.</param>
    /// <returns>A <see cref="CharSpanSplitEnumerator"/> that can be enumerated to evaluate the segments.</returns>
    internal static CharSpanSplitEnumerator Split(this ReadOnlySpan<char> span, ReadOnlySpan<char> separator,
        StringSplitOptions splitOptions = StringSplitOptions.None)
    {
        return new CharSpanSplitEnumerator(span, separator, int.MaxValue, splitOptions);
    }

    /// <summary>
    /// Breaks the provided <paramref name="span"/> into sections based on the provided <paramref name="separator"/>.
    /// </summary>
    /// <param name="span">The input span.</param>
    /// <param name="separator">The separator to use.</param>
    /// <param name="count">The maximum number of elements to return.</param>
    /// <param name="splitOptions"><see cref="StringSplitOptions"/> enum indicating how split should function.</param>
    /// <returns>A <see cref="CharSpanSplitEnumerator"/> that can be enumerated to evaluate the segments.</returns>
    internal static CharSpanSplitEnumerator Split(this ReadOnlySpan<char> span, ReadOnlySpan<char> separator, int count,
        StringSplitOptions splitOptions = StringSplitOptions.None)
    {
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        return new CharSpanSplitEnumerator(span, separator, count, splitOptions);
    }

    /// <summary>
    /// Breaks the provided <paramref name="span"/> into sections based on the provided <paramref name="separator"/>.
    /// </summary>
    /// <param name="span">The input span.</param>
    /// <param name="separator">The separator to use.</param>
    /// <param name="splitOptions"><see cref="StringSplitOptions"/> enum indicating how split should function.</param>
    /// <returns>A <see cref="CharSpanSplitEnumerator"/> that can be enumerated to evaluate the segments.</returns>
    internal static StringSplitEnumerator Split(this ReadOnlySpan<char> span, string separator,
        StringSplitOptions splitOptions = StringSplitOptions.None)
    {
        return new StringSplitEnumerator(span, separator, int.MaxValue, splitOptions);
    }

    /// <summary>
    /// Breaks the provided <paramref name="span"/> into sections based on the provided <paramref name="separator"/>.
    /// </summary>
    /// <param name="span">The input span.</param>
    /// <param name="separator">The separator to use.</param>
    /// <param name="count">The maximum number of elements to return.</param>
    /// <param name="splitOptions"><see cref="StringSplitOptions"/> enum indicating how split should function.</param>
    /// <returns>A <see cref="CharSpanSplitEnumerator"/> that can be enumerated to evaluate the segments.</returns>
    internal static StringSplitEnumerator Split(this ReadOnlySpan<char> span, string separator, int count,
        StringSplitOptions splitOptions = StringSplitOptions.None)
    {
        return new StringSplitEnumerator(span, separator, count, splitOptions);
    }

    /// <summary>
    /// Breaks the provided <paramref name="span"/> into sections based on the provided <paramref name="separator"/>.
    /// </summary>
    /// <param name="span">The input span.</param>
    /// <param name="separator">The separator to use.</param>
    /// <param name="splitOptions"><see cref="StringSplitOptions"/> enum indicating how split should function.</param>
    /// <returns>A <see cref="CharSpanSplitEnumerator"/> that can be enumerated to evaluate the segments.</returns>
    internal static StringSplitEnumerator Split(this ReadOnlySpan<char> span, ReadOnlySpan<string> separator,
        StringSplitOptions splitOptions = StringSplitOptions.None)
    {
        return new StringSplitEnumerator(span, separator, int.MaxValue, splitOptions);
    }

    /// <summary>
    /// Breaks the provided <paramref name="span"/> into sections based on the provided <paramref name="separator"/>.
    /// </summary>
    /// <param name="span">The input span.</param>
    /// <param name="separator">The separator to use.</param>
    /// <param name="count">The maximum number of elements to return.</param>
    /// <param name="splitOptions"><see cref="StringSplitOptions"/> enum indicating how split should function.</param>
    /// <returns>A <see cref="CharSpanSplitEnumerator"/> that can be enumerated to evaluate the segments.</returns>
    internal static StringSplitEnumerator Split(this ReadOnlySpan<char> span, string[] separator, int count,
        StringSplitOptions splitOptions = StringSplitOptions.None)
    {
        return new StringSplitEnumerator(span, separator, count, splitOptions);
    }

    /// <summary>
    /// Finds the index of the first whitespace character in <paramref name="span"/>.
    /// </summary>
    /// <param name="span">The <see cref="ReadOnlySpan{Char}"/>.</param>
    /// <returns>The zero-based index of the first whitespace character or -1.</returns>
    internal static int IndexOfFirstWhitespaceCharacter(this ReadOnlySpan<char> span)
    {
        for (int i = 0; i < span.Length; ++i)
        {
            if (char.IsWhiteSpace(span[i]))
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// A struct enumerator for a split span.
    /// </summary>
    internal ref struct CharSpanSplitEnumerator
    {
        private readonly StringSplitOptions splitOptions;
        private readonly ReadOnlySpan<char> separators;
        private readonly char separator;
        private readonly bool multiCharSeparator;
        private readonly bool removeEmptyEntries;
#if NET5_0_OR_GREATER
        private readonly bool trimEntries;
#endif
        private readonly ReadOnlySpan<char> originalSpan;
        private readonly int originalCount;
        private ReadOnlySpan<char> internalSpan;
        private int count;
        private bool endReached;

        /// <summary>
        /// Initializes a new instance of the <see cref="CharSpanSplitEnumerator"/> struct.
        /// </summary>
        /// <param name="span">The input span.</param>
        /// <param name="separator">The separator to use.</param>
        /// <param name="count">The maximum number of elements to return.</param>
        /// <param name="splitOptions"><see cref="StringSplitOptions"/> enum indicating how split should function.</param>
        internal CharSpanSplitEnumerator(ReadOnlySpan<char> span, char separator, int count, StringSplitOptions splitOptions)
            : this(span, separator, [], multiCharSeparator: false, count, splitOptions)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CharSpanSplitEnumerator"/> struct.
        /// </summary>
        /// <param name="span">The input span.</param>
        /// <param name="separator">The separator to use.</param>
        /// <param name="count">The maximum number of elements to return.</param>
        /// <param name="splitOptions"><see cref="StringSplitOptions"/> enum indicating how split should function.</param>
        internal CharSpanSplitEnumerator(ReadOnlySpan<char> span, ReadOnlySpan<char> separator, int count,
            StringSplitOptions splitOptions)
            : this(span, default, separator, multiCharSeparator: true, count, splitOptions)
        {
        }

        private CharSpanSplitEnumerator(ReadOnlySpan<char> span, char separator, ReadOnlySpan<char> separators,
            bool multiCharSeparator, int count, StringSplitOptions splitOptions)
        {
            this.splitOptions = splitOptions;
            this.separator = separator;
            this.separators = separators;
            this.multiCharSeparator = multiCharSeparator;

            if (multiCharSeparator)
            {
                removeEmptyEntries = (splitOptions & StringSplitOptions.RemoveEmptyEntries) ==
                                     StringSplitOptions.RemoveEmptyEntries;
#if NET5_0_OR_GREATER
                this.trimEntries =
 (splitOptions & StringSplitOptions.TrimEntries) == StringSplitOptions.TrimEntries && !separators.IsEmpty;
#endif
            }
            else
            {
                removeEmptyEntries = (splitOptions & StringSplitOptions.RemoveEmptyEntries) ==
                                     StringSplitOptions.RemoveEmptyEntries;
#if NET5_0_OR_GREATER
                this.trimEntries = (splitOptions & StringSplitOptions.TrimEntries) == StringSplitOptions.TrimEntries;
#endif
            }

            originalSpan = span;
            internalSpan = span;
            originalCount = count;
            this.count = count;
            Current = default;
            endReached = false;
        }

        /// <summary>
        /// Gets the current item.
        /// </summary>
        public ReadOnlySpan<char> Current { get; private set; }

        /// <summary>
        /// Gets the Enumerator.
        /// </summary>
        /// <returns><see cref="CharSpanSplitEnumerator"/>.</returns>
        public readonly CharSpanSplitEnumerator GetEnumerator() => this;

        /// <summary>
        /// Advances to the next item.
        /// </summary>
        /// <returns><see langword="bool"/> indicating if there was another item.</returns>
        public bool MoveNext()
        {
            if (endReached || count == 0)
            {
                return false;
            }

            if (count == 1)
            {
                return CalculateFinalItem();
            }

            while (true)
            {
                int separatorIndex = GetSeparatorIndex();

                if (separatorIndex < 0)
                {
                    Current = internalSpan;
                    internalSpan = [];
                    endReached = true;

                    return NextSectionFound();
                }
                else
                {
                    Current = internalSpan.Slice(0, separatorIndex);
                    internalSpan = internalSpan.Slice(separatorIndex + 1);

                    if (NextSectionFound())
                    {
                        --count;

                        return true;
                    }
                }
            }
        }

        /// <summary>
        /// Resets the <see cref="CharSpanSplitEnumerator"/> to its initial state.
        /// </summary>
        internal void Reset()
        {
            internalSpan = originalSpan;
            count = originalCount;
            Current = default;
            endReached = false;
        }

        /// <summary>
        /// Converts a the current <see cref="CharSpanSplitEnumerator"/> to an array of <see cref="string"/>.
        /// This method doesn't modify the current <see cref="CharSpanSplitEnumerator"/> and starts at the beginning.
        /// </summary>
        /// <returns>The array of <see cref="string"/>.</returns>
        internal readonly string[] ToArray()
        {
            int count = Count();

            if (count == 0)
            {
                return [];
            }

            CharSpanSplitEnumerator toArrayEnumerator =
                new(originalSpan, separator, separators, multiCharSeparator, originalCount, splitOptions);

            string[] result = new string[count];
            for (int i = 0; i < result.Length && toArrayEnumerator.MoveNext(); ++i)
            {
                result[i] = toArrayEnumerator.Current.ToString();
            }

            return result;
        }

        /// <summary>
        /// Converts a the current <see cref="CharSpanSplitEnumerator"/> to a <see cref="List{T}"/> of <see cref="string"/>.
        /// This method doesn't modify the current <see cref="CharSpanSplitEnumerator"/> and starts at the beginning.
        /// </summary>
        /// <returns>A <see cref="List{T}"/> of <see cref="string"/>.</returns>
        internal readonly List<string> ToList()
        {
            int count = Count();
            List<string> result = new(count);

            if (count == 0)
            {
                return result;
            }

            CharSpanSplitEnumerator toArrayEnumerator =
                new(originalSpan, separator, separators, multiCharSeparator, originalCount, splitOptions);

            foreach (ReadOnlySpan<char> item in toArrayEnumerator)
            {
                result.Add(item.ToString());
            }

            return result;
        }

        /// <summary>
        /// Gets the count of elements returned by the current <see cref="CharSpanSplitEnumerator"/>.
        /// This method doesn't modify the current <see cref="CharSpanSplitEnumerator"/> and starts at the beginning.
        /// </summary>
        /// <returns>A count of the results.</returns>
        internal readonly int Count()
        {
            int count = 0;
            CharSpanSplitEnumerator countEnumerator =
                new(originalSpan, separator, separators, multiCharSeparator, originalCount, splitOptions);

            while (countEnumerator.MoveNext())
            {
                ++count;
            }

            return count;
        }

        /// <summary>
        /// Gets the first element returned by the current <see cref="CharSpanSplitEnumerator"/>.
        /// This method doesn't modify the current <see cref="CharSpanSplitEnumerator"/> and starts at the beginning.
        /// </summary>
        /// <returns>The first result or throws if there are none.</returns>
        internal readonly ReadOnlySpan<char> First()
        {
            CharSpanSplitEnumerator firstEnumerator =
                new(originalSpan, separator, separators, multiCharSeparator, originalCount, splitOptions);

            if (!firstEnumerator.MoveNext())
            {
                throw new InvalidOperationException();
            }

            return firstEnumerator.Current;
        }

        /// <summary>
        /// Gets the last element returned by the current <see cref="CharSpanSplitEnumerator"/>.
        /// This method doesn't modify the current <see cref="CharSpanSplitEnumerator"/> and starts at the beginning.
        /// </summary>
        /// <returns>The last result or throws if there are none.</returns>
        internal readonly ReadOnlySpan<char> Last()
        {
            CharSpanSplitEnumerator lastEnumerator =
                new(originalSpan, separator, separators, multiCharSeparator, originalCount, splitOptions);

            ReadOnlySpan<char> result = [];
            bool anyFound = false;

            foreach (ReadOnlySpan<char> section in lastEnumerator)
            {
                anyFound = true;
                result = section;
            }

            if (!anyFound)
            {
                throw new InvalidOperationException();
            }

            return result;
        }

        private readonly int GetSeparatorIndex()
        {
            if (!multiCharSeparator)
            {
                return internalSpan.IndexOf(separator);
            }

            if (separators.Length != 0)
            {
                return internalSpan.IndexOfAny(separators);
            }

            return internalSpan.IndexOfFirstWhitespaceCharacter();
        }

        private bool CalculateFinalItem()
        {
            if (removeEmptyEntries)
            {
                int i = 0;
                for (; i < internalSpan.Length; ++i)
                {
#if NET5_0_OR_GREATER
                    if (this.trimEntries)
                    {
                        for (; i < this.internalSpan.Length; ++i)
                        {
                            if (!char.IsWhiteSpace(this.internalSpan[i]))
                            {
                                break;
                            }
                        }

                        if (i >= this.internalSpan.Length)
                        {
                            break;
                        }
                    }
#endif
                    char currentChar = internalSpan[i];

                    if (multiCharSeparator)
                    {
                        if (!AnyMultiCharSeparatorMatches(currentChar))
                        {
                            break;
                        }
                    }
                    else if (currentChar != separator)
                    {
                        break;
                    }
                }

                if (i < internalSpan.Length)
                {
                    internalSpan = internalSpan.Slice(i);
                }
                else
                {
                    internalSpan = [];
                }
            }

            count = 0;
            endReached = true;
            Current = internalSpan;
            internalSpan = [];

            return NextSectionFound();
        }

        private bool NextSectionFound()
        {
#if NET5_0_OR_GREATER
            if (this.trimEntries)
            {
                this.Current = this.Current.Trim();
            }
#endif
            return !removeEmptyEntries || !Current.IsEmpty;
        }

        private readonly bool AnyMultiCharSeparatorMatches(char currentChar)
        {
            if (UseWhitespaceAsSeparator())
            {
                if (char.IsWhiteSpace(currentChar))
                {
                    return true;
                }
            }
            else
            {
                for (int i = 0; i < separators.Length; ++i)
                {
                    if (currentChar == separators[i])
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private readonly bool UseWhitespaceAsSeparator()
        {
            return separators.Length == 0;
        }
    }

    /// <summary>
    /// A struct enumerator for a split span.
    /// </summary>
    internal ref struct StringSplitEnumerator
    {
        private readonly StringSplitOptions splitOptions;
        private readonly ReadOnlySpan<string> separators;
        private readonly ReadOnlySpan<char> separator;
        private readonly bool multiStringSeparator;
        private readonly bool removeEmptyEntries;
#if NET5_0_OR_GREATER
        private readonly bool trimEntries;
#endif
        private readonly ReadOnlySpan<char> originalSpan;
        private readonly int originalCount;
        private ReadOnlySpan<char> internalSpan;
        private int count;
        private bool endReached;

#if NET5_0_OR_GREATER
        private bool firstIteration;
#endif

        /// <summary>
        /// Initializes a new instance of the <see cref="StringSplitEnumerator"/> struct.
        /// </summary>
        /// <param name="span">The input span.</param>
        /// <param name="separator">The separator to use.</param>
        /// <param name="count">The maximum number of elements to return.</param>
        /// <param name="splitOptions"><see cref="StringSplitOptions"/> enum indicating how split should function.</param>
        internal StringSplitEnumerator(ReadOnlySpan<char> span, string separator, int count, StringSplitOptions splitOptions)
            : this(span, separator.AsSpan(), [], multiStringSeparator: false, count, splitOptions)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StringSplitEnumerator"/> struct.
        /// </summary>
        /// <param name="span">The input span.</param>
        /// <param name="separator">The separator to use.</param>
        /// <param name="count">The maximum number of elements to return.</param>
        /// <param name="splitOptions"><see cref="StringSplitOptions"/> enum indicating how split should function.</param>
        internal StringSplitEnumerator(ReadOnlySpan<char> span, ReadOnlySpan<string> separator, int count,
            StringSplitOptions splitOptions)
            : this(span, [], separator, multiStringSeparator: true, count, splitOptions)
        {
        }

        private StringSplitEnumerator(ReadOnlySpan<char> span, ReadOnlySpan<char> separator, ReadOnlySpan<string> separators,
            bool multiStringSeparator, int count, StringSplitOptions splitOptions)
        {
            this.splitOptions = splitOptions;
            this.separator = separator;
            this.separators = separators;
            this.multiStringSeparator = multiStringSeparator;

            removeEmptyEntries = (splitOptions & StringSplitOptions.RemoveEmptyEntries) == StringSplitOptions.RemoveEmptyEntries;
            originalSpan = span;
            internalSpan = span;
            originalCount = count;
            this.count = count;
            Current = default;
            endReached = false;

            if (multiStringSeparator)
            {
#if NET5_0_OR_GREATER
                this.trimEntries =
 (splitOptions & StringSplitOptions.TrimEntries) == StringSplitOptions.TrimEntries && this.separators.Length > 0;
                this.firstIteration = true;
#endif
            }
            else
            {
#if NET5_0_OR_GREATER
                this.trimEntries = (splitOptions & StringSplitOptions.TrimEntries) == StringSplitOptions.TrimEntries;
                this.firstIteration = true;
#endif
            }
        }

        /// <summary>
        /// Gets the current item.
        /// </summary>
        public ReadOnlySpan<char> Current { get; private set; }

        /// <summary>
        /// Gets the Enumerator.
        /// </summary>
        /// <returns><see cref="StringSplitEnumerator"/>.</returns>
        public readonly StringSplitEnumerator GetEnumerator() => this;

        /// <summary>
        /// Advances to the next item.
        /// </summary>
        /// <returns><see langword="bool"/> indicating if there was another item.</returns>
        public bool MoveNext()
        {
            // we were passed a count of 0 and should return an empty enumerator.
            if (endReached || count == 0)
            {
                return false;
            }

            if (count == 1)
            {
                return CalculateFinalItem();
            }

            if (!multiStringSeparator && separator.IsEmpty)
            {
                Current = internalSpan;
                internalSpan = [];
                endReached = true;

                return NextSectionFound();
            }

            while (true)
            {
                (int separatorIndex, int separatorLength) = GetNextSeparatorAndLength();

                if (separatorIndex < 0 || separatorLength < 0)
                {
                    Current = internalSpan;
                    internalSpan = [];
                    endReached = true;

#if NET5_0_OR_GREATER
                    if (this.trimEntries && (!this.firstIteration || !this.multiStringSeparator))
                    {
                        this.Current = this.Current.Trim();
                    }

                    this.firstIteration = false;
#endif

                    return !removeEmptyEntries || !Current.IsEmpty;
                }
                else
                {
                    Current = internalSpan.Slice(0, separatorIndex);
                    internalSpan = internalSpan.Slice(separatorIndex + separatorLength);
#if NET5_0_OR_GREATER
                    this.firstIteration = false;
#endif
                    if (NextSectionFound())
                    {
                        --count;

                        return true;
                    }
                }
            }
        }

        /// <summary>
        /// Gets the count of elements returned by the current <see cref="StringSplitEnumerator"/>.
        /// This method doesn't modify the current <see cref="StringSplitEnumerator"/> and starts at the beginning.
        /// </summary>
        /// <returns>A count of the results.</returns>
        internal readonly int Count()
        {
            int count = 0;
            StringSplitEnumerator countEnumerator = new(originalSpan, separator, separators, multiStringSeparator, originalCount,
                splitOptions);

            while (countEnumerator.MoveNext())
            {
                ++count;
            }

            return count;
        }

        /// <summary>
        /// Gets the first element returned by the current <see cref="StringSplitEnumerator"/>.
        /// This method doesn't modify the current <see cref="StringSplitEnumerator"/> and starts at the beginning.
        /// </summary>
        /// <returns>The first result or throws if there are none.</returns>
        internal readonly ReadOnlySpan<char> First()
        {
            StringSplitEnumerator firstEnumerator = new(originalSpan, separator, separators, multiStringSeparator, originalCount,
                splitOptions);

            if (!firstEnumerator.MoveNext())
            {
                throw new InvalidOperationException();
            }

            return firstEnumerator.Current;
        }

        /// <summary>
        /// Gets the last element returned by the current <see cref="StringSplitEnumerator"/>.
        /// This method doesn't modify the current <see cref="StringSplitEnumerator"/> and starts at the beginning.
        /// </summary>
        /// <returns>The last result or throws if there are none.</returns>
        internal readonly ReadOnlySpan<char> Last()
        {
            StringSplitEnumerator lastEnumerator = new(originalSpan, separator, separators, multiStringSeparator, originalCount,
                splitOptions);

            ReadOnlySpan<char> result = [];
            bool anyFound = false;
            while (lastEnumerator.MoveNext())
            {
                anyFound = true;
                result = lastEnumerator.Current;
            }

            if (!anyFound)
            {
                throw new InvalidOperationException();
            }

            return result;
        }

        /// <summary>
        /// Converts the current <see cref="StringSplitEnumerator"/> to an array of <see cref="string"/>.
        /// This method doesn't modify the current <see cref="StringSplitEnumerator"/> and starts at the beginning.
        /// </summary>
        /// <returns>The array of <see cref="string"/>.</returns>
        internal readonly string[] ToArray()
        {
            int count = Count();

            if (count == 0)
            {
                return [];
            }

            StringSplitEnumerator toArrayEnumerator = new(originalSpan, separator, separators, multiStringSeparator,
                originalCount, splitOptions);

            string[] result = new string[count];
            for (int i = 0; i < result.Length && toArrayEnumerator.MoveNext(); ++i)
            {
                result[i] = toArrayEnumerator.Current.ToString();
            }

            return result;
        }

        /// <summary>
        /// Converts the current <see cref="StringSplitEnumerator"/> to a <see cref="List{T}"/> of <see cref="string"/>
        /// This method doesn't modify the current <see cref="StringSplitEnumerator"/> and starts at the beginning.
        /// </summary>
        /// <returns>A <see cref="List{T}"/> of <see cref="string"/>.</returns>
        internal readonly List<string> ToList()
        {
            int count = Count();

            List<string> result = new(count);
            if (count == 0)
            {
                return result;
            }

            StringSplitEnumerator toArrayEnumerator = new(originalSpan, separator, separators, multiStringSeparator,
                originalCount, splitOptions);

            foreach (ReadOnlySpan<char> item in toArrayEnumerator)
            {
                result.Add(item.ToString());
            }

            return result;
        }

        /// <summary>
        /// Resets the <see cref="StringSplitEnumerator"/> to its initial state.
        /// </summary>
        internal void Reset()
        {
            internalSpan = originalSpan;
            count = originalCount;
            Current = default;
            endReached = false;
        }

        private readonly (int Index, int SeparatorLength) GetNextSeparatorAndLength()
        {
            if (multiStringSeparator)
            {
                return FindFirstSeparator();
            }
            else
            {
                return (internalSpan.IndexOf(separator), separator.Length);
            }
        }

        private bool CalculateFinalItem()
        {
            if (removeEmptyEntries)
            {
                while (!internalSpan.IsEmpty)
                {
#if NET5_0_OR_GREATER
                    if (this.trimEntries)
                    {
                        this.internalSpan = this.internalSpan.TrimStart();
                    }
#endif

                    if (multiStringSeparator)
                    {
                        if (!AnyMultiStringSeparatorMatches())
                        {
                            break;
                        }
                    }
                    else
                    {
                        if (!internalSpan.StartsWith(separator, StringComparison.Ordinal))
                        {
                            break;
                        }

                        internalSpan = internalSpan.Slice(separator.Length);
                    }
                }
            }

            count = 0;
            endReached = true;
            Current = internalSpan;
            internalSpan = [];

            return NextSectionFound();
        }

        private bool AnyMultiStringSeparatorMatches()
        {
            if (UseWhitespaceAsSeparator())
            {
                if (char.IsWhiteSpace(internalSpan[0]))
                {
                    internalSpan = internalSpan.Slice(1);
                    return true;
                }
            }
            else
            {
                for (int i = 0; i < separators.Length; ++i)
                {
                    ReadOnlySpan<char> separatorSpan = separators[i].AsSpan();
                    if (!separatorSpan.IsEmpty && internalSpan.StartsWith(separatorSpan, StringComparison.Ordinal))
                    {
                        internalSpan = internalSpan.Slice(separatorSpan.Length);
                        return true;
                    }
                }
            }

            return false;
        }

        private readonly bool UseWhitespaceAsSeparator()
        {
            return separators.Length == 0;
        }

        private bool NextSectionFound()
        {
#if NET5_0_OR_GREATER
            if (this.trimEntries)
            {
                this.Current = this.Current.Trim();
            }
#endif

            return !removeEmptyEntries || !Current.IsEmpty;
        }

        private readonly (int Index, int SeparatorLength) FindFirstSeparator()
        {
            // string.Split treats an empty array as split on whitespace.
            if (UseWhitespaceAsSeparator())
            {
                return (internalSpan.IndexOfFirstWhitespaceCharacter(), 1);
            }
            else
            {
                int index = -1;
                int separatorLength = -1;

                for (int i = 0; i < separators.Length; ++i)
                {
                    string currentSeparator = separators[i];
                    if (!string.IsNullOrEmpty(currentSeparator))
                    {
                        int currentIndex = internalSpan.IndexOf(separators[i].AsSpan());
                        if (currentIndex >= 0 && (index < 0 || currentIndex < index))
                        {
                            separatorLength = currentSeparator.Length;
                            index = currentIndex;
                        }
                    }
                }

                return (index, separatorLength);
            }
        }
    }
}
