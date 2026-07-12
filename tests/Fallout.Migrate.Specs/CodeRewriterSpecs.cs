using FluentAssertions;
using Xunit;

namespace Fallout.Migrate.Specs;

public class CodeRewriterSpecs
{
    [Fact]
    public void RewritesUsingDirective()
    {
        const string input = """
                             using Nuke.Common;
                             using Nuke.Common.IO;
                             using Fallout.Common;
                             """;

        var result = CodeRewriter.Rewrite(input);

        result.EditCount.Should().Be(2);
        result.Content.Should().Contain("using Fallout.Common;");
        result.Content.Should().Contain("using Fallout.Common.IO;");
    }

    [Fact]
    public void RewritesQualifiedTypeReference()
    {
        const string input = "var x = new Nuke.Common.Tools.DotNet.DotNetTasks();";
        var result = CodeRewriter.Rewrite(input);
        result.EditCount.Should().Be(1);
        result.Content.Should().Be("var x = new Fallout.Common.Tools.DotNet.DotNetTasks();");
    }

    [Fact]
    public void RewritesNukeBuildBaseType()
    {
        const string input = "class Build : NukeBuild { }";
        var result = CodeRewriter.Rewrite(input);
        result.EditCount.Should().Be(1);
        result.Content.Should().Be("class Build : FalloutBuild { }");
    }

    [Fact]
    public void RewritesINukeBuildInterface()
    {
        const string input = "public static int IsApplicable(INukeBuild build) => 0;";
        var result = CodeRewriter.Rewrite(input);
        result.EditCount.Should().Be(1);
        result.Content.Should().Be("public static int IsApplicable(IFalloutBuild build) => 0;");
    }

    [Fact]
    public void DoesNotMatchNukeAsPartOfAnotherIdentifier()
    {
        // A type like `NukeAdjacentThing` must not match `\bNukeBuild\b`.
        const string input = "var x = new NukeBuilderXYZ();";
        var result = CodeRewriter.Rewrite(input);
        result.EditCount.Should().Be(0);
        result.Content.Should().Be(input);
    }

    [Fact]
    public void DoesNotMatchLowercaseNukePrefix()
    {
        // ".nuke/foo" filenames stay as-is — handled by ScriptRewriter / DirectoryRenamer.
        const string input = """var path = "/repo/.nuke/parameters.json";""";
        var result = CodeRewriter.Rewrite(input);
        result.EditCount.Should().Be(0);
    }
}
