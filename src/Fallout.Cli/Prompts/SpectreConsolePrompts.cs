using System;
using System.Linq;
using System.Threading.Tasks;
using Fallout.Common;
using Fallout.Common.Utilities;
using Spectre.Console;

namespace Fallout.Cli.Prompts;

/// <summary>
/// <see cref="IConsolePrompts"/> backed by <see cref="AnsiConsole"/>. This is the single home for
/// the interactive prompt logic that previously lived as <c>static</c> helpers on <c>Program</c>.
/// </summary>
internal sealed class SpectreConsolePrompts : IConsolePrompts
{
    public void ShowInput(string emoji, string title, string value)
    {
        AnsiConsole.MarkupLine($":{emoji}:  {$"{title}:",-25} [turquoise2 bold]{value}[/]");
    }

    public void ShowCompletion(string title)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[bold green]{title} completed![/] :party_popper:");
    }

    public void ClearPreviousLine()
    {
        AnsiConsole.Cursor.MoveUp();
        System.Console.WriteLine(' '.Repeat(System.Console.WindowWidth));
        AnsiConsole.Cursor.MoveUp();
    }

    public bool PromptForConfirmation(string question)
    {
        return AnsiConsole.Confirm(question);
    }

    public string PromptForInput(string question, string defaultValue = null)
    {
        return AnsiConsole.Prompt(
            new TextPrompt<string>(question)
                .DefaultValue(defaultValue));
    }

    public string PromptForSecret(string title, int? minLength = null)
    {
        Assert.False(title.EndsWith(':'));

        return AnsiConsole.Prompt(
            new TextPrompt<string>($"{title}:")
                .Secret()
                .Validate(x => minLength == null || x.Length >= minLength,
                    message: $"Secret must be at least {minLength} characters long"));
    }

    public T PromptForChoice<T>(string question, params (T Value, string Description)[] choices)
    {
        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<T>()
                .Title(question)
                .HighlightStyle(new Style(Color.Turquoise2))
                .UseConverter(x => choices.Single(y => Equals(x, y.Value)).Description)
                .AddChoices(choices.Select(x => x.Value)));
        return choice;
    }

    public void ConfirmExecution(string title, Action action)
    {
        Assert.False(title.EndsWith('?'));

        var confirmation = PromptForConfirmation($"{title}?");
        ClearPreviousLine();

        if (confirmation)
        {
            AnsiConsole.MarkupLine($":hourglass_not_done:  {title} ...");
            try
            {
                action.Invoke();
            }
            catch (Exception)
            {
                confirmation = false;
                title = $"{title} (failed)";
            }
            finally
            {
                ClearPreviousLine();
            }
        }

        var (emoji, color) = confirmation ? ("check_mark", "green") : ("multiply", "red");
        AnsiConsole.MarkupLine($"[{color}]:{emoji}:[/]  {title}");
    }

    public async Task ConfirmExecutionAsync(string title, Func<Task> action)
    {
        Assert.False(title.EndsWith('?'));

        var confirmation = PromptForConfirmation($"{title}?");
        ClearPreviousLine();

        if (confirmation)
        {
            AnsiConsole.MarkupLine($":hourglass_not_done:  {title} ...");
            try
            {
                await action();
            }
            catch (Exception)
            {
                confirmation = false;
                title = $"{title} (failed)";
            }
            finally
            {
                ClearPreviousLine();
            }
        }

        var (emoji, color) = confirmation ? ("check_mark", "green") : ("multiply", "red");
        AnsiConsole.MarkupLine($"[{color}]:{emoji}:[/]  {title}");
    }
}
