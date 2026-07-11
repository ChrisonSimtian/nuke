using System;
using System.IO;
using Fallout.Common.IO;
using FluentAssertions;
using Xunit;

namespace Fallout.Cli.Specs;

public class ConfigurationReaderSpecs
{
    private static AbsolutePath WriteScript(AbsolutePath dir) =>
        WriteScript(dir, string.Join("\n",
            "# CONFIGURATION",
            "##############",
            "",
            "BUILD_PROJECT_FILE=\"build/_build.csproj\"",
            "TEMP_DIRECTORY=\"$SCRIPT_DIR/.fallout/temp\"",
            "",
            "# EXECUTION",
            "dotnet run"));

    private static AbsolutePath WriteScript(AbsolutePath dir, string content)
    {
        var buildScript = dir / "build.sh";
        File.WriteAllText(buildScript, content);
        return buildScript;
    }

    [Fact]
    public void Read_ParsesConfigurationEntries()
    {
        using var temp = TempDir.Create();
        var buildScript = WriteScript(temp.Path);

        var configuration = new ConfigurationReader().Read(buildScript, evaluate: false);

        configuration.Should().ContainKey(ConfigurationReader.BuildProjectFileKey)
            .WhoseValue.Should().Be("build/_build.csproj");
    }

    [Fact]
    public void Read_WhenEvaluating_ReplacesScriptDirectoryToken()
    {
        using var temp = TempDir.Create();
        var buildScript = WriteScript(temp.Path);

        var configuration = new ConfigurationReader().Read(buildScript, evaluate: true);

        configuration["TEMP_DIRECTORY"].Should().NotContain("$SCRIPT_DIR")
            .And.Contain(buildScript.Parent);
    }

    private sealed class TempDir : IDisposable
    {
        public AbsolutePath Path { get; }

        private TempDir(AbsolutePath path) => Path = path;

        public static TempDir Create()
        {
            var dir = (AbsolutePath)System.IO.Path.Combine(System.IO.Path.GetTempPath(), "fallout-cfgreader-" + Guid.NewGuid().ToString("N"));
            dir.CreateDirectory();
            return new TempDir(dir);
        }

        public void Dispose()
        {
            if (Directory.Exists(Path))
                Directory.Delete(Path, recursive: true);
        }
    }
}
