using System;
using System.Threading.Tasks;
using Fallout.Common;
using Fallout.Common.IO;
using Fallout.Common.Utilities.Collections;

namespace Fallout.Cli.Commands;

/// <summary>
/// <c>fallout :get-configuration</c>: prints the build configuration parsed from the build script.
/// </summary>
internal sealed class GetConfigurationCommand : IFalloutCommand
{
    private readonly IConfigurationReader configuration;

    public GetConfigurationCommand(IConfigurationReader configuration) => this.configuration = configuration;

    public string Name => "get-configuration";

    public Task<int> ExecuteAsync(string[] args, AbsolutePath rootDirectory, AbsolutePath buildScript)
        => Task.FromResult(Execute(buildScript));

    private int Execute(AbsolutePath buildScript)
    {
        var configuration = this.configuration.Read(buildScript.NotNull(), evaluate: false);

        Host.Information($"Configuration from {buildScript}:");
        configuration.ForEach(x => Console.WriteLine($"{x.Key} = {x.Value}"));

        return 0;
    }
}
