using FluentAssertions;
using Fallout.Migrate.Steps;
using Xunit;

namespace Fallout.Migrate.Specs;

/// <summary>
/// Covers the parsing and command-rendering parts of the global tool switch. Running
/// <c>dotnet tool</c> itself is not exercised here — that would install software on the machine
/// running the specs.
/// </summary>
public class SwitchGlobalToolStepSpecs
{
    /// <summary>Representative <c>dotnet tool list --global</c> output.</summary>
    private const string ToolList = """
                                    Package Id             Version         Commands
                                    ---------------------------------------------------
                                    fallout.cli            11.0.18         fallout
                                    dotnet-format          5.1.250801      dotnet-format
                                    """;

    [Fact]
    public void ReadsThePackageIdColumnFromTheToolList()
    {
        var installed = SwitchGlobalToolStep.ParseInstalledToolIds(ToolList);

        installed.Should().BeEquivalentTo("fallout.cli", "dotnet-format");
    }

    [Fact]
    public void SkipsTheHeaderRows()
    {
        var installed = SwitchGlobalToolStep.ParseInstalledToolIds(ToolList);

        installed.Should().NotContain("package", "the header row is not a tool");
    }

    [Fact]
    public void LowercasesThePackageId()
    {
        // `dotnet tool install Fallout.GlobalTool` records the display casing, but a manifest key
        // and our retired-id list are both lowercase.
        const string input = """
                             Package Id             Version         Commands
                             ------------------------------------------------
                             Fallout.GlobalTools    10.4.0-rc.4     fallout
                             """;

        var installed = SwitchGlobalToolStep.ParseInstalledToolIds(input);

        installed.Should().Contain("fallout.globaltools");
    }

    [Fact]
    public void HandlesWindowsLineEndings()
    {
        var installed = SwitchGlobalToolStep.ParseInstalledToolIds(ToolList.Replace("\n", "\r\n"));

        installed.Should().BeEquivalentTo("fallout.cli", "dotnet-format");
    }

    [Fact]
    public void ReturnsNothingWhenNoToolsAreInstalled()
    {
        const string input = """
                             Package Id      Version      Commands
                             -------------------------------------
                             """;

        var installed = SwitchGlobalToolStep.ParseInstalledToolIds(input);

        installed.Should().BeEmpty();
    }

    [Fact]
    public void ReturnsNothingWhenTheOutputIsNotATable()
    {
        // Guards against treating a header, or an error message, as a package id.
        var installed = SwitchGlobalToolStep.ParseInstalledToolIds("Could not execute because the command was not found.");

        installed.Should().BeEmpty();
    }

    [Fact]
    public void PinsTheResolvedVersionInTheInstallCommand()
    {
        SwitchGlobalToolStep.DescribeInstall("10.4.0").Should().Be("fallout.globaltool --version 10.4.0");
    }

    [Fact]
    public void OmitsTheVersionWhenNoneWasResolved()
    {
        // Offline run: install whatever is latest rather than a version we could not verify.
        SwitchGlobalToolStep.DescribeInstall(toolVersion: null).Should().Be("fallout.globaltool");
    }
}
