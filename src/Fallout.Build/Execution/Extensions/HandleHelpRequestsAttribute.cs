using System;
using System.Collections.Generic;
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
        // Every displayed field comes from the same projection --describe emits, so the human and
        // machine views cannot drift (#642). Only the iteration order is ours: the model sorts
        // ordinally, while the help listing keeps declaration order, which usually mirrors the
        // pipeline (Restore, Compile, Test, Pack) and reads better than alphabetical.
        var model = BuildGraphUtility.GetModel(Build.ExecutableTargets, falloutVersion: null)
            .Targets.ToDictionary(x => x.Name, StringComparer.Ordinal);

        var builder = new StringBuilder();

        var longestTargetName = Build.ExecutableTargets.Select(x => x.Name.Length).OrderByDescending(x => x).First();
        var padRightTargets = Math.Max(longestTargetName, val2: 20);
        builder.AppendLine("Targets (with their direct dependencies):");
        builder.AppendLine();
        foreach (var target in Build.ExecutableTargets.Select(x => model[x.Name]).Where(x => x.Listed))
        {
            var dependencies = target.DependsOn.Count > 0
                ? $" -> {target.DependsOn.JoinCommaSpace()}"
                : string.Empty;
            var targetEntry = target.Name + (target.Default ? " (default)" : string.Empty);
            builder.AppendLine($"  {targetEntry.PadRight(padRightTargets)}{dependencies}");
            if (!string.IsNullOrWhiteSpace(target.Description))
                builder.AppendLine($"    {target.Description}");
        }

        return builder.ToString();
    }

    public string GetParametersText()
    {
        var defaultTargets = Build.ExecutableTargets.Where(x => x.IsDefault).Select(x => x.Name).ToList();
        var builder = new StringBuilder();

        // Same projection as --describe (#642): name, description and declaring type all come from
        // the model rather than being re-derived from reflection here.
        var members = ValueInjectionUtility.GetParameterMembers(Build.GetType(), includeUnlisted: false);
        var parameters = BuildGraphUtility
            .GetModel(Build.ExecutableTargets, falloutVersion: null, members).Parameters;
        var padRightParameter = Math.Max(parameters.Max(x => x.Name.Length), val2: 16);

        List<string> SplitLines(string text)
        {
            var words = new Queue<string>(text.Split(' ').ToList());
            var lines = new List<string> { string.Empty };
            foreach (var word in words)
            {
                var nextLength = padRightParameter + 6 + lines.Last().Length + word.Length;
                if (nextLength >= Console.BufferWidth || nextLength > 90)
                    lines.Add(string.Empty);

                lines[lines.Count - 1] = $"{lines.Last()} {word}";
            }

            return lines;
        }

        void PrintParameter(BuildGraphUtility.ParameterModel parameter)
        {
            var description = SplitLines(
                // TODO: remove
                parameter.Description
                    ?.Replace("{default_target}", defaultTargets.Count > 0 ? defaultTargets.JoinCommaSpace() : "<none>")
                    .TrimEnd(".").Append(".")
                ?? "<no description>");
            builder.AppendLine($"  --{parameter.Name.PadRight(padRightParameter)}  {description.First()}");
            foreach (var line in description.Skip(count: 1))
                builder.AppendLine($"{' '.Repeat(padRightParameter + 6)}{line}");
        }

        builder.AppendLine("Parameters:");

        var customParameters = parameters.Where(x => x.DeclaredIn != nameof(FalloutBuild)).ToList();
        if (customParameters.Count > 0)
            builder.AppendLine();
        customParameters.ForEach(PrintParameter);

        builder.AppendLine();

        var inheritedParameters = parameters.Where(x => x.DeclaredIn == nameof(FalloutBuild)).ToList();
        inheritedParameters.ForEach(PrintParameter);

        return builder.ToString();
    }
}
