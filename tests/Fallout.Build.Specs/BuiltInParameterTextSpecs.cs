using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Fallout.Common.Utilities;
using FluentAssertions;
using Xunit;

namespace Fallout.Common.Specs;

/// <summary>
/// Guards the descriptions of the built-in <see cref="ParameterAttribute" /> parameters — the text
/// <c>--help</c> prints and the generated <c>.fallout/build.schema.json</c> embeds. See #553, where
/// <c>--no-logo</c> still described the NUKE logo after the rebrand.
/// </summary>
public class BuiltInParameterTextSpecs
{
    /// <summary>Every listed and unlisted built-in parameter, with its description.</summary>
    public static TheoryData<string, string> BuiltInParameters
    {
        get
        {
            var data = new TheoryData<string, string>();
            foreach (var (name, description) in GetBuiltInParameters())
            {
                data.Add(name, description);
            }

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(BuiltInParameters))]
    public void No_built_in_parameter_description_names_a_pre_rebrand_product(string name, string description)
    {
        // Deliberate NUKE references live in attribution text and the logo tagline, not in the
        // parameter descriptions a consumer reads out of --help.
        // NotMatchRegex fails on a null subject, and a missing description is
        // Every_built_in_parameter_carries_a_description's concern, so normalise before asserting.
        (description ?? string.Empty).Should().NotMatchRegex(
            new Regex(@"\bnukes?\b", RegexOptions.IgnoreCase),
            "the {0} parameter's description should not name NUKE", name);
    }

    [Fact]
    public void The_no_logo_parameter_names_the_Fallout_logo()
    {
        GetDescription(nameof(FalloutBuild.NoLogo)).Should().Be("Disables displaying the Fallout logo.");
    }

    [Fact]
    public void Every_built_in_parameter_carries_a_description()
    {
        GetBuiltInParameters().Should().NotContain(x => x.Description.IsNullOrWhiteSpace());
    }

    private static string GetDescription(string memberName) =>
        GetBuiltInParameters().Single(x => x.Name == memberName).Description;

    private static (string Name, string Description)[] GetBuiltInParameters() =>
        typeof(FalloutBuild)
            .GetMembers(ReflectionUtility.All)
            .Select(x => (Member: x, Attribute: x.GetCustomAttribute<ParameterAttribute>()))
            .Where(x => x.Attribute != null)
            .Select(x => (x.Member.Name, x.Attribute.Description))
            .Distinct()
            .OrderBy(x => x.Name)
            .ToArray();
}
