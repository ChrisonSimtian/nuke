using System;
using System.Linq.Expressions;
using System.Reflection;
using Fallout.Common.Execution;
using Fallout.Common.Utilities;

namespace Fallout.Common;

internal partial class ParameterService
{
    // FT-4 (#309): the active service is the per-run one on BuildContext, so a build reads parameters
    // through the same instance form the specs use and nothing leaks into the next run. The fallback
    // covers access outside a run — process-wide, but there is no cross-run state to leak there — and
    // can retire once that path is confirmed dead.
    private static readonly Lazy<ParameterService> ambientInstance = new(
        () => new ParameterService(() => EnvironmentInfo.ArgumentParser, () => EnvironmentInfo.Variables));

    internal static ParameterService Instance => BuildContext.Current?.Parameters ?? ambientInstance.Value;

    public static T GetParameter<T>(string name, char? separator = null)
    {
        return (T)Instance.GetParameter(name, typeof(T), separator);
    }

    public static T GetParameter<T>(Expression<Func<T>> expression)
    {
        return GetParameter<T>(expression.GetMemberInfo());
    }

    public static T GetParameter<T>(Expression<Func<object>> expression)
    {
        return GetParameter<T>(expression.GetMemberInfo());
    }

    public static T GetParameter<T>(MemberInfo member, Type destinationType = null)
    {
        return (T)GetFromMemberInfo(member, destinationType ?? typeof(T), Instance.GetParameter);
    }

    public static T GetNamedArgument<T>(string parameterName, char? separator = null)
    {
        return (T)Instance.GetCommandLineArgument(parameterName, typeof(T), separator);
    }

    public static T GetNamedArgument<T>(Expression<Func<T>> expression)
    {
        return GetNamedArgument<T>(expression.GetMemberInfo());
    }

    public static T GetNamedArgument<T>(Expression<Func<object>> expression)
    {
        return GetNamedArgument<T>(expression.GetMemberInfo());
    }

    public static T GetNamedArgument<T>(MemberInfo member, Type destinationType = null)
    {
        return (T)GetFromMemberInfo(member, destinationType ?? typeof(T), Instance.GetCommandLineArgument);
    }

    public static T GetPositionalArgument<T>(int position, char? separator = null)
    {
        return (T)Instance.GetCommandLineArgument(position, typeof(T), separator);
    }

    public static T[] GetAllPositionalArguments<T>(char? separator = null)
    {
        return (T[])Instance.GetPositionalCommandLineArguments(typeof(T), separator);
    }

    public static T GetVariable<T>(Expression<Func<T>> expression)
    {
        return GetVariable<T>(expression.GetMemberInfo());
    }

    public static T GetVariable<T>(Expression<Func<object>> expression)
    {
        return GetVariable<T>(expression.GetMemberInfo());
    }

    public static T GetVariable<T>(MemberInfo member, Type destinationType = null)
    {
        return (T)GetFromMemberInfo(member, destinationType ?? typeof(T), Instance.GetEnvironmentVariable);
    }

    public static T GetVariable<T>(string parameterName, char? separator = null)
    {
        return (T)Instance.GetEnvironmentVariable(parameterName, typeof(T), separator);
    }

    public static bool HasArgument(MemberInfo member)
    {
        return Instance.HasCommandLineArgument(GetParameterMemberName(member));
    }
}
