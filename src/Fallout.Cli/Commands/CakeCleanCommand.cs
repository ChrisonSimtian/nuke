using System.Linq;
using System.Threading.Tasks;
using Fallout.Cli.Prompts;
using Fallout.Common;
using Fallout.Common.IO;

namespace Fallout.Cli.Commands;

/// <summary>
/// <c>fallout :cake-clean</c>: lists and optionally deletes the <c>*.cake</c> scripts.
/// </summary>
internal sealed class CakeCleanCommand : IFalloutCommand
{
    private readonly IConsolePrompts prompts;

    public CakeCleanCommand(IConsolePrompts prompts) => this.prompts = prompts;

    public string Name => "cake-clean";

    public Task<int> ExecuteAsync(string[] args, AbsolutePath rootDirectory, AbsolutePath buildScript)
        => Task.FromResult(Execute());

    private int Execute()
    {
        var cakeFiles = CakeConverter.GetCakeFiles().ToList();
        Host.Information("Found .cake files:");
        cakeFiles.ForEach(x => Host.Debug($"  - {x}"));

        if (prompts.PromptForConfirmation("Delete?"))
        {
            cakeFiles.ForEach(x => x.DeleteFile());
        }

        return 0;
    }
}
