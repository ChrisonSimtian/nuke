using System;
using System.Collections.Generic;
using System.Reflection;
using Fallout.Common.Execution;
using Fallout.Common.IO;

namespace Fallout.Build.Execution.Extensions;

/// <summary>
/// Emits <c>build-graph.json</c> into the temporary directory at build initialization, then re-emits
/// it on every target state transition so editor tooling (the VS Code extension) can watch the file
/// and animate the run live — queued → running → succeeded/failed. The projection itself lives in
/// <see cref="BuildGraphUtility"/>; this attribute only owns the build-lifecycle hooks and the file
/// write. Best-effort — a serialization failure never fails the build.
/// </summary>
internal class SerializeBuildGraphAttribute : BuildExtensionAttributeBase,
    IOnBuildInitialized, IOnTargetRunning, IOnTargetSucceeded, IOnTargetFailed, IOnTargetSkipped
{
    private const string GraphFileName = "build-graph.json";

    // Guards the file write only; the executor updates target.Status in place, so each transition
    // re-serializes the same captured collection with its now-current statuses.
    private readonly object writeLock = new();
    private IReadOnlyCollection<ExecutableTarget> targets;
    private string falloutVersion;

    private AbsolutePath GraphFile => Build.TemporaryDirectory / GraphFileName;

    public void OnBuildInitialized(
        IReadOnlyCollection<ExecutableTarget> executableTargets,
        IReadOnlyCollection<ExecutableTarget> executionPlan)
    {
        targets = executableTargets;
        falloutVersion = FindFalloutVersion();
        WriteGraph();
    }

    // Each transition re-emits the whole graph with current statuses. The executor sets
    // target.Status before invoking these, so the write reflects the transition that just happened.
    public void OnTargetRunning(ExecutableTarget target) => WriteGraph();

    public void OnTargetSucceeded(ExecutableTarget target) => WriteGraph();

    public void OnTargetFailed(ExecutableTarget target) => WriteGraph();

    public void OnTargetSkipped(ExecutableTarget target) => WriteGraph();

    private void WriteGraph()
    {
        if (targets == null)
        {
            return;
        }

        try
        {
            var json = BuildGraphUtility.GetJsonString(targets, falloutVersion);
            lock (writeLock)
            {
                GraphFile.WriteAllText(json);
            }
        }
        catch (Exception exception)
        {
            // Emission is a convenience for editor tooling — never let it break a build.
            Serilog.Log.Verbose(exception, "Failed to emit {GraphFileName}", GraphFileName);
        }
    }

    // Mirrors Fallout.Migrate: the informational version of the running Fallout assembly, up to the
    // build-metadata separator, so the pin aligns with the running tool. Null when unstamped.
    private static string FindFalloutVersion()
        => BuildGraphUtility.NormalizeVersion(
            typeof(SerializeBuildGraphAttribute).Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion);
}
