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
        => Task.FromResult(Execute(rootDirectory));

    private int Execute(AbsolutePath rootDirectory)
    {
        return NavigationSession.PushAndSetNext(() => rootDirectory.NotNull("No root directory"));
    }
}
