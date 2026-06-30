using System;
using System.Linq;
using System.Reflection;
using Fallout.Common.IO;
using Fallout.Common.Utilities;
using Serilog;
using Fallout.Common;

namespace Fallout.Solutions;

/// <summary>
///     Injects an instance of <see cref="Solution"/>. The solution path is resolved in the following order:
///     <ul>
///         <li>From the constructor argument</li>
///         <li>From command-line arguments (e.g., <c>-[MemberName] path/to/solution.sln</c>)</li>
///         <li>From environment variables (e.g., <c>[MemberName]=path/to/solution.sln</c>)</li>
///         <li>From the <c>.nuke</c> configuration file</li>
///     </ul>
/// </summary>
/// <example>
///     <code>
/// [Solution("common.sln")] readonly Solution Solution;
///     </code>
/// </example>
public class SolutionAttribute(string relativePath)
    : ParameterAttribute(GetDescription(relativePath))
{
    private static string GetDescription(string relativePath)
    {
        return "Path to a solution file that is automatically loaded."
               + (relativePath != null ? $" Default is {relativePath}." : string.Empty);
    }

    public SolutionAttribute()
        : this(relativePath: null)
    {
    }

    public override bool List { get; set; }

    public bool GenerateProjects { get; set; }

    public bool FancyNames { get; set; }

    public override object GetValue(MemberInfo member, object instance)
    {
        var solutionFile = TryGetSolutionFileFromFalloutFile() ??
                           GetSolutionFileFromParametersFile(member);

        var deserializer = typeof(SolutionModelExtensions).GetMethods()
            .Single(x => x.Name == nameof(SolutionModelExtensions.ReadSolution) && x.ContainsGenericParameters)
            .MakeGenericMethod(member.GetMemberType());

        return ((Solution)deserializer.Invoke(obj: null, [solutionFile])).NotNull();
    }

    // TODO: allow wildcard matching? [Solution("nuke-*.sln")] -- no globbing?
    // TODO: for just [Solution] without parameter being passed, do wildcard search?
    private AbsolutePath GetSolutionFileFromParametersFile(MemberInfo member)
    {
        return relativePath != null
            ? Build.RootDirectory / relativePath
            : ParameterService.GetParameter<AbsolutePath>(member).NotNull($"No solution file defined for '{member.Name}'.");
    }

    private AbsolutePath TryGetSolutionFileFromFalloutFile()
    {
        var falloutFile = Build.RootDirectory / Constants.FalloutFileName;
        if (!falloutFile.Exists())
            return null;

        var solutionFileRelative = falloutFile.ReadAllLines().ElementAtOrDefault(0);
        Assert.True(solutionFileRelative != null && !solutionFileRelative.Contains(value: '\\'),
            $"First line of {Constants.FalloutFileName} must provide solution path using UNIX separators");

        var solutionFile = Build.RootDirectory / solutionFileRelative;
        Assert.FileExists(solutionFile,
            $"Solution file '{solutionFile}' provided via {Constants.FalloutFileName} does not exist");

        return solutionFile;
    }
}
