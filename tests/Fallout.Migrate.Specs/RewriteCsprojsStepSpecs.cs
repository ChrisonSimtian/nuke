using System;
using System.IO;
using System.Threading.Tasks;
using Fallout.Common.IO;
using Fallout.Migrate.Common;
using Fallout.Migrate.Steps;
using FluentAssertions;
using Xunit;

namespace Fallout.Migrate.Specs;

public class RewriteCsprojsStepSpecs : IDisposable
{
    private const string TestFalloutVersion = "11.0.0";

    private readonly AbsolutePath tempDirectory;
    private readonly MigrationContext context;
    private readonly Summary summary = new();

    public RewriteCsprojsStepSpecs()
    {
        tempDirectory = AbsolutePath.Temp("fallout-migrate-test");
        (tempDirectory / "build").CreateDirectory();
        context = new MigrationContext(tempDirectory, dryRun: false, TextWriter.Null)
        {
            FalloutVersion = TestFalloutVersion
        };
    }

    public void Dispose()
    {
        tempDirectory.DeleteDirectory();
    }

    [Fact]
    public async Task Nuke_package_references_are_renamed_to_their_fallout_equivalents()
    {
        (tempDirectory / "build" / "_build.csproj").WriteAllText("""
                                                                 <Project Sdk="Microsoft.NET.Sdk">
                                                                   <ItemGroup>
                                                                     <PackageReference Include="Nuke.Common" Version="9.0.0" />
                                                                     <PackageReference Include="Nuke.Components" />
                                                                   </ItemGroup>
                                                                 </Project>
                                                                 """);

        await new RewriteCsprojsStep().ExecuteAsync(context, summary);

        summary.EditCount.Should().Be(2);
        var buildCsproj = (tempDirectory / "build" / "_build.csproj").ReadAllText();
        buildCsproj.Should().Contain(@"Include=""Fallout.Common""");
        buildCsproj.Should().Contain(@"Include=""Fallout.Components""");
        buildCsproj.Should().NotContain(@"Include=""Nuke.");
    }

    [Fact]
    public async Task Nuke_root_directory_property_is_renamed()
    {
        (tempDirectory / "build" / "_build.csproj").WriteAllText("""
                                                                 <Project Sdk="Microsoft.NET.Sdk">
                                                                   <PropertyGroup>
                                                                     <NukeRootDirectory>.\..</NukeRootDirectory>
                                                                   </PropertyGroup>
                                                                 </Project>
                                                                 """);

        await new RewriteCsprojsStep().ExecuteAsync(context, summary);

        summary.EditCount.Should().Be(2); // 1 opening + 1 closing tag
        var buildCsproj = (tempDirectory / "build" / "_build.csproj").ReadAllText();
        buildCsproj.Should().Contain("<FalloutRootDirectory>");
        buildCsproj.Should().Contain("</FalloutRootDirectory>");
        buildCsproj.Should().NotContain("<NukeRootDirectory>");
    }

    [Fact]
    public async Task Telemetry_version_property_is_stripped_instead_of_renamed()
    {
        // Telemetry was removed from Fallout (ADR-0010): NukeTelemetryVersion is dropped, not
        // renamed to a dead FalloutTelemetryVersion. Sibling properties are left intact.
        (tempDirectory / "build" / "_build.csproj").WriteAllText("""
                                                                 <Project Sdk="Microsoft.NET.Sdk">
                                                                   <PropertyGroup>
                                                                     <NukeRootDirectory>.\..</NukeRootDirectory>
                                                                     <NukeTelemetryVersion>1</NukeTelemetryVersion>
                                                                   </PropertyGroup>
                                                                 </Project>
                                                                 """);

        await new RewriteCsprojsStep().ExecuteAsync(context, summary);

        var buildCsproj = (tempDirectory / "build" / "_build.csproj").ReadAllText();
        buildCsproj.Should().NotContain("TelemetryVersion");
        buildCsproj.Should().Contain("<FalloutRootDirectory>");
    }

