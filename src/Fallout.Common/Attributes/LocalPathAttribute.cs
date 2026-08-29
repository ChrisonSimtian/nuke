using System;
using System.IO;
using System.Reflection;
using Fallout.Common.IO;

namespace Fallout.Common.Tooling;

/// <summary>
///     Injects a delegate for process execution. The path relative to the root directory is passed as constructor argument.
/// </summary>
/// <example>
///     <code>
/// [LocalTool("./tools/custom.exe")] readonly Tool Custom;
/// Target FooBar => _ => _
///     .Executes(() =>
///     {
///         var output = Custom("test");
///     });
///     </code>
/// </example>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class LocalPathAttribute : ToolInjectionAttributeBase
{
    private readonly string absoluteOrRelativePath;

    public LocalPathAttribute(string absoluteOrRelativePath)
    {
        this.absoluteOrRelativePath = absoluteOrRelativePath;
    }

    public LocalPathAttribute(string windowsPath, string unixPath)
    {
        absoluteOrRelativePath = EnvironmentInfo.IsWin ? windowsPath : unixPath;
    }

    public override ToolRequirement GetRequirement(MemberInfo member)
    {
        return null;
    }

    public override object GetValue(MemberInfo member, object instance)
    {
        var toolPath = PathConstruction.HasPathRoot(absoluteOrRelativePath)
            ? absoluteOrRelativePath
            : Path.Combine(Build.RootDirectory, absoluteOrRelativePath);

        return ToolResolver.GetTool(toolPath);
    }
}
