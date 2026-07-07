using System;
using FluentAssertions;
using Fallout.Common.IO;
using Fallout.Common.Tooling;
using Xunit;

namespace Fallout.Common.Specs;

/// <summary>
/// Covers the per-run <c>Reset()</c> hooks added in FT-1 / #306: <see cref="BuildManager"/>'s
/// <c>finally</c> calls these so a subsequent build in the same process starts from default
/// package-location config instead of inheriting the previous run's residue.
///
/// Shares the resolvers' process-global static fields with <see cref="ToolTasksToolPathSpecs"/>,
/// so both classes live in a non-parallelized collection — otherwise a concurrent test could
/// observe a field mid-reset.
/// </summary>
[Collection(ToolPathResolverStateCollection.Name)]
public class ToolPathResolverResetSpecs : IDisposable
{
    private readonly string _embeddedPackagesDirectory = NuGetToolPathResolver.EmbeddedPackagesDirectory;
    private readonly string _nuGetPackagesConfigFile = NuGetToolPathResolver.NuGetPackagesConfigFile;
    private readonly string _nuGetAssetsConfigFile = NuGetToolPathResolver.NuGetAssetsConfigFile;
    private readonly string _paketPackagesConfigFile = NuGetToolPathResolver.PaketPackagesConfigFile;
    private readonly AbsolutePath _npmPackageJsonFile = NpmToolPathResolver.NpmPackageJsonFile;

    [Fact]
    public void NuGetToolPathResolver_Reset_clears_every_package_location_field()
    {
        NuGetToolPathResolver.EmbeddedPackagesDirectory = "/packages";
        NuGetToolPathResolver.NuGetPackagesConfigFile = "/packages.config";
        NuGetToolPathResolver.NuGetAssetsConfigFile = "/project.assets.json";
        NuGetToolPathResolver.PaketPackagesConfigFile = "/paket.dependencies";

        NuGetToolPathResolver.Reset();

        NuGetToolPathResolver.EmbeddedPackagesDirectory.Should().BeNull();
        NuGetToolPathResolver.NuGetPackagesConfigFile.Should().BeNull();
        NuGetToolPathResolver.NuGetAssetsConfigFile.Should().BeNull();
        NuGetToolPathResolver.PaketPackagesConfigFile.Should().BeNull();
    }

    [Fact]
    public void NpmToolPathResolver_Reset_clears_the_package_json_location()
    {
        NpmToolPathResolver.NpmPackageJsonFile = "/tmp/package.json";

        NpmToolPathResolver.Reset();

        NpmToolPathResolver.NpmPackageJsonFile.Should().BeNull();
    }

    // Restore whatever the ambient environment had so these mutations don't leak into sibling tests.
    public void Dispose()
    {
        NuGetToolPathResolver.EmbeddedPackagesDirectory = _embeddedPackagesDirectory;
        NuGetToolPathResolver.NuGetPackagesConfigFile = _nuGetPackagesConfigFile;
        NuGetToolPathResolver.NuGetAssetsConfigFile = _nuGetAssetsConfigFile;
        NuGetToolPathResolver.PaketPackagesConfigFile = _paketPackagesConfigFile;
        NpmToolPathResolver.NpmPackageJsonFile = _npmPackageJsonFile;
    }
}

[CollectionDefinition(Name, DisableParallelization = true)]
public class ToolPathResolverStateCollection
{
    public const string Name = "ToolPathResolverState";
}
