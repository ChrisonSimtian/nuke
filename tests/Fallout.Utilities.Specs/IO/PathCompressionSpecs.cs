using System;
using System.Linq;
using Fallout.Common.IO;
using FluentAssertions;
using Xunit;

namespace Fallout.Common.Specs;

public class PathCompressionSpecs : IDisposable
{
    private readonly AbsolutePath testDirectory = AbsolutePath.Temp();
    private readonly AbsolutePath uncompressDirectory = AbsolutePath.Temp();
    private const string ExpectedHash = "516e16d7450ea2d4f75e9cad5878d3fe";
    private readonly Random random;

    public PathCompressionSpecs()
    {
        testDirectory.CreateDirectory();
        random = new Random(53463464);

        GenerateTestFiles(testDirectory, 100, 5);
    }

    [Theory]
    [InlineData("archive.zip")]
    [InlineData("archive.tar.bz2")]
    [InlineData("archive.tar.gz")]
    public void A_directory_has_the_same_hash_from_the_old_implementation(string archiveFile)
    {
        // Arrange
        var archivePath = AbsolutePath.Temp() / archiveFile;
        testDirectory.CompressTo(archivePath);

        // Act
        archivePath.UncompressTo(uncompressDirectory);

        // Assert
        var hash = uncompressDirectory.GetDirectoryHash();
        hash.Should().Be(ExpectedHash);
    }

    private void GenerateTestFiles(AbsolutePath absolutePath, int fileCount, int maximumNestingLevel)
    {
        for (var i = 0; i < fileCount; i++)
        {
            var nestingLevel = random.Next(maximumNestingLevel + 1);
            var directory = Enumerable.Range(0, nestingLevel)
                .Aggregate(absolutePath, (current, _) => current / $"dir-{random.Next(int.MaxValue)}");

            if (random.Next() % 2344 == 0)
            {
                directory.CreateDirectory();
                continue;
            }

            var file = directory / $"file-{random.Next(int.MaxValue)}.txt";
            file.WriteAllText(random.Next().ToString(), eofLineBreak: false);
        }
    }

    public void Dispose()
    {
        testDirectory.DeleteDirectory();
        uncompressDirectory.DeleteDirectory();
    }
}
