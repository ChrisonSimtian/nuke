using FluentAssertions;
using Xunit;
using Fallout.Migrate.Steps;

namespace Fallout.Migrate.Specs;

public class RewriteCsprojsStepSpecs
{
    private const string TestFalloutVersion = "11.0.0";

    [Fact]
    public void Nuke_package_references_are_renamed_to_their_fallout_equivalents()
    {
        const string input = """
                             <Project Sdk="Microsoft.NET.Sdk">
                               <ItemGroup>
                                 <PackageReference Include="Nuke.Common" Version="9.0.0" />
                                 <PackageReference Include="Nuke.Components" />
                               </ItemGroup>
                             </Project>
                             """;

        var result = RewriteCsprojsStep.Rewrite(input, TestFalloutVersion);

        result.EditCount.Should().Be(2);
        result.Content.Should().Contain(@"Include=""Fallout.Common""");
        result.Content.Should().Contain(@"Include=""Fallout.Components""");
        result.Content.Should().NotContain(@"Include=""Nuke.");
    }

    [Fact]
    public void Nuke_root_directory_property_is_renamed()
    {
        const string input = """
                             <Project Sdk="Microsoft.NET.Sdk">
                               <PropertyGroup>
                                 <NukeRootDirectory>.\..</NukeRootDirectory>
                               </PropertyGroup>
                             </Project>
                             """;

        var result = RewriteCsprojsStep.Rewrite(input, TestFalloutVersion);

        result.EditCount.Should().Be(2); // 1 opening + 1 closing tag
        result.Content.Should().Contain("<FalloutRootDirectory>");
        result.Content.Should().Contain("</FalloutRootDirectory>");
        result.Content.Should().NotContain("<NukeRootDirectory>");
    }

    [Fact]
    public void Telemetry_version_property_is_stripped_instead_of_renamed()
    {
        // Telemetry was removed from Fallout (ADR-0010): NukeTelemetryVersion is dropped, not
        // renamed to a dead FalloutTelemetryVersion. Sibling properties are left intact.
        const string input = """
                             <Project Sdk="Microsoft.NET.Sdk">
                               <PropertyGroup>
                                 <NukeRootDirectory>.\..</NukeRootDirectory>
                                 <NukeTelemetryVersion>1</NukeTelemetryVersion>
                               </PropertyGroup>
                             </Project>
                             """;

        var result = RewriteCsprojsStep.Rewrite(input, TestFalloutVersion);

        result.Content.Should().NotContain("TelemetryVersion");
        result.Content.Should().Contain("<FalloutRootDirectory>");
    }

    [Fact]
    public void Unrelated_nuke_prefixed_properties_are_left_alone()
    {
        const string input = """
                             <Project Sdk="Microsoft.NET.Sdk">
                               <PropertyGroup>
                                 <NukeSomeRandomConsumerProp>x</NukeSomeRandomConsumerProp>
                               </PropertyGroup>
                             </Project>
                             """;

        var result = RewriteCsprojsStep.Rewrite(input, TestFalloutVersion);

        result.EditCount.Should().Be(0);
        result.Content.Should().Be(input);
    }

    [Fact]
    public void Content_without_nuke_references_is_returned_unchanged()
    {
        const string input = """
                             <Project Sdk="Microsoft.NET.Sdk">
                               <PropertyGroup>
                                 <TargetFramework>net10.0</TargetFramework>
                               </PropertyGroup>
                             </Project>
                             """;

        var result = RewriteCsprojsStep.Rewrite(input, TestFalloutVersion);

        result.EditCount.Should().Be(0);
        result.Content.Should().Be(input);
    }

    [Fact]
    public void Inline_nuke_package_versions_are_bumped_to_the_current_fallout_version()
    {
        // Regression guard for #217: NUKE-era pins like 10.1.0 never existed as Fallout.X
        // and would trip NU1603 on the migrated project. Migrate must bump in the same pass.
        const string input = """
                             <Project Sdk="Microsoft.NET.Sdk">
                               <ItemGroup>
                                 <PackageReference Include="Nuke.Common" Version="10.1.0" />
                                 <PackageReference Include="Nuke.Components" Version="10.1.0" />
                               </ItemGroup>
                             </Project>
                             """;

        var result = RewriteCsprojsStep.Rewrite(input, TestFalloutVersion);

        result.Content.Should().Contain(@"Include=""Fallout.Common"" Version=""11.0.0""");
        result.Content.Should().Contain(@"Include=""Fallout.Components"" Version=""11.0.0""");
        result.Content.Should().NotContain(@"Version=""10.1.0""");
    }

