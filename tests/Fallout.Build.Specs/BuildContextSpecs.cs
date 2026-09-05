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
/// process-global state a build touches, plus the services that have since moved onto it (the
/// parameter service, FT-4 / #309; the in-memory log sink, FT-6 / #311).
/// <see cref="BuildContext.Current"/> is <c>AsyncLocal</c>, so each case resolves its own context, but
/// the singletons the teardown still resets (value-injection cache, resolver config) remain
/// process-wide — assertions that touch those key on a sentinel rather than on the singleton being empty.
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
    public void Parameter_service_facade_resolves_the_current_contexts_instance()
    {
        using var context = BuildContext.Activate();

        // FT-4 (#309): the static facade is a pointer at the running context, not its own singleton.
        ParameterService.Instance.Should().BeSameAs(context.Parameters);
    }

    [Fact]
    public void Parameter_service_facade_falls_back_to_an_ambient_instance_outside_a_run()
    {
        BuildContext.Current.Should().BeNull();

        // Nothing to route to outside a run, so the facade hands back a stable process-wide instance
        // rather than throwing — this is the path the fallback exists for.
        ParameterService.Instance.Should().NotBeNull();
        ParameterService.Instance.Should().BeSameAs(ParameterService.Instance);
    }

    [Fact]
    public void Each_run_gets_its_own_parameter_service()
    {
        ParameterService first;
        ParameterService second;

        using (var context = BuildContext.Activate())
            first = context.Parameters;
        using (var context = BuildContext.Activate())
            second = context.Parameters;

        second.Should().NotBeSameAs(first);
    }

    [Fact]
    public void Parameter_service_state_does_not_leak_into_the_next_run()
    {
        using (BuildContext.Activate())
            ParameterService.Instance.ArgumentsFromCommitMessageService = new ArgumentParser(["-arg", "value"]);

        // The mutable per-run fields (commit-message args, args-from-files) died with the previous
        // context — the whole point of moving the service off a process-global singleton.
        using (BuildContext.Activate())
            ParameterService.Instance.ArgumentsFromCommitMessageService.Should().BeNull();
    }

    [Fact]
    public void Log_sink_facade_resolves_the_current_contexts_sink()
    {
        using var context = BuildContext.Activate();

        // FT-6 (#311): same seam as the parameter service — the static sink is a pointer at the run.
        Logging.InMemorySink.Instance.Should().BeSameAs(context.LogSink);
    }

    [Fact]
    public void Log_sink_facade_falls_back_to_an_ambient_sink_outside_a_run()
    {
        BuildContext.Current.Should().BeNull();

        // Logging outside a run (a CLI command, a spec) has to land somewhere rather than throw.
        Logging.InMemorySink.Instance.Should().NotBeNull();
        Logging.InMemorySink.Instance.Should().BeSameAs(Logging.InMemorySink.Instance);
    }

    [Fact]
    public void Each_run_gets_its_own_log_sink()
    {
        Logging.InMemorySink first;
        Logging.InMemorySink second;

        using (var context = BuildContext.Activate())
            first = context.LogSink;
        using (var context = BuildContext.Activate())
            second = context.LogSink;

        second.Should().NotBeSameAs(first);
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
        var sentinel = $"build-context-superseded-{Guid.NewGuid()}";

        var first = BuildContext.Activate();
        using var second = BuildContext.Activate();
        NuGetToolPathResolver.EmbeddedPackagesDirectory = sentinel;

        // `second` is the live run and this config belongs to it. The teardown resets are process-wide,
        // so a superseded scope disposing out of order must skip them entirely — clearing Current is
        // not the only thing the identity guard protects.
        first.Dispose();

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
    public void Log_events_do_not_carry_into_the_next_run()
    {
        var sentinel = $"build-context-log-{Guid.NewGuid()}";

        using (BuildContext.Activate())
        {
            Logging.InMemorySink.Instance.Emit(CreateLogEvent(sentinel));
        }

        // FT-6 (#311): the sink belonged to the run that just ended and went out of scope with it, so
        // the next run starts on an empty one. Nothing clears a shared sink — there is no shared sink.
        // Assert on the sentinel specifically so a build emitting on another flow can't make it flaky.
        using (BuildContext.Activate())
        {
            Logging.InMemorySink.Instance.LogEvents.Should().NotContain(x => x.MessageTemplate.Text == sentinel);
        }
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
