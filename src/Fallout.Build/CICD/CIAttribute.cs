using System;
using System.Reflection;
using Fallout.Common.Utilities;
using Fallout.Common.ValueInjection;

namespace Fallout.Common.CI;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Field | AttributeTargets.Property)]
public class CIAttribute : ValueInjectionAttributeBase
{
    public override object GetValue(MemberInfo member, object instance)
    {
        // TODO: allow with conversion?
        var memberType = member.GetMemberType();
        var instanceProperty = memberType.GetProperty(nameof(Host.Instance), ReflectionUtility.Static);
        Assert.True(instanceProperty != null, $"Type '{memberType}' is not compatible for injection via '{nameof(CIAttribute)}'");
        return instanceProperty.GetValue(obj: null);
    }
}
