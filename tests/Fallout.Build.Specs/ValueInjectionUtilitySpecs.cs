using System.Reflection;
using FluentAssertions;
using Fallout.Common.ValueInjection;
using Xunit;

namespace Fallout.Common.Specs;

/// <summary>
/// Covers <see cref="ValueInjectionUtility.ClearCache"/> added in FT-1 / #306. The injected-value
/// cache is keyed by member and lives for the process, so <c>BuildManager.Execute</c>'s <c>finally</c>
/// clears it — otherwise a second build in the same process would re-serve the first run's injected
/// values instead of re-computing them.
///
/// A counting attribute makes the cache observable: the injected value is the running call count, so
/// a cached read repeats the previous value while a post-<c>ClearCache</c> read re-injects a fresh one.
/// </summary>
public class ValueInjectionUtilitySpecs
{
    [Fact]
    public void Value_is_cached_after_the_first_injection()
    {
        ValueInjectionUtility.ClearCache();
        CountingInjectionAttribute.Reset();
        var subject = new Subject();

        var first = ValueInjectionUtility.TryGetValue(() => subject.Value);
        var second = ValueInjectionUtility.TryGetValue(() => subject.Value);

        first.Should().Be("1");
        second.Should().Be("1", "the second read is served from the cache, not re-injected");
        CountingInjectionAttribute.Invocations.Should().Be(1);
    }

    [Fact]
    public void ClearCache_forces_re_injection_on_the_next_read()
    {
        ValueInjectionUtility.ClearCache();
        CountingInjectionAttribute.Reset();
        var subject = new Subject();

        var first = ValueInjectionUtility.TryGetValue(() => subject.Value);

        ValueInjectionUtility.ClearCache();
        var afterClear = ValueInjectionUtility.TryGetValue(() => subject.Value);

        first.Should().Be("1");
        afterClear.Should().Be("2", "clearing the cache re-runs the injection");
        CountingInjectionAttribute.Invocations.Should().Be(2);
    }

    private sealed class Subject
    {
        [CountingInjection]
        public string Value;
    }

    private sealed class CountingInjectionAttribute : ValueInjectionAttributeBase
    {
        public static int Invocations { get; private set; }

        public static void Reset() => Invocations = 0;

        public override object GetValue(MemberInfo member, object instance) => (++Invocations).ToString();
    }
}
