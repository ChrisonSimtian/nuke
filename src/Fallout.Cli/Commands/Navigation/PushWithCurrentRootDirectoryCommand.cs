using Fallout.Common;
using System.Threading.Tasks;
using Fallout.Common.IO;
using Fallout.Common.Utilities;

namespace Fallout.Cli.Commands.Navigation;

/// <summary><c>fallout :PushWithCurrentRootDirectory</c>: queues the current root directory.</summary>
internal sealed class PushWithCurrentRootDirectoryCommand : IFalloutCommand
{
    public string Name => "PushWithCurrentRootDirectory";

    public Task<int> ExecuteAsync(string[] args, AbsolutePath rootDirectory, AbsolutePath buildScript)
        => Task.FromResult(Execute(args, rootDirectory, buildScript));

    private int Execute(string[] args, AbsolutePath rootDirectory, AbsolutePath buildScript)
    {
        return NavigationSession.PushAndSetNext(() => rootDirectory.NotNull("No root directory"));
    }
}
