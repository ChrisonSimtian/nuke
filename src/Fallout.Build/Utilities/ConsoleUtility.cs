using System;
using System.Drawing;
using System.Linq;
using System.Text;
using Fallout.Build.Utilities;

namespace Fallout.Common.Utilities;

public class ConsoleUtility
{
    public static IConsole ConsoleWrapper { get; set; } = new SystemConsole();

    private static int BufferWidth => ConsoleWrapper.BufferWidth;

    private static string Default => "[default: {0}]";

    private static string Confirmed => "¬";

    private static string Selected => "»";

    private static string Unselected => " ";

    private const ConsoleKey ConfirmationKey = ConsoleKey.Enter;
    private const ConsoleKey InterruptKey = ConsoleKey.F8;

    internal static bool IsInterrupted;
    private static readonly char[] AllowedSpecialCharacters = ['.', '/', '\\', '_', '-'];

    internal static bool IsValidInputKey(ConsoleKeyInfo key) =>
        key.Key is >= ConsoleKey.A and <= ConsoleKey.Z
        || key.Key is >= ConsoleKey.D0 and <= ConsoleKey.D9
        || AllowedSpecialCharacters.Any(x => x == key.KeyChar)
        || char.IsLetterOrDigit(key.KeyChar);

    // ReSharper disable once CognitiveComplexity
    public static string PromptForInput(string question, string defaultValue)
    {
        if (IsInterrupted)
            return defaultValue;

        Host.Information(question);

        ConsoleKeyInfo key;
        var input = new StringBuilder();
        do
        {
            ConsoleWrapper.CursorLeft = 0;
            ConsoleWrapper.WriteLine(Selected.PadRight(BufferWidth), Color.DeepSkyBlue);
            ConsoleWrapper.CursorTop--;
            ConsoleWrapper.CursorLeft = 3;

            if (input.Length > 0)
            {
                ConsoleWrapper.Write(input.ToString());
            }
            else if (defaultValue != null)
            {
                ConsoleWrapper.Write($"               {string.Format(Default, defaultValue)}", Color.DarkGray);
                ConsoleWrapper.CursorLeft = 3;
            }

            key = ConsoleWrapper.ReadKey(intercept: true);
            if (IsValidInputKey(key))
                input.Append(key.KeyChar);
            else if (key.Key == ConsoleKey.Backspace && input.Length > 0)
                input.Remove(input.Length - 1, length: 1);
            else if (key.Key == InterruptKey)
                IsInterrupted = true;
        } while (key.Key is not (ConfirmationKey or InterruptKey));

        var result = input.Length > 0 ? input.ToString() : defaultValue;
        ConsoleWrapper.CursorLeft = 0;
        ConsoleWrapper.WriteLine($"{Confirmed}  {result ?? "<null>"}".PadRight(BufferWidth), Color.Lime);
        return result;
    }

    // ReSharper disable once CognitiveComplexity
    public static T PromptForChoice<T>(string question, params (T Value, string Description)[] options)
    {
        if (IsInterrupted)
            return options.First().Value;

        var selection = 0;
        ConsoleKey key;

        Host.Information(question);
        do
        {
            for (var i = 0; i < options.Length; i++)
            {
                var option = options[i];
                var selected = i == selection;
                var prefix = selected ? Selected : Unselected;
                var color = selected ? Color.DeepSkyBlue : Color.DarkGray;
                ConsoleWrapper.WriteLine($"{prefix}  {option.Description}", color);
            }

            key = ConsoleWrapper.ReadKey(intercept: true).Key;
            if (key == ConsoleKey.UpArrow)
                selection--;
            else if (key == ConsoleKey.DownArrow)
                selection++;
            else if (key == InterruptKey)
                IsInterrupted = true;

            selection = Math.Max(val1: 0, Math.Min(options.Length - 1, selection));

            ConsoleWrapper.CursorTop -= options.Length;
            foreach (var unused in options)
                ConsoleWrapper.WriteLine(' '.Repeat(BufferWidth));

            ConsoleWrapper.CursorTop -= options.Length;
        }
        while (key is not (ConfirmationKey or InterruptKey));

        ConsoleWrapper.WriteLine($"{Confirmed}  {options[selection].Description}", Color.Lime);

        return options[selection].Value;
    }

    // ReSharper disable once CognitiveComplexity
    public static string ReadSecret()
    {
        var secret = string.Empty;

        do
        {
            var key = ConsoleWrapper.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Backspace)
            {
                if (secret.Length > 0)
                {
                    var charsToRemove =
                        (key.Modifiers & ConsoleModifiers.Control) != 0 && !EnvironmentInfo.IsOsx ||
                        (key.Modifiers & ConsoleModifiers.Alt) != 0 && EnvironmentInfo.IsOsx
                            ? secret.Length
                            : 1;

                    var length = secret.Length - charsToRemove;
                    secret = secret[..length];
                    ConsoleWrapper.Write(string.Concat(Enumerable.Repeat("\b \b", charsToRemove)));
                }
            }
            else if (key.Key == ConsoleKey.Enter)
            {
                ConsoleWrapper.WriteLine();
                break;
            }
            else if (!char.IsControl(key.KeyChar))
            {
                secret += key.KeyChar;
                ConsoleWrapper.Write("*");
            }
        }
        while (true);

        return secret;
    }
}
