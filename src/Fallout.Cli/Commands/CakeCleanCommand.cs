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
    private readonly IConsolePrompts _prompts;

    public CakeCleanCommand(IConsolePrompts prompts) => _prompts = prompts;

    public string Name => "cake-clean";

    public Task<int> ExecuteAsync(string[] args, AbsolutePath rootDirectory, AbsolutePath buildScript)
        => Task.FromResult(Execute(args, rootDirectory, buildScript));

    private int Execute(string[] args, AbsolutePath rootDirectory, AbsolutePath buildScript)
    {
        var cakeFiles = CakeConverter.GetCakeFiles().ToList();
        Host.Information("Found .cake files:");
        cakeFiles.ForEach(x => Host.Debug($"  - {x}"));

        if (_prompts.PromptForConfirmation("Delete?"))
            cakeFiles.ForEach(x => x.DeleteFile());

        return 0;
    }
}
