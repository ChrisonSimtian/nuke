using System;
using System.Collections.Generic;
using System.Reflection;
using Fallout.Common.Execution;
using Fallout.Common.IO;

namespace Fallout.Build.Execution.Extensions;

/// <summary>
/// Emits <c>build-graph.json</c> into the temporary directory on every build initialization,
/// giving editor tooling (the VS Code extension) a machine-readable projection of the target
/// graph. The projection itself lives in <see cref="BuildGraphUtility"/>; this attribute only owns
/// the build-lifecycle hook and the file write. Best-effort — a serialization failure never fails
/// the build.
/// </summary>
internal class SerializeBuildGraphAttribute : BuildExtensionAttributeBase, IOnBuildInitialized
{
    private const string GraphFileName = "build-graph.json";

    private AbsolutePath GraphFile => Build.TemporaryDirectory / GraphFileName;

    public void OnBuildInitialized(
        IReadOnlyCollection<ExecutableTarget> executableTargets,
        IReadOnlyCollection<ExecutableTarget> executionPlan)
    {
        try
        {
            var json = BuildGraphUtility.GetJsonString(executableTargets, FindFalloutVersion());
            GraphFile.WriteAllText(json);
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
