using System;
using System.Collections.Generic;
using System.Linq;
using Fallout.Common.Utilities;
using Fallout.Common.Utilities.Collections;

namespace Fallout.Common;

internal class ArgumentParser(IEnumerable<string> arguments)
{
    public static bool IsArgument(string value)
    {
        return value != null && value.StartsWith("-");
    }

    public static string GetArgumentMemberName(string name)
    {
        return name.Replace("-", string.Empty);
    }

    public static string GetParameterDashedName(string name)
    {
        return name.SplitCamelHumpsWithKnownWords().JoinDash().ToLowerInvariant();
    }

    private static string[] Parse(string arguments)
    {
        var inSingleQuotes = false;
        var inDoubleQuotes = false;
        var escaped = false;
        return arguments.Split((c, _) =>
                {
                    if (c == '\"' && !inSingleQuotes && !escaped)
                    {
                        inDoubleQuotes = !inDoubleQuotes;
                    }

                    if (c == '\'' && !inDoubleQuotes && !escaped)
                    {
                        inSingleQuotes = !inSingleQuotes;
                    }

                    escaped = c == '\\' && !escaped;

                    return c == ' ' && !(inDoubleQuotes || inSingleQuotes);
                },
                includeSplitCharacter: true)
            .Select(x => x.Trim().TrimMatchingDoubleQuotes().TrimMatchingSingleQuotes().Replace("\\\"", "\"")
                .Replace("\\\'", "'"))
            .Where(x => !string.IsNullOrEmpty(x))
            .ToArray();
    }

    private readonly string[] arguments = arguments.ToArray();

    public ArgumentParser(string arguments)
        : this(Parse(arguments))
    {
    }

    public IReadOnlyList<string> Arguments => arguments;

    public bool HasArgument(string argumentName)
    {
        return GetArgumentIndex(argumentName) != -1;
    }

    public object GetNamedArgument(string argumentName, Type destinationType, char? separator = null)
    {
        var index = GetArgumentIndex(argumentName);
        if (index == -1)
        {
            return destinationType.GetDefaultValue();
        }

        var values = arguments.Skip(index + 1).TakeUntil(IsArgument).ToArray();
        return ConvertArgument(argumentName, values, destinationType, separator);
    }

    public object GetPositionalArgument(int position, Type destinationType, char? separator = null)
    {
        var positionalArgumentsCount = arguments.TakeUntil(IsArgument).Count();
        if (position < 0)
        {
            position = positionalArgumentsCount + position % positionalArgumentsCount;
        }

        if (positionalArgumentsCount <= position)
        {
            return null;
        }

        return ConvertArgument(
            $"$positional[{position}]",
            new[]
            {
                arguments[position]
            },
            destinationType,
            separator);
    }

    public object GetAllPositionalArguments(Type destinationType, char? separator = null)
    {
        var positionalArguments = Arguments.TakeUntil(IsArgument).ToArray();
        if (positionalArguments.Length == 0)
        {
            return destinationType.GetDefaultValue();
        }

        return ConvertArgument(
            "$all-positional",
            positionalArguments,
            destinationType,
            separator);
    }

    private int GetArgumentIndex(string argumentName)
    {
        var argumentMemberName = GetArgumentMemberName(argumentName);
        return Array.FindLastIndex(arguments,
            x => IsArgument(x) && GetArgumentMemberName(x).EqualsOrdinalIgnoreCase(argumentMemberName));
    }

    private object ConvertArgument(
        string argumentName,
        string[] values,
        Type destinationType,
        char? separator)
    {
        Assert.True(values.Length == 1 || !separator.HasValue || values.All(x => !x.Contains(separator.Value)),
            $"Argumenet '{argumentName}' with value [ {values.JoinCommaSpace()} ] cannot be split with separator '{separator}'");

        values = separator.HasValue && values.Any(x => x.Contains(separator.Value))
            ? values.SingleOrDefault()?.Split(separator.Value) ?? new string[0]
            : values;

        try
        {
            return ConvertValues(argumentName, values, destinationType);
        }
        catch (Exception ex)
        {
            Assert.Fail(
                new[]
                    {
                        ex.Message,
                        "Arguments were:"
                    }
                    .Concat(arguments.Select((x, i) => $"  [{i}] = {x}"))
                    .JoinNewLine());

            // ReSharper disable once HeuristicUnreachableCode
            return null;
        }
    }

    private object ConvertValues(string argumentName, IReadOnlyCollection<string> values, Type destinationType)
    {
        try
        {
            return ReflectionUtility.Convert(values, destinationType, booleanDefault: true);
        }
        catch (Exception ex)
        {
            Assert.Fail(new[]
            {
                $"Resolving argument '{argumentName}' failed.",
                ex.Message
            }.JoinNewLine());

            // ReSharper disable once HeuristicUnreachableCode
            return null;
        }
    }
}
