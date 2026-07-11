using System;
using System.Threading.Tasks;
using System.Linq;
using Fallout.Common;
using Fallout.Common.IO;

namespace Fallout.Cli.Commands.Navigation;

/// <summary><c>fallout :GetNextDirectory</c>: prints (and consumes) the queued next directory.</summary>
internal sealed class GetNextDirectoryCommand : IFalloutCommand
{
    public string Name => "GetNextDirectory";

    public Task<int> ExecuteAsync(string[] args, AbsolutePath rootDirectory, AbsolutePath buildScript)
        => Task.FromResult(Execute(args, rootDirectory, buildScript));

    private int Execute(string[] args, AbsolutePath rootDirectory, AbsolutePath buildScript)
    {
        var content = NavigationSession.SessionFile.Existing()?.ReadAllLines();
        if (content == null || string.IsNullOrWhiteSpace(content[0]))
        {
            Console.WriteLine(EnvironmentInfo.WorkingDirectory);
            return 1;
        }

        var nextDirectory = content[0];
        content[0] = string.Empty;
        NavigationSession.SessionFile.WriteAllLines(content);
        Console.WriteLine(nextDirectory);
        return 0;
    }
}
