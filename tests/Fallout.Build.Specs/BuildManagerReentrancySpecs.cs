using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using FluentAssertions;
using Fallout.Common.Execution;
using Fallout.Common.IO;
using Fallout.Common.Utilities;
using Serilog;
using Xunit;

namespace Fallout.Common.Specs.Execution;

/// <summary>
/// Reentrancy harness for <see cref="BuildManager.Execute{T}"/> (FT-9 /
/// <see href="https://github.com/Fallout-build/Fallout/issues/314">#314</see>). Where
/// <c>BuildContextSpecs</c> drives the per-run scope directly, this runs a real build end-to-end —
/// targets, logging pipeline, build extensions and all — <em>twice in one process</em>, which is the
/// scenario the de-static work (FT-1/2/4/6) exists to make safe.
/// </summary>
/// <remarks>
/// Two process-wide inputs have to be pinned for a build to run under a test host, and both are
/// themselves de-statification candidates:
/// <list type="bullet">
/// <item><description><see cref="EnvironmentInfo.ArgumentParser"/> — <c>Execute</c> resolves the
/// invoked targets from the process command line, which here is the test runner's own argv. It is
/// swapped for a controlled parser and restored afterwards.</description></item>
/// <item><description><see cref="FalloutBuild.RootDirectory"/> — resolved once in a static
/// constructor, so it cannot be varied per run. It lands on this repo's root, which carries the
/// <c>.fallout</c> marker the first case asserts on.</description></item>
/// </list>
/// </remarks>
[Collection(ProcessGlobalStateCollection.Name)]
public class BuildManagerReentrancySpecs : IDisposable
{
    private static readonly Expression<Func<ReentrancyBuild, Target>>[] defaultTargets = { x => x.Ping };

    private readonly ArgumentParser originalArguments = EnvironmentInfo.ArgumentParser;

    public BuildManagerReentrancySpecs()
    {
        EnvironmentInfo.ArgumentParser = new ArgumentParser(new[] { nameof(ReentrancyBuild.Ping) });
        ReentrancyBuild.Runs.Clear();
    }

    public void Dispose()
    {
        EnvironmentInfo.ArgumentParser = originalArguments;
        ReentrancyBuild.Runs.Clear();
    }

    [Fact]
    public void A_build_runs_end_to_end_under_the_harness()
    {
        // Guard rather than hang: UpdateNotificationAttribute prompts for a key press on a local build
        // whose root has no `.fallout` marker. Inside this repo it exists — assert it, so a future move
        // of these specs fails loudly here instead of stalling.
        Constants.GetFalloutDirectory(FalloutBuild.RootDirectory).DirectoryExists().Should().BeTrue(
            "the harness runs a local build, which prompts for input when the root has no .fallout marker");

        var exitCode = Run();

        exitCode.Should().Be(0);
        ReentrancyBuild.Runs.Should().ContainSingle();
        BuildContext.Current.Should().BeNull("the scope is disposed when Execute returns");
    }

    [Fact]
    public void A_second_run_in_the_same_process_also_succeeds()
    {
        // Regression guard. The first run emits a warning, which makes Host.WriteErrorsAndWarnings
        // replace Log.Logger — and the replaced logger owns this run's file sinks. Leaking it kept
        // `build.log` open, so the next run threw truncating it and returned the error exit code
        // without a word (the throw lands before Logging.Configure reassigns Log.Logger).
        Run().Should().Be(0);

        Run().Should().Be(0, "a second in-process run must not inherit the first run's open log file");
    }

    [Fact]
    public void A_second_run_gets_its_own_context_and_services()
    {
        Run();
        Run();

        var (first, second) = FirstTwoRuns();

        second.Context.Should().NotBeSameAs(first.Context, "FT-2 (#307)");
        second.Parameters.Should().NotBeSameAs(first.Parameters, "FT-4 (#309)");
        second.LogSink.Should().NotBeSameAs(first.LogSink, "FT-6 (#311)");
    }

    [Fact]
    public void A_second_run_does_not_see_the_first_runs_log_events()
    {
        Run();
        Run();

        var (first, second) = FirstTwoRuns();

        // Each run opens on an empty sink and ends holding only its own warning — the carry-over FT-6
        // closes would show up as the first run's sentinel in the second run's entry snapshot.
        first.EventsOnEntry.Should().BeEmpty();
        second.EventsOnEntry.Should().BeEmpty();
        second.EventsAfterOwnWarning.Should().ContainSingle().Which.Should().Be(second.Sentinel);
    }

    [Fact]
    public void The_static_facades_inside_a_run_point_at_that_run()
    {
        Run();

        var run = ReentrancyBuild.Runs.Single();

        run.Parameters.Should().BeSameAs(run.Context.Parameters);
        run.LogSink.Should().BeSameAs(run.Context.LogSink);
    }

    private static int Run() => BuildManager.Execute(defaultTargets);

    private static (RunObservation First, RunObservation Second) FirstTwoRuns()
    {
        ReentrancyBuild.Runs.Should().HaveCount(2, "both runs must reach their target");
        return (ReentrancyBuild.Runs[0], ReentrancyBuild.Runs[1]);
    }

    /// <summary>What one run of the fixture build observed about its own per-run state.</summary>
    private sealed record RunObservation(
        BuildContext Context,
        ParameterService Parameters,
        Logging.InMemorySink LogSink,
        string Sentinel,
        IReadOnlyCollection<string> EventsOnEntry,
        IReadOnlyCollection<string> EventsAfterOwnWarning);

    /// <summary>
    /// Minimal build the harness invokes. Its single target records the per-run state that run resolved
    /// and emits one warning, so a later run can be checked for carry-over.
    /// </summary>
    private class ReentrancyBuild : FalloutBuild
    {
        public static readonly List<RunObservation> Runs = new();

        public Target Ping => _ => _
            .Executes(() =>
            {
                var sink = Logging.InMemorySink.Instance;
                var eventsOnEntry = Texts(sink);
                var sentinel = $"reentrancy-run-{Runs.Count}";

                Log.Warning(sentinel);

                Runs.Add(new RunObservation(
                    Context: BuildContext.Current,
                    Parameters: ParameterService.Instance,
                    LogSink: sink,
                    Sentinel: sentinel,
                    EventsOnEntry: eventsOnEntry,
                    EventsAfterOwnWarning: Texts(sink)));
            });

        private static IReadOnlyCollection<string> Texts(Logging.InMemorySink sink) =>
            sink.LogEvents.Select(x => x.MessageTemplate.Text).ToList();
    }
}
