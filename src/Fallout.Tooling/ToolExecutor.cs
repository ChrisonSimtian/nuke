using System;
using System.Collections.Generic;

namespace Fallout.Common.Tooling;

internal class ToolExecutor
{
    private readonly string toolPath;

    public ToolExecutor(string toolPath)
    {
        this.toolPath = toolPath;
    }

#if NET6_0_OR_GREATER
    public IReadOnlyCollection<Output> Execute(
        ArgumentStringHandler arguments,
        string workingDirectory = null,
        IReadOnlyDictionary<string, string> environmentVariables = null,
        int? timeout = null,
        bool? logOutput = null,
        bool? logInvocation = null,
        Action<OutputType, string> logger = null,
        Action<IProcess> exitHandler = null)
    {
        var process = ProcessTasks.StartProcess(
            toolPath,
            arguments,
            workingDirectory,
            environmentVariables,
            timeout,
            logOutput,
            logInvocation,
            logger);
#else
    public IReadOnlyCollection<Output> Execute(
        string arguments,
        string workingDirectory = null,
        IReadOnlyDictionary<string, string> environmentVariables = null,
        int? timeout = null,
        bool? logOutput = null,
        bool? logInvocation = null,
        Action<OutputType, string> logger = null,
        Action<IProcess> exitHandler = null,
        Func<string, string> outputFilter = null)
    {
        var process = ProcessTasks.StartProcess(
            toolPath,
            arguments,
            workingDirectory,
            environmentVariables,
            timeout,
            logOutput,
            logInvocation,
            logger,
            outputFilter);
#endif
        (exitHandler ?? (p => p.AssertZeroExitCode())).Invoke(process.AssertWaitForExit());
        return process.Output;
    }
}
