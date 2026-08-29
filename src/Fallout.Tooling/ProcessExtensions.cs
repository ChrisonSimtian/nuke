using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Fallout.Common.Utilities;

namespace Fallout.Common.Tooling;

[DebuggerStepThrough]
[DebuggerNonUserCode]
public static class ProcessExtensions
{
    public static IProcess AssertWaitForExit(
        this IProcess process)
    {
        Assert.True(process != null);
        Assert.True(process.WaitForExit());
        return process;
    }

    public static IProcess AssertZeroExitCode(
        this IProcess process)
    {
        process.AssertWaitForExit();

        if (process.ExitCode != 0)
        {
            throw new ProcessException(process);
        }

        return process;
    }

    public static IReadOnlyCollection<Output> EnsureOnlyStd(this IReadOnlyCollection<Output> output)
    {
        foreach (var o in output)
        {
            Assert.True(o.Type == OutputType.Std);
        }

        return output;
    }

    public static string StdToText(this IEnumerable<Output> output)
    {
        return output.Where(x => x.Type == OutputType.Std)
            .Select(x => x.Text)
            .JoinNewLine();
    }

    public static T StdToJson<T>(this IEnumerable<Output> output)
    {
        return JsonSerializer.Deserialize<T>(output.StdToText(), JsonExtensions.DefaultSerializerOptions);
    }

    public static JsonObject StdToJson(this IEnumerable<Output> output)
    {
        return JsonNode.Parse(output.StdToText())?.AsObject();
    }
}
