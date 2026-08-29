using System;
using System.IO;
using System.Threading.Tasks;
using Fallout.Common.IO;
using Fallout.Migrate.Common;
using Fallout.Migrate.Steps;
using FluentAssertions;
using Xunit;

namespace Fallout.Migrate.Specs;

public class RewriteBootstrapScriptsStepSpecs : IDisposable
{
    private readonly AbsolutePath tempDirectory;
    private readonly MigrationContext context;
    private readonly Summary summary = new();

    public RewriteBootstrapScriptsStepSpecs()
    {
        tempDirectory = AbsolutePath.Temp("fallout-migrate-test");
        context = new MigrationContext(tempDirectory, dryRun: false, TextWriter.Null);
    }

    public void Dispose()
    {
        tempDirectory.DeleteDirectory();
    }

    [Fact]
    public async Task Dotnet_nuke_invocations_become_dotnet_fallout()
    {
        (tempDirectory / "build.sh").WriteAllText("dotnet nuke Compile", eofLineBreak: false);

        await new RewriteBootstrapScriptsStep().ExecuteAsync(context, summary);

        summary.EditCount.Should().Be(1);
        var buildSh = (tempDirectory / "build.sh").ReadAllText();
        buildSh.Should().Be("dotnet fallout Compile");
    }

    [Fact]
    public async Task Dot_nuke_directory_references_become_dot_fallout()
    {
        (tempDirectory / "build.sh").WriteAllText("""TEMP_DIRECTORY="$SCRIPT_DIR/.nuke/temp" """, eofLineBreak: false);

        await new RewriteBootstrapScriptsStep().ExecuteAsync(context, summary);

        summary.EditCount.Should().Be(1);
        var buildSh = (tempDirectory / "build.sh").ReadAllText();
        buildSh.Should().Contain(".fallout/temp");
        buildSh.Should().NotContain(".nuke/");
    }

    [Fact]
    public async Task Legacy_nuke_env_vars_are_renamed_to_their_fallout_equivalents()
    {
        (tempDirectory / "build.ps1").WriteAllText("""
                                                   $env:NUKE_GLOBAL_TOOL_VERSION = "10.0"
                                                   """, eofLineBreak: false);

        await new RewriteBootstrapScriptsStep().ExecuteAsync(context, summary);

        summary.EditCount.Should().Be(1);
        var buildPs1 = (tempDirectory / "build.ps1").ReadAllText();
        buildPs1.Should().Contain("FALLOUT_GLOBAL_TOOL_VERSION");
    }

    [Theory]
    [InlineData("build.sh", "export NUKE_TELEMETRY_OPTOUT=1")] // build.sh
    [InlineData("build.ps1", """$env:NUKE_TELEMETRY_OPTOUT = "1" """)] // build.ps1
    [InlineData("build.cmd", "set NUKE_TELEMETRY_OPTOUT=1")] // build.cmd
    public async Task Telemetry_opt_out_line_is_stripped_entirely(string fileName, string optOutLine)
    {
        // Telemetry was removed from Fallout (ADR-0010) — the opt-out is dropped, not renamed
        // to a dead FALLOUT_TELEMETRY_OPTOUT. Whichever bootstrap script spelled it, the whole
        // line goes and the surrounding ones are untouched.
        var input = $"""
                     export DOTNET_ROLL_FORWARD="Major"
                     {optOutLine}
                     dotnet nuke "$@"
                     """;

        (tempDirectory / fileName).WriteAllText(input, eofLineBreak: false);

        await new RewriteBootstrapScriptsStep().ExecuteAsync(context, summary);

        var content = (tempDirectory / fileName).ReadAllText();
        content.Should().NotContain("TELEMETRY_OPTOUT");
        content.Should().Contain("DOTNET_ROLL_FORWARD");
        content.Should().Contain("dotnet fallout");
    }

    [Fact]
    public async Task Plain_word_nuke_in_prose_is_left_alone()
    {
        // The word "nuke" in a comment or string isn't a command invocation.
        (tempDirectory / "build.sh").WriteAllText("# This was previously a NUKE-based build.", eofLineBreak: false);

        await new RewriteBootstrapScriptsStep().ExecuteAsync(context, summary);

        summary.EditCount.Should().Be(0);
    }

