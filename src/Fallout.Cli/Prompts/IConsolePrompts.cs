using System;
using System.Threading.Tasks;

namespace Fallout.Cli.Prompts;

/// <summary>
/// Interactive console prompts and status rendering used by commands. Injected into commands
/// so their interaction logic can be unit-tested with a fake, instead of reaching the historical
/// <c>static</c> Spectre helpers on <c>Program</c>. The default implementation is
/// <see cref="SpectreConsolePrompts"/>.
/// </summary>
internal interface IConsolePrompts
{
    /// <summary>Renders a labelled, read-only input line (used to echo resolved setup choices).</summary>
    void ShowInput(string emoji, string title, string value);

    /// <summary>Renders a "<paramref name="title"/> completed!" banner.</summary>
    void ShowCompletion(string title);

    /// <summary>Clears the previously written console line.</summary>
    void ClearPreviousLine();

    /// <summary>Asks a yes/no question and returns the answer.</summary>
    bool PromptForConfirmation(string question);

    /// <summary>Asks for free-text input, optionally with a default value.</summary>
    string PromptForInput(string question, string defaultValue = null);

    /// <summary>Asks for a masked secret value, optionally enforcing a minimum length.</summary>
    string PromptForSecret(string title, int? minLength = null);

    /// <summary>Asks the user to pick one of the supplied <paramref name="choices"/>.</summary>
    T PromptForChoice<T>(string question, params (T Value, string Description)[] choices);

    /// <summary>
    /// Confirms, then runs <paramref name="action"/> with progress/completion rendering,
    /// reporting success or failure without throwing.
    /// </summary>
    void ConfirmExecution(string title, Action action);

    /// <summary>
    /// Asynchronous counterpart of <see cref="ConfirmExecution(string, Action)"/>: awaits the confirmed
    /// work instead of running it synchronously, so commands with genuinely async steps don't block.
    /// </summary>
    Task ConfirmExecutionAsync(string title, Func<Task> action);
}
