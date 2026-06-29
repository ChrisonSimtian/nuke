using Fallout.Common.Utilities;
using Xunit;
using System;
using System.Collections.Generic;
using System.Drawing;
using Fallout.Build.Utilities;
using FluentAssertions;

namespace Fallout.Build.Tests.Utilities;

public class ConsoleUtilitySpecs
{
    private class MockConsole : IConsole
    {
        public int BufferWidth { get; set; } = 80;
        public int CursorLeft { get; set; }
        public int CursorTop { get; set; }
        public Queue<ConsoleKeyInfo> Keys { get; } = new();
        public void Write(string value, Color? color = null) { }
        public void WriteLine() { }
        public void WriteLine(string value, Color? color = null) { }
        public ConsoleKeyInfo ReadKey(bool intercept) => Keys.Count > 0
            ? Keys.Dequeue()
            : new ConsoleKeyInfo('a', ConsoleKey.A, false, false, false);
    }

    [Theory]
    [ClassData(typeof(ValidInputData))]
    public void Valid_characters_are_accepted_as_input(ConsoleKey key, char keyChar, bool expected)
    {
        var keyInfo = new ConsoleKeyInfo(keyChar, key, false, false, false);
        var result = ConsoleUtility.IsValidInputKey(keyInfo);
        result.Should().Be(expected);
    }

    [Fact]
    public void Prompt_returns_default_value_when_interrupted()
    {
        // Arrange
        var mockConsole = new MockConsole();
        mockConsole.Keys.Enqueue(new ConsoleKeyInfo('\0', ConsoleKey.F8, false, false, false));
        ConsoleUtility.ConsoleWrapper = mockConsole;
        ConsoleUtility.IsInterrupted = false;

        // Act
        var result = ConsoleUtility.PromptForInput("Question", "Default");

        // Assert
        result.Should().Be("Default");
    }
}
