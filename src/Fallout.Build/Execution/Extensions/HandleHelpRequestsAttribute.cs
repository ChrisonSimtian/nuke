using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Fallout.Build.Execution.Extensions;
using Fallout.Common.Utilities;
using Fallout.Common.ValueInjection;

namespace Fallout.Common.Execution;

internal class HandleHelpRequestsAttribute : BuildExtensionAttributeBase, IOnBuildInitialized
{
    public void OnBuildInitialized(
        IReadOnlyCollection<ExecutableTarget> executableTargets,
        IReadOnlyCollection<ExecutableTarget> executionPlan)
    {
        if (Build.Help || executionPlan.Count == 0)
        {
            Host.Debug(GetTargetsText());
            Host.Debug(GetParametersText());
            Environment.Exit(exitCode: 0);
        }
    }

    public string GetTargetsText()
    {
        // The displayed FIELDS come from the same projection --describe emits, so what the two
        // views say about a target cannot drift (#642). ORDER stays with the declarations, for
        // both the target list and each dependency line: the model sorts ordinally for
        // determinism, whereas declaration order carries the pipeline reading a human wants
        // (Restore, Compile, Test, Pack). Rendering the sorted lists here would silently
        // alphabetize --help, which this deliberately does not do.
        var model = BuildGraphUtility.GetModel(Build.ExecutableTargets, falloutVersion: null)
            .Targets.ToDictionary(x => x.Name, StringComparer.Ordinal);

        var builder = new StringBuilder();

        var longestTargetName = Build.ExecutableTargets.Select(x => x.Name.Length).OrderByDescending(x => x).First();
        var padRightTargets = Math.Max(longestTargetName, val2: 20);
        builder.AppendLine("Targets (with their direct dependencies):");
        builder.AppendLine();
        foreach (var target in Build.ExecutableTargets)
        {
            var projected = model[target.Name];
            if (!projected.Listed)
                continue;

            var dependencies = target.ExecutionDependencies.Count > 0
                ? $" -> {target.ExecutionDependencies.Select(x => x.Name).JoinCommaSpace()}"
                : string.Empty;
            var targetEntry = projected.Name + (projected.Default ? " (default)" : string.Empty);
            builder.AppendLine($"  {targetEntry.PadRight(padRightTargets)}{dependencies}");
            if (!string.IsNullOrWhiteSpace(projected.Description))
                builder.AppendLine($"    {projected.Description}");
        }

        return builder.ToString();
    }

    // Console.BufferWidth throws IOException ("The handle is invalid") when standard output has no
    // console behind it — a redirected pipe, a file, or a CI agent without a console — which used to
    // abort --help outright, printing the targets and then dying before the parameters (#616). The
    // wrap width is cosmetic, so an unavailable console falls back to the 90-column cap below.
    private static int GetBufferWidth()
    {
        try
        {
            return Console.BufferWidth;
        }
        catch (IOException)
        {
            return int.MaxValue;
        }
    }

    public string GetParametersText()
    {
        var defaultTargets = Build.ExecutableTargets.Where(x => x.IsDefault).Select(x => x.Name).ToList();
        var builder = new StringBuilder();

        // Same projection as --describe (#642) for the displayed name and description. As above,
        // ORDER stays with GetParameterMembers (culture-ordered by member name), not the model's
        // ordinal-by-dashed-name, so --help's listing is unchanged.
        var members = ValueInjectionUtility.GetParameterMembers(Build.GetType(), includeUnlisted: false);
        var model = BuildGraphUtility.GetParameterModels(members)
            .ToDictionary(x => x.Name, StringComparer.Ordinal);
        var parameters = members
            .Select(x => (Member: x, Model: model[ParameterService.GetParameterDashedName(x)]))
            .ToList();
        var padRightParameter = Math.Max(parameters.Max(x => x.Model.Name.Length), val2: 16);
        var bufferWidth = GetBufferWidth();

        List<string> SplitLines(string text)
        {
            var words = new Queue<string>(text.Split(' ').ToList());
            var lines = new List<string> { string.Empty };
            foreach (var word in words)
            {
                var nextLength = padRightParameter + 6 + lines.Last().Length + word.Length;
                if (nextLength >= bufferWidth || nextLength > 90)
                    lines.Add(string.Empty);

                lines[lines.Count - 1] = $"{lines.Last()} {word}";
            }

            return lines;
        }

        void PrintParameter((MemberInfo Member, BuildGraphUtility.ParameterModel Model) parameter)
        {
            var description = SplitLines(
                // TODO: remove
                parameter.Model.Description
                    ?.Replace("{default_target}", defaultTargets.Count > 0 ? defaultTargets.JoinCommaSpace() : "<none>")
                    .TrimEnd(".").Append(".")
                ?? "<no description>");
            builder.AppendLine($"  --{parameter.Model.Name.PadRight(padRightParameter)}  {description.First()}");
            foreach (var line in description.Skip(count: 1))
                builder.AppendLine($"{' '.Repeat(padRightParameter + 6)}{line}");
        }

        builder.AppendLine("Parameters:");

        // Type identity, not the model's DeclaredIn name: a user type merely *called* FalloutBuild
        // in another namespace must not have its parameters filed under the built-in block.
        var customParameters = parameters.Where(x => x.Member.DeclaringType != typeof(FalloutBuild)).ToList();
        if (customParameters.Count > 0)
            builder.AppendLine();
        customParameters.ForEach(PrintParameter);

        builder.AppendLine();

        var inheritedParameters = parameters.Where(x => x.Member.DeclaringType == typeof(FalloutBuild)).ToList();
        inheritedParameters.ForEach(PrintParameter);

        return builder.ToString();
    }
}
