using FluentAssertions;
using Xunit;
using Fallout.Migrate.Steps;

namespace Fallout.Migrate.Specs;

public class ScriptRewriterSpecs
{
    [Fact]
    public void Dotnet_nuke_invocations_become_dotnet_fallout()
    {
        var result = ScriptRewriter.Rewrite("dotnet nuke Compile");
        result.EditCount.Should().Be(1);
        result.Content.Should().Be("dotnet fallout Compile");
    }

    [Fact]
    public void Dot_nuke_directory_references_become_dot_fallout()
    {
        var result = ScriptRewriter.Rewrite("""TEMP_DIRECTORY="$SCRIPT_DIR/.nuke/temp" """);
        result.EditCount.Should().Be(1);
        result.Content.Should().Contain(".fallout/temp");
        result.Content.Should().NotContain(".nuke/");
    }

    [Fact]
    public void Legacy_nuke_env_vars_are_renamed_to_their_fallout_equivalents()
    {
        const string input = """
                             $env:NUKE_GLOBAL_TOOL_VERSION = "10.0"
                             """;

        var result = ScriptRewriter.Rewrite(input);
        result.EditCount.Should().Be(1);
        result.Content.Should().Contain("FALLOUT_GLOBAL_TOOL_VERSION");
    }

    [Theory]
    [InlineData("export NUKE_TELEMETRY_OPTOUT=1")]         // build.sh
    [InlineData("""$env:NUKE_TELEMETRY_OPTOUT = "1" """)]  // build.ps1
    [InlineData("set NUKE_TELEMETRY_OPTOUT=1")]            // build.cmd
    public void Telemetry_opt_out_line_is_stripped_entirely(string optOutLine)
    {
        // Telemetry was removed from Fallout (ADR-0010) — the opt-out is dropped, not renamed
        // to a dead FALLOUT_TELEMETRY_OPTOUT. Whichever bootstrap script spelled it, the whole
        // line goes and the surrounding ones are untouched.
        var input = $"""
                     export DOTNET_ROLL_FORWARD="Major"
                     {optOutLine}
                     dotnet nuke "$@"
                     """;

        var result = ScriptRewriter.Rewrite(input);

        result.Content.Should().NotContain("TELEMETRY_OPTOUT");
        result.Content.Should().Contain("DOTNET_ROLL_FORWARD");
        result.Content.Should().Contain("dotnet fallout");
    }

    [Fact]
    public void Plain_word_nuke_in_prose_is_left_alone()
    {
        // The word "nuke" in a comment or string isn't a command invocation.
        const string input = "# This was previously a NUKE-based build.";
        var result = ScriptRewriter.Rewrite(input);
        result.EditCount.Should().Be(0);
    }
}
