using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Fallout.Common.Tooling;

public class Process2 : IProcess
{
    private readonly Process process;
    private readonly Func<string, string> outputFilter;
    private readonly int? timeout;

    public Process2(Process process, Func<string, string> outputFilter, int? timeout, IReadOnlyCollection<Output> output)
    {
        this.process = process;
        this.outputFilter = outputFilter;
        this.timeout = timeout;
        Output = output;
    }

    public string FileName => process.StartInfo.FileName;

    public string Arguments => outputFilter.Invoke(process.StartInfo.Arguments);

    public string WorkingDirectory => process.StartInfo.WorkingDirectory;

    public IReadOnlyCollection<Output> Output { get; }

    public int ExitCode => process.ExitCode;

    public bool HasExited => process.HasExited;

    public int Id => process.Id;

    public void Dispose()
    {
        process.Dispose();
    }

    public void Kill()
    {
        process.Kill();
    }

    public bool WaitForExit()
    {
        // TODO: we are assuming that this method is called directly after process creation
        // use process.StartTime
        var hasExited = process.WaitForExit(timeout ?? -1);
        if (!hasExited)
        {
            process.Kill();
        }

        return hasExited;
    }
}
