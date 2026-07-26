using FluentAssertions;
using Fallout.Migrate.Steps;
using Xunit;

namespace Fallout.Migrate.Specs;

/// <summary>
/// Covers the <c>dotnet-tools.json</c> rewrite. See #575: the tool package id changed from
/// <c>nuke.globaltool</c> to <c>fallout.globaltool</c> to <c>fallout.globaltools</c>, and a manifest
/// pinning a retired id cannot reach 10.4 or later.
/// </summary>
public class RewriteToolManifestStepSpecs
{
    private const string ToolVersion = "10.4.0-rc.4";

    [Theory]
    [InlineData("fallout.globaltool")]
    [InlineData("nuke.globaltool")]
    [InlineData("fallout.cli")]
    public void RenamesARetiredToolIdToTheCurrentOne(string retiredId)
    {
        var result = RewriteToolManifestStep.Rewrite(Manifest(retiredId, "10.3.49"), ToolVersion);

        result.Content.Should().Contain("\"fallout.globaltools\"");
        result.Content.Should().NotContain($"\"{retiredId}\"");
    }

    [Fact]
    public void RepinsTheVersionOfTheRenamedEntry()
    {
        // Renaming the key alone leaves a pin that does not exist under the new id, so
        // `dotnet tool restore` would fail on the migrated manifest.
        var result = RewriteToolManifestStep.Rewrite(Manifest("fallout.globaltool", "10.3.49"), ToolVersion);

        result.Content.Should().Contain($"\"version\": \"{ToolVersion}\"");
        result.Content.Should().NotContain("10.3.49");
    }

    [Fact]
    public void CountsTheRenameAndTheRepinSeparately()
    {
        var result = RewriteToolManifestStep.Rewrite(Manifest("fallout.globaltool", "10.3.49"), ToolVersion);

        result.EditCount.Should().Be(2);
    }

    [Fact]
    public void LeavesTheVersionAloneWhenNoToolVersionResolved()
    {
        // Offline run: rename the id rather than write a version we could not verify.
        var result = RewriteToolManifestStep.Rewrite(Manifest("fallout.globaltool", "10.3.49"), toolVersion: null);

        result.Content.Should().Contain("\"fallout.globaltools\"");
        result.Content.Should().Contain("\"version\": \"10.3.49\"");
        result.EditCount.Should().Be(1);
    }

    [Fact]
    public void DoesNotTouchTheVersionOfAnUnrelatedTool()
    {
        const string input = """
                             {
                               "version": 1,
                               "isRoot": true,
                               "tools": {
                                 "fallout.globaltool": {
                                   "version": "10.3.49",
                                   "commands": [ "fallout" ]
                                 },
                                 "dotnet-format": {
                                   "version": "5.1.250801",
                                   "commands": [ "dotnet-format" ]
                                 }
                               }
                             }
                             """;

        var result = RewriteToolManifestStep.Rewrite(input, ToolVersion);

        result.Content.Should().Contain("\"fallout.globaltools\"");
        result.Content.Should().Contain("\"version\": \"5.1.250801\"", "an unrelated tool keeps its own pin");
        result.Content.Should().Contain($"\"version\": \"{ToolVersion}\"");
    }

    [Fact]
    public void LeavesAManifestWithoutAFalloutToolAlone()
    {
        const string input = """
                             {
                               "version": 1,
                               "isRoot": true,
                               "tools": {
                                 "dotnet-format": {
                                   "version": "5.1.250801",
                                   "commands": [ "dotnet-format" ]
                                 }
                               }
                             }
                             """;

        var result = RewriteToolManifestStep.Rewrite(input, ToolVersion);

        result.EditCount.Should().Be(0);
        result.Content.Should().Be(input);
    }

    [Fact]
    public void IsIdempotent()
    {
        var once = RewriteToolManifestStep.Rewrite(Manifest("fallout.globaltool", "10.3.49"), ToolVersion);

        var twice = RewriteToolManifestStep.Rewrite(once.Content, ToolVersion);

        twice.EditCount.Should().Be(0);
        twice.Content.Should().Be(once.Content);
    }

    [Fact]
    public void MatchesTheToolIdCaseInsensitively()
    {
        var result = RewriteToolManifestStep.Rewrite(Manifest("Fallout.GlobalTool", "10.3.49"), ToolVersion);

        result.Content.Should().Contain("\"fallout.globaltools\"");
        result.Content.Should().NotContain("Fallout.GlobalTool\"");
    }

    [Fact]
    public void KeepsTheRestOfTheEntryIntact()
    {
        const string input = """
                             {
                               "version": 1,
                               "isRoot": true,
                               "tools": {
                                 "fallout.globaltool": {
                                   "version": "10.3.49",
                                   "commands": [ "fallout" ],
                                   "rollForward": true
                                 }
                               }
                             }
                             """;

        var result = RewriteToolManifestStep.Rewrite(input, ToolVersion);

        result.Content.Should().Contain("\"commands\": [ \"fallout\" ]");
        result.Content.Should().Contain("\"rollForward\": true");
    }

    [Fact]
    public void DoesNotRenameAToolIdThatMerelyStartsWithARetiredId()
    {
        const string input = """
                             {
                               "tools": {
                                 "fallout.globaltool.extras": {
                                   "version": "1.0.0",
                                   "commands": [ "x" ]
                                 }
                               }
                             }
                             """;

        var result = RewriteToolManifestStep.Rewrite(input, ToolVersion);

        result.EditCount.Should().Be(0);
    }

    private static string Manifest(string toolId, string version) =>
        $$"""
          {
            "version": 1,
            "isRoot": true,
            "tools": {
              "{{toolId}}": {
                "version": "{{version}}",
                "commands": [ "fallout" ]
              }
            }
          }
          """;
}
