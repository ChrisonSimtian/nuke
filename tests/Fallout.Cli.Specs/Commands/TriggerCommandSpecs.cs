using System;
using System.IO;
using System.Threading.Tasks;
using Fallout.Cli.Commands;
using Fallout.Common.IO;
using FluentAssertions;
using Xunit;

namespace Fallout.Cli.Specs.Commands;

public class TriggerCommandSpecs
{
    [Fact]
    public void Command_is_named_trigger()
        => new TriggerCommand().Name.Should().Be("trigger");

    [Fact]
    public async Task Running_outside_a_git_repository_throws()
    {
        var dir = (AbsolutePath)Path.Combine(Path.GetTempPath(), "fallout-trigger-" + Guid.NewGuid().ToString("N"));
        dir.CreateDirectory();
        try
        {
            var action = async () => await new TriggerCommand().ExecuteAsync(["a message"], dir, buildScript: null);

            // No resolvable Git repository at a throwaway temp dir → the command fails rather than
            // pushing anything.
            await action.Should().ThrowAsync<Exception>();
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
