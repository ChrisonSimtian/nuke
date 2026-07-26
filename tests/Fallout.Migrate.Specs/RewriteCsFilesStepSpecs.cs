using FluentAssertions;
using Xunit;
using Fallout.Migrate.Steps;

namespace Fallout.Migrate.Specs;

public class RewriteCsFilesStepSpecs
{
    [Fact]
    public void RewritesUsingDirective()
    {
        const string input = """
                             using Nuke.Common;
                             using Nuke.Common.IO;
                             using Fallout.Common;
                             """;

        var result = RewriteCsFilesStep.Rewrite(input);

        result.EditCount.Should().Be(2);
        result.Content.Should().Contain("using Fallout.Common;");
        result.Content.Should().Contain("using Fallout.Common.IO;");
    }

    [Fact]
    public void RewritesQualifiedTypeReference()
    {
        const string input = "var x = new Nuke.Common.Tools.DotNet.DotNetTasks();";
        var result = RewriteCsFilesStep.Rewrite(input);
        result.EditCount.Should().Be(1);
        result.Content.Should().Be("var x = new Fallout.Common.Tools.DotNet.DotNetTasks();");
    }

    [Fact]
    public void RewritesNukeBuildBaseType()
    {
        const string input = "class Build : NukeBuild { }";
        var result = RewriteCsFilesStep.Rewrite(input);
        result.EditCount.Should().Be(1);
        result.Content.Should().Be("class Build : FalloutBuild { }");
    }

    [Fact]
    public void RewritesINukeBuildInterface()
    {
        const string input = "public static int IsApplicable(INukeBuild build) => 0;";
        var result = RewriteCsFilesStep.Rewrite(input);
        result.EditCount.Should().Be(1);
        result.Content.Should().Be("public static int IsApplicable(IFalloutBuild build) => 0;");
    }

    [Fact]
    public void DoesNotMatchNukeAsPartOfAnotherIdentifier()
    {
        // A type like `NukeAdjacentThing` must not match `\bNukeBuild\b`.
        const string input = "var x = new NukeBuilderXYZ();";
        var result = RewriteCsFilesStep.Rewrite(input);
        result.EditCount.Should().Be(0);
        result.Content.Should().Be(input);
    }

    [Fact]
    public void DoesNotMatchLowercaseNukePrefix()
    {
        // ".nuke/foo" filenames stay as-is — handled by ScriptRewriter / DirectoryRenamer.
        const string input = """var path = "/repo/.nuke/parameters.json";""";
        var result = RewriteCsFilesStep.Rewrite(input);
        result.EditCount.Should().Be(0);
    }

    [Fact]
    public void RewritesNukeProjectModelUsingToSolutions()
    {
        // The solution types moved to Fallout.Solutions in v11 — a NUKE-era
        // `using` must land there, not on the dead Fallout.Common.ProjectModel.
        const string input = "using Nuke.Common.ProjectModel;";
        var result = RewriteCsFilesStep.Rewrite(input);
        result.EditCount.Should().Be(1);
        result.Content.Should().Be("using Fallout.Solutions;");
    }

    [Fact]
    public void RewritesQualifiedNukeProjectModelTypeToSolutions()
    {
        const string input = "Nuke.Common.ProjectModel.Solution x;";
        var result = RewriteCsFilesStep.Rewrite(input);
        result.EditCount.Should().Be(1);
        result.Content.Should().Be("Fallout.Solutions.Solution x;");
    }

    [Fact]
    public void RewritesAlreadyPartiallyMigratedProjectModelNamespace()
    {
        // Code previously run through a prefix-only migrator lands on the dead
        // Fallout.Common.ProjectModel; the ProjectModel rule salvages it.
        const string input = "using Fallout.Common.ProjectModel;";
        var result = RewriteCsFilesStep.Rewrite(input);
        result.EditCount.Should().Be(1);
        result.Content.Should().Be("using Fallout.Solutions;");
    }

    [Fact]
    public void DoesNotMatchProjectModelAsPartOfAnotherIdentifier()
    {
        // `ProjectModelFoo` must not be truncated by the ProjectModel rule.
        const string input = "using Nuke.Common.ProjectModelFoo;";
        var result = RewriteCsFilesStep.Rewrite(input);
        result.EditCount.Should().Be(1);
        result.Content.Should().Be("using Fallout.Common.ProjectModelFoo;");
    }
}
