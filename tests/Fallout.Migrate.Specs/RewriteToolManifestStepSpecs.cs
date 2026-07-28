using FluentAssertions;
using Fallout.Migrate.Steps;
using Xunit;

namespace Fallout.Migrate.Specs;

/// <summary>
/// Covers the <c>dotnet-tools.json</c> rewrite. See #575: the tool has shipped under
/// <c>nuke.globaltool</c>, <c>fallout.cli</c> and <c>fallout.globaltools</c>, and a manifest pinning
/// one of those retired ids cannot reach current releases. The id it moves to is
/// <c>fallout.globaltool</c>, settled in #581.
/// </summary>
public class RewriteToolManifestStepSpecs
{
    private const string ToolVersion = "10.4.0";

    private const string CurrentId = "\"fallout.globaltool\"";

    [Theory]
    [InlineData("nuke.globaltool")]
    [InlineData("fallout.globaltools")]
    [InlineData("fallout.cli")]
    public void RenamesARetiredToolIdToTheCurrentOne(string retiredId)
    {
        var result = RewriteToolManifestStep.Rewrite(Manifest(retiredId, "10.3.49"), ToolVersion);

        result.Content.Should().Contain(CurrentId);
        result.Content.Should().NotContain($"\"{retiredId}\"");
    }

    [Fact]
    public void LeavesAManifestAlreadyOnTheCurrentIdAlone()
    {
        // The whole point of #581: a consumer already pinning fallout.globaltool has nothing to
        // migrate, so the manifest must come back untouched — including its version pin.
        const string input = """
                             {
                               "version": 1,
                               "isRoot": true,
                               "tools": {
                                 "fallout.globaltool": {
                                   "version": "10.3.49",
                                   "commands": [ "fallout" ]
                                 }
                               }
                             }
                             """;

        var result = RewriteToolManifestStep.Rewrite(input, ToolVersion);

        result.EditCount.Should().Be(0);
        result.Content.Should().Be(input);
    }

    [Fact]
    public void RepinsTheVersionOfTheRenamedEntry()
    {
        // Renaming the key alone leaves a pin that does not exist under the new id, so
        // `dotnet tool restore` would fail on the migrated manifest.
        var result = RewriteToolManifestStep.Rewrite(Manifest("fallout.cli", "11.0.18"), ToolVersion);

        result.Content.Should().Contain($"\"version\": \"{ToolVersion}\"");
        result.Content.Should().NotContain("11.0.18");
    }

    [Fact]
    public void CountsTheRenameAndTheRepinSeparately()
    {
        var result = RewriteToolManifestStep.Rewrite(Manifest("fallout.cli", "11.0.18"), ToolVersion);

        result.EditCount.Should().Be(2);
    }

    [Fact]
    public void LeavesTheVersionAloneWhenNoToolVersionResolved()
    {
        // Offline run: rename the id rather than write a version we could not verify.
        var result = RewriteToolManifestStep.Rewrite(Manifest("fallout.cli", "11.0.18"), toolVersion: null);

        result.Content.Should().Contain(CurrentId);
        result.Content.Should().Contain("\"version\": \"11.0.18\"");
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
                                 "fallout.cli": {
                                   "version": "11.0.18",
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

        result.Content.Should().Contain(CurrentId);
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
        var once = RewriteToolManifestStep.Rewrite(Manifest("fallout.cli", "11.0.18"), ToolVersion);

        var twice = RewriteToolManifestStep.Rewrite(once.Content, ToolVersion);

        twice.EditCount.Should().Be(0);
        twice.Content.Should().Be(once.Content);
    }

    [Fact]
    public void MatchesTheToolIdCaseInsensitively()
    {
        var result = RewriteToolManifestStep.Rewrite(Manifest("Fallout.Cli", "11.0.18"), ToolVersion);

        result.Content.Should().Contain(CurrentId);
        result.Content.Should().NotContain("Fallout.Cli\"");
    }

    [Fact]
    public void KeepsTheRestOfTheEntryIntact()
    {
        const string input = """
                             {
                               "version": 1,
                               "isRoot": true,
                               "tools": {
                                 "fallout.cli": {
                                   "version": "11.0.18",
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
                                 "fallout.cli.extras": {
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
