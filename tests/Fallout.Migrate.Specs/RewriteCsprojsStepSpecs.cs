using FluentAssertions;
using Xunit;
using Fallout.Migrate.Steps;

namespace Fallout.Migrate.Specs;

public class RewriteCsprojsStepSpecs
{
    private const string TestFalloutVersion = "11.0.0";

    [Fact]
    public void RewritesPackageReferenceNamespace()
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
    public void RewritesNukeRootDirectoryProperty()
    {
        const string input = """
                             <Project Sdk="Microsoft.NET.Sdk">
                               <PropertyGroup>
                                 <NukeRootDirectory>.\..</NukeRootDirectory>
                                 <NukeTelemetryVersion>1</NukeTelemetryVersion>
                               </PropertyGroup>
                             </Project>
                             """;

        var result = RewriteCsprojsStep.Rewrite(input, TestFalloutVersion);

        result.EditCount.Should().Be(4); // 2 opening + 2 closing tags
        result.Content.Should().Contain("<FalloutRootDirectory>");
        result.Content.Should().Contain("</FalloutRootDirectory>");
        result.Content.Should().Contain("<FalloutTelemetryVersion>");
        result.Content.Should().NotContain("<NukeRootDirectory>");
    }

    [Fact]
    public void LeavesUnrelatedNukePrefixedIdentifiersAlone()
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
    public void ReturnsZeroEditsForUnchangedContent()
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
    public void BumpsNukeInlineVersionToCurrentFalloutVersion()
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
    public void BumpsVersionAcrossExtraAttributesBetweenIncludeAndVersion()
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
    public void LeavesCpmManagedReferencesWithoutInlineVersionUntouchedByVersionPass()
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
    public void StripsSystemSecurityCryptographyXmlPackageReference()
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
    public void LeavesOtherSystemPackagesAlone()
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
}
