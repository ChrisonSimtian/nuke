using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Fallout.SolutionCodegen;
using Xunit;

namespace Fallout.Solution.Codegen.Specs;

public class SolutionMemberDiscoverySpecs
{
    [Theory]
    [InlineData("[Solution(GenerateProjects = true)]")]
    [InlineData("[SolutionAttribute(GenerateProjects = true)]")]
    [InlineData("[Solutions.Solution(GenerateProjects = true)]")]
    [InlineData("[Fallout.Solutions.Solution(GenerateProjects = true)]")]
    [InlineData("[global::Fallout.Solutions.SolutionAttribute(GenerateProjects = true)]")]
    public void Matches_attribute_by_rightmost_name_segment(string attribute)
    {
        Discover(attribute, "readonly Solution Solution;")
            .Should().ContainSingle().Which.MemberName.Should().Be("Solution");
    }

    [Theory]
    [InlineData("[Solution(GenerateProjects = false)]")]
    [InlineData("[Solution]")]
    [InlineData("[Solution(\"x.sln\")]")]
    public void Ignores_members_not_opting_into_GenerateProjects(string attribute)
    {
        Discover(attribute, "readonly Solution Solution;").Should().BeEmpty();
    }

    [Theory]
    [InlineData("[Parameter]")]
    [InlineData("[MySolution(GenerateProjects = true)]")]
    public void Ignores_unrelated_attributes(string attribute)
    {
        Discover(attribute, "readonly Solution Solution;").Should().BeEmpty();
    }

    [Theory]
    [InlineData("[Solution(\"sub/a.sln\", GenerateProjects = true)]")]
    [InlineData("[Solution(relativePath: \"sub/a.sln\", GenerateProjects = true)]")]
    public void Captures_relative_path_whether_positional_or_named(string attribute)
    {
        Discover(attribute, "readonly Solution Solution;")
            .Single().RelativePath.Should().Be("sub/a.sln");
    }

    [Fact]
    public void Relative_path_is_null_when_omitted()
    {
        Discover("[Solution(GenerateProjects = true)]", "readonly Solution Solution;")
            .Single().RelativePath.Should().BeNull();
    }

    [Fact]
    public void Captures_FancyNames_flag()
    {
        Discover("[Solution(GenerateProjects = true, FancyNames = true)]", "readonly Solution Solution;")
            .Single().FancyNames.Should().BeTrue();
    }

    [Fact]
    public void FancyNames_defaults_to_false()
    {
        Discover("[Solution(GenerateProjects = true)]", "readonly Solution Solution;")
            .Single().FancyNames.Should().BeFalse();
    }

    [Fact]
    public void Discovers_property_members()
    {
        Discover("[Solution(GenerateProjects = true)]", "public Solution MySolution { get; set; }")
            .Single().MemberName.Should().Be("MySolution");
    }

    private static IReadOnlyList<SolutionMember> Discover(string attribute, string memberDeclaration)
    {
        var source =
            $$"""
              partial class Build
              {
                  {{attribute}}
                  {{memberDeclaration}}
              }
              """;
        return SolutionMemberDiscovery.DiscoverInText(source).ToList();
    }
}