    [Fact]
    public async Task Unrelated_nuke_prefixed_properties_are_left_alone()
    {
        const string input = """
                             <Project Sdk="Microsoft.NET.Sdk">
                               <PropertyGroup>
                                 <NukeSomeRandomConsumerProp>x</NukeSomeRandomConsumerProp>
                               </PropertyGroup>
                             </Project>
                             """;

        (tempDirectory / "build" / "_build.csproj").WriteAllText(input, eofLineBreak: false);

        await new RewriteCsprojsStep().ExecuteAsync(context, summary);

        summary.EditCount.Should().Be(0);
        var buildCsproj = (tempDirectory / "build" / "_build.csproj").ReadAllText();
        buildCsproj.Should().Be(input);
    }

    [Fact]
    public async Task Content_without_nuke_references_is_returned_unchanged()
    {
        const string input = """
                             <Project Sdk="Microsoft.NET.Sdk">
                               <PropertyGroup>
                                 <TargetFramework>net10.0</TargetFramework>
                               </PropertyGroup>
                             </Project>
                             """;

        (tempDirectory / "build" / "_build.csproj").WriteAllText(input, eofLineBreak: false);

        await new RewriteCsprojsStep().ExecuteAsync(context, summary);

        summary.EditCount.Should().Be(0);
        var buildCsproj = (tempDirectory / "build" / "_build.csproj").ReadAllText();
        buildCsproj.Should().Be(input);
    }

    [Fact]
    public async Task Inline_nuke_package_versions_are_bumped_to_the_current_fallout_version()
    {
        // Regression guard for #217: NUKE-era pins like 10.1.0 never existed as Fallout.X
        // and would trip NU1603 on the migrated project. Migrate must bump in the same pass.
        (tempDirectory / "build" / "_build.csproj").WriteAllText("""
                                                                 <Project Sdk="Microsoft.NET.Sdk">
                                                                   <ItemGroup>
                                                                     <PackageReference Include="Nuke.Common" Version="10.1.0" />
                                                                     <PackageReference Include="Nuke.Components" Version="10.1.0" />
                                                                   </ItemGroup>
                                                                 </Project>
                                                                 """);

        await new RewriteCsprojsStep().ExecuteAsync(context, summary);

        var buildCsproj = (tempDirectory / "build" / "_build.csproj").ReadAllText();
        buildCsproj.Should().Contain(@"Include=""Fallout.Common""");
        buildCsproj.Should().Contain(@"Include=""Fallout.Components""");
        buildCsproj.Should().NotContain(@"Version=""10.1.0""");
        buildCsproj.Should().Contain($@"Version=""{TestFalloutVersion}""");
    }

    [Fact]
    public async Task Version_is_bumped_across_extra_attributes_between_include_and_version()
    {
        // PrivateAssets / IncludeAssets are common NUKE-era attributes that sit between
        // Include and Version. The combined-rewrite pattern needs to tolerate them.
        (tempDirectory / "build" / "_build.csproj").WriteAllText("""
                                                                 <Project Sdk="Microsoft.NET.Sdk">
                                                                   <ItemGroup>
                                                                     <PackageReference Include="Nuke.Common" PrivateAssets="all" Version="10.1.0" />
                                                                   </ItemGroup>
                                                                 </Project>
                                                                 """);

        await new RewriteCsprojsStep().ExecuteAsync(context, summary);

        var buildCsproj = (tempDirectory / "build" / "_build.csproj").ReadAllText();
        buildCsproj.Should().Contain(@"Include=""Fallout.Common"" PrivateAssets=""all""");
        buildCsproj.Should().NotContain(@"Version=""10.1.0""");
    }

