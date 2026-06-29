using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using Fallout.Common.IO;
using Xunit;
using Xunit.Abstractions;

namespace Fallout.Common.Specs;

public class CompressionTasksSpecs : FileSystemDependentSpecs
{
    private AbsolutePath RootFile => TestTempDirectory / "root-file";
    private AbsolutePath NestedFile => TestTempDirectory / "a" / "b" / "c" / "nested-file";

    public CompressionTasksSpecs(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Theory]
    [InlineData("archive.zip")]
    [InlineData("archive.tar.gz")]
    [InlineData("archive.tar.bz2")]
    public void Test(string archiveFile)
    {
        RootFile.WriteAllText("root", eofLineBreak: false);
        NestedFile.WriteAllText("nested", eofLineBreak: false);

        var archive = TestTempDirectory / archiveFile;
        TestTempDirectory.CompressTo(archive);
        File.Exists(archive).Should().BeTrue();

        File.Delete(RootFile);
        File.Delete(NestedFile);
        Directory.GetFiles(TestTempDirectory, "*").Should().HaveCount(1);

        archive.UncompressTo(TestTempDirectory);
        File.Exists(RootFile).Should().BeTrue();
        File.ReadAllText(RootFile).Should().Be("root");
        File.Exists(NestedFile).Should().BeTrue();
        File.ReadAllText(NestedFile).Should().Be("nested");
    }
}
