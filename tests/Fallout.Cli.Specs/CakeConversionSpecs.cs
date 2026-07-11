using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NuGet.Versioning;
using Fallout.Common;
using Fallout.Common.IO;
using VerifyXunit;
using Xunit;

namespace Fallout.Cli.Specs;

public class CakeConversionSpecs
{
    private static AbsolutePath RootDirectory => Constants.TryGetRootDirectoryFrom(EnvironmentInfo.WorkingDirectory);

    [Theory]
    [MemberData(nameof(CakeFileNames))]
    public Task Test(AbsolutePath file)
    {
        var converted = CakeConverter.GetConvertedContent(file.ReadAllText());
        return Verifier.Verify(converted, extension: "cs")
            .UseDirectory(CakeScriptsDirectory)
            .UseFileName(file.NameWithoutExtension);
    }

    [Fact]
    public void TestPackages()
    {
        var content = (CakeScriptsDirectory / "references.cake").ReadAllText();

        var packages = CakeConverter.GetPackages(content).ToList();
        packages.Should().Contain((PackageManager.DownloadType, "GitVersion.CommandLine", "4.0.0"));
        packages.Should().Contain((PackageManager.ReferenceType, "SharpZipLib", "1.2.0"));
        packages.Should().Contain(x => x.Id == "TeamCity.Dotnet.Integration" &&
                                       NuGetVersion.Parse(x.Version) > NuGetVersion.Parse("1.0.10"));
        packages.Should().NotContain(x => x.Id.Contains("Cake"));
    }

    private static AbsolutePath CakeScriptsDirectory => RootDirectory / "tests" / "Fallout.Cli.Specs" / "cake-scripts";

    public static IEnumerable<object[]> CakeFileNames
        => CakeScriptsDirectory.GlobFiles(CakeConverter.FilePattern).Select(x => new object[] { x });
}
