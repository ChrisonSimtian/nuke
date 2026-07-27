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
}
