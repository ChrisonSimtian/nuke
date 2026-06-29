using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Fallout.Common.CI;
using Fallout.Common.CI.AppVeyor;
using Fallout.Common.CI.AzurePipelines;
using Fallout.Common.CI.Bitrise;
using Fallout.Common.CI.GitLab;
using Fallout.Common.CI.Jenkins;
using Fallout.Common.CI.TeamCity;
using Fallout.Common.CI.TravisCI;
using Xunit;

namespace Fallout.Common.Specs;

public class CISpecs
{
    [CITheory(typeof(AppVeyor))]
    [MemberData(nameof(Properties), typeof(AppVeyor))]
    public void TestAppVeyor(PropertyInfo property, AppVeyor instance)
    {
        AssertProperty(instance, property);
    }

    [CITheory(typeof(Bitrise))]
    [MemberData(nameof(Properties), typeof(Bitrise))]
    public void TestBitrise(PropertyInfo property, Bitrise instance)
    {
        AssertProperty(instance, property);
    }

    [CITheory(typeof(TeamCity))]
    [MemberData(nameof(Properties), typeof(TeamCity))]
    public void TestTeamCity(PropertyInfo property, TeamCity instance)
    {
        AssertProperty(instance, property);
    }

    [CITheory(typeof(AzurePipelines))]
    [MemberData(nameof(Properties), typeof(AzurePipelines))]
    public void TestAzureDevOps(PropertyInfo property, AzurePipelines instance)
    {
        AssertProperty(instance, property);
    }

    [CITheory(typeof(Jenkins))]
    [MemberData(nameof(Properties), typeof(Jenkins))]
    public void TestJenkins(PropertyInfo property, Jenkins instance)
    {
        AssertProperty(instance, property);
    }

    [CITheory(typeof(TravisCI))]
    [MemberData(nameof(Properties), typeof(TravisCI))]
    public void TestTravisCI(PropertyInfo property, TravisCI instance)
    {
        AssertProperty(instance, property);
        Assert.True(instance.Ci);
        Assert.True(instance.ContinousIntegration);
    }

    [CITheory(typeof(GitLab))]
    [MemberData(nameof(Properties), typeof(GitLab))]
    public void TestGitLab(PropertyInfo property, GitLab instance)
    {
        AssertProperty(instance, property);
        Assert.True(instance.Ci);
    }

    public static IEnumerable<object[]> Properties(Type type)
    {
        var instance = CreateInstance(type);

        return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(x => new[] { x, instance }).ToArray();
    }

    private static void AssertProperty(object instance, PropertyInfo property)
    {
        object value;
        try
        {
            value = property.GetValue(instance);
        }
        catch (TargetInvocationException exception)
        {
            throw exception.InnerException.NotNull();
        }

        // After the JetBrains.Annotations removal, [CanBeNull] is gone. Allow null on
        // reference types and nullable value types; require non-null on plain value types.
        var allowsNull = !property.PropertyType.IsValueType
                         || Nullable.GetUnderlyingType(property.PropertyType) != null;
        if (!allowsNull)
            value.Should().NotBeNull();

        if (value is not string strValue || property.GetCustomAttribute<NoConvertAttribute>() != null)
            return;

        bool.TryParse(strValue, out _).Should().BeFalse("boolean");
        long.TryParse(strValue, out _).Should().BeFalse("long");
        decimal.TryParse(strValue, out _).Should().BeFalse("decimal");
        DateTime.TryParse(strValue, out _).Should().BeFalse("DateTime");
        TimeSpan.TryParse(strValue, out _).Should().BeFalse("TimeSpan");
        Guid.TryParse(strValue, out _).Should().BeFalse("Guid");
    }

    private static object CreateInstance(Type type)
    {
        var bindingFlags = BindingFlags.CreateInstance | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.OptionalParamBinding;
        return Activator.CreateInstance(type, bindingFlags, binder: null, args: new object[0], culture: CultureInfo.CurrentCulture);
    }

    private static bool IsRunning(Type type)
    {
        var property = type.GetProperty($"IsRunning{type.Name}", BindingFlags.NonPublic | BindingFlags.Static).NotNull();
        return (bool) property.GetValue(obj: null);
    }

    private class CITheoryAttribute : TheoryAttribute
    {
        private readonly Type _type;

        public CITheoryAttribute(Type type)
        {
            _type = type;
        }

        public override string Skip => !IsRunning(_type) ? $"Only applies to {_type.Name}." : null;
    }
}
