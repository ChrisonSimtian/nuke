using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fallout.Cli.Commands;
using Fallout.Cli.Prompts;
using Fallout.Common;
using Fallout.Common.IO;
using Fallout.Common.Utilities;

namespace Fallout.Cli;

/// <summary>
/// Routes a command-line invocation to the matching <see cref="IFalloutCommand"/>. Commands are
/// resolved by <see cref="IFalloutCommand.Name"/> from the registered set — dash- and
/// case-insensitively, preserving every spelling the historical reflection dispatch accepted
/// (e.g. <c>:add-package</c> and <c>:addpackage</c>, <c>:PopDirectory</c> and <c>:popdirectory</c>).
/// </summary>
internal sealed class CommandDispatcher
{
    private const char CommandPrefix = ':';

    private readonly IReadOnlyList<IFalloutCommand> commands;
    private readonly IConsolePrompts prompts;

    public CommandDispatcher(IEnumerable<IFalloutCommand> commands, IConsolePrompts prompts)
    {
        this.commands = commands.ToList();
        this.prompts = prompts;
    }

    public async Task<int> DispatchAsync(string[] args, AbsolutePath rootDirectory, AbsolutePath buildScript)
    {
        var hasCommand = args.FirstOrDefault()?.StartsWithOrdinalIgnoreCase(CommandPrefix.ToString()) ?? false;
        if (hasCommand)
        {
            var token = args.First().Trim(CommandPrefix);
            if (string.IsNullOrWhiteSpace(token))
            {
                Assert.Fail($"No command specified. Usage is: fallout {CommandPrefix}<command> [args]");
            }

            var command = Resolve(token);
            return await command.ExecuteAsync(args.Skip(count: 1).ToArray(), rootDirectory, buildScript);
        }

        if (rootDirectory == null)
        {
            return prompts.PromptForConfirmation(
                $"Could not find {Constants.FalloutDirectoryName} directory/file. Do you want to setup a build?")
                ? await GetRequired("setup").ExecuteAsync(Array.Empty<string>(), rootDirectory: null, buildScript: null)
                : 0;
        }

        // TODO: docker

        return await GetRequired("run").ExecuteAsync(args, rootDirectory, BuildProjectResolver.Resolve(rootDirectory));
    }

    private IFalloutCommand Resolve(string token)
    {
        return commands.SingleOrDefault(x => Normalize(x.Name).EqualsOrdinalIgnoreCase(Normalize(token)))
            .NotNull(new[]
                {
                    $"Command '{token}' is not supported, available commands are:"
                }
                .Concat(commands.Select(x => $"  - {x.Name}").OrderBy(x => x)).JoinNewLine());
    }

    private IFalloutCommand GetRequired(string name)
        => commands.Single(x => x.Name.EqualsOrdinalIgnoreCase(name));

    private static string Normalize(string value) => value.Replace("-", string.Empty);
}
