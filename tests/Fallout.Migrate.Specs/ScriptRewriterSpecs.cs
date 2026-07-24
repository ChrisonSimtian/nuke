using FluentAssertions;
using Xunit;
using Fallout.Migrate.Steps;

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
                             $env:NUKE_GLOBAL_TOOL_VERSION = "10.0"
                             """;

        var result = ScriptRewriter.Rewrite(input);
        result.EditCount.Should().Be(1);
        result.Content.Should().Contain("FALLOUT_GLOBAL_TOOL_VERSION");
    }

    [Fact]
    public void StripsTelemetryOptOutLineEntirely()
    {
        // Telemetry was removed from Fallout (ADR-0010) — the opt-out is dropped, not renamed
        // to a dead FALLOUT_TELEMETRY_OPTOUT. The surrounding lines are untouched.
        const string input = """
                             export DOTNET_ROLL_FORWARD="Major"
                             export NUKE_TELEMETRY_OPTOUT=1
                             dotnet nuke "$@"
                             """;

        var result = ScriptRewriter.Rewrite(input);

        result.Content.Should().NotContain("TELEMETRY_OPTOUT");
        result.Content.Should().Contain("DOTNET_ROLL_FORWARD");
        result.Content.Should().Contain("dotnet fallout");
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
