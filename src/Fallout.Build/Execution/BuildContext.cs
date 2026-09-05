using System;
using System.Collections.Generic;
using System.Threading;
using Fallout.Common.Tooling;
using Fallout.Common.Utilities.Collections;
using Fallout.Common.ValueInjection;

namespace Fallout.Common.Execution;

/// <summary>
/// Per-run, process-ambient build state. Activated once at the top of <see cref="BuildManager.Execute{T}"/>
/// and disposed at the end of the run, so the process-global statics a build touches are owned by a
/// scope rather than leaking across invocations (the cleanup FT-1 centralised now lives here).
/// The static surface (e.g. <see cref="BuildManager.CancellationHandler"/>) reads through
/// <see cref="Current"/>, which is <c>AsyncLocal</c> — so concurrent runs each resolve their own
/// context. That isolates the ambient pointer and the per-run state hanging off it (the cancellation
/// handlers, the event subscriptions, the services below), <em>not</em> the process-global singletons
/// the teardown resets (value-injection cache, tool-path resolver config); those stay shared, so
/// concurrent runs in one process still interfere through them.
/// </summary>
/// <remarks>
/// FT-2 / <see href="https://github.com/Fallout-build/Fallout/issues/307">#307</see>. Intentionally
/// <c>internal</c> — not a public contract until the SDK lands (milestone #7). Services move onto this
/// context one step at a time: the parameter service (FT-4 / <see href="https://github.com/Fallout-build/Fallout/issues/309">#309</see>)
/// and the in-memory log sink (FT-6 / <see href="https://github.com/Fallout-build/Fallout/issues/311">#311</see>)
/// ride it already; the tool-path configuration is next.
/// </remarks>
internal sealed class BuildContext : IDisposable
{
    private static readonly AsyncLocal<BuildContext> currentInstance = new();

    /// <summary>The context for the current build run, or <c>null</c> outside a run.</summary>
    public static BuildContext Current => currentInstance.Value;

    private readonly LinkedList<Action> cancellationHandlers = new();
    private readonly ConsoleCancelEventHandler onCancelKeyPress;
    private readonly EventHandler onToolOptionsCreated;

    /// <summary>
    /// The parameter service for this run. FT-4 / <see href="https://github.com/Fallout-build/Fallout/issues/309">#309</see>:
    /// this instance is what the static <see cref="ParameterService.Instance"/> facade resolves to, so a
    /// build reads parameters through the same instance form the specs already use, and the service's
    /// mutable fields die with the run instead of leaking into the next one.
    /// </summary>
    public ParameterService Parameters { get; } =
        new(() => EnvironmentInfo.ArgumentParser, () => EnvironmentInfo.Variables);

    /// <summary>
    /// The in-memory log sink for this run. FT-6 / <see href="https://github.com/Fallout-build/Fallout/issues/311">#311</see>:
    /// <c>Logging.ConfigureInMemory</c> wires Serilog to this instance at the top of the run and
    /// <c>WriteErrorsAndWarnings</c> reads it back at the end, both inside the scope — so a run's
    /// warnings and errors are discarded with it rather than resurfacing in the next build.
    /// </summary>
    public Logging.InMemorySink LogSink { get; } = new();

    private BuildContext()
    {
        onCancelKeyPress = (_, _) => cancellationHandlers.ForEach(x => x());
        onToolOptionsCreated = (options, _) => VerbosityMapping.Apply((ToolOptions)options);
        Console.CancelKeyPress += onCancelKeyPress;
        ToolOptions.Created += onToolOptionsCreated;
    }

    /// <summary>Creates the ambient context for a build run and installs it as <see cref="Current"/>.</summary>
    public static BuildContext Activate() => currentInstance.Value = new BuildContext();

    public void RegisterCancellationHandler(Action handler) => cancellationHandlers.AddFirst(handler);

    public void UnregisterCancellationHandler(Action handler) => cancellationHandlers.Remove(handler);

    public void Dispose()
    {
        // This scope's own subscriptions come off unconditionally — they are per-instance, so undoing
        // them can never affect another context. The same goes for the per-run services hanging off
        // this instance (Parameters, LogSink): they are discarded with the context, never reset.
        Console.CancelKeyPress -= onCancelKeyPress;
        ToolOptions.Created -= onToolOptionsCreated;

        // What remains below is shared process-wide state, so only the context that is still Current
        // may reset it — a superseded scope disposing out of order must not clobber the newer run.
        if (!ReferenceEquals(currentInstance.Value, this))
        {
            return;
        }

        ValueInjectionUtility.ClearCache();
        NuGetToolPathResolver.Reset();
        NpmToolPathResolver.Reset();

        currentInstance.Value = null;
    }
}