    [Fact]
    public async Task Centrally_managed_references_do_not_gain_an_inline_version()
    {
        // Central package management — no inline Version attribute, version lives in
        // Directory.Packages.props. The namespace-only pass still renames Nuke. → Fallout.
        // but we must NOT inject a Version where there wasn't one.
        (tempDirectory / "build" / "_build.csproj").WriteAllText("""
                                                                 <Project Sdk="Microsoft.NET.Sdk">
                                                                   <ItemGroup>
                                                                     <PackageReference Include="Nuke.Common" />
                                                                   </ItemGroup>
                                                                 </Project>
                                                                 """);

        await new RewriteCsprojsStep().ExecuteAsync(context, summary);

        var buildCsproj = (tempDirectory / "build" / "_build.csproj").ReadAllText();
        buildCsproj.Should().Contain(@"<PackageReference Include=""Fallout.Common"" />");
        buildCsproj.Should().NotContain("Version=");
    }

    [Fact]
    public async Task Conflicting_system_security_cryptography_xml_pin_is_stripped()
    {
        // #217: NUKE-era projects often carry an explicit System.Security.Cryptography.Xml pin
        // that conflicts with Fallout.Common's transitive >= 10.0.6 requirement (NU1605 downgrade).
        // Removing the explicit pin lets the transitive version win.
        (tempDirectory / "build" / "_build.csproj").WriteAllText("""
                                                                 <Project Sdk="Microsoft.NET.Sdk">
                                                                   <ItemGroup>
                                                                     <PackageReference Include="Nuke.Common" Version="10.1.0" />
                                                                     <PackageReference Include="System.Security.Cryptography.Xml" Version="9.0.15" />
                                                                   </ItemGroup>
                                                                 </Project>
                                                                 """);

        await new RewriteCsprojsStep().ExecuteAsync(context, summary);

        var buildCsproj = (tempDirectory / "build" / "_build.csproj").ReadAllText();
        buildCsproj.Should().NotContain("System.Security.Cryptography.Xml");
        buildCsproj.Should().Contain(@"Include=""Fallout.Common""");
    }

    [Fact]
    public async Task Other_system_packages_are_left_alone()
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

        (tempDirectory / "build" / "_build.csproj").WriteAllText(input, eofLineBreak: false);

        await new RewriteCsprojsStep().ExecuteAsync(context, summary);

