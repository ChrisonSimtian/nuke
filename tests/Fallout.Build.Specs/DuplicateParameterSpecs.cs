using System.Linq;
using System.Reflection;
using Fallout.Common.ValueInjection;
using FluentAssertions;
using Xunit;

namespace Fallout.Common.Specs;

/// <summary>
/// Covers duplicate CLI parameters in <see cref="ValueInjectionUtility.GetParameterMembers"/>. See
/// #554. A build class that implements a component interface and re-declares one of the interface's
/// parameters produced two members for one CLI parameter, so <c>--help</c> listed it twice and the
/// generated schema silently overwrote one declaration with the other.
/// </summary>
public class DuplicateParameterSpecs
{
    [Fact]
    public void A_parameter_re_declared_from_an_interface_is_listed_once()
    {
        GetDashedNames<BuildShadowingAnInterfaceParameter>()
            .Should().ContainSingle(x => x == "api-key");
    }

    [Fact]
    public void The_build_class_declaration_wins_over_the_interface()
    {
        var member = GetParameters<BuildShadowingAnInterfaceParameter>()
            .Single(x => ParameterService.GetParameterDashedName(x) == "api-key");

        member.DeclaringType.Should().Be(typeof(BuildShadowingAnInterfaceParameter));
        // GetParameterDescription trims the trailing period.
        ParameterService.GetParameterDescription(member).Should().Be("Declared on the build class");
    }

    [Fact]
    public void Parameters_that_do_not_collide_are_all_kept()
    {
        GetDashedNames<BuildShadowingAnInterfaceParameter>()
            .Should().Contain(new[]
            {
                "api-key",
                "only-on-the-interface",
                "only-on-the-build"
            });
    }

    [Fact]
    public void An_uncontested_interface_parameter_is_still_listed()
    {
        GetDashedNames<BuildUsingInterfaceParametersOnly>()
            .Should().Contain("api-key");
    }

    [Fact]
    public void Every_listed_parameter_has_a_distinct_dashed_name()
    {
        var names = GetDashedNames<BuildShadowingAnInterfaceParameter>();

        names.Should().OnlyHaveUniqueItems();
    }

    private static MemberInfo[] GetParameters<T>() =>
        ValueInjectionUtility.GetParameterMembers(typeof(T), includeUnlisted: true).ToArray();

    private static string[] GetDashedNames<T>() =>
        GetParameters<T>().Select(ParameterService.GetParameterDashedName).ToArray();

    private interface IHaveAnApiKey
    {
        [Parameter("Declared on the interface.")]
        string ApiKey => null;

        [Parameter("Only on the interface.")]
        string OnlyOnTheInterface => null;
    }

    // The fields are private on purpose. That is the idiomatic build-class parameter declaration,
    // and it is what triggers the duplicate: GetAllMembers only drops the interface member when the
    // class declares a *public* member of the same name.
    private class BuildShadowingAnInterfaceParameter : FalloutBuild, IHaveAnApiKey
    {
        [Parameter("Declared on the build class.")]
        private readonly string ApiKey;

        [Parameter("Only on the build.")]
        private readonly string OnlyOnTheBuild;
    }

    private class BuildUsingInterfaceParametersOnly : FalloutBuild, IHaveAnApiKey
    {
    }
}
