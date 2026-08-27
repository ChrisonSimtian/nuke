using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Fallout.Common;
using Fallout.Common.Execution;
using Fallout.Common.IO;
using Fallout.Common.Utilities;
using static Fallout.Common.Constants;

namespace Fallout.Cli.Commands;

/// <summary>
/// <c>fallout :run</c> (and the default, command-less invocation): builds the build project and
/// runs it, forwarding any remaining arguments to the build.
/// </summary>
internal sealed class RunCommand : IFalloutCommand
{
    public string Name => "run";

    public async Task<int> ExecuteAsync(string[] forwardedArgs, AbsolutePath rootDirectory, AbsolutePath buildProjectFile)
    {
        var dotnet = ResolveDotnet(rootDirectory);

        // A read-only introspection request (--describe, --plan --json) promises a JSON document on
        // standard output. Compiling the build project writes there too, so its output would land
        // inside the document a consumer is parsing — send it to standard error for those runs.
        var quietBuildStep = BuildIntrospectionService.IsRequested(forwardedArgs);

        var buildExitCode = await StartDotnetAsync(dotnet, GetBuildArguments(buildProjectFile), quietBuildStep);
        if (buildExitCode != 0)
        {
            return buildExitCode;
        }

        return await StartDotnetAsync(dotnet, GetRunArguments(buildProjectFile, forwardedArgs));
    }

    private static string ResolveDotnet(AbsolutePath rootDirectory)
    {
        var pathDotnet = TryGetDotnetFromPath();
        if (pathDotnet != null)
        {
            return pathDotnet;
        }

        var shimDirectoryName = EnvironmentInfo.IsWin ? "dotnet-win" : "dotnet-unix";
        var shimExecutableName = EnvironmentInfo.IsWin ? "dotnet.exe" : "dotnet";
        var shimPath = GetTemporaryDirectory(rootDirectory) / shimDirectoryName / shimExecutableName;
        Assert.True(File.Exists(shimPath),
            $"Could not locate 'dotnet'. Tried PATH and '{shimPath}'. " +
            $"Run './build.sh' (Unix) or './build.ps1' (Windows) once to provision .NET locally, then retry.");
        return shimPath;
    }

    private static string TryGetDotnetFromPath()
    {
        var executable = EnvironmentInfo.IsWin ? "dotnet.exe" : "dotnet";
        var pathVar = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        return pathVar
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Select(dir => Path.Combine(dir, executable))
            .FirstOrDefault(File.Exists);
    }

    private static async Task<int> StartDotnetAsync(
        string dotnet,
        IEnumerable<string> arguments,
        bool redirectStandardOutputToError = false)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = dotnet,
            UseShellExecute = false,
            RedirectStandardOutput = redirectStandardOutputToError
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        startInfo.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
        startInfo.Environment["DOTNET_NOLOGO"] = "1";
        startInfo.Environment["DOTNET_ROLL_FORWARD"] = "Major";
        startInfo.Environment[GlobalToolVersionEnvironmentKey] = typeof(RunCommand).Assembly.GetVersionText();
        startInfo.Environment[GlobalToolStartTimeEnvironmentKey] = DateTime.Now.ToString("O");

        var process = Process.Start(startInfo).NotNull();

        if (redirectStandardOutputToError)
        {
            // Pump concurrently with the wait: a build project noisy enough to fill the pipe buffer
            // would otherwise block forever with nobody draining it.
            await Task.WhenAll(
                PumpAsync(process.StandardOutput, Console.Error),
                process.WaitForExitAsync());
        }
        else
        {
            await process.WaitForExitAsync();
        }

        return process.ExitCode;
    }

    private static async Task PumpAsync(StreamReader reader, TextWriter writer)
    {
        while (await reader.ReadLineAsync() is { } line)
        {
            await writer.WriteLineAsync(line);
        }
    }

    private static IEnumerable<string> GetBuildArguments(AbsolutePath buildProjectFile)
    {
        // Mirrors the dotnet build invocation in build.sh / build.ps1.
        return new[]
        {
            "build",
            buildProjectFile.ToString(),
            "/nodeReuse:false",
            "/p:UseSharedCompilation=false",
            "-nologo",
            "-clp:NoSummary"
        };
    }

    private static IEnumerable<string> GetRunArguments(AbsolutePath buildProjectFile, string[] forwardedArgs)
    {
        var args = new List<string>
        {
            "run",
            "--project",
            buildProjectFile.ToString(),
            "--no-build",
            "--"
        };
        args.AddRange(forwardedArgs);
        return args;
    }
}
