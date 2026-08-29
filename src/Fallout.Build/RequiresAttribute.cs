using System;
using System.Reflection;
using Fallout.Common.Tooling;

namespace Fallout.Common;

public abstract class RequiresAttribute : Attribute
{
    public abstract ToolRequirement GetRequirement();
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true)]
public class RequiresAttribute<T> : RequiresAttribute
    where T : IRequireTool
{
    public string Version { get; set; }

    public override ToolRequirement GetRequirement()
    {
        return typeof(T).GetCustomAttribute<ToolAttribute>().NotNull().GetRequirement(Version);
    }
}
