using System;
using System.IO;
using System.Threading.Tasks;
using Fallout.Migrate.Common;
using FluentAssertions;
using Xunit;
using Fallout.Migrate.Steps;

namespace Fallout.Migrate.Specs;

public class ScriptRewriterSpecs
{
    [Fact]
    public void Dotnet_nuke_invocations_become_dotnet_fallout()
    {
        var result = ScriptRewriter.Rewrite("dotnet nuke Compile");
        result.EditCount.Should().Be(1);
        result.Content.Should().Be("dotnet fallout Compile");
    }

    [Fact]
    public void Dot_nuke_directory_references_become_dot_fallout()
    {
        var result = ScriptRewriter.Rewrite("""TEMP_DIRECTORY="$SCRIPT_DIR/.nuke/temp" """);
        result.EditCount.Should().Be(1);
        result.Content.Should().Contain(".fallout/temp");
        result.Content.Should().NotContain(".nuke/");
    }

    [Fact]
    public void Legacy_nuke_env_vars_are_renamed_to_their_fallout_equivalents()
    {
        const string input = """
                             $env:NUKE_GLOBAL_TOOL_VERSION = "10.0"
                             """;

        var result = ScriptRewriter.Rewrite(input);
        result.EditCount.Should().Be(1);
        result.Content.Should().Contain("FALLOUT_GLOBAL_TOOL_VERSION");
    }

    [Theory]
    [InlineData("export NUKE_TELEMETRY_OPTOUT=1")]         // build.sh
    [InlineData("""$env:NUKE_TELEMETRY_OPTOUT = "1" """)]  // build.ps1
    [InlineData("set NUKE_TELEMETRY_OPTOUT=1")]            // build.cmd
    public void Telemetry_opt_out_line_is_stripped_entirely(string optOutLine)
    {
        // Telemetry was removed from Fallout (ADR-0010) — the opt-out is dropped, not renamed
        // to a dead FALLOUT_TELEMETRY_OPTOUT. Whichever bootstrap script spelled it, the whole
        // line goes and the surrounding ones are untouched.
        var input = $"""
                     export DOTNET_ROLL_FORWARD="Major"
                     {optOutLine}
                     dotnet nuke "$@"
                     """;

        var result = ScriptRewriter.Rewrite(input);

        result.Content.Should().NotContain("TELEMETRY_OPTOUT");
        result.Content.Should().Contain("DOTNET_ROLL_FORWARD");
        result.Content.Should().Contain("dotnet fallout");
    }

    [Fact]
    public void Plain_word_nuke_in_prose_is_left_alone()
    {
        // The word "nuke" in a comment or string isn't a command invocation.
        const string input = "# This was previously a NUKE-based build.";
        var result = ScriptRewriter.Rewrite(input);
        result.EditCount.Should().Be(0);
    }

    [Fact]
    public async Task Removes_Nuke_enterprise_unix_bootstrapper_leftovers()
    {
        var filename = "build.sh";
        var dir = CreateBootstrapScript(filename, """
                                                  echo "Microsoft (R) .NET SDK version $("$DOTNET_EXE" --version)"

                                                  if [[ ! -z ${NUKE_ENTERPRISE_TOKEN+x} && "$NUKE_ENTERPRISE_TOKEN" != "" ]]; then
                                                      "$DOTNET_EXE" nuget remove source "nuke-enterprise" &>/dev/null || true
                                                      "$DOTNET_EXE" nuget add source "https://f.feedz.io/nuke/enterprise/nuget" --name "nuke-enterprise" --username "PAT" --password "$NUKE_ENTERPRISE_TOKEN" --store-password-in-clear-text &>/dev/null || true
                                                  fi

                                                  "$DOTNET_EXE" build "$BUILD_PROJECT_FILE" /nodeReuse:false /p:UseSharedCompilation=false -nologo -clp:NoSummary --verbosity quiet
                                                  "$DOTNET_EXE" run --project "$BUILD_PROJECT_FILE" --no-build -- "$@"
                                                  """);

        Summary summary = await ExecuteMigrationStep(dir);

        var buildSh = await File.ReadAllTextAsync(Path.Combine(dir, filename));
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
        var filename = "build.sh";
        var dir = CreateBootstrapScript(filename,"""
                                                 if [[ ! -z ${NUKE_ENTERPRISE_TOKEN+x} && "$NUKE_ENTERPRISE_TOKEN" != "" ]]; then
                                                     "$DOTNET_EXE" nuget remove source "nuke-enterprise" &>/dev/null || true
                                                     "$DOTNET_EXE" nuget add source "https://f.feedz.io/nuke/enterprise/nuget" --name "nuke-enterprise" --username "PAT" --password "$NUKE_ENTERPRISE_TOKEN" --store-password-in-clear-text &>/dev/null || true
                                                 fi
                                                 """);

        Summary summary = await ExecuteMigrationStep(dir);

        var buildSh = await File.ReadAllTextAsync(Path.Combine(dir, filename));
        summary.EditCount.Should().Be(1);
        buildSh.Should().Be("");
    }

