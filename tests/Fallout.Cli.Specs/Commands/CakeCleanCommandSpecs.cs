using Fallout.Cli.Commands;
using FluentAssertions;
using System.Threading.Tasks;
using Xunit;

namespace Fallout.Cli.Specs.Commands;

public class CakeCleanCommandSpecs
{
    [Fact]
    public void Name_IsCakeClean()
        => new CakeCleanCommand(new FakeConsolePrompts()).Name.Should().Be("cake-clean");

    [Fact]
    public async Task Execute_WhenDeletionDeclined_ReturnsZeroAndDeletesNothing()
    {
        // ConfirmationResult = false → the "Delete?" prompt is declined, so no .cake files are removed.
        var prompts = new FakeConsolePrompts { ConfirmationResult = false };

        (await new CakeCleanCommand(prompts).ExecuteAsync([], rootDirectory: null, buildScript: null)).Should().Be(0);
    }
}
