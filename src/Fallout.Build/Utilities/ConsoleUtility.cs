using System;
using System.Linq;
using Spectre.Console;

namespace Fallout.Common.Utilities;

public class ConsoleUtility
{
    public static string PromptForInput(string question, string defaultValue)
    {
        var prompt = new TextPrompt<string>(question);
        if (defaultValue != null)
            prompt.DefaultValue(defaultValue);
        else
            prompt.AllowEmpty();

        return AnsiConsole.Prompt(prompt);
    }

    public static T PromptForChoice<T>(string question, params (T Value, string Description)[] options)
    {
        return AnsiConsole.Prompt(
            new SelectionPrompt<T>()
                .Title(question)
                .HighlightStyle(new Style(Color.DeepSkyBlue1))
                .UseConverter(x => options.Single(y => Equals(x, y.Value)).Description)
                .AddChoices(options.Select(x => x.Value)));
    }

    public static string ReadSecret()
    {
        return AnsiConsole.Prompt(
            new TextPrompt<string>(string.Empty)
                .AllowEmpty()
                .Secret());
    }
}
