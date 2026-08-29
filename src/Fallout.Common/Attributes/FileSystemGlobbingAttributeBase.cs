using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Fallout.Common.Utilities;
using Fallout.Common.Utilities.Collections;

namespace Fallout.Common.IO;

// TODO: document
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class FileGlobbingAttribute : FileSystemGlobbingAttributeBase
{
    public FileGlobbingAttribute(params string[] patterns)
        : base(patterns, Globbing.GlobFiles)
    {
    }
}

// TODO: document
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class DirectoryGlobbingAttribute : FileSystemGlobbingAttributeBase
{
    public DirectoryGlobbingAttribute(params string[] patterns)
        : base(patterns, Globbing.GlobDirectories)
    {
    }
}

public abstract class FileSystemGlobbingAttributeBase : ParameterAttribute
{
    private readonly string[] patterns;
    private readonly Func<AbsolutePath, string[], IEnumerable<AbsolutePath>> globber;

    protected FileSystemGlobbingAttributeBase(
        string[] patterns,
        Func<AbsolutePath, string[], IEnumerable<AbsolutePath>> globber)
    {
        this.patterns = patterns;
        this.globber = globber;
    }

    public override object GetValue(MemberInfo member, object instance)
    {
        var memberType = member.GetMemberType();
        Assert.True(memberType == typeof(AbsolutePath) || memberType == typeof(AbsolutePath[]),
            $"Member '{member.Name}' attributed with {GetType().Name} must be of type AbsolutePath or AbsolutePath[]");

        var globbedElements = GetGlobbedElements(member);

        var parameterValue = ParameterService.GetParameter<AbsolutePath[]>(member);
        if (parameterValue != null)
        {
            parameterValue.ForEach(x =>
                Assert.True(
                    globbedElements.Contains(x),
                    $"Value '{x}' for member '{member.Name}' is not contained any pattern '{patterns.JoinCommaSpace()}'"));

            Assert.True(parameterValue.Length == 1 || memberType == typeof(AbsolutePath[]),
                $"Member '{member.Name}' can only accept a single value but got:"
                    .Concat(parameterValue.Select(x => x.ToString()))
                    .JoinNewLine());

            return memberType == typeof(AbsolutePath)
                ? parameterValue.Single()
                : parameterValue;
        }

        return memberType == typeof(AbsolutePath[])
            ? globbedElements
            : globbedElements.Length == 1
                ? globbedElements.Single()
                : null;
    }

    public override IEnumerable<(string, object)> GetValueSet(MemberInfo member, object instance)
    {
        return GetGlobbedElements(member).Select(x => (PathConstruction.GetRelativePath(Build.RootDirectory, x), (object)x));
    }

    private AbsolutePath[] GetGlobbedElements(MemberInfo member)
    {
        Assert.NotEmpty(patterns, $"Member '{member.Name}' has no globbing patterns defined");
        return globber(Build.RootDirectory, patterns).ToArray();
    }
}
