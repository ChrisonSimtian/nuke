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
/// handlers, the event subscriptions), <em>not</em> the process-global singletons the teardown resets
/// (in-memory sink, value-injection cache, tool-path resolver config); those stay shared, so
/// concurrent runs in one process still interfere through them.
/// </summary>
/// <remarks>
/// FT-2 / <see href="https://github.com/Fallout-build/Fallout/issues/307">#307</see>. Intentionally
/// <c>internal</c> — not a public contract until the SDK lands (milestone #7). Subsequent steps move
/// the per-run services (parameters, logging scope, tool-path config) onto this context.
/// </remarks>
internal sealed class BuildContext : IDisposable
{
    private static readonly AsyncLocal<BuildContext> currentInstance = new();

    /// <summary>The context for the current build run, or <c>null</c> outside a run.</summary>
    public static BuildContext Current => currentInstance.Value;

    private readonly LinkedList<Action> cancellationHandlers = new();
    private readonly ConsoleCancelEventHandler onCancelKeyPress;
    private readonly EventHandler onToolOptionsCreated;

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
        // them can never affect another context.
        Console.CancelKeyPress -= onCancelKeyPress;
        ToolOptions.Created -= onToolOptionsCreated;

        // The rest is shared process-wide state, so only the context that is still Current may reset
        // it — a superseded scope disposing out of order must not clobber the newer run.
        if (!ReferenceEquals(currentInstance.Value, this))
            return;

        Logging.InMemorySink.Instance.Clear();
        ValueInjectionUtility.ClearCache();
        NuGetToolPathResolver.Reset();
        NpmToolPathResolver.Reset();

        currentInstance.Value = null;
    }
}
