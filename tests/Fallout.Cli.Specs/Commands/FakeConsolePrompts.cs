using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fallout.Cli.Prompts;

namespace Fallout.Cli.Specs.Commands;

/// <summary>
/// Configurable <see cref="IConsolePrompts"/> test double for exercising command interaction logic
/// without a real console.
/// </summary>
internal sealed class FakeConsolePrompts : IConsolePrompts
{
    /// <summary>Answer returned by <see cref="PromptForConfirmation"/>.</summary>
    public bool ConfirmationResult { get; init; }

    /// <summary>When false, <see cref="ConfirmExecution"/> does not run its action (simulates a decline).</summary>
    public bool InvokeConfirmedActions { get; init; }

    /// <summary>Titles passed to <see cref="ShowCompletion"/>, in order.</summary>
    public List<string> Completions { get; } = new();

    public bool PromptForConfirmation(string question) => ConfirmationResult;
    public void ShowInput(string emoji, string title, string value) { }
    public void ShowCompletion(string title) => Completions.Add(title);
    public void ClearPreviousLine() { }
    public string PromptForInput(string question, string defaultValue = null) => defaultValue;
    public string PromptForSecret(string title, int? minLength = null) => string.Empty;
    public T PromptForChoice<T>(string question, params (T Value, string Description)[] choices) => default;

    public void ConfirmExecution(string title, Action action)
    {
        if (InvokeConfirmedActions)
            action();
    }

    public async Task ConfirmExecutionAsync(string title, Func<Task> action)
    {
        if (InvokeConfirmedActions)
            await action();
    }
}