        summary.EditCount.Should().Be(0);
        var buildCsproj = (tempDirectory / "build" / "_build.csproj").ReadAllText();
        buildCsproj.Should().Be(input);
    }

    [Fact]
    public async Task Telemetry_remove_pattern_does_not_act_greedy()
    {
        const string input = """
                             <Project Sdk="Microsoft.NET.Sdk">
                                 <PropertyGroup>
                                     <FalloutTelemetryVersion>1</FalloutTelemetryVersion>
                                     <IsPackable>false</IsPackable>
                                 </PropertyGroup>
                             </Project>
                             """;

        (tempDirectory / "build" / "_build.csproj").WriteAllText(input, eofLineBreak: false);

        await new RewriteCsprojsStep().ExecuteAsync(context, summary);

        var buildCsproj = (tempDirectory / "build" / "_build.csproj").ReadAllText();
        buildCsproj.Should().Be("""
                                <Project Sdk="Microsoft.NET.Sdk">
                                    <PropertyGroup>
                                        <IsPackable>false</IsPackable>
                                    </PropertyGroup>
                                </Project>
                                """);
    }

    [Fact]
    public async Task Cryptography_package_pin_remove_pattern_does_not_act_greedy()
    {
        const string input = """
                             <Project Sdk="Microsoft.NET.Sdk">
                                 <ItemGroup>
                                     <PackageReference Include="Fallout.Common" Version="10.3.49" />
                                     <PackageReference Include="System.Security.Cryptography.Xml" Version="10.0.10" />
                                 </ItemGroup>
                             </Project>
                             """;

        (tempDirectory / "build" / "_build.csproj").WriteAllText(input, eofLineBreak: false);

        await new RewriteCsprojsStep().ExecuteAsync(context, summary);

        var buildCsproj = (tempDirectory / "build" / "_build.csproj").ReadAllText();
        buildCsproj.Should().Be("""
                                <Project Sdk="Microsoft.NET.Sdk">
                                    <ItemGroup>
                                        <PackageReference Include="Fallout.Common" Version="10.3.49" />
                                    </ItemGroup>
                                </Project>
                                """);
    }

    [Fact]
    public async Task Recognizes_a_version_variable_prefixed_with_Nuke()
    {
        const string input = """
                             <Project Sdk="Microsoft.NET.Sdk">
                               <PropertyGroup>
                                 <NukeVersion >10.1.0</NukeVersion >
                               </PropertyGroup>
                               <ItemGroup>
                                 <PackageReference Include="System.Text.Json" Version="9.0.0" />
                                 <PackageReference Include="System.Linq.Async" Version="6.0.1" />
                                 <PackageReference Include="Nuke.Common" Version="$(NukeVersion)" />
                                 <PackageReference Include="Nuke.Components" Version="$(NukeVersion)" />
                               </ItemGroup>
                             </Project>
                             """;
        (tempDirectory / "build" / "_build.csproj").WriteAllText(input);
        
        await new RewriteCsprojsStep().ExecuteAsync(context, summary);

        var buildCsproj = (tempDirectory / "build" / "_build.csproj").ReadAllText();

        buildCsproj.Should().NotContain("NukeVersion")
            .And.Contain("$(FalloutVersion)", Exactly.Twice())
            .And.Contain("<FalloutVersion >11.0.0</FalloutVersion >");
    }

    [Fact]
    public async Task Leaves_an_arbitrary_version_variable_alone_but_updates_the_version()
    {
        const string input = """
                             <Project Sdk="Microsoft.NET.Sdk">
                               <PropertyGroup>
                                 <CiProjectVersion>10.3.49</CiProjectVersion>
                               </PropertyGroup>
                               <ItemGroup>
                                 <PackageReference Include="System.Text.Json" Version="9.0.0" />
                                 <PackageReference Include="System.Linq.Async" Version="6.0.1" />
                                 <PackageReference Include="Nuke.Common" Version="$(CiProjectVersion)" />
                                 <PackageReference Include="Nuke.Components" Version="$(CiProjectVersion)" />
                               </ItemGroup>
                             </Project>
                             """;

        (tempDirectory / "build" / "_build.csproj").WriteAllText(input);
        
        await new RewriteCsprojsStep().ExecuteAsync(context, summary);

        var buildCsproj = (tempDirectory / "build" / "_build.csproj").ReadAllText();

        buildCsproj.Should().NotContain("FalloutVersion")
            .And.Contain("$(CiProjectVersion)", Exactly.Twice())
            .And.Contain("<CiProjectVersion>11.0.0</CiProjectVersion>");
    }

    [Fact]
    public async Task Leaves_an_unreferenced_arbitrary_variable_alone()
    {
        const string input = """
                             <Project Sdk="Microsoft.NET.Sdk">
                               <PropertyGroup>
                                 <UnreferencedVersion>10.3.49</UnreferencedVersion>
                                 <CiProjectVersion>10.3.49</CiProjectVersion>
                               </PropertyGroup>
                               <ItemGroup>
                                 <PackageReference Include="Nuke.Common" Version="$(CiProjectVersion)" />
                               </ItemGroup>
                             </Project>
                             """;

        (tempDirectory / "build" / "_build.csproj").WriteAllText(input);
        
        await new RewriteCsprojsStep().ExecuteAsync(context, summary);

        var buildCsproj = (tempDirectory / "build" / "_build.csproj").ReadAllText();

        buildCsproj.Should().Contain("<UnreferencedVersion>10.3.49</UnreferencedVersion>")
            .And.Contain("<CiProjectVersion>11.0.0</CiProjectVersion>")
            .And.Contain("$(CiProjectVersion)", Exactly.Once());
    }

    [Fact]
    public async Task Does_not_bump_variables_used_for_non_Fallout_packages()
    {
        const string input = """
                             <Project Sdk="Microsoft.NET.Sdk">
                               <PropertyGroup>
                                 <NewtonsoftVersion>13.0.3</NewtonsoftVersion>
                               </PropertyGroup>
                               <ItemGroup>
                                 <PackageReference Include="Newtonsoft.Json" Version="$(NewtonsoftVersion)" />
                               </ItemGroup>
                             </Project>
                             """;

        (tempDirectory / "build" / "_build.csproj").WriteAllText(input, eofLineBreak: false);
        
        await new RewriteCsprojsStep().ExecuteAsync(context, summary);

        var buildCsproj = (tempDirectory / "build" / "_build.csproj").ReadAllText();

        summary.EditCount.Should().Be(0);
        buildCsproj.Should().Be(input);
    }

    [Fact]
    public async Task Decouples_ambiguously_used_variables()
    {
        const string input = """
                             <Project Sdk="Microsoft.NET.Sdk">
                               <PropertyGroup>
                                 <PkgVersion>10.1.0</PkgVersion>
                               </PropertyGroup>
                               <ItemGroup>
                                 <PackageReference Include="Nuke.Common" Version="$(PkgVersion)" />
                                 <PackageReference Include="Some.ThirdParty" Version="$(PkgVersion)" />
                               </ItemGroup>
                             </Project>
                             """;

        (tempDirectory / "build" / "_build.csproj").WriteAllText(input);
        
        await new RewriteCsprojsStep().ExecuteAsync(context, summary);

        var buildCsproj = (tempDirectory / "build" / "_build.csproj").ReadAllText();

        buildCsproj.Should()
          .Contain("<PkgVersion>10.1.0</PkgVersion>")
          .And.Contain("<FalloutVersion>")
          .And.Contain("$(FalloutVersion)", Exactly.Once());
    }

    [Fact]
    public async Task Decouples_ambiguously_used_variables_even_when_no_property_group_found()
    {
        const string input = """
                             <Project Sdk="Microsoft.NET.Sdk">
                               <ItemGroup>
                                 <PackageReference Include="Nuke.Common" Version="$(PkgVersion)" />
                                 <PackageReference Include="Some.ThirdParty" Version="$(PkgVersion)" />
                               </ItemGroup>
                             </Project>
                             """;

        (tempDirectory / "build" / "_build.csproj").WriteAllText(input);
        
        await new RewriteCsprojsStep().ExecuteAsync(context, summary);

        var buildCsproj = (tempDirectory / "build" / "_build.csproj").ReadAllText();

        buildCsproj.Should()
            .Contain("<PropertyGroup>")
            .And.Contain("<FalloutVersion>11.0.0</FalloutVersion>")
            .And.Contain("$(FalloutVersion)", Exactly.Once())
            .And.Contain("$(PkgVersion)");
    }

    [Fact]
    public async Task Decouples_ambiguously_used_variables_when_a_property_group_but_no_variable_exists()
    {
        const string input = """
                             <Project Sdk="Microsoft.NET.Sdk">
                               <PropertyGroup>
                               </PropertyGroup>
                               <ItemGroup>
                                 <PackageReference Include="Nuke.Common" Version="$(PkgVersion)" />
                                 <PackageReference Include="Some.ThirdParty" Version="$(PkgVersion)" />
                               </ItemGroup>
                             </Project>
                             """;

        (tempDirectory / "build" / "_build.csproj").WriteAllText(input);
        
        await new RewriteCsprojsStep().ExecuteAsync(context, summary);

        var buildCsproj = (tempDirectory / "build" / "_build.csproj").ReadAllText();

        buildCsproj.Should()
            .Contain("<PropertyGroup>")
            .And.Contain("<FalloutVersion>11.0.0</FalloutVersion>")
            .And.Contain("$(FalloutVersion)", Exactly.Once())
            .And.Contain("$(PkgVersion)");
    }
}
