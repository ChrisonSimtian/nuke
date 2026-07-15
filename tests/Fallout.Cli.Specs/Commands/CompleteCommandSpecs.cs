using System;
using System.IO;
using Fallout.Cli.Commands;
using Fallout.Common.IO;
using FluentAssertions;
using System.Threading.Tasks;
using Xunit;

namespace Fallout.Cli.Specs.Commands;

public class CompleteCommandSpecs
{
    [Fact]
    public void Command_is_named_complete()
        => new CompleteCommand().Name.Should().Be("complete");

    [Fact]
    public async Task Completion_without_a_root_directory_returns_zero()
        => (await new CompleteCommand().ExecuteAsync(["fallout "], rootDirectory: null, buildScript: null)).Should().Be(0);

    [Fact]
    public async Task Completion_for_a_non_fallout_command_line_returns_zero()
    {
        var dir = (AbsolutePath)Path.Combine(Path.GetTempPath(), "fallout-complete-" + Guid.NewGuid().ToString("N"));
        dir.CreateDirectory();
        try
        {
            // Completion only fires for the `fallout` command line; anything else short-circuits to 0.
            (await new CompleteCommand().ExecuteAsync(["notfallout foo"], dir, buildScript: null)).Should().Be(0);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
