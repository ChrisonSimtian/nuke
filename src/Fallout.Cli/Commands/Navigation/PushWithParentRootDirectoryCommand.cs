using System.IO;
using System.Threading.Tasks;
using Fallout.Common;
using Fallout.Common.IO;
using Fallout.Common.Utilities;
using static Fallout.Common.Constants;

namespace Fallout.Cli.Commands.Navigation;

/// <summary><c>fallout :PushWithParentRootDirectory</c>: queues the parent repository's root directory.</summary>
internal sealed class PushWithParentRootDirectoryCommand : IFalloutCommand
{
    public string Name => "PushWithParentRootDirectory";

    public Task<int> ExecuteAsync(string[] args, AbsolutePath rootDirectory, AbsolutePath buildScript)
        => Task.FromResult(Execute(rootDirectory));

    private int Execute(AbsolutePath rootDirectory)
    {
        return NavigationSession.PushAndSetNext(() =>
            TryGetRootDirectoryFrom(Path.GetDirectoryName(rootDirectory.NotNull("No root directory")))
                .NotNull("No parent root directory"));
    }
}
