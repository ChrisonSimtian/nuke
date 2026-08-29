using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Fallout.Common.Utilities;
using Fallout.Common.Utilities.Collections;

namespace Fallout.Common.ValueInjection;

internal static class ValueInjectionUtility
{
    private static readonly Dictionary<MemberInfo, object> valueCache = new();

    /// <summary>Clears the per-run injected-value cache so a subsequent build in the same process re-injects. FT-1 / #306.</summary>
    internal static void ClearCache() => valueCache.Clear();

    public static T TryGetValue<T>(Expression<Func<T>> parameterExpression)
        where T : class
    {
        return TryGetValueWithCache<T>(parameterExpression);
    }

    public static T TryGetValue<T>(Expression<Func<object>> parameterExpression)
    {
        return TryGetValueWithCache<T>(parameterExpression);
    }

    private static T TryGetValueWithCache<T>(LambdaExpression lambdaExpression)
    {
        var parameter = lambdaExpression.GetMemberInfo();

        object GetValue()
        {
            var attribute = parameter.GetCustomAttribute<ValueInjectionAttributeBase>().NotNull();
            var instance = lambdaExpression.GetTarget();
            attribute.Build = instance as IFalloutBuild;
            return attribute.TryGetValue(parameter, instance);
        }

        return (T)(valueCache[parameter] = valueCache.GetValueOrDefault(parameter) ?? GetValue());
    }

    public static void InjectValues<T>(T instance = default, Func<MemberInfo, Attribute, bool> filter = null)
    {
        filter ??= (_, _) => true;
        InjectValuesInternal(instance,
            GetInjectionMembers(instance?.GetType() ?? typeof(T)).Where(x => filter(x.Member, x.Attribute)));
    }

    private static void InjectValuesInternal<T>(
        T instance,
        IEnumerable<(MemberInfo Member, ValueInjectionAttributeBase Attribute)> tuples)
    {
        tuples = tuples
            .OrderBy(x => x.Member.DeclaringType.Descendants(y => y.BaseType).Count())
            .ThenByDescending(x => x.Attribute.Priority);

        foreach (var (member, attribute) in tuples)
        {
            if (member.DeclaringType == typeof(FalloutBuild) &&
                !new[]
                {
                    nameof(FalloutBuild.Plan),
                    nameof(FalloutBuild.Help),
                    nameof(FalloutBuild.Continue),
                    nameof(FalloutBuild.NoLogo),
                    nameof(FalloutBuild.Verbosity),
                    nameof(FalloutBuild.Partition)
                }.Contains(member.Name))
            {
                continue;
            }

            if (member.ReflectedType.NotNull().IsInterface)
            {
                continue;
            }

            attribute.Build = instance as IFalloutBuild;
            var value = attribute.TryGetValue(member, instance);
            if (value == null)
            {
                continue;
            }

            var valueType = value.GetType();
            Assert.True(member.GetMemberType().IsAssignableFrom(valueType),
                $"Member '{member.Name}' must be of type '{valueType.Name}' to get its valued injected from '{attribute.GetType().Name}'");

            member.SetValue(instance, value);
        }
    }

    public static IReadOnlyCollection<MemberInfo> GetParameterMembers(Type type, bool includeUnlisted)
    {
        return GetInjectionMembers(type)
            .Where(x => x.Attribute is ParameterAttribute attribute && (includeUnlisted || attribute.List))
            .Select(x => x.Member)
            // A build class implementing a component interface can re-declare one of the interface's
            // parameters. Both members then describe the same CLI parameter, which used to be listed
            // twice by --help and to overwrite itself in the generated schema. Group by the name the
            // user actually types and keep the most specific declaration.
            .GroupBy(ParameterService.GetParameterDashedName, StringComparer.OrdinalIgnoreCase)
            .Select(x => x
                .OrderBy(GetDeclarationSpecificity)
                .ThenBy(y => y.DeclaringType.NotNull().FullName, StringComparer.Ordinal)
                .Last())
            .OrderBy(ParameterService.GetParameterMemberName).ToList();
    }

    /// <summary>
    /// Ranks competing declarations of one parameter. Interface declarations rank below any class
    /// declaration; among classes, the deeper the inheritance chain, the more specific.
    /// </summary>
    private static int GetDeclarationSpecificity(MemberInfo member)
    {
        var declaringType = member.DeclaringType.NotNull();
        return declaringType.IsInterface
            ? 0
            : declaringType.Descendants(x => x.BaseType).Count();
    }

    public static IReadOnlyCollection<(MemberInfo Member, ValueInjectionAttributeBase Attribute)> GetInjectionMembers(Type type)
    {
        return type
            .GetAllMembers(
                x => x.HasCustomAttribute<ValueInjectionAttributeBase>(),
                bindingFlags: ReflectionUtility.All,
                allowAmbiguity: true)
            .OrderBy(x => x.Name)
            .Select(x => (Member: x, Attribute: x.GetCustomAttribute<ValueInjectionAttributeBase>())).ToList();
    }
}
