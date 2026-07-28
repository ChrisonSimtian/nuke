using System.Linq;
using System.Threading.Tasks;
using Fallout.Cli.Prompts;
using Fallout.Common;
using Fallout.Common.IO;
using static Fallout.Constants;

namespace Fallout.Cli.Commands.Navigation;

/// <summary>
/// <c>fallout :PushWithChosenRootDirectory</c>: prompts for a discovered root directory and queues it.
/// </summary>
internal sealed class PushWithChosenRootDirectoryCommand : IFalloutCommand
{
    private readonly IConsolePrompts prompts;

    public PushWithChosenRootDirectoryCommand(IConsolePrompts prompts) => this.prompts = prompts;

    public string Name => "PushWithChosenRootDirectory";

    public Task<int> ExecuteAsync(string[] args, AbsolutePath rootDirectory, AbsolutePath buildScript)
        => Task.FromResult(Execute());

    private int Execute()
    {
        return NavigationSession.PushAndSetNext(() =>
        {
            var directories = EnvironmentInfo.WorkingDirectory.GlobDirectories($"**/{FalloutDirectoryName}")
                .Concat(EnvironmentInfo.WorkingDirectory.GlobFiles($"**/{FalloutDirectoryName}"))
                .Where(x => !x.Equals(EnvironmentInfo.WorkingDirectory))
                .Select(x => x.Parent)
                .Select(x => (x, EnvironmentInfo.WorkingDirectory.GetRelativePathTo(x).ToString()))
                .OrderBy(x => x.Item2).ToArray();

            return prompts.PromptForChoice("Where to go next?", directories);
        });
    }
}
