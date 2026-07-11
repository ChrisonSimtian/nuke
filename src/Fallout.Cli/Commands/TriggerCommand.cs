using System;
using System.Threading.Tasks;
using Fallout.Common;
using Fallout.Common.Git;
using Fallout.Common.IO;
using Fallout.Common.Tools.Git;
using Fallout.Common.Utilities;

namespace Fallout.Cli.Commands;

/// <summary>
/// <c>fallout :trigger</c>: pushes an empty commit with the given message to trigger a remote build.
/// </summary>
public sealed class TriggerCommand : IFalloutCommand
{
    public string Name => "trigger";

    public Task<int> ExecuteAsync(string[] args, AbsolutePath rootDirectory, AbsolutePath buildScript)
        => Task.FromResult(Execute(args, rootDirectory));

    private static int Execute(string[] args, AbsolutePath rootDirectory)
    {
        var repository = GitRepository.FromLocalDirectory(rootDirectory.NotNull()).NotNull("No Git repository");
        Assert.NotNull(repository.Branch, "Git repository must not be detached");
        Assert.NotEmpty(args);

        try
        {
            var messageBody = args.JoinSpace();
            GitTasks.Git($"commit --allow-empty -m {messageBody.DoubleQuote()}");
            GitTasks.Git($"push {repository.RemoteName} {repository.Head}:{repository.RemoteBranch}");
            return 0;
        }
        catch
        {
            return 1;
        }
    }
}
