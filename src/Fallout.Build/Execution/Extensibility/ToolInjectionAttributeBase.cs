using System.Reflection;
using Fallout.Common.ValueInjection;

namespace Fallout.Common.Tooling;

public abstract class ToolInjectionAttributeBase : ValueInjectionAttributeBase
{
    public override bool SuppressWarnings => true;

    public abstract ToolRequirement GetRequirement(MemberInfo member);
}
