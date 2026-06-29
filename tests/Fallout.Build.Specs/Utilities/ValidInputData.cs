using System.Collections;
using System.Collections.Generic;
using System;
using Xunit;

namespace Fallout.Build.Tests.Utilities;

public class ValidInputData : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator()
    {
        yield return [ConsoleKey.A, 'a', true];
        yield return [ConsoleKey.Z, 'z', true ];
        yield return [ConsoleKey.D0, '0', true ];
        yield return [ConsoleKey.D9, '9', true ];
        yield return [ConsoleKey.Enter, '\r', false ];
        yield return [ConsoleKey.Backspace, '\b', false ];
        yield return [ConsoleKey.F8, '\0', false ];
        yield return [ConsoleKey.Spacebar, ' ', false ];
        yield return [ConsoleKey.OemPeriod, '.', true ];
        yield return [ConsoleKey.Divide, '/', true ];
        yield return [ConsoleKey.OemMinus, '-', true ];
        yield return [ConsoleKey.NoName, 'ä', true ];
        yield return [ConsoleKey.NoName, 'ö', true ];
        yield return [ConsoleKey.NoName, '中', true ];
        yield return [ConsoleKey.NoName, 'д', true ];
        yield return [ConsoleKey.NoName, 'α', true ];
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
