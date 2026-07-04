using System;
using System.IO;
using System.Linq;
using Fallout.MSBuildTasks.Protocol;
using Microsoft.Build.Utilities;

namespace Fallout.MSBuildTasks;

/// <summary>
/// Base for the full-framework MSBuild task shims. Serializes the task inputs to a temp JSON file,
/// shells out to the net10 worker (<c>dotnet Fallout.MSBuildTasks.Worker.dll &lt;verb&gt; in out</c>),
/// and lets <see cref="ToolTask"/> surface the worker's stdout/stderr as build messages/errors.
/// Item outputs (if any) are read back from the worker's output JSON.
/// </summary>
public abstract class WorkerBridgeTask : ToolTask
{
    private string _inputPath;
    private string _outputPath;

    protected WorkerBridgeTask()
    {
        // Worker errors arrive on stderr; treat them as build errors so they reach the VS error list.
        LogStandardErrorAsError = true;
    }

    /// <summary>The worker verb (see <c>WorkerVerbs</c>).</summary>
    protected abstract string Verb { get; }

    /// <summary>Serialize this task's inputs to the worker request JSON.</summary>
    protected abstract string BuildRequestJson();

    /// <summary>Map the worker's item outputs back onto this task's [Output] properties.</summary>
    protected virtual void ConsumeResponse(WorkerResponse response) { }

    protected override string ToolName => "dotnet";

    protected override string GenerateFullPathToTool() => DotNetHost.Resolve();

    private string WorkerDll => Path.Combine(
        Path.GetDirectoryName(new Uri(typeof(WorkerBridgeTask).Assembly.Location).LocalPath) ?? ".",
        "Fallout.MSBuildTasks.Worker.dll");

    protected override string GenerateCommandLineCommands()
    {
        _inputPath = Path.GetTempFileName();
        _outputPath = Path.GetTempFileName();
        File.WriteAllText(_inputPath, BuildRequestJson());
        return $"\"{WorkerDll}\" {Verb} \"{_inputPath}\" \"{_outputPath}\"";
    }

    public override bool Execute()
    {
        try
        {
            var succeeded = base.Execute();
            if (succeeded && _outputPath != null && File.Exists(_outputPath))
            {
                var json = File.ReadAllText(_outputPath);
                if (!string.IsNullOrWhiteSpace(json))
                    ConsumeResponse(WorkerJson.Deserialize<WorkerResponse>(json));
            }

            return succeeded && !Log.HasLoggedErrors;
        }
        finally
        {
            TryDelete(_inputPath);
            TryDelete(_outputPath);
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (path != null && File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // Best-effort temp cleanup.
        }
    }
}

/// <summary>Locates the <c>dotnet</c> host to run the net10 worker.</summary>
internal static class DotNetHost
{
    public static string Resolve()
    {
        // The SDK sets DOTNET_HOST_PATH for in-build processes; prefer it.
        var hostPath = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH");
        if (!string.IsNullOrEmpty(hostPath) && File.Exists(hostPath))
            return hostPath;

        var exe = Environment.OSVersion.Platform == PlatformID.Win32NT ? "dotnet.exe" : "dotnet";
        var path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        var fromPath = path.Split(Path.PathSeparator)
            .Where(d => !string.IsNullOrWhiteSpace(d))
            .Select(d => Path.Combine(d.Trim(), exe))
            .FirstOrDefault(File.Exists);

        return fromPath ?? "dotnet";
    }
}
