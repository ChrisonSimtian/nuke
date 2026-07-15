using System;
using Fallout.Cli.Commands;
using FluentAssertions;
using System.Threading.Tasks;
using Xunit;

namespace Fallout.Cli.Specs.Commands;

public class SecretsCommandSpecs
{
    [Fact]
    public void Command_is_named_secrets()
        => new SecretsCommand(new FakeConsolePrompts()).Name.Should().Be("secrets");

    [Fact]
    public async Task Running_without_a_root_directory_throws()
    {
        var action = async () => await new SecretsCommand(new FakeConsolePrompts())
            .ExecuteAsync([], rootDirectory: null, buildScript: null);

        await action.Should().ThrowAsync<Exception>().WithMessage("*No root directory*");
    }
}
