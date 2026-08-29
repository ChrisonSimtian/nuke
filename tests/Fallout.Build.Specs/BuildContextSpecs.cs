using System;
using System.Reflection;
using Fallout.Common.Execution;
using Fallout.Common.Tooling;
using Fallout.Common.ValueInjection;
using FluentAssertions;
using Serilog.Events;
using Serilog.Parsing;
using Xunit;

namespace Fallout.Common.Specs.Execution;

/// <summary>
/// Covers <see cref="BuildContext"/> (FT-2 / #307) — the per-run ambient scope that owns the
/// process-global state a build touches. <see cref="BuildContext.Current"/> is <c>AsyncLocal</c>, so
/// each case resolves its own context, but the singletons the teardown resets (in-memory sink,
/// value-injection cache, resolver config) remain process-wide — assertions that touch them key on a
/// sentinel rather than on the singleton being empty.
/// </summary>
[Collection(ProcessGlobalStateCollection.Name)]
public class BuildContextSpecs
{
    [Fact]
    public void Current_is_null_outside_a_run()
    {
        BuildContext.Current.Should().BeNull();
    }

    [Fact]
    public void Activate_installs_the_returned_context_as_current()
    {
        using var context = BuildContext.Activate();

        BuildContext.Current.Should().BeSameAs(context);
    }

    [Fact]
    public void Dispose_clears_current()
    {
        var context = BuildContext.Activate();
        context.Dispose();

        BuildContext.Current.Should().BeNull();
    }

    [Fact]
    public void Disposing_a_superseded_context_leaves_the_newer_one_current()
    {
        var first = BuildContext.Activate();
        using var second = BuildContext.Activate();

        // Disposing the older scope must not clobber the newer Current — the guard keys on identity.
        first.Dispose();

        BuildContext.Current.Should().BeSameAs(second);
    }

    [Fact]
    public void Disposing_a_superseded_context_leaves_the_shared_state_intact()
    {
        var sink = Logging.InMemorySink.Instance;
        var sentinel = $"build-context-superseded-{Guid.NewGuid()}";

        var first = BuildContext.Activate();
        using var second = BuildContext.Activate();
        NuGetToolPathResolver.EmbeddedPackagesDirectory = sentinel;
        sink.Emit(CreateLogEvent(sentinel));

        // `second` is the live run and this state belongs to it. The teardown resets are process-wide,
        // so a superseded scope disposing out of order must skip them entirely — clearing Current is
        // not the only thing the identity guard protects.
        first.Dispose();

        sink.LogEvents.Should().Contain(x => x.MessageTemplate.Text == sentinel);
        NuGetToolPathResolver.EmbeddedPackagesDirectory.Should().Be(sentinel);
    }

    [Fact]
    public void Dispose_resets_the_tool_path_resolver_configuration()
    {
        using (BuildContext.Activate())
        {
            NuGetToolPathResolver.EmbeddedPackagesDirectory = "/packages";
            NuGetToolPathResolver.NuGetPackagesConfigFile = "/packages.config";
            NpmToolPathResolver.NpmPackageJsonFile = "/npm/package.json";
        }

        NuGetToolPathResolver.EmbeddedPackagesDirectory.Should().BeNull();
        NuGetToolPathResolver.NuGetPackagesConfigFile.Should().BeNull();
        NpmToolPathResolver.NpmPackageJsonFile.Should().BeNull();
    }

    [Fact]
    public void Dispose_clears_the_value_injection_cache()
    {
        ValueInjectionUtility.ClearCache();
        CountingInjectionAttribute.Reset();
        var subject = new Subject();

        using (BuildContext.Activate())
        {
            ValueInjectionUtility.TryGetValue(() => subject.Value).Should().Be("1");
        }

        // The context's teardown cleared the cache, so this read re-injects rather than replaying the
        // value the previous run computed.
        ValueInjectionUtility.TryGetValue(() => subject.Value).Should().Be("2");
        CountingInjectionAttribute.Invocations.Should().Be(2);
    }

    [Fact]
    public void Cancellation_handler_facade_is_a_no_op_without_an_active_context()
    {
        Action noop = () => { };

        // Register/unregister route through Current; outside a run there is nothing to route to, so
        // the facade must swallow the call rather than throw.
        var subscribe = () =>
        {
            BuildManager.CancellationHandler += noop;
            BuildManager.CancellationHandler -= noop;
        };

        subscribe.Should().NotThrow();
    }

    [Fact]
    public void Dispose_runs_the_per_run_teardown()
    {
        var sink = Logging.InMemorySink.Instance;
        var sentinel = $"build-context-teardown-{Guid.NewGuid()}";

        using (BuildContext.Activate())
        {
            sink.Emit(CreateLogEvent(sentinel));
        }

        // Leaving the scope disposes the context, whose teardown clears the shared in-memory sink so a
        // subsequent run in the same process starts clean. Assert on the sentinel specifically so a
        // build emitting on another flow can't make this flaky.
        sink.LogEvents.Should().NotContain(x => x.MessageTemplate.Text == sentinel);
    }

    private static LogEvent CreateLogEvent(string message) =>
        new(
            timestamp: DateTimeOffset.UnixEpoch,
            level: LogEventLevel.Warning,
            exception: null,
            messageTemplate: new MessageTemplateParser().Parse(message),
            properties: Array.Empty<LogEventProperty>());

    // Makes the injected-value cache observable: the value is the running invocation count, so a
    // cached read repeats the previous value while a re-injected one increments.
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
