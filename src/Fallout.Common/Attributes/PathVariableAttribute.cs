using System;
using System.Reflection;

namespace Fallout.Common.Tooling;

/// <summary>
///     Injects a delegate for process execution. The executable name is derived from the member name or can be
///     passed as constructor argument. The path to the executable is resolved in the following order:
///     <ul>
///         <li>From environment variables (e.g., <c>[NAME]_EXE=path</c>)</li>
///         <li>From the PATH variable using <c>which</c> or <c>where</c></li>
///     </ul>
/// </summary>
/// <example>
///     <code>
/// [PathTool] readonly Tool Echo;
/// Target FooBar => _ => _
///     .Executes(() =>
///     {
///         var output = Echo("test");
///     });
///     </code>
/// </example>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class PathVariableAttribute : ToolInjectionAttributeBase
{
    private readonly string pathExecutable;

    public PathVariableAttribute(string pathExecutable = null)
    {
        this.pathExecutable = pathExecutable;
    }

    public override ToolRequirement GetRequirement(MemberInfo member)
    {
        var name = pathExecutable ?? member.Name.ToLowerInvariant();
        return new PathToolRequirement(name);
    }

    public override object GetValue(MemberInfo member, object instance)
    {
        var name = pathExecutable ?? member.Name.ToLowerInvariant();
        return ToolResolver.TryGetEnvironmentTool(name) ??
               ToolResolver.GetPathTool(name);
    }
}