    [Fact]
    public async Task Removes_Nuke_enterprise_unix_bootstrapper_leftovers()
    {
        (tempDirectory / "build.sh").WriteAllText("""
                                                  echo "Microsoft (R) .NET SDK version $("$DOTNET_EXE" --version)"

                                                  if [[ ! -z ${NUKE_ENTERPRISE_TOKEN+x} && "$NUKE_ENTERPRISE_TOKEN" != "" ]]; then
                                                      "$DOTNET_EXE" nuget remove source "nuke-enterprise" &>/dev/null || true
                                                      "$DOTNET_EXE" nuget add source "https://f.feedz.io/nuke/enterprise/nuget" --name "nuke-enterprise" --username "PAT" --password "$NUKE_ENTERPRISE_TOKEN" --store-password-in-clear-text &>/dev/null || true
                                                  fi

                                                  "$DOTNET_EXE" build "$BUILD_PROJECT_FILE" /nodeReuse:false /p:UseSharedCompilation=false -nologo -clp:NoSummary --verbosity quiet
                                                  "$DOTNET_EXE" run --project "$BUILD_PROJECT_FILE" --no-build -- "$@"
                                                  """, eofLineBreak: false);

        await new CleanupBootstrapScriptsStep().ExecuteAsync(context, summary);

        var buildSh = (tempDirectory / "build.sh").ReadAllText();
        summary.EditCount.Should().Be(1);
        buildSh.Should().Be(
            """
            echo "Microsoft (R) .NET SDK version $("$DOTNET_EXE" --version)"

            "$DOTNET_EXE" build "$BUILD_PROJECT_FILE" /nodeReuse:false /p:UseSharedCompilation=false -nologo -clp:NoSummary --verbosity quiet
            "$DOTNET_EXE" run --project "$BUILD_PROJECT_FILE" --no-build -- "$@"
            """);
    }

    [Fact]
    public async Task Does_not_throw_when_Nuke_enterprise_check_is_last_in_file()
    {
        (tempDirectory / "build.sh").WriteAllText("""
                                                  if [[ ! -z ${NUKE_ENTERPRISE_TOKEN+x} && "$NUKE_ENTERPRISE_TOKEN" != "" ]]; then
                                                      "$DOTNET_EXE" nuget remove source "nuke-enterprise" &>/dev/null || true
                                                      "$DOTNET_EXE" nuget add source "https://f.feedz.io/nuke/enterprise/nuget" --name "nuke-enterprise" --username "PAT" --password "$NUKE_ENTERPRISE_TOKEN" --store-password-in-clear-text &>/dev/null || true
                                                  fi
                                                  """);

        await new CleanupBootstrapScriptsStep().ExecuteAsync(context, summary);

        var buildSh = (tempDirectory / "build.sh").ReadAllText();
        summary.EditCount.Should().Be(1);
        buildSh.Should().Be("");
    }

    [Fact]
    public async Task Removes_Nuke_enterprise_windows_bootstrapper_leftovers()
    {
        (tempDirectory / "build.ps1").WriteAllText("""
                                                   Write-Output "Microsoft (R) .NET SDK version $(& $env:DOTNET_EXE --version)"

                                                   if (Test-Path env:NUKE_ENTERPRISE_TOKEN) {
                                                       & $env:DOTNET_EXE nuget remove source "nuke-enterprise" > $null
                                                       & $env:DOTNET_EXE nuget add source "https://f.feedz.io/nuke/enterprise/nuget" --name "nuke-enterprise" --username "PAT" --password $env:NUKE_ENTERPRISE_TOKEN > $null
                                                   }

                                                   ExecSafe { & $env:DOTNET_EXE build $BuildProjectFile /nodeReuse:false /p:UseSharedCompilation=false -nologo -clp:NoSummary --verbosity quiet }
                                                   ExecSafe { & $env:DOTNET_EXE run --project $BuildProjectFile --no-build -- $BuildArguments }
                                                   """, eofLineBreak: false);

        await new CleanupBootstrapScriptsStep().ExecuteAsync(context, summary);

        var buildPs1 = (tempDirectory / "build.ps1").ReadAllText();
        summary.EditCount.Should().Be(1);
        buildPs1.Should().Be(
            """
            Write-Output "Microsoft (R) .NET SDK version $(& $env:DOTNET_EXE --version)"

            ExecSafe { & $env:DOTNET_EXE build $BuildProjectFile /nodeReuse:false /p:UseSharedCompilation=false -nologo -clp:NoSummary --verbosity quiet }
            ExecSafe { & $env:DOTNET_EXE run --project $BuildProjectFile --no-build -- $BuildArguments }
            """);
    }

    [Fact]
    public async Task Leaves_bootstrapper_scripts_without_leftovers_alone()
    {
        (tempDirectory / "build.sh").WriteAllText("""
                                                  echo "Microsoft (R) .NET SDK version $("$DOTNET_EXE" --version)"

                                                  "$DOTNET_EXE" build "$BUILD_PROJECT_FILE" /nodeReuse:false /p:UseSharedCompilation=false -nologo -clp:NoSummary --verbosity quiet
                                                  "$DOTNET_EXE" run --project "$BUILD_PROJECT_FILE" --no-build -- "$@"
                                                  """, eofLineBreak: false);

        await new CleanupBootstrapScriptsStep().ExecuteAsync(context, summary);

        var buildSh = (tempDirectory / "build.sh").ReadAllText();
        summary.EditCount.Should().Be(0);
        buildSh.Should().Be(
            """
            echo "Microsoft (R) .NET SDK version $("$DOTNET_EXE" --version)"

            "$DOTNET_EXE" build "$BUILD_PROJECT_FILE" /nodeReuse:false /p:UseSharedCompilation=false -nologo -clp:NoSummary --verbosity quiet
            "$DOTNET_EXE" run --project "$BUILD_PROJECT_FILE" --no-build -- "$@"
            """);
    }
}
