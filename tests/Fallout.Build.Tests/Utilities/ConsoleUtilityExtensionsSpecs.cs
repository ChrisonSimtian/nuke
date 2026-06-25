using Fallout.Common.Utilities;
using Xunit;
using System;
using FluentAssertions;

namespace Fallout.Build.Tests.Utilities;

public class ConsoleUtilityExtensionsSpecs
{
    [Theory]
    [InlineData(ConsoleKey.A, 'a', true)]
    [InlineData(ConsoleKey.Z, 'z', true)]
    [InlineData(ConsoleKey.D0, '0', true)]
    [InlineData(ConsoleKey.D9, '9', true)]
    [InlineData(ConsoleKey.Enter, '\r', false)]
    [InlineData(ConsoleKey.Backspace, '\b', false)]
    [InlineData(ConsoleKey.F8, '\0', false)]
    [InlineData(ConsoleKey.Spacebar, ' ', false)]
    [InlineData(ConsoleKey.OemPeriod, '.', true)]
    [InlineData(ConsoleKey.Divide, '/', true)]
    [InlineData(ConsoleKey.OemMinus, '-', true)]
    [InlineData(ConsoleKey.NoName, 'ä', true)]
    [InlineData(ConsoleKey.NoName, 'ö', true)]
    [InlineData(ConsoleKey.NoName, '中', true)]
    [InlineData(ConsoleKey.NoName, 'д', true)]
    [InlineData(ConsoleKey.NoName, 'α', true)]
    public void Valid_characters_are_accepted_as_input(ConsoleKey key, char keyChar, bool expected)
    {
        var keyInfo = new ConsoleKeyInfo(keyChar, key, false, false, false);
        var result = keyInfo.IsValidInputKey();
        result.Should().Be(expected);
    }
}
