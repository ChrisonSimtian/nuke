using FluentAssertions;
using Xunit;

namespace Fallout.Components.Tests;

public class PublishPackageRouterTests
{
    [Theory]
    [InlineData("Fallout.*", "Fallout.Common.2026.1.0", true)]
    [InlineData("Fallout.*", "Nuke.Common.2026.1.0", false)]
    [InlineData("*", "anything-at-all", true)]
    [InlineData("Nuke.?ommon*", "Nuke.Common.1.0.0", true)]
    [InlineData("Nuke.?ommon*", "Nuke.Xommon.1.0.0", true)]
    [InlineData("Nuke.?", "Nuke.Common", false)]
    public void GlobMatches_handles_star_and_question(string pattern, string value, bool expected)
        => PublishPackageRouter.GlobMatches(pattern, value).Should().Be(expected);

    [Fact]
    public void GlobMatches_is_case_insensitive()
        => PublishPackageRouter.GlobMatches("fallout.*", "Fallout.Common").Should().BeTrue();

    [Fact]
    public void Accepts_includes_then_applies_excludes()
    {
        // The real nuget.org routing: Fallout.* only, never the Nuke.* shims.
        var nugetOrg = new PublishTarget
        {
            Name = "nuget.org",
            Source = "https://api.nuget.org/v3/index.json",
            IncludePackages = new[] { "Fallout.*" },
            ExcludePackages = new[] { "Nuke.*" },
        };

        nugetOrg.Accepts("Fallout.Common.2026.1.0").Should().BeTrue();
        nugetOrg.Accepts("Nuke.Common.2026.1.0").Should().BeFalse();
    }

    [Fact]
    public void Accepts_exclusion_wins_over_inclusion()
    {
        var target = new PublishTarget
        {
            Name = "t",
            Source = "s",
            IncludePackages = new[] { "*" },
            ExcludePackages = new[] { "Nuke.*" },
        };

        target.Accepts("Fallout.Common").Should().BeTrue();
        target.Accepts("Nuke.Common").Should().BeFalse();
    }

    [Fact]
    public void Default_target_accepts_everything()
    {
        // The real github-packages routing: everything, including the Nuke.* shims.
        var ghPackages = new PublishTarget { Name = "github-packages", Source = "s" };
        var all = new[] { "Fallout.Common.1.0.0", "Nuke.Common.1.0.0" };

        PublishPackageRouter.Route(ghPackages, all).Should().BeEquivalentTo(all);
    }
}
