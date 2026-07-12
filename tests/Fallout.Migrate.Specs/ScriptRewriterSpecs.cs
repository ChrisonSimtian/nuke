using FluentAssertions;
using Xunit;

namespace Fallout.Migrate.Specs;

public class ScriptRewriterSpecs
{
    [Fact]
    public void RewritesDotnetNukeInvocations()
    {
        var result = ScriptRewriter.Rewrite("dotnet nuke Compile");
        result.EditCount.Should().Be(1);
        result.Content.Should().Be("dotnet fallout Compile");
    }

    [Fact]
    public void RewritesDotDirectoryReferences()
    {
        var result = ScriptRewriter.Rewrite("""TEMP_DIRECTORY="$SCRIPT_DIR/.nuke/temp" """);
        result.EditCount.Should().Be(1);
        result.Content.Should().Contain(".fallout/temp");
        result.Content.Should().NotContain(".nuke/");
    }

    [Fact]
    public void RewritesLegacyEnvVars()
    {
        const string input = """
                             export NUKE_TELEMETRY_OPTOUT=1
                             $env:NUKE_GLOBAL_TOOL_VERSION = "10.0"
                             """;

        var result = ScriptRewriter.Rewrite(input);
        result.EditCount.Should().Be(2);
        result.Content.Should().Contain("FALLOUT_TELEMETRY_OPTOUT");
        result.Content.Should().Contain("FALLOUT_GLOBAL_TOOL_VERSION");
    }

    [Fact]
    public void LeavesPlainWordNukeAlone()
    {
        // The word "nuke" in a comment or string isn't a command invocation.
        const string input = "# This was previously a NUKE-based build.";
        var result = ScriptRewriter.Rewrite(input);
        result.EditCount.Should().Be(0);
    }
}
