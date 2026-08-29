using System;
using System.Linq;
using System.Reflection;
using Fallout.Common.Utilities;
using Serilog;

namespace Fallout.Common.ValueInjection;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public abstract class ValueInjectionAttributeBase : Attribute
{
    public IFalloutBuild Build { get; internal set; }

    public object TryGetValue(MemberInfo member, object instance)
    {
        try
        {
            return GetValue(member, instance);
        }
        catch (Exception exception)
        {
            if (!SuppressWarnings && !member.HasCustomAttribute<OptionalAttribute>())
            {
                Log.Warning(exception.Unwrap(), "Could not inject value for {Member}", member.GetDisplayName());
            }

            return null;
        }
    }

    public abstract object GetValue(MemberInfo member, object instance);

    public virtual int Priority => 0;

    public virtual bool SuppressWarnings => false;

    protected T GetMemberValue<T>(string memberName, object instance)
    {
        var type = instance.GetType();
        var member = type
            .GetAllMembers(
                x => x.Name == memberName,
                bindingFlags: ReflectionUtility.All,
                allowAmbiguity: true,
                filterQuasiOverridden: true)
            .FirstOrDefault()
            .NotNull($"No member '{memberName}' found in '{type.Name}'");

        Assert.True(typeof(T).IsAssignableFrom(member.GetMemberType()),
            $"Member '{type.Name}.{member.Name}' must be of type '{typeof(T).Name}'");

        return member.GetValue<T>(instance);
    }

    protected T GetMemberValueOrNull<T>(string memberName, object instance)
    {
        return memberName != null ? GetMemberValue<T>(memberName, instance) : default;
    }
}
