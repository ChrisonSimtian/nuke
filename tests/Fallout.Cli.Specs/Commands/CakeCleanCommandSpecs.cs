using Fallout.Cli.Commands;
using FluentAssertions;
using System.Threading.Tasks;
using Xunit;

namespace Fallout.Cli.Specs.Commands;

public class CakeCleanCommandSpecs
{
    [Fact]
    public void Command_is_named_cake_clean()
        => new CakeCleanCommand(new FakeConsolePrompts()).Name.Should().Be("cake-clean");

    [Fact]
    public async Task Declining_the_deletion_prompt_deletes_nothing_and_returns_zero()
    {
        // ConfirmationResult = false → the "Delete?" prompt is declined, so no .cake files are removed.
        var prompts = new FakeConsolePrompts { ConfirmationResult = false };

        (await new CakeCleanCommand(prompts).ExecuteAsync([], rootDirectory: null, buildScript: null)).Should().Be(0);
    }
}
