using System;
using System.Collections.Generic;
using System.Linq;
using Fallout.Common.Execution;
using Fallout.Common.Utilities.Collections;

namespace Fallout.Common.Tooling;

public class VerbosityMappingAttribute : BuildExtensionAttributeBase, IOnBuildInitialized
{
    private readonly Type targetType;

    public VerbosityMappingAttribute(Type targetType)
    {
        this.targetType = targetType;
    }

    public string Quiet { get; set; }
    public string Minimal { get; set; }
    public string Information { get; set; }
    public string Normal { get; set; }
    public string Verbose { get; set; }

    public void OnBuildInitialized(
        IReadOnlyCollection<ExecutableTarget> executableTargets,
        IReadOnlyCollection<ExecutableTarget> executionPlan)
    {
        object GetMappedValue(string name)
            => targetType
                .GetField(name)
                .NotNull($"Type {targetType} doesn't have a field {name}.")
                .GetValue(obj: null);

        if (Quiet != null)
            VerbosityMapping.Mappings.Add(targetType, (Verbosity.Quiet, GetMappedValue(Quiet)));
        if (Minimal != null)
            VerbosityMapping.Mappings.Add(targetType, (Verbosity.Minimal, GetMappedValue(Minimal)));
        if (Information != null)
            VerbosityMapping.Mappings.Add(targetType, (Verbosity.Information, GetMappedValue(Information)));
        if (Normal != null)
            VerbosityMapping.Mappings.Add(targetType, (Verbosity.Normal, GetMappedValue(Normal)));
        if (Verbose != null)
            VerbosityMapping.Mappings.Add(targetType, (Verbosity.Verbose, GetMappedValue(Verbose)));
    }
}

internal static class VerbosityMapping
{
    public static readonly LookupTable<Type, (Verbosity Verbosity, object MappedVerbosity)> Mappings = new();

    public static void Apply(ToolOptions options)
    {
        foreach (var property in options.GetType().GetProperties())
        {
            if (!Mappings.Contains(property.PropertyType))
                continue;

            var mappings = Mappings[property.PropertyType];
            foreach (var (verbosity, mappedVerbosity) in mappings)
            {
                if (verbosity == FalloutBuild.Verbosity)
                    options.Set(property.Name, mappedVerbosity);
            }
        }
    }
}
