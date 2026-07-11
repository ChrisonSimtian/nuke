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
    public void Name_IsComplete()
        => new CompleteCommand().Name.Should().Be("complete");

    [Fact]
    public async Task Execute_WithoutRootDirectory_ReturnsZero()
        => (await new CompleteCommand().ExecuteAsync(["fallout "], rootDirectory: null, buildScript: null)).Should().Be(0);

    [Fact]
    public async Task Execute_WordNotStartingWithCommandName_ReturnsZero()
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
