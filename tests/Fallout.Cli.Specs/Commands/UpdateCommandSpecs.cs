using System;
using System.IO;
using Fallout.Cli.Commands;
using Fallout.Common.IO;
using FluentAssertions;
using System.Threading.Tasks;
using Xunit;

namespace Fallout.Cli.Specs.Commands;

public class UpdateCommandSpecs
{
    [Fact]
    public void Name_IsUpdate()
        => new UpdateCommand(new FakeConsolePrompts(), new ConfigurationReader(), new BuildScaffolder()).Name.Should().Be("update");

    [Fact]
    public async Task Execute_NoBuildScript_DeclineAll_ReturnsZeroAndReportsCompletion()
    {
        var dir = (AbsolutePath)Path.Combine(Path.GetTempPath(), "fallout-update-" + Guid.NewGuid().ToString("N"));
        dir.CreateDirectory();
        var prompts = new FakeConsolePrompts { InvokeConfirmedActions = false };
        try
        {
            // No build script and every confirmation declined → no update steps run, but the command
            // still completes cleanly.
            (await new UpdateCommand(prompts, new ConfigurationReader(), new BuildScaffolder()).ExecuteAsync([], dir, buildScript: null)).Should().Be(0);

            prompts.Completions.Should().Contain("Updates");
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
