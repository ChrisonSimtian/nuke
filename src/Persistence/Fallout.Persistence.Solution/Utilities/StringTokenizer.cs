// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace Fallout.Persistence.Solution.Utilities;

/// <summary>
/// Similar to original parser StringTokenizer class. With slight additions.
/// </summary>
internal ref struct StringTokenizer
{
    private StringSpan state;

    internal StringTokenizer(string? str)
    {
        IsNull = str is null;
        StringLine = str ?? string.Empty;
        CurrentPos = 0;
        state = StringLine.AsSpan();
    }

    internal bool IsNull { get; }

    internal readonly bool IsEmpty => state.IsEmpty;

    // First char in remaining line, '\0' if empty.
    internal readonly char CurrentChar => state.IsEmpty ? '\0' : state[0];

    internal readonly StringSpan Current => state;

    internal int CurrentPos { get; private set; }

    internal string StringLine { get; }

    // charact in given position, or 0 if index is out of bounds.
    internal readonly char this[int index] => index >= 0 && index < state.Length ? state[index] : '\0';

    // both use the same semantic as VS parser, with minor reduction in slicing and dicing ...
    internal StringSpan NextToken(string delimiters)
    {
        if (IsEmpty)
        {
            return StringSpan.Empty;
        }

        int skipLeading = 0;
        while (skipLeading < state.Length && delimiters.Contains(state[skipLeading]))
        {
            skipLeading++;
        }

        int nextDelimiter = skipLeading;
        while (nextDelimiter < state.Length && delimiters.IndexOf(state[nextDelimiter]) < 0)
        {
            nextDelimiter++;
        }

        return GetNextToken(skipLeading, nextDelimiter);
    }

    internal StringSpan NextTokenKeep(char delimiter)
    {
        if (IsEmpty)
        {
            return StringSpan.Empty;
        }

        int skipLeading = 0;
        while (skipLeading < state.Length && state[skipLeading] == delimiter)
        {
            skipLeading++;
        }

        int nextDelimiter = skipLeading;
        while (nextDelimiter < state.Length && state[nextDelimiter] != delimiter)
        {
            nextDelimiter++;
        }

        StringSpan result = nextDelimiter > skipLeading
            ? state.Slice(skipLeading, nextDelimiter - skipLeading)
            : StringSpan.Empty;

        CurrentPos += nextDelimiter;
        state = state.Slice(nextDelimiter);

        return result;
    }

    internal StringSpan NextToken(char delimiter)
    {
        if (IsEmpty)
        {
            return StringSpan.Empty;
        }

        int skipLeading = 0;
        while (skipLeading < state.Length && state[skipLeading] == delimiter)
        {
            skipLeading++;
        }

        int nextDelimiter = skipLeading;
        while (nextDelimiter < state.Length && state[nextDelimiter] != delimiter)
        {
            nextDelimiter++;
        }

        return GetNextToken(skipLeading, nextDelimiter);
    }

    internal void TrimStart()
    {
        int old = state.Length;
        state = state.TrimStart();
        CurrentPos += old - state.Length;
    }

    internal readonly bool StartsWithAt(string match, int pos)
    {
        if (string.IsNullOrEmpty(match) || state.Length < match.Length + pos)
        {
            return false;
        }

        return state.Slice(pos).StartsWith(match);
    }

    internal readonly bool StartsWith(string match)
    {
        if (string.IsNullOrEmpty(match) || state.Length < match.Length)
        {
            return false;
        }

        return state.StartsWith(match);
    }

    // will advance tokenizer if it starts with the specified prefix, but only if it is followed by whitesapce or end of line.
    internal bool SliceIfStartsWithAndEmptyAfter(string prefix) =>
        this[prefix.Length].IsWhiteSpace() && SliceIfStartsWith(prefix);

    // will advance tokenizer if it starts with the specified prefix.
    internal bool SliceIfStartsWith(string prefix)
    {
        if (StartsWith(prefix))
        {
            Slice(prefix.Length);
            return true;
        }

        return false;
    }

    internal void Slice(int start)
    {
        if (start == 0)
        {
            return;
        }

        if (start >= 0 && start < state.Length)
        {
            CurrentPos += start;
            state = state.Slice(start);
        }
        else
        {
            CurrentPos += state.Length;
            state = StringSpan.Empty;
        }
    }

    internal void TrimStartAndSkip(char c1, char c2 = (char)0)
    {
        if (IsEmpty)
        {
            return;
        }

        int skipLeading = 0;
        while (skipLeading < state.Length)
        {
            char c = state[skipLeading];
            if (!char.IsWhiteSpace(c) && c != c1 && c != c2)
            {
                break;
            }

            skipLeading++;
        }

        Slice(skipLeading);
    }

    internal void Skip(string toSkip)
    {
        if (IsEmpty || string.IsNullOrEmpty(toSkip))
        {
            return;
        }

        int skipLeading = 0;
        while (skipLeading < state.Length && toSkip.Contains(state[skipLeading]))
        {
            skipLeading++;
        }

        Slice(skipLeading);
    }

    internal void SkipAll() => Slice(-1);

    internal void Reset()
    {
        CurrentPos = 0;
        state = StringLine.AsSpan();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private StringSpan GetNextToken(int skipLeading, int nextDelimiter)
    {
        StringSpan result = nextDelimiter > skipLeading
            ? state.Slice(skipLeading, nextDelimiter - skipLeading)
            : StringSpan.Empty;

        // note +1 capture the case when a delimiter is the last character. The code would always remove the closing delimiter if any.
        nextDelimiter++;
        if (nextDelimiter < state.Length)
        {
            CurrentPos += nextDelimiter;
            state = state.Slice(nextDelimiter);
        }
        else
        {
            CurrentPos += state.Length;
            state = StringSpan.Empty;
        }

        return result;
    }
}