    [Fact]
    public async Task Removes_Nuke_enterprise_windows_bootstrapper_leftovers()
    {
        var filename = "build.ps1";
        var dir = CreateBootstrapScript(filename,"""
                                                 Write-Output "Microsoft (R) .NET SDK version $(& $env:DOTNET_EXE --version)"

                                                 if (Test-Path env:NUKE_ENTERPRISE_TOKEN) {
                                                     & $env:DOTNET_EXE nuget remove source "nuke-enterprise" > $null
                                                     & $env:DOTNET_EXE nuget add source "https://f.feedz.io/nuke/enterprise/nuget" --name "nuke-enterprise" --username "PAT" --password $env:NUKE_ENTERPRISE_TOKEN > $null
                                                 }

                                                 ExecSafe { & $env:DOTNET_EXE build $BuildProjectFile /nodeReuse:false /p:UseSharedCompilation=false -nologo -clp:NoSummary --verbosity quiet }
                                                 ExecSafe { & $env:DOTNET_EXE run --project $BuildProjectFile --no-build -- $BuildArguments }
                                                 """);

        Summary summary = await ExecuteMigrationStep(dir);

        var buildPs1 = await File.ReadAllTextAsync(Path.Combine(dir, filename));
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
        var filename = "build.sh";
        var dir = CreateBootstrapScript(filename,"""
                                                 echo "Microsoft (R) .NET SDK version $("$DOTNET_EXE" --version)"

                                                 "$DOTNET_EXE" build "$BUILD_PROJECT_FILE" /nodeReuse:false /p:UseSharedCompilation=false -nologo -clp:NoSummary --verbosity quiet
                                                 "$DOTNET_EXE" run --project "$BUILD_PROJECT_FILE" --no-build -- "$@"
                                                 """);

        Summary summary = await ExecuteMigrationStep(dir);

        var buildSh = await File.ReadAllTextAsync(Path.Combine(dir, filename));
        summary.EditCount.Should().Be(0);
        buildSh.Should().Be(
            """
            echo "Microsoft (R) .NET SDK version $("$DOTNET_EXE" --version)"

            "$DOTNET_EXE" build "$BUILD_PROJECT_FILE" /nodeReuse:false /p:UseSharedCompilation=false -nologo -clp:NoSummary --verbosity quiet
            "$DOTNET_EXE" run --project "$BUILD_PROJECT_FILE" --no-build -- "$@"
            """);
    }

    private string CreateBootstrapScript(string bootstrapFile, string bootstrapFileConent)
    {
        var dir = Path.Combine(Path.GetTempPath(), "fallout-migrate-test-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(dir);

        File.WriteAllText(Path.Combine(dir, bootstrapFile), bootstrapFileConent);

        return dir;
    }

    private static async Task<Summary> ExecuteMigrationStep(string dir)
    {
        var step = new CleanupBootstrapScriptsStep();
        var summary = new Summary();
        await step.ExecuteAsync(new MigrationContext(dir, false, TextWriter.Null), summary);
        return summary;
    }

}