    [Fact]
    public void Version_is_bumped_across_extra_attributes_between_include_and_version()
    {
        // PrivateAssets / IncludeAssets are common NUKE-era attributes that sit between
        // Include and Version. The combined-rewrite pattern needs to tolerate them.
        const string input = """
                             <Project Sdk="Microsoft.NET.Sdk">
                               <ItemGroup>
                                 <PackageReference Include="Nuke.Common" PrivateAssets="all" Version="10.1.0" />
                               </ItemGroup>
                             </Project>
                             """;

        var result = RewriteCsprojsStep.Rewrite(input, TestFalloutVersion);

        result.Content.Should().Contain(@"Include=""Fallout.Common"" PrivateAssets=""all"" Version=""11.0.0""");
    }

    [Fact]
    public void Centrally_managed_references_do_not_gain_an_inline_version()
    {
        // Central package management — no inline Version attribute, version lives in
        // Directory.Packages.props. The namespace-only pass still renames Nuke. → Fallout.
        // but we must NOT inject a Version where there wasn't one.
        const string input = """
                             <Project Sdk="Microsoft.NET.Sdk">
                               <ItemGroup>
                                 <PackageReference Include="Nuke.Common" />
                               </ItemGroup>
                             </Project>
                             """;

        var result = RewriteCsprojsStep.Rewrite(input, TestFalloutVersion);

        result.Content.Should().Contain(@"<PackageReference Include=""Fallout.Common"" />");
        result.Content.Should().NotContain(@"Version=");
    }

    [Fact]
    public void Conflicting_system_security_cryptography_xml_pin_is_stripped()
    {
        // #217: NUKE-era projects often carry an explicit System.Security.Cryptography.Xml pin
        // that conflicts with Fallout.Common's transitive >= 10.0.6 requirement (NU1605 downgrade).
        // Removing the explicit pin lets the transitive version win.
        const string input = """
                             <Project Sdk="Microsoft.NET.Sdk">
                               <ItemGroup>
                                 <PackageReference Include="Nuke.Common" Version="10.1.0" />
                                 <PackageReference Include="System.Security.Cryptography.Xml" Version="9.0.15" />
                               </ItemGroup>
                             </Project>
                             """;

        var result = RewriteCsprojsStep.Rewrite(input, TestFalloutVersion);

        result.Content.Should().NotContain("System.Security.Cryptography.Xml");
        result.Content.Should().Contain(@"Include=""Fallout.Common"" Version=""11.0.0""");
    }

    [Fact]
    public void Other_system_packages_are_left_alone()
    {
        // Only System.Security.Cryptography.Xml is the known culprit. Other System.* packages
        // (System.Text.Json etc.) stay as the user pinned them — they're not in any known
        // conflict path.
        const string input = """
                             <Project Sdk="Microsoft.NET.Sdk">
                               <ItemGroup>
                                 <PackageReference Include="System.Text.Json" Version="9.0.0" />
                                 <PackageReference Include="System.Linq.Async" Version="6.0.1" />
                               </ItemGroup>
                             </Project>
                             """;

        var result = RewriteCsprojsStep.Rewrite(input, TestFalloutVersion);

        result.EditCount.Should().Be(0);
        result.Content.Should().Be(input);
    }

    [Fact]
    public void Telemetry_remove_pattern_does_not_act_greedy()
    {
        const string input = """
                             <Project Sdk="Microsoft.NET.Sdk">
                                 <PropertyGroup>
                                     <FalloutTelemetryVersion>1</FalloutTelemetryVersion>
                                     <IsPackable>false</IsPackable>
                                 </PropertyGroup>
                             </Project>
                             """;

        var result = RewriteCsprojsStep.Rewrite(input, TestFalloutVersion);

        result.Content.Should().Be("""
                                   <Project Sdk="Microsoft.NET.Sdk">
                                       <PropertyGroup>
                                           <IsPackable>false</IsPackable>
                                       </PropertyGroup>
                                   </Project>
                                   """);
    }

    [Fact]
    public void Cryptography_package_pin_remove_pattern_does_not_act_greedy()
    {
        const string input = """
                             <Project Sdk="Microsoft.NET.Sdk">
                                 <ItemGroup>
                                     <PackageReference Include="Fallout.Common" Version="10.3.49" />
                                     <PackageReference Include="System.Security.Cryptography.Xml" Version="10.0.10" />
                                 </ItemGroup>
                             </Project>
                             """;

        var result = RewriteCsprojsStep.Rewrite(input, TestFalloutVersion);

        result.Content.Should().Be("""
                                   <Project Sdk="Microsoft.NET.Sdk">
                                       <ItemGroup>
                                           <PackageReference Include="Fallout.Common" Version="10.3.49" />
                                       </ItemGroup>
                                   </Project>
                                   """);
    }
}
