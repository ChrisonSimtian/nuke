using System;
using System.Threading.Tasks;
using System.Linq;
using Fallout.Common.IO;

namespace Fallout.Cli.Commands.Navigation;

/// <summary><c>fallout :PopDirectory</c>: pops the previous directory back to the front of the queue.</summary>
internal sealed class PopDirectoryCommand : IFalloutCommand
{
    public string Name => "PopDirectory";

    public Task<int> ExecuteAsync(string[] args, AbsolutePath rootDirectory, AbsolutePath buildScript)
        => Task.FromResult(Execute(args, rootDirectory, buildScript));

    private int Execute(string[] args, AbsolutePath rootDirectory, AbsolutePath buildScript)
    {
        var content = NavigationSession.SessionFile.Existing()?.ReadAllLines().ToList();
        if (content == null || content.Count <= 1)
        {
            Console.Error.WriteLine("No previous directory");
            return 1;
        }

        content[0] = content[1];
        content.RemoveAt(1);
        NavigationSession.SessionFile.WriteAllLines(content);
        return 0;
    }
}
